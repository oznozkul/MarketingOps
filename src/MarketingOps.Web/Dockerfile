# Build context: repo kökü (docker-compose.yml bunu ayarlar).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# global.json kopyalanmaz: imajdaki SDK yaması yereldekinden farklı olabilir.
COPY Directory.Build.props Directory.Packages.props ./
COPY src/MarketingOps.Domain/MarketingOps.Domain.csproj src/MarketingOps.Domain/
COPY src/MarketingOps.Application/MarketingOps.Application.csproj src/MarketingOps.Application/
COPY src/MarketingOps.Infrastructure/MarketingOps.Infrastructure.csproj src/MarketingOps.Infrastructure/
COPY src/MarketingOps.Web/MarketingOps.Web.csproj src/MarketingOps.Web/
RUN dotnet restore src/MarketingOps.Web/MarketingOps.Web.csproj

COPY tools/tailwind/get-tailwind.sh tools/tailwind/
COPY src/ src/
# Tailwind CLI, MSBuild hedefi tarafından konteynerin mimarisine uygun olarak indirilir.
RUN dotnet publish src/MarketingOps.Web/MarketingOps.Web.csproj -c Release -o /app/publish --no-restore -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
# Data Protection anahtarları için volume bağlanan klasör; uygulama kullanıcısı yazabilmeli.
RUN mkdir -p /app/keys && chown $APP_UID /app/keys
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "MarketingOps.Web.dll"]
