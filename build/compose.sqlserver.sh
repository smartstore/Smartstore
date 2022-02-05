cd ..
docker-compose -f docker-compose.yml -f docker-compose.sqlserver.yml up -d
echo 'Press enter to exit...'; read dummy;