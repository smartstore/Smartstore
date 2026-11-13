<#
.SYNOPSIS
    Renders the preview images of the Flex theme family with a headless Chromium browser.

.DESCRIPTION
    Loads previews.html once per theme and writes a 1280 x 960 screenshot to
    src/Smartstore.Web/Themes/<Theme>/wwwroot/images/preview.png, or to -OutDir.

.EXAMPLE
    ./render.ps1
    ./render.ps1 -Themes FlexTidal, FlexRiso
    ./render.ps1 -OutDir ./out
#>
param(
    [string[]]$Themes = @('Flex', 'FlexAfterglow', 'FlexBlack', 'FlexBlue', 'FlexLumen', 'FlexPorcelain', 'FlexRiso', 'FlexSorbet', 'FlexTidal'),
    [string]$OutDir,
    [string]$Browser
)

$ErrorActionPreference = 'Stop'

$toolDir = $PSScriptRoot
$themesDir = Join-Path $toolDir '..\..\src\Smartstore.Web\Themes' | Resolve-Path

if (-not $Browser) {
    $Browser = @(
        "${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe",
        "$env:ProgramFiles\Microsoft\Edge\Application\msedge.exe",
        "$env:ProgramFiles\Google\Chrome\Application\chrome.exe",
        "${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe"
    ) | Where-Object { Test-Path $_ } | Select-Object -First 1
}

if (-not $Browser) {
    throw 'No Edge or Chrome installation found. Pass the browser executable with -Browser.'
}

$page = ([System.Uri](Join-Path $toolDir 'previews.html')).AbsoluteUri
$profileDir = Join-Path ([System.IO.Path]::GetTempPath()) 'smartstore-theme-previews'
New-Item -ItemType Directory -Force $profileDir | Out-Null

foreach ($theme in $Themes) {
    if ($OutDir) {
        New-Item -ItemType Directory -Force $OutDir | Out-Null
        $out = Join-Path (Resolve-Path $OutDir) "$theme.png"
    }
    else {
        $out = Join-Path $themesDir "$theme\wwwroot\images\preview.png"
        if (-not (Test-Path (Split-Path $out))) {
            Write-Warning "Skipping $theme`: theme folder not found."
            continue
        }
    }

    # Arguments with paths are quoted because Start-Process joins them with spaces.
    Start-Process -FilePath $Browser -Wait -NoNewWindow `
        -RedirectStandardOutput (Join-Path $profileDir 'stdout.log') `
        -RedirectStandardError (Join-Path $profileDir 'stderr.log') `
        -ArgumentList @(
        '--headless=new', '--disable-gpu', '--hide-scrollbars', '--force-device-scale-factor=1',
        '--window-size=1280,960', '--virtual-time-budget=4000', '--allow-file-access-from-files',
        "`"--user-data-dir=$profileDir`"", "`"--screenshot=$out`"", "`"$page`?theme=$theme`"")

    if (Test-Path $out) {
        '{0,-14} {1,8:N0} bytes  {2}' -f $theme, (Get-Item $out).Length, $out
    }
    else {
        Write-Warning "Rendering $theme failed."
    }
}
