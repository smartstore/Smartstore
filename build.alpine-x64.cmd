REM Powershell -NoProfile -ExecutionPolicy Bypass -File "build.ps1"
dotnet cake --target=Deploy --runtime=alpine-x64
dotnet cake --target=Zip --runtime=alpine-x64
pause