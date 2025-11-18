@echo off
setlocal enabledelayedexpansion

REM ============================================================
REM Full SBOM generation for Smartstore
REM 1) Backend SBOM (.NET / NuGet) via CycloneDX .NET tool
REM 2) Frontend SBOM (wwwroot\lib) via Syft (dir-scan, file cataloger)
REM    - if Syft finds nothing, fallback to PowerShell generator
REM 3) Merge both SBOMs via CycloneDX CLI
REM Output:
REM   sbom\sbom-cyclonedx.Community.6.2.0.backend.json
REM   sbom\sbom-cyclonedx.frontend.json
REM   sbom\sbom-cyclonedx.Community.6.2.0.combined.json
REM Requirements:
REM   - Windows
REM   - .NET SDK
REM   - PowerShell (comes with Windows)
REM   - npm (for CycloneDX CLI install, if not already present)
REM ============================================================

REM Base directory (solution root, with trailing backslash)
set "SCRIPT_DIR=%~dp0"

REM ==== GENERAL CONFIG ========================================
set "APP_VERSION=6.2.0"
set "APP_EDITION=Community"
set "PLATFORM=win-x64"

REM Solution / main entry (used by CycloneDX .NET)
set "MAIN_PROJECT_REL=Smartstore.sln"
set "MAIN_PROJECT=%SCRIPT_DIR%%MAIN_PROJECT_REL%"

REM Frontend libs folder (relative to solution root)
set "FRONTEND_LIB_REL=src\Smartstore.Web\wwwroot\lib"
set "FRONTEND_LIB_DIR=%SCRIPT_DIR%%FRONTEND_LIB_REL%"

REM SBOM output directory
set "OUTPUT_DIR=%SCRIPT_DIR%sbom"

REM Backend SBOM filenames
set "BACKEND_SBOM_NAME=sbom-cyclonedx.%APP_EDITION%.%APP_VERSION%.backend.json"
set "BACKEND_SBOM=%OUTPUT_DIR%\%BACKEND_SBOM_NAME%"

REM Frontend SBOM filenames
set "FRONTEND_SBOM_NAME=sbom-cyclonedx.frontend.json"
set "FRONTEND_SBOM=%OUTPUT_DIR%\%FRONTEND_SBOM_NAME%"
set "FRONTEND_SBOM_RAW=%FRONTEND_SBOM%.raw"

REM Combined SBOM filename
set "COMBINED_SBOM_NAME=sbom-cyclonedx.%APP_EDITION%.%APP_VERSION%.combined.json"
set "COMBINED_SBOM=%OUTPUT_DIR%\%COMBINED_SBOM_NAME%"

REM Tools
set "CYCLONEDX_DOTNET_TOOL=CycloneDX"
set "SYFT_VERSION=1.38.0"
set "SYFT_TOOLS_DIR=%SCRIPT_DIR%.tools\syft"
set "SYFT_EXE=%SYFT_TOOLS_DIR%\syft.exe"
set "SYFT_URL=https://github.com/anchore/syft/releases/download/v%SYFT_VERSION%/syft_%SYFT_VERSION%_windows_amd64.zip"

REM ============================================================
echo.
echo [SBOM] Full SBOM generation for Smartstore
echo [SBOM] Solution root:      %SCRIPT_DIR%
echo [SBOM] Solution / main:    %MAIN_PROJECT%
echo [SBOM] Frontend libs:      %FRONTEND_LIB_DIR%
echo [SBOM] Output directory:   %OUTPUT_DIR%
echo [SBOM] Edition / Version:  %APP_EDITION% / %APP_VERSION%
echo.

REM ==== BASIC CHECKS =========================================
if not exist "%MAIN_PROJECT%" (
    echo [ERROR] Solution/Main "%MAIN_PROJECT%" was not found.
    goto :Fail
)

if not exist "%FRONTEND_LIB_DIR%" (
    echo [ERROR] Frontend lib directory "%FRONTEND_LIB_DIR%" was not found.
    goto :Fail
)

if not exist "%OUTPUT_DIR%" (
    mkdir "%OUTPUT_DIR%" || (
        echo [ERROR] Could not create output directory "%OUTPUT_DIR%".
        goto :Fail
    )
)

REM ============================================================
REM 1) BACKEND SBOM (CycloneDX .NET, NuGet deps)
REM ============================================================
echo.
echo [BACKEND] Ensuring CycloneDX .NET tool is installed/updated...
dotnet tool update --global %CYCLONEDX_DOTNET_TOOL% >nul 2>&1
if errorlevel 1 (
    dotnet tool install --global %CYCLONEDX_DOTNET_TOOL% >nul 2>&1
    if errorlevel 1 (
        echo [ERROR] Failed to install or update CycloneDX .NET tool.
        goto :Fail
    )
)
echo [BACKEND] CycloneDX .NET tool is available.

echo.
echo [BACKEND] Generating backend SBOM (NuGet / .NET dependencies)...
set "CYDX_CMD=dotnet %CYCLONEDX_DOTNET_TOOL% "%MAIN_PROJECT%" --json --recursive --set-type Application --set-name "Smartstore %APP_EDITION%" --set-version "%APP_VERSION%" --output "%OUTPUT_DIR%" --filename "%BACKEND_SBOM_NAME%""

echo [BACKEND] Running:
echo           %CYDX_CMD%
echo.

call %CYDX_CMD%
set "EXITCODE=%ERRORLEVEL%"
if not "%EXITCODE%"=="0" (
    echo [ERROR] CycloneDX .NET exited with code %EXITCODE%.
    goto :Fail
)

if not exist "%BACKEND_SBOM%" (
    echo [ERROR] Backend SBOM was not created at "%BACKEND_SBOM%".
    goto :Fail
)

echo [BACKEND] Backend SBOM created:
echo           %BACKEND_SBOM%

REM ------------------------------------------------------------
REM FRONTEND: generate SBOM by scanning files (robust implementation)
REM  - run syft dir: scan into raw file
REM  - detect presence of "components" by text search (findstr)
REM  - if present -> prettify with PowerShell
REM  - if not present -> fallback generator (PowerShell) as before
REM ------------------------------------------------------------

echo.
echo [FRONTEND] Generating frontend SBOM (file-based) from:
echo           %FRONTEND_LIB_DIR%

REM create temporary raw file path
set "FRONTEND_SBOM_RAW=%FRONTEND_SBOM%.raw"

"%SYFT_EXE%" dir:"%FRONTEND_LIB_DIR%" ^
  --override-default-catalogers "file" ^
  --exclude "**/*.map" ^
  -o cyclonedx-json="%FRONTEND_SBOM_RAW%"

set "EXITCODE=%ERRORLEVEL%"
if not "%EXITCODE%"=="0" (
    echo [WARN] Syft exited with code %EXITCODE% for frontend SBOM (continuing to check output)...
)

REM Quick textual check: look for the "components" token in the raw JSON.
REM This avoids running complex inline PowerShell that may break on quoting.
findstr /C:"\"components\"" "%FRONTEND_SBOM_RAW%" >nul 2>&1
if errorlevel 1 (
    REM no "components" token found -> fallback generator
    echo [FRONTEND] Syft did not detect frontend components (text check). Running fallback generator...
    powershell -NoProfile -Command ^
      "$libPath = '%FRONTEND_LIB_DIR%';"^
      "if (-not (Test-Path -Path $libPath)) { Write-Error 'frontend lib path not found'; exit 1 };"^
      "$dirs = Get-ChildItem -Path $libPath -Directory -Recurse | Sort-Object FullName;"^
      "$components = @(); foreach ($d in $dirs) { $files = Get-ChildItem -Path $d.FullName -File -Recurse -ErrorAction SilentlyContinue; if ($files.Count -eq 0) { continue } ; $comp = [PSCustomObject]@{ type='library'; name=$d.Name; version=$null; description = \"path=$($d.FullName); files=$($files.Count)\" }; $components += $comp };"^
      "$sbom = [PSCustomObject]@{ bomFormat='CycloneDX'; specVersion='1.6'; version=1; metadata = [PSCustomObject]@{ timestamp = (Get-Date).ToString('o'); tools = @([PSCustomObject]@{ name='fallback-frontend-scan'; version='1' }) }; components = $components };"^
      "$sbom | ConvertTo-Json -Depth 10 | Set-Content -Path '%FRONTEND_SBOM%'; exit 0"
    if errorlevel 1 (
        echo [ERROR] Frontend fallback SBOM generation failed.
        goto :Fail
    )
    echo [FRONTEND] Frontend fallback SBOM created: %FRONTEND_SBOM%
    if exist "%FRONTEND_SBOM_RAW%" del "%FRONTEND_SBOM_RAW%" >nul 2>&1
) else (
    REM found "components" token -> try to prettify JSON into final file
    echo [FRONTEND] Syft produced frontend components, prettifying...
    powershell -NoProfile -Command ^
      "try { Get-Content -Path '%FRONTEND_SBOM_RAW%' -Raw | ConvertFrom-Json -ErrorAction Stop | ConvertTo-Json -Depth 100 | Set-Content -Path '%FRONTEND_SBOM%'; exit 0 } catch { exit 1 }"
    if errorlevel 1 (
        echo [WARN] Prettify failed — keeping raw frontend SBOM as final file.
        copy /Y "%FRONTEND_SBOM_RAW%" "%FRONTEND_SBOM%" >nul 2>&1
    ) else (
        del "%FRONTEND_SBOM_RAW%" >nul 2>&1
    )
)

if not exist "%FRONTEND_SBOM%" (
    echo [ERROR] Frontend SBOM was not created at "%FRONTEND_SBOM%".
    goto :Fail
)

echo [FRONTEND] Frontend SBOM ready:
echo           %FRONTEND_SBOM%


REM --------------------------------------------
REM Use a directory scan so subfolders like "vue/vue.js" are captured.
REM Use file cataloger so components are file-based (no package.json needed).
REM Exclude source maps (*.map) which are not needed.
REM --------------------------------------------
echo.
echo [FRONTEND] Generating frontend SBOM (file-based) from:
echo           %FRONTEND_LIB_DIR%

REM Create a temporary raw output
set "FRONTEND_SBOM_RAW=%FRONTEND_SBOM%.raw"

"%SYFT_EXE%" dir:"%FRONTEND_LIB_DIR%" ^
  --override-default-catalogers "file" ^
  --exclude "**/*.map" ^
  -o cyclonedx-json="%FRONTEND_SBOM_RAW%"

set "EXITCODE=%ERRORLEVEL%"
if not "%EXITCODE%"=="0" (
    echo [WARN] Syft exited with code %EXITCODE% for frontend SBOM (continuing to fallback check)...
)

REM Check if Syft produced any components
powershell -NoProfile -Command ^
  "try { $j = Get-Content -Raw -Path '%FRONTEND_SBOM_RAW%' | ConvertFrom-Json; if ($null -eq $j.components -or $j.components.Count -eq 0) { exit 2 } else { exit 0 } } catch { exit 2 }"

set "PSRC=%ERRORLEVEL%"

if "%PSRC%"=="0" (
    rem Syft produced components - prettify into final file
    echo [FRONTEND] Syft produced frontend components, prettifying...
    powershell -Command ^
      "Get-Content -Path '%FRONTEND_SBOM_RAW%' -Raw | ConvertFrom-Json | ConvertTo-Json -Depth 100 | Set-Content -Path '%FRONTEND_SBOM%'" || (
        echo [ERROR] Failed to prettify frontend SBOM.
        goto :Fail
    )
    del "%FRONTEND_SBOM_RAW%" >nul 2>&1
) else (
    rem Syft found nothing -> fallback generator
    echo [FRONTEND] Syft did not detect frontend components. Running fallback generator...
    powershell -NoProfile -Command ^
      "$libPath = '%FRONTEND_LIB_DIR%';"^
      "if (-not (Test-Path -Path $libPath)) { Write-Error 'frontend lib path not found'; exit 1 };"^
      "$dirs = Get-ChildItem -Path $libPath -Directory -Recurse | Sort-Object FullName;"^
      "$components = @(); foreach ($d in $dirs) { $files = Get-ChildItem -Path $d.FullName -File -Recurse -ErrorAction SilentlyContinue; if ($files.Count -eq 0) { continue } ; $comp = [PSCustomObject]@{ type='library'; name=$d.Name; version=$null; description = \"path=$($d.FullName); files=$($files.Count)\" }; $components += $comp };"^
      "$sbom = [PSCustomObject]@{ bomFormat='CycloneDX'; specVersion='1.6'; version=1; metadata = [PSCustomObject]@{ timestamp = (Get-Date).ToString('o'); tools = @([PSCustomObject]@{ name='fallback-frontend-scan'; version='1' }) }; components = $components };"^
      "$sbom | ConvertTo-Json -Depth 10 | Set-Content -Path '%FRONTEND_SBOM%'; exit 0"

    if errorlevel 1 (
        echo [ERROR] Frontend fallback SBOM generation failed.
        goto :Fail
    )
    echo [FRONTEND] Frontend fallback SBOM created: %FRONTEND_SBOM%
    if exist "%FRONTEND_SBOM_RAW%" del "%FRONTEND_SBOM_RAW%" >nul 2>&1
)

if not exist "%FRONTEND_SBOM%" (
    echo [ERROR] Frontend SBOM was not created at "%FRONTEND_SBOM%".
    goto :Fail
)

echo [FRONTEND] Frontend SBOM ready:
echo           %FRONTEND_SBOM%

REM ============================================================
REM 3) MERGE BACKEND + FRONTEND (CycloneDX CLI)
REM ============================================================
echo.
echo [MERGE] Ensuring CycloneDX CLI (cyclonedx) is available...

cyclonedx --version >nul 2>&1
if errorlevel 1 (
    echo [MERGE] CycloneDX CLI not found. Installing via npm...
    npm install -g @cyclonedx/cyclonedx-cli >nul 2>&1
)

cyclonedx --version >nul 2>&1
if errorlevel 1 (
    echo [ERROR] CycloneDX CLI is not available even after npm install.
    echo [ERROR] Please ensure 'cyclonedx' is on PATH (npm global bin).
    goto :Fail
)

echo [MERGE] CycloneDX CLI is available.

echo.
echo [MERGE] Merging backend + frontend SBOMs...
echo [MERGE] Backend:  %BACKEND_SBOM%
echo [MERGE] Frontend: %FRONTEND_SBOM%
echo [MERGE] Output:   %COMBINED_SBOM%
echo.

cyclonedx merge ^
  --input "%BACKEND_SBOM%" ^
  --input "%FRONTEND_SBOM%" ^
  --output "%COMBINED_SBOM%" ^
  --output-format json

if errorlevel 1 (
    echo [ERROR] SBOM merge failed.
    goto :Fail
)

echo.
echo [OK] Combined SBOM successfully created:
echo      %COMBINED_SBOM%
echo.
echo [OK] Backend SBOM:   %BACKEND_SBOM%
echo [OK] Frontend SBOM:  %FRONTEND_SBOM%
echo.

goto :Success

:Fail
echo.
echo [SBOM] Full SBOM generation FAILED.
endlocal
exit /b 1

:Success
echo [SBOM] Full SBOM generation COMPLETED.
endlocal
exit /b 0
