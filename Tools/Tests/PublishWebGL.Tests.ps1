# 実 Git とローカル bare リポジトリで検証し、GitHub・Discord への通信はモックする
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '../PublishWebGL.ps1')
$script:projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot ('../../Roll-a-Ball/Builds/PublishingTests-' + [Guid]::NewGuid().ToString('N'))))
$script:OutputDirectory = Join-Path $projectRoot 'Builds/WebGL'
$script:scratchDirectory = Join-Path $projectRoot 'Builds/Publishing'
$script:remotePath = Join-Path $projectRoot 'remote.git'
New-Item -ItemType Directory -Path (Join-Path $OutputDirectory 'Build') -Force | Out-Null
New-Item -ItemType Directory -Path $scratchDirectory -Force | Out-Null
$script:githubStatus = '422'
function gh
{
    <# .SYNOPSIS
    API の失敗応答だけを再現し、HTTP ステータスと本来の原因が表示されるか確認する
    #>
    $global:LASTEXITCODE = 1
    Write-Error "gh: fixture (HTTP $githubStatus)"
    return '{"message":"Sorry, your input was too large to process."}'
}
$message = ''
try
{
    Invoke-GitHub "repos/$Repository/git/blobs" 'POST' @{ content = 'fixture'; encoding = 'base64' } | Out-Null
}
catch
{
    $message = $_.Exception.Message
}
$html = '<script src="Build/test.loader.js"></script><script>const config = {dataUrl:"Build/test.data.unityweb",frameworkUrl:"Build/test.framework.js.unityweb",codeUrl:"Build/test.wasm.unityweb"};</script>'
Set-Content -LiteralPath (Join-Path $OutputDirectory 'index.html') -Value $html
foreach ($name in @('test.loader.js', 'test.data.unityweb', 'test.framework.js.unityweb', 'test.wasm.unityweb'))
{
    Set-Content -LiteralPath (Join-Path $OutputDirectory "Build/$name") -Value 'fixture'
}

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

Assert-Test ($message -match 'HTTP 422' -and $message -match 'too large' -and $message -notmatch 'gh auth login') '容量エラーの応答を認証エラーへ置き換えない'
$script:githubStatus = '404'
Assert-Test ($null -eq (Invoke-GitHub "repos/$Repository/pages" -AllowMissing)) '初回の Pages 不在を許容する'
Remove-Item Function:\gh
$files = @(Assert-PublishInput $OutputDirectory $Repository $RoleId)
Assert-Test ($files.Count -eq 5) '完成済みの専用フォルダーだけを読み込む'
foreach ($relative in @('Build/obsolete.data.unityweb', 'Build/test.symbols.json', 'readme.txt', 'UserSettings/DiscordWebhook.xml', 'Game_BackUpThisFolder_ButDontShipItWithYourGame/backup.bin', 'StreamingAssets/test.json', 'TemplateData/icon.png', 'StreamingAssets/test.json.meta', 'StreamingAssets/Library/cache.bin'))
{
    $path = Join-Path $OutputDirectory $relative
    New-Item -ItemType Directory -Path (Split-Path $path) -Force | Out-Null
    Set-Content -LiteralPath $path -Value 'fixture'
}
$files = @(Assert-PublishInput $OutputDirectory $Repository $RoleId)
Assert-Test ($files.Count -eq 7) '旧ビルド・シンボル・バックアップ・秘密設定・meta を除外し、実行用データを保持する'
foreach ($badDirectory in @($projectRoot, ($OutputDirectory + 'Other')))
{
    $rejected = $false
    try
    {
        Assert-PublishInput $badDirectory $Repository $RoleId | Out-Null
    }
    catch
    {
        $rejected = $true
    }
    Assert-Test $rejected 'プロジェクトや同名接頭辞の別フォルダーを拒否する'
}
$rejected = $false
try
{
    Assert-PublishInput $OutputDirectory $Repository '@everyone' | Out-Null
}
catch
{
    $rejected = $true
}
Assert-Test $rejected 'ロールの文字列・メンション挿入を拒否する'
Set-Content -LiteralPath (Join-Path $OutputDirectory 'index.html') -Value ($html.Replace('test.data.unityweb', 'missing.data.unityweb'))
$rejected = $false
try
{
    Assert-PublishInput $OutputDirectory $Repository $RoleId | Out-Null
}
catch
{
    $rejected = $true
}
Assert-Test $rejected 'HTML が参照する必須ファイルの不足を拒否する'
Set-Content -LiteralPath (Join-Path $OutputDirectory 'index.html') -Value $html
$payload = New-DiscordPayload $RoleId 'https://example.test/build/' 'fixture'
Assert-Test ($payload.content.Contains("<@&$RoleId>")) 'ID を使う実ロールメンション'
Assert-Test ($payload.allowed_mentions.parse.Count -eq 0 -and $payload.allowed_mentions.roles.Count -eq 1) '指定ロール以外を通知対象にしない'

# サイズの大きいバイナリも実際の Git オブジェクトとして転送する
$dataPath = Join-Path $OutputDirectory 'Build/test.data.unityweb'
$stream = [IO.File]::Create($dataPath)
$stream.SetLength(96MB)
$stream.Close()
$rejected = $false
try
{
    Assert-PublishInput $OutputDirectory $Repository $RoleId | Out-Null
}
catch
{
    $rejected = $_.Exception.Message -match '95 MiB' -and $_.Exception.Message -match 'test.data.unityweb'
}
Assert-Test $rejected '単体サイズ超過を転送前に対象ファイル名付きで拒否する'
$stream = [IO.File]::Create($dataPath)
$buffer = New-Object byte[] 1MB
$random = New-Object Random(12345)
for ($index = 0; $index -lt 55; $index++)
{
    $random.NextBytes($buffer)
    $stream.Write($buffer, 0, $buffer.Length)
}
$stream.Close()
$script:runGit = ${function:Invoke-PublishGit}
& $runGit $projectRoot @('init', '--bare', '--quiet', '--template=', $remotePath) | Out-Null
$script:gitCalls = New-Object System.Collections.Generic.List[object]
$script:failPush = $false
$script:competeOnPush = $false
$script:competingCommit = ''
function Invoke-PublishGit
{
    <# .SYNOPSIS
    本番 Git コマンドを記録し、転送先だけローカル bare リポジトリに置換する
    #>
    param([string] $Directory, [string[]] $GitArguments)
    $script:gitCalls.Add($GitArguments)
    if ($failPush -and $GitArguments[0] -eq 'push')
    {
        throw 'fixture push rejected'
    }
    if ($competeOnPush -and $GitArguments[0] -eq 'push')
    {
        $previous = & $runGit $remotePath @('rev-parse', "refs/heads/$publishBranch")
        $tree = & $runGit $remotePath @('rev-parse', "${previous}^{tree}")
        $script:competingCommit = & $runGit $remotePath @('commit-tree', $tree, '-p', $previous, '-m', 'competing publication')
        & $runGit $remotePath @('update-ref', "refs/heads/$publishBranch", $competingCommit) | Out-Null
    }
    $localArguments = @($GitArguments | ForEach-Object {
        if ($_ -eq "https://github.com/$Repository.git")
        {
            $remotePath
        }
        else
        {
            $_
        }
    })
    return (& $runGit $Directory $localArguments)
}

function Get-DiscordWebhook
{
    <# .SYNOPSIS
    実 Webhook を使わないモック通知先を返す
    #>
    return 'https://discord.com/api/webhooks/123456789012345678/MockToken'
}

$script:pageResult = 'built'
$script:notifications = 0
$script:siteExists = $false
$script:denyPushPermission = $false
$script:pageFixtures = $null
$script:pageChecks = 0
$script:discordResult = 'accepted'
function Invoke-GitHub
{
    <# .SYNOPSIS
    外部 API を使わず、実 Git リポジトリから公開履歴と Pages の応答を再現する
    #>
    param([string] $Endpoint, [string] $Method = 'GET', $Body, [switch] $AllowMissing)
    if ($Endpoint -eq "repos/$Repository")
    {
        return @{ permissions = @{ push = !$denyPushPermission } }
    }
    if ($Endpoint -eq "repos/$Repository/pages")
    {
        if ($Method -eq 'POST')
        {
            $script:siteExists = $true
        }
        if (!$siteExists)
        {
            return $null
        }
        return @{ html_url = 'https://example.test/game/'; build_type = 'legacy'; source = @{ branch = $publishBranch; path = '/' } }
    }
    if ($Endpoint -like '*/git/ref/heads/*')
    {
        if (!$siteExists)
        {
            return $null
        }
        $sha = & $runGit $remotePath @('rev-parse', "refs/heads/$publishBranch")
        return @{ object = @{ sha = $sha } }
    }
    if ($Endpoint -like '*/git/commits/*')
    {
        $sha = $Endpoint.Split('/')[-1]
        $tree = & $runGit $remotePath @('rev-parse', "${sha}^{tree}")
        return @{ sha = $sha; tree = @{ sha = $tree } }
    }
    if ($Endpoint -like '*/contents/*')
    {
        $path = $Endpoint.Split('/')[4].Split('?')[0]
        $content = (& $runGit $remotePath @('show', "refs/heads/${publishBranch}:$path")) -join "`n"
        return @{ content = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($content)) }
    }
    if ($Endpoint -like '*/git/trees/*')
    {
        $tree = $Endpoint.Split('/')[-1].Split('?')[0]
        $entries = @(& $runGit $remotePath @('ls-tree', '-r', '-l', $tree) | ForEach-Object {
            $parts = $_ -split '\s+', 5
            @{ path = $parts[4]; type = $parts[1]; sha = $parts[2]; size = [long] $parts[3] }
        })
        return @{ tree = $entries; truncated = $false }
    }
    if ($Endpoint -like '*/actions/runs?*')
    {
        $script:pageChecks++
        if ($null -ne $pageFixtures)
        {
            return @{ workflow_runs = @($pageFixtures) }
        }
        $sha = & $runGit $remotePath @('rev-parse', "refs/heads/$publishBranch")
        Assert-Test ($Endpoint -like "*head_sha=$sha&branch=webgl-builds&event=dynamic&*") '公開したコミットとブランチの Pages 実行を照会する'
        $conclusion = 'success'
        if ($pageResult -eq 'errored')
        {
            $conclusion = 'failure'
        }
        return @{ workflow_runs = @(@{ id = 2; head_sha = $sha; path = 'dynamic/pages/pages-build-deployment'; status = 'completed'; conclusion = $conclusion; html_url = 'https://example.test/actions/2' }) }
    }
    throw "Unexpected mocked API: $Method $Endpoint"
}

$script:pageFixtures = @(
    @{ id = 1; head_sha = 'fixture'; path = 'dynamic/pages/pages-build-deployment'; status = 'completed'; conclusion = 'cancelled'; html_url = 'https://example.test/actions/1' },
    @{ id = 2; head_sha = 'fixture'; path = 'dynamic/pages/pages-build-deployment'; status = 'in_progress'; conclusion = $null; html_url = 'https://example.test/actions/2' },
    @{ id = 3; head_sha = 'unrelated'; path = 'dynamic/pages/pages-build-deployment'; status = 'completed'; conclusion = 'failure'; html_url = 'https://example.test/actions/3' }
)
$publication = Get-PagesPublication 'fixture'
Assert-Test ($publication.status -eq 'pending' -and $publication.url -eq 'https://example.test/actions/2') 'キャンセル済み・他コミットの失敗を無視して最新の進行中デプロイを待つ'
$pageFixtures[1].status = 'completed'
$pageFixtures[1].conclusion = 'success'
Assert-Test ((Get-PagesPublication 'fixture').status -eq 'built') '最新の Actions 成功を公開の到達確認へ進める'
$pageFixtures[1].conclusion = 'failure'
Assert-Test ((Get-PagesPublication 'fixture').status -eq 'failed') '実際の最新デプロイ失敗は失敗として返す'
$script:pageFixtures = @()
Assert-Test ((Get-PagesPublication 'fixture').status -eq 'pending') 'Actions がまだ作成されていない期間を失敗扱いしない'
$script:pageFixtures = $null

function Invoke-RestMethod
{
    <# .SYNOPSIS
    公開された実データで到達確認を再現し、通知対象と回数を検証する
    #>
    param($Uri, $Method, $Body, $TimeoutSec, $ContentType)
    if ($Uri -like 'https://example.test/*')
    {
        $buildId = ([uri] $Uri).Segments[-2].TrimEnd('/')
        return ((& $runGit $remotePath @('show', "refs/heads/${publishBranch}:builds/$buildId/build-info.json")) | ConvertFrom-Json)
    }
    Assert-Test ($Uri -like 'https://discord.com/*?wait=true') '通知のサーバー確認を要求する'
    if ($discordResult -eq 'transportError')
    {
        throw 'fixture https://discord.com/api/webhooks/123456789012345678/MockToken'
    }
    $message = [Text.Encoding]::UTF8.GetString($Body) | ConvertFrom-Json
    Assert-Test ($message.allowed_mentions.roles[0] -eq $RoleId) '実行経路の指定ロール'
    $script:notifications++
    $roles = @($RoleId)
    if ($discordResult -eq 'roleRejected')
    {
        $roles = @()
    }
    return @{ id = '1558403282173558999'; channel_id = '1558403282173558784'; mention_roles = $roles }
}

function Invoke-WebRequest
{
    <# .SYNOPSIS
    プレイページの到達成功を再現する
    #>
    param($Uri, [switch] $UseBasicParsing, $TimeoutSec)
    return @{ StatusCode = 200 }
}

for ($version = 1; $version -le 4; $version++)
{
    Publish-WebGL | Out-Null
}
Assert-Test ($notifications -eq 4) '公開成功後に一度ずつ通知する'
$before = $gitCalls.Count
Notify-LatestWebGL | Out-Null
Assert-Test ($notifications -eq 5 -and $gitCalls.Count -eq $before) '公開済み最新版の通知だけを再ビルド・push せず実行する'
$receipt = Get-Content -LiteralPath (Join-Path $scratchDirectory 'notification-result.json') -Raw | ConvertFrom-Json
Assert-Test ($receipt.roleAccepted -and $receipt.messageId -eq '1558403282173558999') 'Discord が受け付けたメッセージとロールを記録する'
$script:discordResult = 'roleRejected'
$message = ''
try
{
    Send-DiscordBuildNotification (Get-DiscordWebhook) 'https://example.test/game/' 'fixture' $RoleId | Out-Null
}
catch
{
    $message = $_.Exception.Message
}
Assert-Test ($message -match 'メンションが受理されません' -and $message -match '1558403282173558999') '本文送信成功とロールメンション失敗を区別する'
$script:discordResult = 'transportError'
$message = ''
try
{
    Send-DiscordBuildNotification (Get-DiscordWebhook) 'https://example.test/game/' 'fixture' $RoleId | Out-Null
}
catch
{
    $message = $_.Exception.Message
}
Assert-Test ($message -match 'NotifyLatest' -and $message -notmatch 'MockToken|api/webhooks') '通知の通信失敗は秘密情報を隠し、通知のみの再実行を案内する'
$script:discordResult = 'accepted'
$paths = @(& $runGit $remotePath @('ls-tree', '-r', '--name-only', "refs/heads/$publishBranch"))
$versions = @($paths | Where-Object { $_ -match '^builds/[^/]+/build-info.json$' })
Assert-Test ($versions.Count -eq 3) '現在版と過去二版だけを配信する'
Assert-Test ($paths.Count -eq (3 * 8 + 3)) '実行ファイル七件とメタデータだけを格納する'
Assert-Test (@($paths | Where-Object { $_ -match 'obsolete|symbols|UserSettings|\.xml$|\.meta$|\.cs$|ButDontShip|Library|README|Assets/Script' }).Count -eq 0) '不要物・秘密設定・開発ソースが push に含まれない'
$commitCount = & $runGit $remotePath @('rev-list', '--count', "refs/heads/$publishBranch")
Assert-Test ($commitCount -eq 4) '開発履歴を含めず公開の履歴だけを維持する'
Assert-Test (@($gitCalls | Where-Object { $_ -contains '--force' -and $_[0] -eq 'push' }).Count -eq 0) 'push で強制上書きしない'
Assert-Test (@(Get-ChildItem -LiteralPath $scratchDirectory -Directory -Filter 'git-*').Count -eq 0) '転送成功後に専用 Git 作業領域を削除する'
$script:notifications = 0
$script:failPush = $true
$rejected = $false
try
{
    Publish-WebGL | Out-Null
}
catch
{
    $rejected = $true
}
Assert-Test ($rejected -and $notifications -eq 0) '転送失敗時には通知しない'
$script:failPush = $false
$script:competeOnPush = $true
$rejected = $false
try
{
    Publish-WebGL | Out-Null
}
catch
{
    $rejected = $true
}
$head = & $runGit $remotePath @('rev-parse', "refs/heads/$publishBranch")
Assert-Test ($rejected -and $notifications -eq 0 -and $head -eq $competingCommit) '同時公開の push を拒否し、先行する公開履歴を保持する'
$script:competeOnPush = $false
$script:pageResult = 'errored'
$rejected = $false
try
{
    Publish-WebGL | Out-Null
}
catch
{
    $rejected = $true
}
Assert-Test ($rejected -and $notifications -eq 0) 'Pages 公開失敗時には通知しない'
$script:denyPushPermission = $true
$before = $gitCalls.Count
$rejected = $false
try
{
    Publish-WebGL | Out-Null
}
catch
{
    $rejected = $true
}
Assert-Test ($rejected -and $gitCalls.Count -eq $before) '書き込み権限がなければ転送前に停止する'
Write-Output 'PASS: 55 MiB バイナリの実 Git 転送、初回・再公開、過去版保持、公開境界、失敗時の通知抑止（外部通信なし）'
# テストが生成した絶対パスだけを、プロジェクトの Builds 配下であることを確認して削除する
$buildsRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../Roll-a-Ball/Builds')).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
if ($projectRoot.StartsWith($buildsRoot, [StringComparison]::OrdinalIgnoreCase))
{
    Remove-Item -LiteralPath $projectRoot -Recurse -Force
}
