cd ..
docker build -t ghcr.io/smartstore/smartstore-linux -f NoBuild.Dockerfile .
echo 'Press enter to exit...'; read dummy;