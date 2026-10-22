# Build context: repo kökü (docker-compose.yml bunu ayarlar).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./
COPY src/MarketingOps.Domain/MarketingOps.Domain.csproj src/MarketingOps.Domain/
COPY src/MarketingOps.Application/MarketingOps.Application.csproj src/MarketingOps.Application/
COPY src/MarketingOps.Infrastructure/MarketingOps.Infrastructure.csproj src/MarketingOps.Infrastructure/
COPY src/MarketingOps.Worker/MarketingOps.Worker.csproj src/MarketingOps.Worker/
RUN dotnet restore src/MarketingOps.Worker/MarketingOps.Worker.csproj

COPY src/ src/
RUN dotnet publish src/MarketingOps.Worker/MarketingOps.Worker.csproj -c Release -o /app/publish --no-restore -p:UseAppHost=false

# Infrastructure, ASP.NET Core paylaşılan çerçevesine (Hangfire, Data Protection) bağlı; bu yüzden aspnet imajı.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "MarketingOps.Worker.dll"]
