FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build

WORKDIR /src

COPY SqliteArchive/SqliteArchive.csproj SqliteArchive/
COPY SqliteArchive.Ftp/SqliteArchive.Ftp.csproj SqliteArchive.Ftp/
COPY SqliteArchive.Server/SqliteArchive.Server.csproj SqliteArchive.Server/

RUN dotnet restore SqliteArchive.Server/SqliteArchive.Server.csproj

COPY . .

RUN dotnet publish SqliteArchive.Server/SqliteArchive.Server.csproj \
    --no-restore -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine

RUN apk add --no-cache tzdata icu-libs

USER app
WORKDIR /srv
EXPOSE 80 21 10000-10009

ENV ASPNETCORE_CONTENTROOT=/app
ENV ASPNETCORE_HTTP_PORTS=80
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV LANG=en_US

COPY --from=build /app/publish /app

ENTRYPOINT ["dotnet", "/app/SqliteArchive.Server.dll"]

HEALTHCHECK --start-period=1m --start-interval=1s \
  CMD wget --no-verbose --tries=1 --spider http://localhost || exit 1
