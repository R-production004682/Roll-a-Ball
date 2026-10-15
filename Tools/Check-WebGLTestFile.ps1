<# .SYNOPSIS
WebGL テストファイルのアクセス拒否と Git 差分を読み取り専用で診断する
#>
[CmdletBinding()]
param(
    [string] $RepositoryRoot,
    [string] $GitExecutable = 'git'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($RepositoryRoot))
{
    $RepositoryRoot = Split-Path $PSScriptRoot -Parent
}
$RepositoryRoot = [IO.Path]::GetFullPath($RepositoryRoot)
$relativePath = 'Tools/Tests/PublishWebGL.Tests.ps1'

function Invoke-GitCheck
{
    <# .SYNOPSIS
    Git の読み取り結果と終了コードを返し、index の任意更新を抑止する
    #>
    param([string[]] $GitArguments)
    $ErrorActionPreference = 'Continue'
    $output = @(& $GitExecutable --no-optional-locks -C $RepositoryRoot @GitArguments 2>&1 |
        ForEach-Object { $_.ToString() })
    return [pscustomobject]@{ ExitCode = $LASTEXITCODE; Output = ($output -join "`n") }
}

$root = Invoke-GitCheck @('rev-parse', '--show-toplevel')
if ($root.ExitCode -ne 0)
{
    throw "Git repository could not be read: $($root.Output)"
}
$RepositoryRoot = $root.Output.Trim()
$targetPath = Join-Path $RepositoryRoot $relativePath
$attributes = $null
$readError = $null
$access = $null
$canRead = $false

try
{
    $item = Get-Item -LiteralPath $targetPath
    $attributes = $item.Attributes.ToString()
    if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)
    {
        throw 'The test file is a link; its target was not read.'
    }
    # ReadAllBytes がハンドルを解放する。ファイル内容はレポートに出力しない
    $null = [IO.File]::ReadAllBytes($targetPath)
    $canRead = $true
}
catch
{
    # OS のファイル読み取り境界で、アクセス拒否・共有違反等の識別情報を返す
    $exception = $_.Exception.GetBaseException()
    $readError = [pscustomobject]@{
        Type = $exception.GetType().FullName
        HResult = ('0x{0:X8}' -f $exception.HResult)
        Message = $exception.Message
    }
}

try
{
    $acl = Get-Acl -LiteralPath $targetPath
    $access = [pscustomobject]@{ Owner = $acl.Owner; Rules = $acl.AccessToString }
}
catch
{
    # ACL の参照権限がない場合も、ファイルの読み取り結果は失わない
    $access = [pscustomobject]@{ Error = $_.Exception.GetBaseException().Message }
}

$index = Invoke-GitCheck @('rev-parse', ":$relativePath")
$working = $null
$matchesIndex = $null
if ($canRead)
{
    $working = Invoke-GitCheck @('hash-object', "--path=$relativePath", '--', $targetPath)
    if ($index.ExitCode -eq 0 -and $working.ExitCode -eq 0)
    {
        $matchesIndex = $index.Output.Trim() -eq $working.Output.Trim()
    }
}

[pscustomobject]@{
    RepositoryRoot = $RepositoryRoot
    Path = $relativePath
    GitVersion = (Invoke-GitCheck @('--version')).Output
    GitExecutable = (Get-Command $GitExecutable -ErrorAction Stop).Source
    CanRead = $canRead
    Attributes = $attributes
    ReadError = $readError
    Access = $access
    Status = (Invoke-GitCheck @('status', '--short', '--', $relativePath))
    LineEndings = (Invoke-GitCheck @('ls-files', '--eol', '--', $relativePath)).Output
    AttributesRule = (Invoke-GitCheck @('check-attr', '-a', '--', $relativePath)).Output
    AutoCrlf = (Invoke-GitCheck @('config', '--get', 'core.autocrlf')).Output
    ContentMatchesIndex = $matchesIndex
    WorkingHashResult = $working
} | ConvertTo-Json -Depth 5
