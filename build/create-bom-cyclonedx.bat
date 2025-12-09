@echo off
setlocal enabledelayedexpansion

REM ============================================================
REM Generate CycloneDX SBOM for Smartstore Solution
REM Output: Smartstore.{Edition}.{Version}.sbom.cyclone.json (in solution root)
REM Script location: /build
REM Compatible with Windows PowerShell 5.1
REM ============================================================

set "APP_VERSION=6.3.0"
set "APP_EDITION=Community"

set "ROOT_DIR=%~dp0.."
set "TOOLS_DIR=%~dp0.tools"
set "CYCLONEDX_EXE=%TOOLS_DIR%\dotnet-CycloneDX.exe"
set "SOLUTION_FILE=%ROOT_DIR%\Smartstore.sln"
set "SBOM_FILE=%ROOT_DIR%\Smartstore.%APP_EDITION%.%APP_VERSION%.sbom.cyclone.json"
set "SBOM_TMP=%SBOM_FILE%.tmp"

echo.
echo [SBOM] Creating CycloneDX SBOM...
echo [SBOM] Solution: %SOLUTION_FILE%
echo [SBOM] Output:   %SBOM_FILE%
echo.

if not exist "%TOOLS_DIR%" mkdir "%TOOLS_DIR%"
if not exist "%CYCLONEDX_EXE%" (
    echo [INFO] Installing CycloneDX tool...
    dotnet tool install --tool-path "%TOOLS_DIR%" CycloneDX
)

if not exist "%SOLUTION_FILE%" (
    echo [ERROR] Solution file not found: %SOLUTION_FILE%
    exit /b 1
)

pushd "%ROOT_DIR%"
"%CYCLONEDX_EXE%" "%SOLUTION_FILE%" --output "%SBOM_FILE%" --json
if errorlevel 1 (
    echo [ERROR] SBOM generation failed.
    popd
    exit /b 1
)

REM === Wait until file is readable ===
set "retries=10"
:wait_loop
(
    type "%SBOM_FILE%" >nul 2>&1
) && goto ready
ping -n 2 127.0.0.1 >nul
set /a retries-=1
if %retries% gtr 0 goto wait_loop
echo [WARN] File still locked after retries, skipping prettify.
goto done

:ready
copy /y "%SBOM_FILE%" "%SBOM_TMP%" >nul

echo [SBOM] Prettifying JSON output...
powershell -NoLogo -NoProfile -Command ^
    "try {" ^
    "  $json = Get-Content -Raw -Path '%SBOM_TMP%';" ^
    "  if($json) { $obj = $json | ConvertFrom-Json; $obj | ConvertTo-Json -Depth 100 | Out-File -FilePath '%SBOM_FILE%' -Encoding UTF8 }" ^
    "} catch { Write-Host 'PowerShell prettify failed:' $_ }"

del "%SBOM_TMP%" >nul 2>&1

:done
popd

echo.
echo [OK] CycloneDX SBOM successfully created and prettified (if possible):
echo      %SBOM_FILE%
echo.

endlocal
exit /b 0
