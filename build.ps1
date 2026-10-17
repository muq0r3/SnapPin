param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$sln = Join-Path $root "SnapPin.sln"

$msbuild = $null
$vswhere = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"
if (Test-Path $vswhere) {
    $vs = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath
    if ($vs) {
        $candidate = Join-Path $vs "MSBuild\Current\Bin\MSBuild.exe"
        if (Test-Path $candidate) { $msbuild = $candidate }
    }
}

if (-not $msbuild) {
    throw "未找到 MSBuild.exe，请安装 Visual Studio 2019+ 或 Build Tools。"
}

& $msbuild $sln /p:Configuration=$Configuration /p:Platform="Any CPU" /m /v:minimal /nologo
exit $LASTEXITCODE
