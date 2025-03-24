git pull
docker rm educhemlp -f
docker build -t educhemlp .
docker run -d --name educhemlp -p 8120:80 --restart always educhemlp