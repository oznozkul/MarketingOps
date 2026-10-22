# MarketingOps — çalışma kuralları

Bu dosya her oturumda okunur. Kaynak: `marketingops-backend-promptlari.md` (Prompt 0) ve `marketingops-ekran-promptlari.md`.

## Ürün özeti
E-ticaret siteleri için salt okunur bir pazarlama denetim platformu. Siteleri tarar; Search Console, GA4, GTM, Google Ads, Meta ve sitenin sipariş veritabanından veri okur. Teknik SEO, performans ve tracking sorunlarını bulgu (Finding) olarak çıkarır, görev (WorkTask) olarak ekibe açar ve sonraki taramada düzelip düzelmediğini doğrular. Sistem siteye veya reklam hesaplarına ASLA yazmaz. Çok siteli ve generic: Organization > Site. Hiçbir şey belirli bir siteye göre hardcode edilmez.

## Teknoloji
- .NET 10, C#, nullable açık, implicit usings açık. SDK `global.json` ile sabit (10.0.401). PATH'teki `dotnet` .NET 10 değilse `~/.dotnet/dotnet` kullanın.
- ASP.NET Core MVC. Tek web uygulaması: MVC ekranları ve `/api` altındaki JSON endpoint'leri aynı host'ta. Ayrı API projesi YOK.
- MediatR 12.5.0 (13 ve sonrası ticari lisans gerektirir, yükseltmeyin).
- FluentValidation (DI extensions ile).
- EF Core + Npgsql, EFCore.NamingConventions ile snake_case tablo ve kolon adları.
- PostgreSQL 17, Docker Compose.
- Test: xUnit, Shouldly (FluentAssertions değil), Testcontainers.PostgreSql ile entegrasyon testleri.
- Central Package Management (`Directory.Packages.props`, tüm sürümler sabit) ve `Directory.Build.props`.

## Proje yapısı
```
src/
  MarketingOps.Domain          Entity, value object, enum, domain exception. Hiçbir pakete bağımlı değil.
  MarketingOps.Application     MediatR command/query, handler, validator, DTO, arayüzler (IAppDbContext vb.)
  MarketingOps.Infrastructure  EF Core DbContext, konfigürasyonlar, migration'lar, seed, dış servis implementasyonları
  MarketingOps.Web             MVC + API controller'ları, Program.cs, hata yönetimi, Razor ekranları
tests/
  MarketingOps.UnitTests         Domain ve handler/validator testleri
  MarketingOps.IntegrationTests  Testcontainers ile gerçek Postgres üzerinde testler
```
Bağımlılık yönü: Web → Application + Infrastructure, Infrastructure → Application, Application → Domain.

## DOSYA KURALI (çok önemli)
Her tip kendi `.cs` dosyasındadır. Bir dosyada birden fazla class/record/enum/interface OLMAZ. Bu kural Web dahil tüm repo için geçerlidir.
- Command, Query, Handler, Validator, Response/DTO, Enum, Exception, EF konfigürasyonu, Controller, ViewModel, TagHelper ayrı dosyalardır.
- Dosya adı tip adıyla birebir aynıdır.

Feature (dikey dilim) klasörleri:
```
Application/Sites/Commands/CreateSite/CreateSiteCommand.cs, ...Handler.cs, ...Validator.cs
Application/Sites/Queries/GetSiteById/GetSiteByIdQuery.cs, ...Handler.cs, SiteDetailResponse.cs
Domain/Sites/Site.cs, PageType.cs, CrawlSettings.cs, MatchCondition.cs, MatchConditionType.cs, MatchMode.cs
```

## Over-design YASAK
- Generic repository ve Unit of Work yok. Application, `IAppDbContext` üzerinden DbSet'leri doğrudan kullanır; `SaveChangesAsync` de oradan çağrılır.
- AutoMapper vb. yok. Map'leme sorgularda LINQ `Select` ile ya da elle yazılır.
- Specification pattern, `Result<T>`, her şey için interface yok.
- Domain event / outbox altyapısı, ihtiyaç doğan fazın promptunda açıkça istenene kadar yok.
- Kullanılmayan base class, extension veya "ileride lazım olur" kodu yok.
- Strongly-typed ID yok. `Guid` kullanılır (`Guid.CreateVersion7()`).
- Soyutlama ancak en az iki gerçek implementasyon varsa (ör. `IOrderSource`) ya da dış sistem test için ayrılacaksa (ör. `ICrawlerHttpClient`) açılır.

## Okunabilirlik
- Kısa metotlar, açıklayıcı isimler. Yorumlar NEDEN'i anlatır, NE'yi değil.
- Domain dili kodda aynen: Site, PageType, Crawl, Finding, WorkTask, Rule.
- Kod İngilizce. Kullanıcıya dönen hata mesajları Türkçe.
- Primary constructor kullanılabilir. Domain entity'lerinde public setter yok; değişiklik metotlarla (`site.UpdateCrawlSettings(...)`).
- Magic string/number yok; ama tek yerde kullanılan değer için ayrı sabit sınıfı da açılmaz.

## DDD
- Aggregate root kendi tutarlılığını korur. Child entity'ler yalnızca root üzerinden değişir (`site.AddPageType(...)`).
- Geçersiz durum oluşturulamaz: factory (`Site.Create`) ve davranış metotları kuralları kontrol eder, ihlalde `DomainException`.
- Aggregate'ler arası referans yalnızca ID ile.
- Value object'ler `record`.

## CQRS
- Command: durum değiştirir. Aggregate'i yükler, domain metodunu çağırır, `SaveChanges`. Yalnızca ID ya da minimal sonuç döner.
- Query: durum değiştirmez. `AsNoTracking` + `Select` projection ile doğrudan Response DTO. Aggregate yüklemez, `Include` kullanmaz. Listeler sayfalıdır (`PagedResponse<T>`).
- Response/DTO'lar `sealed record`.
- Girdi doğrulaması FluentValidation validator'ında; `ValidationBehavior` hepsini çalıştırır. Domain kuralları domain'de; DB gerektiren benzersizlik kontrolleri handler'da.

## Hata yönetimi
- `DomainException` (400), FluentValidation `ValidationException` (400, alan bazlı), `NotFoundException` (404), `ConflictException` (409). Her biri ayrı dosyada.
- Web'de tek bir `IExceptionHandler` (`GlobalExceptionHandler`) bunları RFC 7807 ProblemDetails'e çevirir (`/api` istekleri). Beklenmeyen hatalar 500, loglanır, detay sızmaz. MVC sayfalarında `/error` sayfası gösterilir.

## Zaman ve kimlik
- Tüm zamanlar `DateTimeOffset`, UTC. Şimdiki zaman `TimeProvider`'dan.
- Kimlik doğrulama henüz yok. Tek varsayılan Organization seed edilir.

## Arayüz (ekran promptları)
- Razor + Tailwind (standalone CLI, MSBuild ile derlenir) + htmx + Alpine.js + Chart.js. SPA yok. SignalR canlı ilerleme için.
- Tasarım `ui-ux-pro-max` skill'iyle yapılır ve `design-system/MASTER.md`'ye uyar: Swiss Modernism 2.0 + Data-Dense Dashboard, IBM Plex Sans/Mono, mürekkep birincil renk, tek mavi vurgu. Jenerik "AI dashboard" görünümü (mor/indigo, gradient, gölgeli yuvarlak kartlar) yok.
- MVC controller'ları yalnızca MediatR'a query/command gönderir. Ekranın ihtiyacı olan sorgu yoksa Application'a kurallara uygun yeni query eklenir.
- **Her backend fazının command/query'leri aynı fazda ekranlara bağlanır.** Yalnızca `/api` yetmez; fazın açtığı ekranlar gerçek formlar ve listelerle çalışır, tarayıcıda akış olarak doğrulanır. Faz planları bu UI işini içerir.
- Form akışı: htmx ile form kendi bölgesini yeniler (`hx-target="this" hx-swap="outerHTML"`). Controller `TrySendAsync` ile gönderir; doğrulama hataları alan adına, domain/çakışma hataları forma (`<form-errors />`) yazılır. Alan hatası: `<field-error for="Alan" />` + girdide `aria-invalid`/`aria-describedby`. Başarıda `Response.Toast(...)` ya da yönlendirme gerekiyorsa `HtmxRedirect(url, flashMesajı)`.
- View model'ler `Web/ViewModels` altında, her biri ayrı dosyada. Application Response'ları doğrudan view'a verilebiliyorsa ayrı view model açılmaz.
- Henüz backend'i yazılmamış fazların ekranları için seed verisi Infrastructure Development seed'ine eklenir. Ayrı mock katmanı açılmaz.
- Seçili site URL'de: `/sites/{siteId}/...`. Liste ekranlarında filtre/sıralama/sayfalama sunucu tarafında, query string'de; htmx yalnızca tablo partial'ını yeniler (`hx-push-url`).
- Salt okunur ilke: "Uygula", "Düzelt", "Yayınla" gibi siteyi değiştiriyormuş izlenimi veren buton/metin yok.
- Arayüz Türkçe, tr-TR biçim (1.206, %38, 21 Eki). Enum → Türkçe metin tek kaynaktan: `Web/Infrastructure/DisplayText.cs`.
- Enum tipli Tag Helper özniteliklerinde değer `@` ile verilir: `<severity-badge value="@f.Severity" />`.

## Komutlar
- Build: `dotnet build` · Test: `dotnet test` (entegrasyon testleri için Docker çalışıyor olmalı)
- Çalıştır: `docker compose up --build` ya da `dotnet run --project src/MarketingOps.Web` (yerel Postgres ile)
- Yapılandırma yalnızca `src/MarketingOps.Web/appsettings.json`'dadır (Worker aynı dosyayı kullanır). `.env`, `.env.example`, `appsettings.{Ortam}.json` açılmaz; docker-compose ayar taşımaz.
- Migration: `dotnet ef migrations add <Ad> -p src/MarketingOps.Infrastructure -s src/MarketingOps.Web -o Persistence/Migrations`
- Razor derleyici garip konum hataları verirse (ör. `'scor' adı yok`): `dotnet build-server shutdown` ve `bin/obj` temizliği.
