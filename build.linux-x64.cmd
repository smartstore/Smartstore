REM Powershell -NoProfile -ExecutionPolicy Bypass -File "build.ps1"
dotnet cake --target=Deploy --runtime=linux-x64
dotnet cake --target=Zip --runtime=linux-x64
pause