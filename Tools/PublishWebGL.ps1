[CmdletBinding()]
param(
    [string] $OutputDirectory,
    [string] $Repository = 'R-production004682/Roll-a-Ball',
    [string] $RoleId = '1538587867746668554',
    [switch] $ConfigureWebhook,
    [switch] $ValidateOnly,
    [switch] $NotifyLatest
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
    Assert-PublishSettings $Repo $MentionRole
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
    $allFiles = @($items | Where-Object { !$_.PSIsContainer })
    $html = [IO.File]::ReadAllText((Join-Path $Directory 'index.html'))
    $buildPaths = @([regex]::Matches($html, '["''](?<path>Build/[^"'']+)["'']') |
        ForEach-Object { $_.Groups['path'].Value } | Sort-Object -Unique)
    foreach ($path in $buildPaths)
    {
        if ($path -notmatch '\ABuild/[A-Za-z0-9_.-]+\z' -or !(Test-Path -LiteralPath (Join-Path $Directory $path) -PathType Leaf))
        {
            throw "index.html が参照するビルドファイルが不正または不足しています: $path"
        }
    }
    foreach ($pattern in @('\.loader\.js$', '\.data(?:\.unityweb|\.gz|\.br)?$', '\.framework\.js(?:\.unityweb|\.gz|\.br)?$', '\.wasm(?:\.unityweb|\.gz|\.br)?$'))
    {
        if (@($buildPaths | Where-Object { $_ -match $pattern }).Count -ne 1)
        {
            throw 'index.html の loader / data / framework / wasm の参照を確認してください。'
        }
    }
    $root = [IO.Path]::GetFullPath($Directory).TrimEnd('\', '/')
    $files = @($allFiles | Where-Object {
        $relative = $_.FullName.Substring($root.Length + 1).Replace('\', '/')
        $relative -eq 'index.html' -or $relative -in $buildPaths -or
            ($relative -match '\A(TemplateData|StreamingAssets)/' -and
            $relative -notmatch '(^|/)(\.[^/]+|UserSettings|Library|Logs|Temp|obj|[^/]*_DoNotShip|[^/]*_BackUpThisFolder_ButDontShipItWithYourGame)(/|$)|\.(meta|pdb|cs|ps1|log)$|(^|/)(Thumbs\.db|DiscordWebhook\.xml)$')
    })
    if (@($files | Where-Object { $_.Length -gt 95MB }).Count -gt 0)
    {
        $largest = $files | Sort-Object Length -Descending | Select-Object -First 1
        throw "95 MiB を超える公開ファイルがあります: $($largest.Name)。圧縮やアセットの軽量化が必要です。"
    }
    if (($files | Measure-Object -Property Length -Sum).Sum -gt 850MB)
    {
        throw 'WebGL ビルドが大きすぎます。公開サイズは 850 MiB 以下にしてください。'
    }
    return $files
}

function Assert-PublishSettings
{
    <# .SYNOPSIS
    GitHub リポジトリ名とメンション対象のロール ID を検証する
    #>
    param([string] $Repo, [string] $MentionRole)
    if ($Repo -notmatch '\A[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+\z' -or $MentionRole -notmatch '\A[0-9]{17,20}\z')
    {
        throw 'Repository または Discord Role ID が不正です。'
    }
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
        $status = [regex]::Match($errorText, 'HTTP \d{3}').Value
        $apiMessage = [regex]::Match(($response -join "`n"), '"message"\s*:\s*"([^"\\]{1,300})"').Groups[1].Value
        $detail = switch -Regex ($status)
        {
            '401|403'
            {
                'gh の認証・リポジトリ権限・API 制限を確認してください。'
                break
            }
            '404'
            {
                'リポジトリまたは対象の公開設定が見つかりません。'
                break
            }
            '413|422'
            {
                '送信サイズ・内容・公開ブランチの更新競合を確認してください。'
                break
            }
            default
            {
                '通信状態と GitHub の稼働状況を確認してください。'
            }
        }
        throw "GitHub API に失敗しました: $Method $Endpoint ($status)。$apiMessage $detail"
    }
    return ($response -join "`n" | ConvertFrom-Json)
}

function Invoke-PublishGit
{
    <# .SYNOPSIS
    公開専用リポジトリで Git を実行し、gh の認証を使ってビルドだけを転送する
    #>
    param([string] $Directory, [string[]] $GitArguments)
    $arguments = @('-C', $Directory, '-c', 'credential.helper=', '-c', 'credential.helper=!gh auth git-credential',
        '-c', 'core.autocrlf=false', '-c', "core.hooksPath=$scratchDirectory/hooks",
        '-c', 'user.name=Roll-a-Ball WebGL Publisher', '-c', 'user.email=webgl-publisher@users.noreply.github.com',
        '-c', 'commit.gpgSign=false') + $GitArguments
    $nativeErrorPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $response = & git @arguments 2> (Join-Path $scratchDirectory 'git-error.txt')
    $nativeExitCode = $LASTEXITCODE
    $ErrorActionPreference = $nativeErrorPreference
    if ($nativeExitCode -ne 0)
    {
        throw "公開用 Git に失敗しました: $($GitArguments[0])。gh auth status、通信状態、書き込み権限、公開の同時実行を確認してください。詳細: $scratchDirectory/git-error.txt"
    }
    return $response
}

function Push-WebGLBuild
{
    <# .SYNOPSIS
    開発リポジトリから分離した Git 領域で公開ファイルと過去二版だけをコミットし、強制せず push する
    #>
    param($Files, [string] $BuildId, $PreviousCommit, $PreviousTree, [string[]] $RetainedIds)
    $directory = Join-Path $scratchDirectory ('git-' + [Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $scratchDirectory 'hooks') -Force | Out-Null
    Invoke-PublishGit $directory @('init', '--quiet', '--template=', '--initial-branch=webgl-builds') | Out-Null
    $remote = "https://github.com/$Repository.git"
    if ($null -ne $PreviousCommit)
    {
        Invoke-PublishGit $directory @('fetch', '--quiet', '--depth=1', '--no-tags', $remote, "refs/heads/$publishBranch") | Out-Null
        $fetched = Invoke-PublishGit $directory @('rev-parse', 'FETCH_HEAD')
        if ($fetched -ne $PreviousCommit.sha)
        {
            throw '確認中に公開ブランチが更新されました。再実行してください。'
        }
    }
    Invoke-PublishGit $directory @('read-tree', '--empty') | Out-Null
    foreach ($item in $PreviousTree)
    {
        if ($item.type -eq 'blob' -and $item.path -match '^builds/([^/]+)/' -and $item.path.Split('/')[1] -in $RetainedIds)
        {
            Invoke-PublishGit $directory @('update-index', '--add', '--cacheinfo', "100644,$($item.sha),$($item.path)") | Out-Null
        }
    }
    foreach ($file in $Files)
    {
        $relative = $file.FullName.Substring([IO.Path]::GetFullPath($OutputDirectory).TrimEnd('\', '/').Length + 1).Replace('\', '/')
        $destination = Join-Path $directory "builds/$BuildId/$relative"
        New-Item -ItemType Directory -Path (Split-Path $destination) -Force | Out-Null
        [IO.File]::Copy($file.FullName, $destination)
    }
    $metadata = @{
        '.nojekyll' = ''
        'roll-a-ball-publishing.json' = '{"tool":"RollABall.WebGL","version":1}'
        "builds/$BuildId/build-info.json" = (@{ buildId = $BuildId } | ConvertTo-Json -Compress)
        'index.html' = "<!doctype html><meta charset=utf-8><meta http-equiv=refresh content='0;url=builds/$BuildId/'><a href='builds/$BuildId/'>Play Roll-a-Ball</a>"
    }
    foreach ($path in $metadata.Keys)
    {
        [IO.File]::WriteAllText((Join-Path $directory $path), $metadata[$path], (New-Object Text.UTF8Encoding($false)))
    }
    Invoke-PublishGit $directory @('add', '--force', '--', '.nojekyll', 'roll-a-ball-publishing.json', 'index.html', "builds/$BuildId") | Out-Null
    $tree = Invoke-PublishGit $directory @('write-tree')
    $commitArguments = @('commit-tree', $tree, '-m', "Publish WebGL $BuildId")
    if ($null -ne $PreviousCommit)
    {
        $commitArguments += @('-p', $PreviousCommit.sha)
    }
    $commit = Invoke-PublishGit $directory $commitArguments
    Write-Host "Git 転送中: $($Files.Count) ファイル（Base64 変換なし）"
    Invoke-PublishGit $directory @('push', '--porcelain', $remote, "${commit}:refs/heads/$publishBranch") | Out-Null
    # この呼び出しで生成した専用ディレクトリだけを、絶対パスの境界を確認して削除する
    $scratchRoot = [IO.Path]::GetFullPath($scratchDirectory).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    if ([IO.Path]::GetFullPath($directory).StartsWith($scratchRoot, [StringComparison]::OrdinalIgnoreCase))
    {
        Remove-Item -LiteralPath $directory -Recurse -Force
    }
    return $commit
}

function Get-PagesPublication
{
    <# .SYNOPSIS
    対象コミットの最新 Pages Actions を取得し、古い Pages API の失敗やキャンセルを完了判定に使わない
    #>
    param([string] $CommitSha)
    $response = Invoke-GitHub "repos/$Repository/actions/runs?head_sha=$CommitSha&branch=$publishBranch&event=dynamic&per_page=100"
    $run = $response.workflow_runs | Where-Object {
        $_.head_sha -eq $CommitSha -and $_.path -eq 'dynamic/pages/pages-build-deployment'
    } | Sort-Object id -Descending | Select-Object -First 1
    if ($null -eq $run)
    {
        return @{ status = 'pending'; detail = 'Pages Actions の開始を待っています'; url = "https://github.com/$Repository/actions" }
    }
    if ($run.status -ne 'completed' -or $run.conclusion -eq 'cancelled')
    {
        return @{ status = 'pending'; detail = "Pages Actions: $($run.status) / $($run.conclusion)"; url = $run.html_url }
    }
    if ($run.conclusion -eq 'success')
    {
        return @{ status = 'built'; detail = 'Pages Actions が成功しました'; url = $run.html_url }
    }
    return @{ status = 'failed'; detail = "Pages Actions: $($run.conclusion)"; url = $run.html_url }
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
    $files = @(Assert-PublishInput $OutputDirectory $Repository $RoleId)
    $size = [math]::Round(($files | Measure-Object Length -Sum).Sum / 1MB, 2)
    Write-Output "公開対象: $($files.Count) ファイル / $size MiB"
    foreach ($file in ($files | Sort-Object Length -Descending))
    {
        Write-Output ("  {0:N2} MiB  {1}" -f ($file.Length / 1MB), $file.FullName.Substring($OutputDirectory.TrimEnd('\', '/').Length + 1))
    }
    if ($ValidateOnly)
    {
        Write-Output "公開設定の検証完了: $($files.Count) ファイル / ロール $RoleId。アップロード・通知は行っていません。"
        return
    }
    $webhook = Get-DiscordWebhook
    if ($null -eq (Get-Command gh -ErrorAction SilentlyContinue) -or $null -eq (Get-Command git -ErrorAction SilentlyContinue))
    {
        throw 'Git と GitHub CLI が必要です。インストール後に gh auth login を実行してください。'
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
        $treeResponse = Invoke-GitHub "repos/$Repository/git/trees/$($previousCommit.tree.sha)?recursive=1"
        if ($treeResponse.truncated)
        {
            throw '公開ツリーの取得結果が省略されました。過去版を安全に保持できないため停止しました。'
        }
        $previousTree = @($treeResponse.tree)
        if (@($previousTree | Where-Object { $_.path -eq 'roll-a-ball-publishing.json' }).Count -ne 1)
        {
            throw 'webgl-builds はこのツールの公開ブランチではありません。上書きせずに停止しました。'
        }
    }
    $buildId = [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 6)
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
    $commitSha = Push-WebGLBuild $files $buildId $previousCommit $previousTree $retainedIds
    if ($null -eq $pages)
    {
        $pages = Invoke-GitHub "repos/$Repository/pages" 'POST' @{ build_type = 'legacy'; source = @{ branch = $publishBranch; path = '/' } }
    }
    # push または Pages の初期設定で自動起動するため、追加のビルド要求で二重起動させない
    $playUrl = $pages.html_url.TrimEnd('/') + "/builds/$buildId/"
    Complete-WebGLPublication $commitSha $playUrl $buildId $webhook
}

function Complete-WebGLPublication
{
    <# .SYNOPSIS
    対象コミットのデプロイ成功とページ到達を待ち、公開済みの版だけを通知する
    #>
    param([string] $CommitSha, [string] $PlayUrl, [string] $BuildId, [string] $Webhook)
    Write-Output 'GitHub Pages の公開と到達確認を待っています…'
    $deadline = [DateTime]::UtcNow.AddMinutes(10)
    $reachable = $false
    while ([DateTime]::UtcNow -lt $deadline)
    {
        $publication = Get-PagesPublication $commitSha
        if ($publication.status -eq 'failed')
        {
            throw "GitHub Pages の公開が失敗しました: $($publication.detail)。実行ログ: $($publication.url)"
        }
        if ($publication.status -eq 'built')
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
        throw "公開の到達確認がタイムアウトしました: $($publication.detail)。実行ログ: $($publication.url)。Discord 通知は行っていません。"
    }
    Send-DiscordBuildNotification $Webhook $PlayUrl $BuildId $RoleId
}

function Send-DiscordBuildNotification
{
    <# .SYNOPSIS
    公開済みビルドのロール通知を送り、メッセージ ID と Discord が受け付けたロールを記録する
    #>
    param([string] $Webhook, [string] $PlayUrl, [string] $BuildId, [string] $MentionRole)
    $payload = New-DiscordPayload $MentionRole $PlayUrl $BuildId
    Write-Output "Discord 通知を送信しています: ロール $MentionRole / $BuildId"
    # Webhook の URI を例外・ログへ出さず、通知の成功と失敗を区別する
    try
    {
        $notification = Invoke-RestMethod -Method Post -Uri ($webhook + '?wait=true') -ContentType 'application/json; charset=utf-8' -Body ([Text.Encoding]::UTF8.GetBytes(($payload | ConvertTo-Json -Depth 5))) -TimeoutSec 30
    }
    catch
    {
        $httpStatus = ''
        if ($_.Exception.PSObject.Properties['Response'] -and $null -ne $_.Exception.Response)
        {
            $httpStatus = " HTTP $([int] $_.Exception.Response.StatusCode)"
        }
        throw "公開済みですが Discord 通知に失敗しました$httpStatus。Webhook と通知先を確認してください。再ビルドせず -NotifyLatest で再送できます。URL: $PlayUrl"
    }
    $result = @{
        buildId = $BuildId; playUrl = $PlayUrl; roleId = $MentionRole
        messageId = $notification.id; channelId = $notification.channel_id
        roleAccepted = $MentionRole -in @($notification.mention_roles)
        sentAt = [DateTime]::UtcNow.ToString('o')
    }
    [IO.File]::WriteAllText((Join-Path $scratchDirectory 'notification-result.json'), ($result | ConvertTo-Json -Compress), (New-Object Text.UTF8Encoding($false)))
    if (!$result.roleAccepted)
    {
        throw "Discord メッセージは送信済みですが、ロール $MentionRole のメンションが受理されませんでした。ロール ID とメンション許可を確認してください。メッセージ ID: $($result.messageId)。URL: $PlayUrl"
    }
    Write-Output "公開・@roll-a-ball 通知完了: $PlayUrl（メッセージ ID: $($result.messageId) / チャンネル ID: $($result.channelId)）"
}

function Notify-LatestWebGL
{
    <# .SYNOPSIS
    公開ブランチの最新版を検証し、ビルドや push をせずに Discord 通知だけを再実行する
    #>
    Assert-PublishSettings $Repository $RoleId
    if ($null -eq (Get-Command gh -ErrorAction SilentlyContinue))
    {
        throw 'GitHub CLI が必要です。インストール後に gh auth login を実行してください。'
    }
    $script:scratchDirectory = Join-Path $projectRoot 'Builds/Publishing'
    New-Item -ItemType Directory -Path $scratchDirectory -Force | Out-Null
    $webhook = Get-DiscordWebhook
    $pages = Invoke-GitHub "repos/$Repository/pages"
    if ($pages.build_type -ne 'legacy' -or $pages.source.branch -ne $publishBranch -or $pages.source.path -ne '/')
    {
        throw 'このツールの WebGL 公開元ではないため、通知を送らず停止しました。'
    }
    $branch = Invoke-GitHub "repos/$Repository/git/ref/heads/$publishBranch"
    $commitSha = $branch.object.sha
    $marker = Invoke-GitHub "repos/$Repository/contents/roll-a-ball-publishing.json?ref=$commitSha"
    $metadata = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($marker.content)) | ConvertFrom-Json
    if ($metadata.tool -ne 'RollABall.WebGL' -or $metadata.version -ne 1)
    {
        throw 'このツールが作成した公開版ではないため、通知を送らず停止しました。'
    }
    $index = Invoke-GitHub "repos/$Repository/contents/index.html?ref=$commitSha"
    $html = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($index.content))
    $buildId = [regex]::Match($html, 'builds/([0-9]{8}-[0-9]{6}-[a-f0-9]{6})/').Groups[1].Value
    if ([string]::IsNullOrEmpty($buildId))
    {
        throw '公開された最新版の ID が見つかりません。通知を送らず停止しました。'
    }
    $playUrl = $pages.html_url.TrimEnd('/') + "/builds/$buildId/"
    Complete-WebGLPublication $commitSha $playUrl $buildId $webhook
}

if ($MyInvocation.InvocationName -ne '.')
{
    # OS・ネットワークの失敗を処理の境界で返し、Webhook の秘密情報を出力しない
    try
    {
        if ($NotifyLatest -and ($ConfigureWebhook -or $ValidateOnly))
        {
            throw '-NotifyLatest は -ConfigureWebhook または -ValidateOnly と同時に指定できません。'
        }
        if ($ConfigureWebhook)
        {
            New-Item -ItemType Directory -Path (Split-Path $secretPath) -Force | Out-Null
            Read-Host 'Discord Webhook URL（非表示）' -AsSecureString | Export-Clixml -LiteralPath $secretPath
            Get-DiscordWebhook | Out-Null
            Write-Output 'Webhook を Windows ユーザーに紐付けて暗号化保存しました。'
        }
        elseif ($NotifyLatest)
        {
            Notify-LatestWebGL
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
