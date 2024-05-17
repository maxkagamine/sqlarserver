FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build

WORKDIR /src

COPY SqliteArchive.Server/SqliteArchive.Server.csproj SqliteArchive.Server/
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
  dotnet restore SqliteArchive.Server/SqliteArchive.Server.csproj

COPY . .
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
  dotnet publish SqliteArchive.Server/SqliteArchive.Server.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine

RUN apk add --no-cache tzdata

USER app
WORKDIR /srv
EXPOSE 80

ENV ASPNETCORE_CONTENTROOT=/app
ENV ASPNETCORE_HTTP_PORTS=80

COPY --from=build /app/publish /app

ENTRYPOINT ["dotnet", "/app/SqliteArchive.Server.dll"]
