[CmdletBinding()]
param(
    [string] $OutputDirectory,
    [string] $Repository = 'R-production004682/Roll-a-Ball',
    [string] $RoleId = '1538587867746668554',
    [string] $PreviewId,
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
        $detail = Get-SafeCommandError $errorText
        $hint = if ($errorText -match 'HTTP (401|403)') { '認証・権限または API 制限を確認してください。' } else { '' }
        throw "GitHub API に失敗しました: $Method $Endpoint。$detail $hint"
    }
    return ($response -join "`n" | ConvertFrom-Json)
}

function Get-SafeCommandError
{
    <# .SYNOPSIS
    GitHub CLI と Git のエラーから認証情報を伏せた診断を作成する
    #>
    param([string] $Message)
    $lines = @($Message -split '\r?\n' | Where-Object { $_ -match 'gh:|fatal:|remote:|error:' })
    $detail = if ($lines.Count -gt 0) { $lines -join ' ' } else { '外部コマンドが失敗しました。' }
    $detail = $detail -replace 'https://discord\.com/api/webhooks/[^\s]+', '[Webhook redacted]'
    $detail = $detail -replace '(gh[pousr]_[A-Za-z0-9_]+|github_pat_[A-Za-z0-9_]+)', '[Token redacted]'
    return $detail
}

function Invoke-PublishingGit
{
    <# .SYNOPSIS
    認証情報を使って Git コマンドを実行し、失敗時に安全な診断を返す
    #>
    param([string] $GitDirectory, [string[]] $GitArguments)
    $stagingDirectory = Join-Path $scratchDirectory 'git-staging'
    New-Item -ItemType Directory -Path $stagingDirectory -Force | Out-Null
    $arguments = @('-c', 'credential.helper=', '-c', 'credential.helper=!gh auth git-credential',
        '-c', 'commit.gpgSign=false', "--git-dir=$GitDirectory")
    if ($GitArguments -contains '--force-remove')
    {
        $arguments += "--work-tree=$stagingDirectory"
    }
    $arguments += $GitArguments
    $errorPath = Join-Path $scratchDirectory 'git-error.txt'
    $previousPreference = $ErrorActionPreference
    $previousPrompt = $env:GIT_TERMINAL_PROMPT
    $ErrorActionPreference = 'Continue'
    $env:GIT_TERMINAL_PROMPT = '0'
    $response = & git @arguments 2> $errorPath
    $exitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousPreference
    $env:GIT_TERMINAL_PROMPT = $previousPrompt
    if ($exitCode -ne 0)
    {
        $detail = Get-SafeCommandError (Get-Content -LiteralPath $errorPath -Raw)
        throw "Git によるビルド送信に失敗しました。$detail"
    }
    return ($response -join "`n").Trim()
}

function Initialize-PublishingRepository
{
    <# .SYNOPSIS
    専用の bare repository を作成または安全に再利用する
    #>
    param([string] $Repo)
    $directory = Join-Path $scratchDirectory 'upload.git'
    $marker = Join-Path $directory 'roll-a-ball-publisher.txt'
    if (Test-Path -LiteralPath $directory)
    {
        if (!(Test-Path -LiteralPath $marker -PathType Leaf) -or
            [IO.File]::ReadAllText($marker).Trim() -ne $Repo)
        {
            throw '公開用 Git フォルダーが別用途で使われています。上書きせずに停止しました。'
        }
    }
    else
    {
        Invoke-PublishingGit $directory @('init', '--bare') | Out-Null
        Invoke-PublishingGit $directory @('config', 'remote.origin.url', "https://github.com/$Repo.git") | Out-Null
        Invoke-PublishingGit $directory @('config', 'remote.origin.promisor', 'true') | Out-Null
        Invoke-PublishingGit $directory @('config', 'remote.origin.partialclonefilter', 'blob:none') | Out-Null
        [IO.File]::WriteAllText($marker, $Repo, (New-Object Text.UTF8Encoding($false)))
    }
    return $directory
}

function Push-GitBuild
{
    <# .SYNOPSIS
    大きなファイルを JSON/Base64 にせず、専用の bare repository から通常の Git push で送る
    #>
    param([string] $Repo, [string] $Branch, [string] $PreviousSha, [object[]] $Entries, [string] $BuildId)
    $directory = Initialize-PublishingRepository $Repo
    if (![string]::IsNullOrWhiteSpace($PreviousSha))
    {
        Invoke-PublishingGit $directory @('fetch', '--no-tags', '--depth=1', '--filter=blob:none', 'origin', "refs/heads/$Branch") | Out-Null
        $fetched = Invoke-PublishingGit $directory @('rev-parse', 'FETCH_HEAD')
        if ($fetched -ne $PreviousSha)
        {
            throw '公開ブランチが別の操作で更新されました。再実行してください。'
        }
        Invoke-PublishingGit $directory @('read-tree', $PreviousSha) | Out-Null
    }
    else
    {
        Invoke-PublishingGit $directory @('read-tree', '--empty') | Out-Null
    }
    foreach ($entry in $Entries)
    {
        if ($entry.ContainsKey('sha') -and $null -eq $entry.sha)
        {
            Invoke-PublishingGit $directory @('update-index', '--force-remove', '--', $entry.path) | Out-Null
            continue
        }
        if ($entry.ContainsKey('file'))
        {
            $filePath = $entry.file
        }
        else
        {
            $filePath = Join-Path $scratchDirectory 'generated-content.txt'
            [IO.File]::WriteAllText($filePath, [string]$entry.content, (New-Object Text.UTF8Encoding($false)))
        }
        # Pages では LFS pointer を配信できないため、フィルターを通さず実ファイルを登録する。
        $sha = Invoke-PublishingGit $directory @('hash-object', '--no-filters', '-w', '--', $filePath)
        Invoke-PublishingGit $directory @('update-index', '--add', '--cacheinfo', "100644,$sha,$($entry.path)") | Out-Null
    }
    $treeSha = Invoke-PublishingGit $directory @('write-tree')
    $arguments = @('-c', 'user.name=Roll-a-Ball WebGL', '-c', 'user.email=webgl-publisher@users.noreply.github.com',
        'commit-tree', $treeSha, '-m', "Publish WebGL $BuildId")
    if (![string]::IsNullOrWhiteSpace($PreviousSha))
    {
        $arguments += @('-p', $PreviousSha)
    }
    $commitSha = Invoke-PublishingGit $directory $arguments
    Write-Output 'Git でビルドを送信しています…' | Out-Host
    # 強制更新しない。別の公開処理が先行した場合は push を失敗させる。
    Invoke-PublishingGit $directory @('push', 'origin', "${commitSha}:refs/heads/$Branch") | Out-Null
    return $commitSha
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
    if (![string]::IsNullOrWhiteSpace($env:DISCORD_WEBHOOK_URL))
    {
        $value = $env:DISCORD_WEBHOOK_URL
    }
    elseif (!(Test-Path -LiteralPath $secretPath -PathType Leaf))
    {
        throw 'Webhook 未設定です。PublishWebGL.ps1 -ConfigureWebhook を実行してください。'
    }
    else
    {
        $secure = Import-Clixml -LiteralPath $secretPath
        if ($secure -isnot [Security.SecureString])
        {
            throw 'Webhook 設定形式が不正です。再設定してください。'
        }
        $value = (New-Object System.Net.NetworkCredential('', $secure)).Password
    }
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
    if (![string]::IsNullOrWhiteSpace($PreviewId) -and $PreviewId -notmatch '\Apr-[1-9][0-9]*\z')
    {
        throw 'PreviewId は pr-123 の形式で指定してください。'
    }
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
    if ($null -eq (Get-Command git -ErrorAction SilentlyContinue))
    {
        throw 'Git が必要です。Git for Windows をインストールしてください。'
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
    if (![string]::IsNullOrWhiteSpace($PreviewId))
    {
        $buildId = "$PreviewId-$buildId"
    }
    $entries = New-Object System.Collections.Generic.List[object]
    $retainedIds = @($previousTree | Where-Object { $_.type -eq 'blob' -and $_.path -match '^builds/([^/]+)/' } |
        ForEach-Object { $_.path.Split('/')[1] } | Sort-Object -Unique |
        Sort-Object { $_ -replace '^pr-[0-9]+-', '' } -Descending | Select-Object -First 2)
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
        Write-Output "公開ファイル: $relative"
        $entries.Add(@{ path = "builds/$buildId/$relative"; file = $file.FullName })
    }
    $entries.Add(@{ path = '.nojekyll'; mode = '100644'; type = 'blob'; content = '' })
    $entries.Add(@{ path = 'roll-a-ball-publishing.json'; mode = '100644'; type = 'blob'; content = '{"tool":"RollABall.WebGL","version":1}' })
    $entries.Add(@{ path = "builds/$buildId/build-info.json"; mode = '100644'; type = 'blob'; content = (@{ buildId = $buildId } | ConvertTo-Json -Compress) })
    $entries.Add(@{ path = 'index.html'; mode = '100644'; type = 'blob'; content = "<!doctype html><meta charset=utf-8><meta http-equiv=refresh content='0;url=builds/$buildId/'><a href='builds/$buildId/'>Play Roll-a-Ball</a>" })
    $previousSha = if ($null -ne $previousCommit) { $previousCommit.sha } else { $null }
    $commitSha = Push-GitBuild $Repository $publishBranch $previousSha @($entries.ToArray()) $buildId
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
        if ($build.commit -eq $commitSha -and $build.status -eq 'errored')
        {
            throw 'GitHub Pages の公開が失敗しました。リポジトリの Actions と Pages 設定を確認してください。'
        }
        if ($build.commit -eq $commitSha -and $build.status -eq 'built')
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
