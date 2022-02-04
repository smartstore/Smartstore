cd ..
dotnet cake --target=Deploy --runtime=linux-x64
dotnet cake --target=Zip --runtime=linux-x64
echo 'Press enter to exit...'; read dummy;