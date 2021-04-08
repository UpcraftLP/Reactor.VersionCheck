# https://hub.docker.com/_/microsoft-dotnet

# build stage:
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /source

COPY *.csproj .
RUN dotnet restore

# copy app files
COPY . .

RUN dotnet publish -c Release -o /app --self-contained false --no-restore

# runtime stage:
FROM mcr.microsoft.com/dotnet/runtime:5.0
ENV WORKING_DIRECTORY="/app"
WORKDIR /app
COPY --from=build /app .

ENV COMPlus_EnableDiagnostics=0

ENTRYPOINT dotnet Reactor.VersionCheck.dll