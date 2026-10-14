# Git の送信経路をローカル bare repository で検証する。GitHub・Discord へ接続しない。
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '../PublishWebGL.ps1')
$fixtureRoot = Join-Path $projectRoot ('Builds/PublishingTests/Git-' + [Guid]::NewGuid().ToString('N'))
$script:scratchDirectory = $fixtureRoot
New-Item -ItemType Directory -Path $fixtureRoot -Force | Out-Null
$script:uploadRepository = Join-Path $fixtureRoot 'upload.git'
$remoteRepository = Join-Path $fixtureRoot 'remote.git'
Invoke-PublishingGit $remoteRepository @('init', '--bare') | Out-Null
Invoke-PublishingGit $uploadRepository @('init', '--bare') | Out-Null
Invoke-PublishingGit $uploadRepository @('config', 'remote.origin.url', $remoteRepository) | Out-Null

function Initialize-PublishingRepository
{
    param([string] $Repo)
    return $script:uploadRepository
}

function Assert-Test
{
    param([bool] $Condition, [string] $Description)
    if (!$Condition)
    {
        throw "TEST FAILED: $Description"
    }
}

$seed = @(
    @{ path = 'builds/keep/file.txt'; content = 'keep' },
    @{ path = 'builds/old/file.txt'; content = 'old' },
    @{ path = '.gitattributes'; content = '*.unityweb filter=lfs diff=lfs merge=lfs -text' }
)
$first = Push-GitBuild 'fixture/repo' 'webgl-builds' $null $seed 'seed'
$largeFile = Join-Path $fixtureRoot 'test.data.unityweb'
$stream = [IO.File]::Open($largeFile, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write)
$stream.SetLength(57791614)
$stream.Dispose()
$entries = @(
    @{ path = 'builds/old/file.txt'; sha = $null },
    @{ path = 'builds/current/Build/test.data.unityweb'; file = $largeFile },
    @{ path = 'index.html'; content = '<a href="builds/current/">Play</a>' }
)
$second = Push-GitBuild 'fixture/repo' 'webgl-builds' $first $entries 'current'
$head = Invoke-PublishingGit $remoteRepository @('rev-parse', 'refs/heads/webgl-builds')
Assert-Test ($head -eq $second) '新しいビルドをリモートへ送る'
$parent = Invoke-PublishingGit $remoteRepository @('rev-parse', "$second^1")
Assert-Test ($parent -eq $first) '履歴を維持する'
$paths = Invoke-PublishingGit $remoteRepository @('ls-tree', '-r', '--name-only', $second)
Assert-Test ($paths -match 'builds/keep/file.txt' -and $paths -notmatch 'builds/old/file.txt') '過去版保持と古い公開版の削除'
$blob = Invoke-PublishingGit $remoteRepository @('rev-parse', "${second}:builds/current/Build/test.data.unityweb")
$size = Invoke-PublishingGit $remoteRepository @('cat-file', '-s', $blob)
Assert-Test ($size -eq '57791614') '約55 MiB の実データを保持し、LFS pointer や Base64 にしない'
$conflictRejected = $false
try
{
    Push-GitBuild 'fixture/repo' 'webgl-builds' $first $entries 'conflict' | Out-Null
}
catch
{
    $conflictRejected = $_.Exception.Message -match '別の操作で更新'
}
Assert-Test $conflictRejected '古い公開元から競合する更新を上書きしない'
$afterConflict = Invoke-PublishingGit $remoteRepository @('rev-parse', 'refs/heads/webgl-builds')
Assert-Test ($afterConflict -eq $second) '競合後も公開済みブランチを維持する'
Remove-Item -LiteralPath $fixtureRoot -Recurse -Force
Write-Output 'PASS: 大きなビルド、実データ保持、Git push、履歴・過去版保持、競合拒否（ローカルのみ）'
