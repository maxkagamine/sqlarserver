FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build

WORKDIR /src

COPY SqlarServer/SqlarServer.csproj SqlarServer/
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
  dotnet restore SqlarServer/SqlarServer.csproj

COPY . .
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
  dotnet publish SqlarServer/SqlarServer.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine

RUN apk add --no-cache tzdata

USER app
WORKDIR /srv
EXPOSE 80

ENV ASPNETCORE_CONTENTROOT=/app
ENV ASPNETCORE_HTTP_PORTS=80

COPY --from=build /app/publish /app

ENTRYPOINT ["dotnet", "/app/SqlarServer.dll"]
