@echo off

docker build -t dotnetapp .
docker run "--volume=%CD%/run/config:/app/config" --rm -it dotnetapp
