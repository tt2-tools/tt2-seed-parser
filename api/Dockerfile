FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY seedapi.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
RUN mkdir -p /tokens && chown $APP_UID:$APP_UID /tokens
USER $APP_UID:2000
ENTRYPOINT ["dotnet", "seedapi.dll"]
