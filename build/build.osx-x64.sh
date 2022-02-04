cd ..
dotnet cake --target=Deploy --runtime=osx-x64
dotnet cake --target=Zip --runtime=osx-x64
echo 'Press enter to exit...'; read dummy;