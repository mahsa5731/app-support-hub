FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["global.json", "Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["src/AppSupportHub.Domain/AppSupportHub.Domain.csproj", "src/AppSupportHub.Domain/"]
COPY ["src/AppSupportHub.Application/AppSupportHub.Application.csproj", "src/AppSupportHub.Application/"]
COPY ["src/AppSupportHub.Infrastructure/AppSupportHub.Infrastructure.csproj", "src/AppSupportHub.Infrastructure/"]
COPY ["src/AppSupportHub.Web/AppSupportHub.Web.csproj", "src/AppSupportHub.Web/"]
RUN dotnet restore "src/AppSupportHub.Web/AppSupportHub.Web.csproj" --property:Configuration=Release

COPY src/ src/
RUN dotnet publish "src/AppSupportHub.Web/AppSupportHub.Web.csproj" \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .

USER $APP_UID
ENTRYPOINT ["dotnet", "AppSupportHub.Web.dll"]
