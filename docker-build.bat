@echo off

docker build -t dotnetapp .
docker run "--volume=%CD%/.env:/app/.env:ro" --rm -it dotnetapp
