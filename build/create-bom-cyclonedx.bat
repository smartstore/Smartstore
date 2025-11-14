@echo off
setlocal enabledelayedexpansion

REM ============================================================
REM SBOM generation for Smartstore build artifacts using Syft
REM Format: CycloneDX JSON
REM Output: sbom-cyclonedx.json in the artifacts directory
REM Requirements:
REM   - Windows
REM   - Internet access on first run (for Syft download)
REM ============================================================

REM Base directory of this script (with trailing backslash)
set "SCRIPT_DIR=%~dp0"

REM ==== CONFIGURATION (ADJUST AS NEEDED) ======================
REM Application metadata
set "APP_VERSION=6.2.0"
set "APP_EDITION=Community"
set "PLATFORM=win-x64"

REM Artifacts directory relative to build directory
REM Resulting path: artifacts\{APP_EDITION}.{APP_VERSION}.{PLATFORM}
set "ARTIFACTS_DIR_REL=artifacts\%APP_EDITION%.%APP_VERSION%.%PLATFORM%"

REM Compose absolute scan path
set "SCAN_DIR=%SCRIPT_DIR%%ARTIFACTS_DIR_REL%"

REM SBOM output file name (relative to SCAN_DIR)
set "SBOM_FILE=sbom-cyclonedx.json"

REM Syft version (adjust if needed) (without leading "v")
set "SYFT_VERSION=1.37.0"

REM Tools directory and Syft path (absolute)
set "TOOLS_DIR=%SCRIPT_DIR%.tools\syft"
set "SYFT_EXE=%TOOLS_DIR%\syft.exe"

REM Build Syft download URL (with leading "v" here)
set "SYFT_URL=https://github.com/anchore/syft/releases/download/v%SYFT_VERSION%/syft_%SYFT_VERSION%_windows_amd64.zip"
REM ============================================================

echo.
echo [SBOM] Creating CycloneDX SBOM for Smartstore build artifacts...
echo [SBOM] Script directory: %SCRIPT_DIR%
echo [SBOM] Artifacts directory (absolute): %SCAN_DIR%
echo [SBOM] Edition: %APP_EDITION%
echo [SBOM] Version: %APP_VERSION%
echo [SBOM] Platform: %PLATFORM%
echo [SBOM] Output file: %SBOM_FILE%
echo.

REM ==== Check if scan directory exists ========================
if not exist "%SCAN_DIR%" (
    echo [ERROR] Artifacts directory "%SCAN_DIR%" was not found.
    echo Please adjust APP_VERSION, APP_EDITION or PLATFORM in this script.
    goto :Fail
)

REM ==== Ensure Syft is available ==============================
if exist "%SYFT_EXE%" (
    echo [OK] Syft already present: %SYFT_EXE%
) else (
    echo [INFO] Syft not found. Downloading Syft...

    REM Create tools directory
    if not exist "%TOOLS_DIR%" (
        mkdir "%TOOLS_DIR%" || (
            echo [ERROR] Could not create tools directory "%TOOLS_DIR%".
            goto :Fail
        )
    )

    echo [INFO] Downloading Syft from:
    echo        %SYFT_URL%
    echo.

    REM Download with PowerShell
    powershell -Command "Invoke-WebRequest -Uri '%SYFT_URL%' -OutFile '%TOOLS_DIR%\syft.zip'" || (
        echo [ERROR] Download of Syft failed.
        goto :Fail
    )

    REM Extract ZIP
    powershell -Command "Expand-Archive -Path '%TOOLS_DIR%\syft.zip' -DestinationPath '%TOOLS_DIR%' -Force" || (
        echo [ERROR] Extracting Syft failed.
        goto :Fail
    )

    del "%TOOLS_DIR%\syft.zip" >nul 2>&1

    REM If syft.exe is not directly in the tools folder, try to locate it recursively
    if not exist "%SYFT_EXE%" (
        for /r "%TOOLS_DIR%" %%F in (syft.exe) do (
            copy "%%F" "%SYFT_EXE%" >nul
            goto FoundSyft
        )

        echo [ERROR] syft.exe was not found after extraction.
        goto :Fail

        :FoundSyft
        echo [OK] Syft was successfully downloaded and prepared.
    ) else (
        echo [OK] Syft was successfully downloaded and prepared.
    )
)

REM Final check that syft.exe really exists
if not exist "%SYFT_EXE%" (
    echo [ERROR] Syft executable not found at "%SYFT_EXE%".
    goto :Fail
)

REM ==== Generate CycloneDX SBOM ===============================
echo.
echo [INFO] Generating CycloneDX SBOM with Syft from build artifacts...
pushd "%SCAN_DIR%" || (
    echo [ERROR] Could not change directory to "%SCAN_DIR%".
    goto :Fail
)

REM 1) Generate raw CycloneDX SBOM (compact JSON) with exclusions
REM    - Exclude .NET runtime folders (runtimes, refs)
REM    - Exclude Microsoft.*.dll and System.*.dll
REM    - Exclude additional framework-related patterns
"%SYFT_EXE%" dir:. ^
  --exclude "./runtimes/**" ^
  --exclude "./refs/**" ^
  --exclude "**/Microsoft.*.dll" ^
  --exclude "**/System.*.dll" ^
  --exclude "**/*.resources.dll" ^
  --exclude "**/aspnet*" ^
  --exclude "**/clr*.dll" ^
  --exclude "**/coreclr.dll" ^
  --exclude "**/host*.dll" ^
  --exclude "**/mscor*.dll" ^
  --exclude "**/netstandard.dll" ^
  --exclude "**/Windows*.dll" ^
  -o cyclonedx-json=%SBOM_FILE%

set "EXITCODE=%ERRORLEVEL%"

if not "%EXITCODE%"=="0" (
    popd
    echo [ERROR] Syft exited with code %EXITCODE%.
    goto :Fail
)

REM 2) Prettify JSON using PowerShell (in-place)
echo [INFO] Prettifying CycloneDX JSON SBOM...
powershell -Command ^
  "$p='%SBOM_FILE%';"^
  "Get-Content -Path $p -Raw | ConvertFrom-Json | ConvertTo-Json -Depth 100 | Set-Content -Path $p"

if errorlevel 1 (
    popd
    echo [ERROR] Failed to prettify CycloneDX JSON SBOM.
    goto :Fail
)

popd

echo.
echo [OK] CycloneDX SBOM successfully created and prettified:
echo      %SCAN_DIR%\%SBOM_FILE%
echo.

goto :Success

:Fail
echo.
echo [SBOM] Failed.
endlocal
pause
exit /b 1

:Success
endlocal
pause
exit /b 0
