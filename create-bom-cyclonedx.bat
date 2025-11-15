@echo off
setlocal enabledelayedexpansion

REM ============================================================
REM SBOM generation for Smartstore using CycloneDX .NET tool
REM Scope: NuGet / 3rd party dependencies of the whole solution
REM Format: CycloneDX JSON
REM Output: sbom-cyclonedx.Community.6.2.0.json in the solution root
REM ============================================================

set "SCRIPT_DIR=%~dp0"

REM Application metadata
set "APP_VERSION=6.2.0"
set "APP_EDITION=Community"

REM IMPORTANT: use solution instead of single project
set "MAIN_PROJECT_REL=Smartstore.Full-sym.sln"
set "MAIN_PROJECT=%SCRIPT_DIR%%MAIN_PROJECT_REL%"

set "OUTPUT_DIR=%SCRIPT_DIR%sbom"
set "OUTPUT_FILE_NAME=sbom-cyclonedx.%APP_EDITION%.%APP_VERSION%.json"
set "SBOM_FILE=%OUTPUT_DIR%\%OUTPUT_FILE_NAME%"

set "BASE_INTERMEDIATE_REL="
set "CYCLONEDX_TOOL=CycloneDX"

echo.
echo [SBOM] Creating CycloneDX SBOM via CycloneDX .NET tool (solution-wide)...
echo [SBOM] Solution / main:     %MAIN_PROJECT%
echo [SBOM] Output:              %SBOM_FILE%
echo.

if not exist "%MAIN_PROJECT%" (
    echo [ERROR] Main project/solution "%MAIN_PROJECT%" was not found.
    goto :Fail
)

if not exist "%OUTPUT_DIR%" (
    mkdir "%OUTPUT_DIR%" || (
        echo [ERROR] Could not create output directory "%OUTPUT_DIR%".
        goto :Fail
    )
)

echo [INFO] Ensuring CycloneDX .NET tool is installed/updated...
dotnet tool update --global %CYCLONEDX_TOOL% >nul 2>&1
if errorlevel 1 (
    dotnet tool install --global %CYCLONEDX_TOOL% >nul 2>&1
    if errorlevel 1 (
        echo [ERROR] Failed to install or update CycloneDX .NET tool.
        goto :Fail
    )
)
echo [OK] CycloneDX .NET tool is available.

echo.
echo [INFO] Generating CycloneDX SBOM (solution-wide NuGet dependencies)...

set "CYCLONEDX_CMD=dotnet %CYCLONEDX_TOOL% "%MAIN_PROJECT%" --json --recursive --set-type Application --set-name "Smartstore %APP_EDITION%" --set-version "%APP_VERSION%" --output "%OUTPUT_DIR%" --filename "%OUTPUT_FILE_NAME%""

if not "%BASE_INTERMEDIATE_REL%"=="" (
    set "BASE_INTERMEDIATE=%SCRIPT_DIR%%BASE_INTERMEDIATE_REL%"
    set "CYCLONEDX_CMD=%CYCLONEDX_CMD% --base-intermediate-output-path "%BASE_INTERMEDIATE%""
)

echo [INFO] Running:
echo        %CYCLONEDX_CMD%
echo.

call %CYCLONEDX_CMD%
set "EXITCODE=%ERRORLEVEL%"

if not "%EXITCODE%"=="0" (
    echo [ERROR] CycloneDX exited with code %EXITCODE%.
    goto :Fail
)

echo.
echo [OK] CycloneDX SBOM successfully created:
echo      %SBOM_FILE%
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
