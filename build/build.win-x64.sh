cd ..
dotnet cake --target=Deploy --runtime=win-x64
dotnet cake --target=Zip --runtime=win-x64
echo 'Press enter to exit...'; read dummy;