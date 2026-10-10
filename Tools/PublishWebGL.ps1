[CmdletBinding()]
param(
    [string] $OutputDirectory,
    [string] $Repository = 'R-production004682/Roll-a-Ball',
    [string] $RoleId = '1538587867746668554',
    [switch] $ConfigureWebhook,
    [switch] $ValidateOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../Roll-a-Ball'))
$secretPath = Join-Path $projectRoot 'UserSettings/DiscordWebhook.xml'
$publishBranch = 'webgl-builds'

function Assert-PublishInput
{
    <# .SYNOPSIS
    公開対象を専用ビルドフォルダーに限定し、リンクや不正な設定を拒否する
    #>
    param([string] $Directory, [string] $Repo, [string] $MentionRole)
    if ($Repo -notmatch '\A[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+\z' -or $MentionRole -notmatch '\A[0-9]{17,20}\z')
    {
        throw 'Repository または Discord Role ID が不正です。'
    }
    $expected = [IO.Path]::GetFullPath((Join-Path $projectRoot 'Builds/WebGL')).TrimEnd('\', '/')
    if ([string]::IsNullOrWhiteSpace($Directory) -or [IO.Path]::GetFullPath($Directory).TrimEnd('\', '/') -ne $expected)
    {
        throw '公開できるのは、このプロジェクトの Builds/WebGL だけです。'
    }
    if (!(Test-Path -LiteralPath (Join-Path $Directory 'index.html') -PathType Leaf) -or
        !(Test-Path -LiteralPath (Join-Path $Directory 'Build') -PathType Container))
    {
        throw '完成した WebGL ビルドがありません。先に WebGL ビルドを実行してください。'
    }
    $items = @(Get-Item -LiteralPath $Directory) + @(Get-ChildItem -LiteralPath $Directory -Recurse -Force)
    if (@($items | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint }).Count -gt 0)
    {
        throw 'ビルドフォルダー内のリンクは公開できません。'
    }
    $files = @($items | Where-Object { !$_.PSIsContainer })
    if (@($files | Where-Object { $_.Length -gt 95MB }).Count -gt 0)
    {
        throw '95 MiB を超えるビルドファイルがあります。GitHub Pages 以外の公開先が必要です。'
    }
    if (($files | Measure-Object -Property Length -Sum).Sum -gt 850MB)
    {
        throw 'WebGL ビルドが大きすぎます。公開サイズは 850 MiB 以下にしてください。'
    }
    return $files
}

function Invoke-GitHub
{
    <# .SYNOPSIS
    GitHub CLI の認証を利用して API を呼び、秘密情報を表示せずに失敗を返す
    #>
    param([string] $Endpoint, [string] $Method = 'GET', $Body, [switch] $AllowMissing)
    $arguments = @('api', $Endpoint, '--method', $Method, '-H', 'Accept: application/vnd.github+json')
    if ($null -ne $Body)
    {
        $bodyPath = Join-Path $scratchDirectory 'request.json'
        [IO.File]::WriteAllText($bodyPath, ($Body | ConvertTo-Json -Depth 12 -Compress), (New-Object Text.UTF8Encoding($false)))
        $arguments += @('--input', $bodyPath)
    }
    # Windows PowerShell 5.1 の stderr は ErrorRecord になるため、CLI の終了コードで判定する
    $nativeErrorPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $response = & gh @arguments 2> (Join-Path $scratchDirectory 'github-error.txt')
    $nativeExitCode = $LASTEXITCODE
    $ErrorActionPreference = $nativeErrorPreference
    if ($nativeExitCode -ne 0)
    {
        $errorText = Get-Content -LiteralPath (Join-Path $scratchDirectory 'github-error.txt') -Raw
        if ($AllowMissing -and $errorText -match 'HTTP 404')
        {
            return $null
        }
        throw "GitHub API に失敗しました: $Method $Endpoint。gh auth login とリポジトリ権限を確認してください。"
    }
    return ($response -join "`n" | ConvertFrom-Json)
}

function New-DiscordPayload
{
    <# .SYNOPSIS
    指定したロールだけへのメンションを許可したビルド通知を作成する
    #>
    param([string] $MentionRole, [string] $Url, [string] $BuildId)
    return @{
        content = "<@&$MentionRole> WebGL ビルドを公開しました。`n1920 x 1080 / $BuildId`nブラウザーで動作確認できます: $Url"
        allowed_mentions = @{ parse = @(); roles = @($MentionRole) }
    }
}

function Get-DiscordWebhook
{
    <# .SYNOPSIS
    Windows ユーザーに紐付いた暗号化設定を読み込み、Discord の通知先を検証する
    #>
    if (!(Test-Path -LiteralPath $secretPath -PathType Leaf))
    {
        throw 'Webhook 未設定です。PublishWebGL.ps1 -ConfigureWebhook を実行してください。'
    }
    $secure = Import-Clixml -LiteralPath $secretPath
    if ($secure -isnot [Security.SecureString])
    {
        throw 'Webhook 設定形式が不正です。再設定してください。'
    }
    $value = (New-Object System.Net.NetworkCredential('', $secure)).Password
    if ($value -notmatch '\Ahttps://discord\.com/api/webhooks/[0-9]+/[A-Za-z0-9_-]+\z')
    {
        throw 'Discord Webhook の設定が不正です。再設定してください。'
    }
    return $value
}

function Publish-WebGL
{
    <# .SYNOPSIS
    成功済み WebGL を GitHub Pages へ公開し、到達確認後に Discord へ通知する
    #>
    $files = Assert-PublishInput $OutputDirectory $Repository $RoleId
    $webhook = Get-DiscordWebhook
    if ($ValidateOnly)
    {
        Write-Output "公開設定の検証完了: $($files.Count) ファイル / ロール $RoleId。アップロード・通知は行っていません。"
        return
    }
    if ($null -eq (Get-Command gh -ErrorAction SilentlyContinue))
    {
        throw 'GitHub CLI が必要です。インストール後に gh auth login を実行してください。'
    }
    $script:scratchDirectory = Join-Path $projectRoot 'Builds/Publishing'
    New-Item -ItemType Directory -Path $scratchDirectory -Force | Out-Null
    $repo = Invoke-GitHub "repos/$Repository"
    if (!$repo.permissions.push)
    {
        throw '公開リポジトリへの書き込み権限がありません。'
    }
    $pages = Invoke-GitHub "repos/$Repository/pages" -AllowMissing
    if ($null -ne $pages -and ($pages.build_type -ne 'legacy' -or $pages.source.branch -ne $publishBranch -or $pages.source.path -ne '/'))
    {
        throw '既存の GitHub Pages が別の公開元を使っています。既存サイトを変更せずに停止しました。'
    }
    $branch = Invoke-GitHub "repos/$Repository/git/ref/heads/$publishBranch" -AllowMissing
    $previousCommit = $null
    $previousTree = @()
    if ($null -ne $branch)
    {
        $previousCommit = Invoke-GitHub "repos/$Repository/git/commits/$($branch.object.sha)"
        $previousTree = @((Invoke-GitHub "repos/$Repository/git/trees/$($previousCommit.tree.sha)?recursive=1").tree)
        if (@($previousTree | Where-Object { $_.path -eq 'roll-a-ball-publishing.json' }).Count -ne 1)
        {
            throw 'webgl-builds はこのツールの公開ブランチではありません。上書きせずに停止しました。'
        }
    }
    $buildId = [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 6)
    $entries = New-Object System.Collections.Generic.List[object]
    $retainedIds = @($previousTree | Where-Object { $_.type -eq 'blob' -and $_.path -match '^builds/([^/]+)/' } |
        ForEach-Object { $_.path.Split('/')[1] } | Sort-Object -Unique -Descending | Select-Object -First 2)
    $newSize = ($files | Measure-Object -Property Length -Sum).Sum
    while ($retainedIds.Count -gt 0)
    {
        $oldSize = ($previousTree | Where-Object { $_.type -eq 'blob' -and $_.path -match '^builds/([^/]+)/' -and $_.path.Split('/')[1] -in $retainedIds } |
            ForEach-Object { $_.size } | Measure-Object -Sum).Sum
        if ($newSize + $oldSize -lt 900MB)
        {
            break
        }
        $retainedIds = @($retainedIds | Select-Object -First ($retainedIds.Count - 1))
    }
    foreach ($item in $previousTree)
    {
        if ($item.type -eq 'blob' -and $item.path -match '^builds/([^/]+)/' -and $item.path.Split('/')[1] -notin $retainedIds)
        {
            $entries.Add(@{ path = $item.path; mode = '100644'; type = 'blob'; sha = $null })
        }
    }
    foreach ($file in $files)
    {
        $relative = $file.FullName.Substring([IO.Path]::GetFullPath($OutputDirectory).TrimEnd('\', '/').Length + 1).Replace('\', '/')
        Write-Output "アップロード: $relative"
        $blob = Invoke-GitHub "repos/$Repository/git/blobs" 'POST' @{
            content = [Convert]::ToBase64String([IO.File]::ReadAllBytes($file.FullName)); encoding = 'base64'
        }
        $entries.Add(@{ path = "builds/$buildId/$relative"; mode = '100644'; type = 'blob'; sha = $blob.sha })
    }
    $entries.Add(@{ path = '.nojekyll'; mode = '100644'; type = 'blob'; content = '' })
    $entries.Add(@{ path = 'roll-a-ball-publishing.json'; mode = '100644'; type = 'blob'; content = '{"tool":"RollABall.WebGL","version":1}' })
    $entries.Add(@{ path = "builds/$buildId/build-info.json"; mode = '100644'; type = 'blob'; content = (@{ buildId = $buildId } | ConvertTo-Json -Compress) })
    $entries.Add(@{ path = 'index.html'; mode = '100644'; type = 'blob'; content = "<!doctype html><meta charset=utf-8><meta http-equiv=refresh content='0;url=builds/$buildId/'><a href='builds/$buildId/'>Play Roll-a-Ball</a>" })
    $treeBody = @{ tree = @($entries.ToArray()) }
    if ($null -ne $previousCommit)
    {
        $treeBody.base_tree = $previousCommit.tree.sha
    }
    $tree = Invoke-GitHub "repos/$Repository/git/trees" 'POST' $treeBody
    $commitBody = @{ message = "Publish WebGL $buildId"; tree = $tree.sha }
    if ($null -ne $previousCommit)
    {
        $commitBody.parents = @($previousCommit.sha)
    }
    $commit = Invoke-GitHub "repos/$Repository/git/commits" 'POST' $commitBody
    if ($null -eq $branch)
    {
        Invoke-GitHub "repos/$Repository/git/refs" 'POST' @{ ref = "refs/heads/$publishBranch"; sha = $commit.sha } | Out-Null
    }
    else
    {
        Invoke-GitHub "repos/$Repository/git/refs/heads/$publishBranch" 'PATCH' @{ sha = $commit.sha; force = $false } | Out-Null
    }
    if ($null -eq $pages)
    {
        $pages = Invoke-GitHub "repos/$Repository/pages" 'POST' @{ build_type = 'legacy'; source = @{ branch = $publishBranch; path = '/' } }
    }
    Invoke-GitHub "repos/$Repository/pages/builds" 'POST' | Out-Null
    $playUrl = $pages.html_url.TrimEnd('/') + "/builds/$buildId/"
    Write-Output 'GitHub Pages の公開と到達確認を待っています…'
    $deadline = [DateTime]::UtcNow.AddMinutes(10)
    $reachable = $false
    while ([DateTime]::UtcNow -lt $deadline)
    {
        $build = Invoke-GitHub "repos/$Repository/pages/builds/latest"
        if ($build.commit -eq $commit.sha -and $build.status -eq 'errored')
        {
            throw 'GitHub Pages の公開が失敗しました。リポジトリの Actions と Pages 設定を確認してください。'
        }
        if ($build.commit -eq $commit.sha -and $build.status -eq 'built')
        {
            # 公開の反映待ちによる HTTP 404 は、短い待機の後に再確認する
            try
            {
                $manifest = Invoke-RestMethod -Uri ($playUrl + 'build-info.json') -TimeoutSec 20
                $page = Invoke-WebRequest -Uri $playUrl -UseBasicParsing -TimeoutSec 20
                $reachable = $manifest.buildId -eq $buildId -and $page.StatusCode -eq 200
            }
            catch
            {
                $reachable = $false
            }
            if ($reachable)
            {
                break
            }
        }
        Start-Sleep -Seconds 5
    }
    if (!$reachable)
    {
        throw '公開の到達確認がタイムアウトしました。Discord 通知は行っていません。'
    }
    $payload = New-DiscordPayload $RoleId $playUrl $buildId
    # Webhook の URI を例外・ログへ出さず、通知の成功と失敗を区別する
    try
    {
        $notification = Invoke-RestMethod -Method Post -Uri ($webhook + '?wait=true') -ContentType 'application/json; charset=utf-8' -Body ([Text.Encoding]::UTF8.GetBytes(($payload | ConvertTo-Json -Depth 5))) -TimeoutSec 30
    }
    catch
    {
        throw "公開済みですが Discord 通知に失敗しました。Webhook とロールのメンション許可を確認してください。URL: $playUrl"
    }
    if ($RoleId -notin @($notification.mention_roles))
    {
        throw "公開とメッセージ送信は完了しましたが、指定ロールのメンションを確認できません。Discord の設定を確認してください。URL: $playUrl"
    }
    Write-Output "公開・@roll-a-ball 通知完了: $playUrl"
}

if ($MyInvocation.InvocationName -ne '.')
{
    # OS・ネットワークの失敗を処理の境界で返し、Webhook の秘密情報を出力しない
    try
    {
        if ($ConfigureWebhook)
        {
            New-Item -ItemType Directory -Path (Split-Path $secretPath) -Force | Out-Null
            Read-Host 'Discord Webhook URL（非表示）' -AsSecureString | Export-Clixml -LiteralPath $secretPath
            Get-DiscordWebhook | Out-Null
            Write-Output 'Webhook を Windows ユーザーに紐付けて暗号化保存しました。'
        }
        else
        {
            Publish-WebGL
        }
    }
    catch
    {
        $safeMessage = $_.Exception.Message -replace 'https://discord\.com/api/webhooks/[^\s]+', '[Webhook redacted]'
        Write-Output "ERROR: $safeMessage"
        exit 1
    }
}
