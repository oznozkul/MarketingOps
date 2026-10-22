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
# Playwright Chromium yalnızca worker'da. Tarayıcı dosyaları uygulama kullanıcısının okuyabildiği yolda durur.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
USER root
ENV PLAYWRIGHT_BROWSERS_PATH=/ms-playwright
# Tarayıcı katmanı yalnızca Playwright paket dosyalarına bağlı; uygulama kodu değişince Chromium yeniden inmez.
COPY --from=build /app/publish/.playwright .playwright
RUN arch="$(dpkg --print-architecture)" \
    && case "$arch" in amd64) nodearch=linux-x64 ;; arm64) nodearch=linux-arm64 ;; *) echo "unsupported $arch" && exit 1 ;; esac \
    && chmod +x ".playwright/node/${nodearch}/node" \
    && ".playwright/node/${nodearch}/node" .playwright/package/cli.js install-deps chromium \
    && ".playwright/node/${nodearch}/node" .playwright/package/cli.js install chromium \
    && chown -R "$APP_UID:$APP_UID" /ms-playwright
COPY --from=build /app/publish .
# Tam kopya .playwright'ı yayın çıktısıyla değiştirir; node ikilisi yeniden çalıştırılabilir olmalı.
RUN arch="$(dpkg --print-architecture)" \
    && case "$arch" in amd64) nodearch=linux-x64 ;; arm64) nodearch=linux-arm64 ;; *) echo "unsupported $arch" && exit 1 ;; esac \
    && chmod +x ".playwright/node/${nodearch}/node"
USER $APP_UID
ENTRYPOINT ["dotnet", "MarketingOps.Worker.dll"]
