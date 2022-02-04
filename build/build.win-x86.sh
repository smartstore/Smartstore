cd ..
dotnet cake --target=Deploy --runtime=win-x86
dotnet cake --target=Zip --runtime=win-x86
echo 'Press enter to exit...'; read dummy;