REM Powershell -NoProfile -ExecutionPolicy Bypass -File "build.ps1"
dotnet cake --target=Deploy --runtime=win-x64
dotnet cake --target=Zip --runtime=win-x64
pause