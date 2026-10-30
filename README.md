# MarketingOps

E-ticaret siteleri için **salt okunur** pazarlama denetim platformu. .NET 10, ASP.NET Core MVC (ekranlar + `/api`), DDD + CQRS (MediatR 12.5.0), FluentValidation, EF Core + PostgreSQL 17, Docker. Ekranlar: Razor + Tailwind + htmx + Alpine.js.

Çalışma kuralları: [CLAUDE.md](CLAUDE.md) · Tasarım sistemi: [design-system/MASTER.md](design-system/MASTER.md)

## Gereksinimler
- .NET SDK 10.0.401 (`global.json` ile sabit). PATH'teki `dotnet` .NET 10 değilse `~/.dotnet/dotnet` kullanın.
- Docker (Compose ve entegrasyon testleri için).

## Çalıştırma

### Yapılandırma
Tüm ayarlar tek dosyadadır: `src/MarketingOps.Web/appsettings.json` (`.env` ya da ortama göre ayrı appsettings dosyası yok).
Worker aynı dosyayı kullanır; Docker'da dosya konteynerlere bağlanır, değişiklikten sonra `docker compose restart web worker` yeterlidir.
- `ConnectionStrings:Default` — `Host=postgres,localhost`: Docker içinde `postgres`, yerelde `localhost` kullanılır (Npgsql sırayla dener).
- `Ai` — görev önerileri sağlayıcısı (aşağıda). `DataProtection:KeysPath`, `Knowledge:Path`.

### Docker Compose (önerilen)
```sh
docker compose up --build
```
- Uygulama: http://localhost:8080 (Development: migration'lar ve örnek veri başlangıçta otomatik uygulanır)
- Sağlık: http://localhost:8080/health
- OpenAPI: http://localhost:8080/openapi/v1.json (yalnızca Development)
- Tasarım sistemi vitrini: http://localhost:8080/_design (yalnızca Development)

### Yerel
```sh
docker compose up -d postgres
dotnet run --project src/MarketingOps.Web
```
Ayarlar aynı `appsettings.json`'dan okunur. İlk build Tailwind standalone CLI'yi `tools/tailwind/` altına indirir; Node gerekmez. `dotnet watch` sırasında CSS'i canlı derlemek için:
```sh
cd src/MarketingOps.Web && ../../tools/tailwind/tailwindcss -c tailwind.config.js -i Styles/app.css -o wwwroot/css/app.css --watch
```

## Taramalar (Faz 2)
- **Worker** (`src/MarketingOps.Worker`): Hangfire sunucusu. Taramaları ve zamanlanmış işleri yürütür. Web yalnızca iş ekler.
- **Hangfire dashboard:** http://localhost:8080/jobs (yalnızca Development).
- **Zamanlama:** Her site için tarama sıklığına göre tekrarlayan iş (Türkiye saatiyle 02:00; 12 saatlikte 02:00 ve 14:00; haftalıkta pazartesi). Sahipliği doğrulanmamış sitede zamanlanmış tarama çalışmaz; manuel tarama çalışır.
- **Tarayıcı:** robots.txt'ye uyar, sitemap (index dahil) ve iç linklerle keşfeder, hariç tutulan path'leri ve maksimum sayfayı uygular, sitenin hız limitine (istek/sn) uyar. User-Agent: `MarketingOpsBot/1.0`.
- **Canlı ilerleme:** Worker ilerlemeyi veritabanına yazar; Web 2 saniyede bir aktif taramaları okuyup SignalR (`/hubs/crawls`) ile ilgili sitenin grubuna yayınlar. İki süreç arasında Redis backplane ya da Postgres LISTEN/NOTIFY yerine bu yöntem seçildi: ek altyapı gerektirmez ve 2 saniyelik gecikme ilerleme göstergesi için yeterli.
- Yerelde Worker'ı çalıştırmak için: `dotnet run --project src/MarketingOps.Worker`

### Büyük siteler (üretim)
- **Kapasite:** Site başına en fazla 500.000 sayfa, 1–10 istek/sn. Tahmini süre ayar ekranında gösterilir (sayfa ÷ hız). Tarama sıklığından uzun süren taramada bir sonraki zamanlanmış tur atlanır.
- **İşçi havuzu:** Eşzamanlılık hızdan türetilir (yavaş yanıt veren sitede de hedef hıza ulaşılır, en fazla 30 bağlantı). robots.txt `Crawl-delay` ayardaki hızdan yavaşsa uygulanır; 429/503'te `Retry-After` süresince (en fazla 5 dk) tüm işçiler bekler ve sayfa bir kez yeniden denenir.
- **Kesinti:** İşçi kapanırsa tarama "hata" olmaz; Hangfire işi (kayan görünmezlik süresi, 5 dk) yeniden çalıştırır ve tarama yazılmış sayfaları atlayıp kaldığı yerden devam eder.
- **Sitemap:** Sitemap index, `.xml.gz` ve 50 MB'a kadar dosyalar; en fazla 1.000 sitemap.
- **Saklama:** Her sitede son 3 bitmiş taramanın sayfa verisi tutulur (`Crawling:KeepSnapshotsForCrawls`); daha eskilerin özeti ve bulgu değişimleri kalır. Temizlik her gün 06:00.

## Kural motoru ve bulgular (Faz 3)
- **Kurallar** kodda tanımlıdır (`Domain/Rules/Definitions`, her biri ayrı sınıf; liste `RuleCatalog`). 21 kural: 13 teknik SEO/içerik, 8 performans. Sayfa kuralları (`IPageRule`) tek snapshot'a, site kuralları (`ISiteRule`; title/meta tekrarı, TTFB ortalaması, statik dosya önbelleği) taramanın tamamına bakar.
- **Değerlendirme:** Tarama bitince tarayıcı `EvaluateCrawlJob`'u kuyruğa ekler; iş `EvaluateCrawlCommand`'ı gönderir. Snapshot'lar tek geçişte akış halinde okunur. Aynı kural + aynı sayfa tipi tek bulgudur (parmak izi `sha256(siteId|kural|sayfaTipi)`); yeniden görülen sorun yeni kayıt açmaz.
- **Yaşam döngüsü:** Açık → (görülmedi) Çözüldü → (yeniden görüldü) Tekrar açıldı. Kısmi tarama hiçbir bulguyu çözülmüş saymaz. Yanlış alarm taramalardan etkilenmez ve geri alınabilir; ertelenen bulgu tarih gelene kadar durumunu korur. Kapalı kuralın bulgularına dokunulmaz.
- **Domain olayları:** `FindingOpened` ve `FindingReopened`, kayıt başarılı olduktan sonra `DomainEventNotification<T>` olarak MediatR ile yayınlanır (outbox yok). Faz 4 görev üretimi ve Faz 7 bildirimleri bunları dinleyecek.
- **Statik dosyalar:** Tarayıcı, sayfalardan sonra en fazla 10 iç CSS/JS dosyasının başlıklarını (sıkıştırma, Cache-Control) hız limitine uyarak kontrol eder.
- **Eski taramalar:** Kural motorundan önce yapılmış ya da değerlendirmesi hata almış tarama, tarama detayındaki "Kuralları çalıştır" ile kayıtlı sayfa verisi üzerinden değerlendirilir; site yeniden taranmaz. Daha yeni bir tarama değerlendirildiyse eskisi değerlendirilemez.

## Görevler ve AI önerileri (Faz 4)
- **Görev üretimi:** Her bulgu için tek görev (`WorkTask`) açılır; `FindingOpened` olayını dinleyen handler oluşturur. Anahtar site önekinden gelir (`LB-142`; Ayarlar > Siteler > Genel). Faz 4'ten önce açılmış bulgular için Görevler ekranındaki "Görevleri oluştur" ya da bulgu listesindeki toplu işlem kullanılır.
- **İçerik:** Başlık, problem, sayılar, örnek sayfalar ve kabul kriterleri kural şablonundan gelir; görev AI olmadan da eksiksizdir. Her kural ekibini, öneri türünü (metin / kod / adımlar) ve ekip onayı kriterini kendi sınıfında tanımlar.
- **Kapanış:** Bulgu taramada çözülünce otomatik kriterler sağlanır (`FindingResolved`); ekip kriteri de onaylanınca görev kapanır. Bulgu geri gelirse (`FindingReopened`) görev Yapılacak'a döner, yeni görev açılmaz. Görev elle kapatılamaz.
- **AI zenginleştirme (isteğe bağlı):** Hangfire işi etki ve öneri metnini üretir; sayılar ve URL'ler şablonda kalır, model yalnızca verilen URL'ler için önerilen değer ekleyebilir. Sağlayıcı `Ai:Provider` ile seçilir:
  - `Anthropic` — `Ai:Anthropic:ApiKey`, model `claude-opus-5-5` (yapılandırılmış çıktı; güvenlik reddinde `Ai:Anthropic:FallbackModel` ile sunucu tarafı yedek).
  - `DeepSeek` — `Ai:DeepSeek:ApiKey`, model `deepseek-chat` (OpenAI uyumlu API; sunucular Çin'de).
  - `Ollama` (şu an seçili) — `Ai:Ollama:BaseUrl` (Docker'dan makinedeki Ollama: `http://host.docker.internal:11434/v1`), `Ai:Ollama:Model` (`qwen2.5:7b`), `Ai:Ollama:TimeoutSeconds`. Veri makineden çıkmaz. 7B modelde bir öneri M1/16 GB'ta yaklaşık 1,5–2 dakika sürer; Türkçe metin kalitesi sınırlıdır, belleğiniz yetiyorsa `qwen2.5:14b` daha iyi sonuç verir.
  - `LmStudio` — `Ai:LmStudio:BaseUrl` (`http://localhost:1234/v1`), `Ai:LmStudio:Model` (`qwen/qwen3.5-9b`). LM Studio'nun OpenAI uyumlu sunucusu; `json_object` kabul etmediği için çıktı `json_schema` ile istenir. Veri makineden çıkmaz.
  - `None` — adım atlanır. Entegrasyon testleri her zaman `None` ile çalışır.
- Modele kuralın sitedeki eşikleri ve etkilenen sayfaların ayrıntısı (ör. asıl title) verilir; mevcut değerin aynısı olan öneriler atılır.
- **Bilgi tabanı:** `knowledge/*.md` içindeki `<!-- ref: perf-doc:p30 -->` işaretli parçalar, kuralın referans anahtarına göre AI'a verilir (vektör DB yok). Biçim: `knowledge/README.md`.

## Google connector'ları (Faz 5)
- **Bağlama:** Ayarlar > Siteler > Bağlantılar'da "Google ile bağlan". Kullanıcı kendi Google hesabıyla salt okunur izin verir (Search Console, Analytics, Tag Manager), döner ve siteye ait mülkleri seçer. Refresh token Data Protection ile şifreli saklanır.
- **Sahiplik:** Seçilen Search Console mülkü sitenin domainine aitse ve erişim doğrulanmışsa site sahipliği doğrulanır; zamanlanmış taramalar ve günlük Web Vitals ölçümü yalnızca doğrulanmış sitelerde çalışır.
- **Günlük senkron (05:00, Türkiye saati):** Search Console son 28 gün sayfa performansı, sitemap durumu, URL Inspection (`Google:UrlInspectionDailyLimit`, öncelik açık bulgulu sayfalar); GA4 son 30 gün e-ticaret event'leri; PageSpeed Insights ile sayfa tipi başına örnek sayfaların LCP/INP/CLS/TTFB değerleri. Tekrar çalıştırma kayıtları günceller (upsert). Kota/429 için standart yeniden deneme.
- **Yeni kurallar:** `seo.opportunity.striking_distance` (11–20. sıra, yüksek gösterim), `seo.index.not_indexed`, `perf.cwv.lcp|inp|cls`. Kaynağın güncel verisi yoksa kural çalışmaz; bağlantı koparsa mevcut bulgular yanlışlıkla çözülmez.
- **Kurulum (SaaS sahibi, bir kez):** Google Cloud Console'da bir proje açın → Search Console API, Google Analytics Admin API, Google Analytics Data API, Tag Manager API ve PageSpeed Insights API'yi etkinleştirin → OAuth izin ekranını yapılandırın (geliştirmede "Testing" + test kullanıcıları) → "Web application" türünde OAuth istemcisi oluşturup yetkili yönlendirme adresine `http://localhost:8080/connectors/google/callback` (canlıda kendi domaininiz) ekleyin → Client ID/Secret'ı `appsettings.json` içindeki `Google` bölümüne yazın. Herkese açık kullanım için Google'ın hassas scope doğrulaması gerekir. İsteğe bağlı: `Google:PageSpeedApiKey`.

## Migration
```sh
dotnet tool restore
dotnet ef migrations add <Ad> -p src/MarketingOps.Infrastructure -s src/MarketingOps.Web -o Persistence/Migrations
```
Development'ta uygulama açılırken bekleyen migration'lar uygulanır. Production'da otomatik uygulanmaz.

## Testler
```sh
dotnet test tests/MarketingOps.UnitTests          # domain kuralları, validator'lar
dotnet test tests/MarketingOps.IntegrationTests   # Testcontainers ile gerçek Postgres; Docker çalışıyor olmalı
```

## Yapı
| Proje | İçerik |
|---|---|
| `MarketingOps.Domain` | Aggregate'ler (Organization, Site, Crawl, Finding, SiteRuleSetting), kurallar, value object'ler, enum'lar, `DomainException` |
| `MarketingOps.Application` | Command/Query + Handler + Validator + Response (her biri ayrı dosyada), `IAppDbContext`, `ValidationBehavior` |
| `MarketingOps.Infrastructure` | `AppDbContext` (snake_case), EF yapılandırmaları, migration'lar, Development seed |
| `MarketingOps.Web` | MVC ekranları, `/api` controller'ları, `GlobalExceptionHandler` (ProblemDetails), tasarım sistemi |

## Ekranlar
- **Ayarlar > Siteler** (`/sites/{siteId}/settings/sites`): site listesi, site ekleme, ad, tarama ayarları, beklenen kimlikler, sahiplik durumu, silme (domain yazarak onay).
- **Ayarlar > Sayfa Tipleri** (`/sites/{siteId}/settings/page-types`): liste, ekleme/düzenleme (VE/VEYA, glob/regex/schema koşulları), diğer tiplerle çakışmayı da gösteren canlı önizleme, silme.
- Hiç site yokken `/` ilk site ekleme formunu gösterir.
- **Bulgular** (`/sites/{siteId}/findings`): önem/kategori/durum/sayfa tipi/kural filtreleri, "son taramada yeni", arama (hepsi URL'de), sıralama, kolon görünürlüğü, toplu erteleme ve yanlış alarm, CSV.
- **Bulgu detayı** (`/sites/{siteId}/findings/{id}`): problem, etki, kaynak, etkilenen sayfalar (arama, CSV), ayrıntılar, geçmiş; ertele, yanlış alarm / geri al. Görev alanları Faz 4'te.
- **Ayarlar > Kurallar** (`/sites/{siteId}/settings/rules`): kategoriye göre liste ve aç/kapa; detayda eşikler (canlı etki önizlemesi), site geneli ve sayfa tipine göre önem, kuralın bulguları ve susturulduğu sayfa tipleri.
- **Taramalar:** listede ve detayda bulgu değişimleri (yeni / çözülen / tekrar açılan).
- **Performans** (`/sites/{siteId}/performance`): sayfa tipi bazında Core Web Vitals (mobil/masaüstü, Google eşiklerine göre İyi / İyileştirilmeli / Zayıf, 28 günlük trend), performans kuralları ve bulgu sayıları, TTFB kartı ("Kesin bulgu" / "Kontrol önerisi").
- **Bulgu detayı / Etki:** Search Console bağlıysa etkilenen sayfaların son 28 günlük tıklama, gösterim, CTR ve ortalama sırası.
- **Görevler** (`/sites/{siteId}/tasks`): tablo ve pano görünümü; durum, ekip, önem filtreleri. Görev detayı bulgu detayının içindedir (önerilen çözüm, kabul kriterleri, durum/ekip, yorumlar, geri bildirim).

## API
- `GET/POST /api/sites`, `GET/PUT/DELETE /api/sites/{siteId}`
- `PUT /api/sites/{siteId}/crawl-settings`, `PUT /api/sites/{siteId}/expected-tracking-ids`
- `POST /api/sites/{siteId}/page-types`, `PUT/DELETE /api/sites/{siteId}/page-types/{pageTypeId}`
- `POST /api/sites/{siteId}/page-types/preview` (kaydetmeden eşleşme önizlemesi)

- `GET /api/sites/{siteId}/findings` (filtreler: `severity`, `category`, `status`, `pageTypeId`, `unmatched`, `ruleCode`, `newInLastCrawl`, `search`, `sort`, `descending`), `GET .../findings/summary`, `GET .../findings/{id}`
- `POST .../findings/{id}/false-positive`, `.../revert-false-positive`, `.../snooze`, `POST .../findings/bulk`
- `GET /api/sites/{siteId}/rules`, `GET/PUT .../rules/{code}`, `POST .../rules/{code}/impact`
- `POST /api/sites/{siteId}/crawls/{crawlId}/evaluate`
- `GET/POST /api/sites/{siteId}/tasks`, `GET .../tasks/{id}`, `POST .../tasks/{id}/status|team|comments|feedback`, `POST .../tasks/{id}/criteria/{criterionId}`

Hatalar RFC 7807 ProblemDetails olarak, Türkçe mesajlarla döner: 400 (doğrulama/domain kuralı, alan bazlı `errors`), 404, 409.

## Notlar
- .NET 10 SDK'nın paylaşılan derleyici sunucusu Razor dosyaları hızlı değiştiğinde eski içerikle anlamsız hatalar üretebildiği için Web projesinde `UseSharedCompilation=false` açıktır. Başka projede benzer hata görülürse: `dotnet build-server shutdown`.
