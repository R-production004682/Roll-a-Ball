# 外部 API はモックに置き換え、公開成功・失敗時の通知と公開対象の境界を確認する
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '../PublishWebGL.ps1')
$script:projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../Roll-a-Ball/Builds/PublishingTests'))
$script:OutputDirectory = Join-Path $projectRoot 'Builds/WebGL'
New-Item -ItemType Directory -Path (Join-Path $OutputDirectory 'Build') -Force | Out-Null
Set-Content -LiteralPath (Join-Path $OutputDirectory 'index.html') -Value '<html>fixture</html>'
Set-Content -LiteralPath (Join-Path $OutputDirectory 'Build/test.loader.js') -Value 'fixture'

function Assert-Test
{
    <# .SYNOPSIS
    期待した公開境界・通知条件を満たさない場合に確認を失敗させる
    #>
    param([bool] $Condition, [string] $Description)
    if (!$Condition)
    {
        throw "TEST FAILED: $Description"
    }
}

$files = @(Assert-PublishInput $OutputDirectory $Repository $RoleId)
Assert-Test ($files.Count -eq 2) '完成済みの専用フォルダーだけを読み込む'
foreach ($badDirectory in @($projectRoot, ($OutputDirectory + 'Other')))
{
    $rejected = $false
    try { Assert-PublishInput $badDirectory $Repository $RoleId | Out-Null }
    catch { $rejected = $true }
    Assert-Test $rejected 'プロジェクトや同名接頭辞の別フォルダーを拒否する'
}
$rejected = $false
try { Assert-PublishInput $OutputDirectory $Repository '@everyone' | Out-Null }
catch { $rejected = $true }
Assert-Test $rejected 'ロールの文字列・メンション挿入を拒否する'
$payload = New-DiscordPayload $RoleId 'https://example.test/build/' 'fixture'
Assert-Test ($payload.content.Contains("<@&$RoleId>")) 'ID を使う実ロールメンション'
Assert-Test ($payload.allowed_mentions.parse.Count -eq 0 -and $payload.allowed_mentions.roles.Count -eq 1) '指定ロール以外を通知対象にしない'

function Get-DiscordWebhook
{
    <# .SYNOPSIS
    実 Webhook を使わないモック通知先を返す
    #>
    return 'https://discord.com/api/webhooks/123456789012345678/MockToken'
}

function Invoke-GitHub
{
    <# .SYNOPSIS
    外部通信を行わずに、公開成功と公開失敗の API 応答を再現する
    #>
    param([string] $Endpoint, [string] $Method = 'GET', $Body, [switch] $AllowMissing)
    if ($Endpoint -eq "repos/$Repository") { return @{ permissions = @{ push = $true } } }
    if ($Endpoint -eq "repos/$Repository/pages" -and $Method -eq 'GET') { return $null }
    if ($Endpoint -like '*/git/ref/heads/*') { return @{ object = @{ sha = 'previous-commit' } } }
    if ($Endpoint -like '*/git/commits/*') { return @{ sha = 'previous-commit'; tree = @{ sha = 'previous-tree' } } }
    if ($Endpoint -like '*/git/trees/*')
    {
        return @{ tree = @(
            @{ path = 'roll-a-ball-publishing.json'; type = 'blob'; size = 40 },
            @{ path = 'builds/20260101-old/a'; type = 'blob'; size = 10 },
            @{ path = 'builds/20260201-old/a'; type = 'blob'; size = 10 },
            @{ path = 'builds/20260301-old/a'; type = 'blob'; size = 10 }
        ) }
    }
    if ($Endpoint -like '*/git/blobs') { return @{ sha = 'fixture-blob' } }
    if ($Endpoint -like '*/git/trees')
    {
        $script:treeBody = $Body
        $script:buildId = (($Body.tree | Where-Object { $_.path -like '*/build-info.json' }).content | ConvertFrom-Json).buildId
        return @{ sha = 'new-tree' }
    }
    if ($Endpoint -like '*/git/commits')
    {
        Assert-Test ($Body.parents[0] -eq 'previous-commit') '公開履歴を維持する'
        return @{ sha = 'new-commit' }
    }
    if ($Endpoint -like '*/git/refs/heads/*')
    {
        Assert-Test (!$Body.force) '競合する更新を強制上書きしない'
        return @{ object = @{ sha = 'new-commit' } }
    }
    if ($Endpoint -eq "repos/$Repository/pages" -and $Method -eq 'POST') { return @{ html_url = 'https://example.test/game/' } }
    if ($Endpoint -like '*/pages/builds/latest') { return @{ commit = 'new-commit'; status = $script:pageResult } }
    if ($Endpoint -like '*/pages/builds') { return @{ status = 'queued' } }
    throw "Unexpected mocked API: $Endpoint"
}

function Invoke-RestMethod
{
    <# .SYNOPSIS
    到達確認と Discord の応答をモックし、送信内容と回数を検証する
    #>
    param($Uri, $Method, $Body, $TimeoutSec, $ContentType)
    if ($Uri -like 'https://example.test/*') { return @{ buildId = $script:buildId } }
    Assert-Test ($Uri -like 'https://discord.com/*?wait=true') '通知のサーバー確認を要求する'
    $message = [Text.Encoding]::UTF8.GetString($Body) | ConvertFrom-Json
    Assert-Test ($message.allowed_mentions.roles[0] -eq $RoleId) '実行経路の指定ロール'
    $script:notifications++
    return @{ mention_roles = @($RoleId) }
}

function Invoke-WebRequest
{
    <# .SYNOPSIS
    プレイページの到達成功を再現する
    #>
    param($Uri, [switch] $UseBasicParsing, $TimeoutSec)
    return @{ StatusCode = 200 }
}

$script:notifications = 0
$script:pageResult = 'built'
Publish-WebGL | Out-Null
Assert-Test ($notifications -eq 1) '公開成功後に一度だけ通知する'
Assert-Test (@($treeBody.tree | Where-Object { $_.path -eq 'builds/20260101-old/a' -and $_.ContainsKey('sha') -and $null -eq $_.sha }).Count -eq 1) '古い公開版を削除する'
Assert-Test (@($treeBody.tree | Where-Object { $_.path -eq 'builds/20260201-old/a' }).Count -eq 0) '過去二件を既存ツリーに維持する'
Assert-Test (@($treeBody.tree | Where-Object { $_.path -match 'UserSettings|\.xml$|\.cs$' }).Count -eq 0) '設定・秘密情報・コードを公開しない'
$script:notifications = 0
$script:pageResult = 'errored'
$rejected = $false
try { Publish-WebGL | Out-Null }
catch { $rejected = $true }
Assert-Test ($rejected -and $notifications -eq 0) '公開失敗時には通知しない'
Write-Output 'PASS: 公開境界、ロール通知、公開成功・失敗、過去版保持（外部通信なし）'
