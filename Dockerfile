FROM mcr.microsoft.com/dotnet/sdk:10.0.401 AS build
WORKDIR /src
COPY global.json ./
COPY PongChampions.Api/PongChampions.Api.csproj PongChampions.Api/
COPY PongChampions.Data/PongChampions.Data.csproj PongChampions.Data/
RUN dotnet restore PongChampions.Api/PongChampions.Api.csproj
COPY PongChampions.Api/ PongChampions.Api/
COPY PongChampions.Data/ PongChampions.Data/
RUN dotnet publish PongChampions.Api/PongChampions.Api.csproj -c Release --no-restore -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0.13 AS final
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENTRYPOINT ["dotnet", "PongChampions.Api.dll"]
