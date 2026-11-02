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
- **Domain olayları:** `FindingOpened` ve `FindingReopened`, kayıt başarılı olduktan sonra `DomainEventNotification<T>` olarak MediatR ile yayınlanır (outbox yok). Faz 4 görev üretimi ve Faz 7 bildirimleri bunları dinler.
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

## Tracking denetimi (Faz 6)
- **Başlatma:** Tracking ekranındaki "Denetimi başlat" Hangfire işini (`TrackingAuditJob`) kuyruğa ekler; zamanlanmış çalışma yoktur.
- **Tarayıcı yolculuğu:** Playwright ana sayfayı, son taramadaki sayfa tipi örneklerini ve Ayarlar > Siteler > Denetim'deki sepet, ödeme, teşekkür adreslerini birer kez açar. Çerez onayı seçicisi verildiyse sayfa yüklendikten sonra tıklanır; tıklamadan önce giden etiket istekleri "onay öncesi" işaretlenir (tıklama olmazsa işaret konmaz). GA4, GTM, Meta ve Google Ads istekleri ve kimlikleri kaydedilir; tag matrisi ve hücre ayrıntısı bu ölçümden gelir.
- **Sipariş karşılaştırması:** Sipariş kaynağı (SQL Server sorgusu, `IOrderSource`) günlük sipariş sayısını, GA4 purchase sayısıyla 7/30 gün karşılaştırılır. Kaynak yoksa ya da sorgu düşerse satır yine yazılır, purchase boş kalır ve fark kuralı çalışmaz.
- **Kurallar:** `track.tag.missing`, `track.tag.duplicate`, `track.tag.wrong_id` (beklenen kimliklere göre), `track.consent.prefire` (onaydan önce tetiklenen etiket), `track.meta.pixel_capi`, `track.event.anomaly` (event sayısı düşüşü), `track.purchase.gap` (eşik kural ayarında).

## Skorlar, bildirimler ve raporlar (Faz 7)
- **Skor anlık görüntüsü:** Her değerlendirmeden sonra kategori skorları kaydedilir (`RecordScoreSnapshots`); Genel Bakış'taki trend ve "Bu skor nasıl hesaplandı?" paneli bundan beslenir. Formül Faz 8'de değişti, bkz. aşağıdaki "Skor formülü".
- **Bildirim olayları:** yeni kritik bulgu, tekrar açılan bulgu, purchase farkı eşiği, skor düşüşü (eşik puanı kuralda), tarama başarısız, bağlantı yetkisi doldu. Domain olayları dinlenir, gönderim Hangfire işinde yapılır.
- **Kanallar:** panel içi (her zaman), e-posta (SMTP, `Mail` bölümü), Teams ve Slack webhook'u (adres şifreli saklanır). Kural başına İstanbul saatine göre sessiz saatler ve aynı bulgu için tekrar aralığı. Ayarlar ve zil için bkz. Ekranlar.
- **Yönetici özeti:** günlük, haftalık (pazartesi) ve aylık (ayın ilk günü) e-posta; `SendDueReportsJob` 15 dakikada bir vadesi gelenleri gönderir. İçerik: skorlar ve değişimleri, en önemli 5 sorun, kapanan görevler.

## HTML denetimi ve skorlar (Faz 8, adım 1)
Plan ve kararlar: [marketingops-faz8-plani.md](marketingops-faz8-plani.md).
- **Sıkı denetim:** Kural tek sayfada bile ihlal görürse bulgu açılır; yeni kurallar her sitede açık başlar. SEO kuralları indekslenebilir, erişilebilirlik kuralları bütün HTML sayfalara bakar.
- **Parser:** `HtmlSnapshotParser` sayfadan ayrıca structured data düğümlerini (`StructuredDataExtractor`; dolu özellik yolları, ör. `offers.price`), linkleri, başlık sırasını, form alanlarını, tabloları, iframe'leri ve sayfa bölgelerini (`MarkupExtractor`) çıkarır. Sorun başına sayı ve en fazla 5 örnek saklanır. Snapshot'taki `Version` 2'den küçükse (eski tarama) bu alanlara bakan kurallar çalışmaz.
- **Structured data zorunlu:** Her indekslenebilir sayfada geçerli JSON-LD ya da microdata olmalı (`seo.schema.missing`); bozuk blok (`seo.schema.invalid`), bilinen tiplerde zorunlu alan (`seo.schema.properties`, tablo `SchemaRequirements`), ana sayfada Organization + WebSite, diğer sayfalarda BreadcrumbList.
- **Diğer yeni kurallar:** Open Graph (eksik, görsel boyutu/açıklaması, og:url ↔ canonical), meta description uzunluğu, jenerik ve hedefsiz link, `_blank` + noopener, iframe lazy; erişilebilirlik ve mobil kategorisinde adsız link, başlık sırası, `th`'siz tablo, başlıksız iframe, etiketsiz form alanı, sayfa bölgeleri, etiketsiz menüler, viewport, `<html lang>`.
- **Dördüncü skor:** "Erişilebilirlik ve Mobil Sağlığı" (`FindingCategory.Accessibility`).
- **Skor formülü:** 100'den başlar; bulgu `önem ağırlığı × sitede etkilenen sayfa oranı` kadar düşürür. Bir kuralın bütün sayfa tiplerindeki bulguları birlikte en fazla o kuralın ağırlığı kadar düşürür, böylece şablondan gelen tek sorun her sayfa tipinde ayrı cezalandırılmaz. Oran sitenin bütün HTML sayfalarına göredir ve yalnızca taranan sayfalara dayanan kurallarda uygulanır; tracking denetimi ve PageSpeed gibi örnek ölçen kaynakların bulguları tam ağırlık düşürür. Genel Bakış'taki sayfa tipi skoru oranı tipin kendi sayfa sayısından hesaplar. Ağırlıklar: Critical 15, High 8, Medium 3, Low 1.
- **Bilgi tabanı:** `knowledge/erisilebilirlik.md` (erişilebilirlik ve mobil), `knowledge/seo-dokumani.md` (iç SEO dokümanının özetleri), `google-search.md`'ye Open Graph, breadcrumb ve organizasyon notları.

## Render denetimi (Faz 8, adım 2)
- **Ne zaman:** Tarama bitince, kural değerlendirmesinden önce (`CrawlStage.Render`, `RenderCrawlJob`). Ayarlar > Siteler > Genel'de açılıp kapatılır (varsayılan açık) ve sayfa tipi başına örnek sayısı seçilir (1–10, varsayılan 3).
- **Hangi sayfalar:** Her sayfa tipinden doğrudan 2xx dönen HTML sayfalar URL uzunluğuna göre sıralanıp eşit aralıklarla seçilir; ana sayfa her zaman eklenir. Her sayfa masaüstü (1366×900), tablet (768×1024) ve telefonda (375×812, dokunmatik) açılır. Sitenin hız limitine uyulur; çerez onayı seçicisi (tracking ayarı) varsa tıklanır.
- **Ölçüm:** `Infrastructure/Rendering/render-audit.js` (Elevare CMS SEO analizöründen uyarlandı) sayfanın kendi betikleri çalıştıktan sonra yatay taşmayı, ekrandan geniş öğeleri, WCAG AA kontrastını, küçük yazıyı, küçük dokunma alanlarını, ilk ekrandaki lazy ve ekran altındaki eager görselleri ve orantısız görselleri ölçer. Ekranın %30'undan fazlasını sabit bir katman kaplıyorsa ölçüm "engellenmiş" sayılır; ilk ekran ve kontrast kuralları o örneği kullanmaz.
- **Kurallar:** `mobile.overflow`, `mobile.wide_element`, `a11y.contrast`, `a11y.text.small`, `mobile.tap_target.small`, `perf.image.lazy_above_fold`, `perf.image.eager_below_fold`, `content.image.distorted`. Render ölçümü olan sayfa tiplerinde sezgisel `perf.image.lazy_missing` yerini kesin ölçüme bırakır.
- **Skor:** Örneklenen sorun şablon sorunudur; tipin ölçülen örneklerindeki oran tipin bütün sayfalarına yansıtılır (3 örneğin 2'si sorunluysa tipin üçte ikisi). PageSpeed bulguları da aynı şekilde hesaplanır.
- **Hata:** Tarayıcı açılamazsa ya da sayfalar ölçülemezse tarama başarısız olmaz; aşama kaydedilir, render kuralları o taramada çalışmaz ve mevcut bulguları çözülmez. Ölçülemeyen sayfalar tarama detayında listelenir.
- **Yerelde Chromium:** Worker Docker imajında kurulu. Yerelde bir kez: `pwsh src/MarketingOps.Worker/bin/Debug/net10.0/playwright.ps1 install chromium` (betiğin entegrasyon testleri de bunu ister).

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
- **Ayarlar > Sayfa Tipleri** (`/sites/{siteId}/settings/page-types`): liste, ekleme/düzenleme (VE/VEYA, glob/regex/schema koşulları), diğer tiplerle çakışmayı da gösteren canlı önizleme, silme. "AI ile tip öner": son taramada hiçbir tipe uymayan sayfalardan öneri kartları (ad ve glob düzenlenebilir; ekle / reddet).
- Hiç site yokken `/` ilk site ekleme formunu gösterir.
- **Yeni site sihirbazı** (`/sites/{siteId}/setup/{adım}`): domain (site ekleme formu) → Search Console ve sahiplik (Google'a bağlanınca akış sihirbaza döner) → diğer bağlantılar (beklenen kimlikler, Meta) → sayfa tipleri (sitemap'ten okunan adresler URL kalıbına göre gruplanır, AI bunlardan tip önerir; AI yoksa gruplara elle ad verilir) → tarama ayarları → ilk tarama. Sıradaki adım sitede saklanır (`sites.setup_step`, null = tamamlandı); kurulumu yarım kalan sitede her ekranın üstünde "Kuruluma devam et" şeridi görünür. Sahiplik doğrulanmamış sitede ilk tarama elle başlatılabilir, zamanlanmış taramalar doğrulamadan sonra çalışır.
- **Bulgular** (`/sites/{siteId}/findings`): önem/kategori/durum/sayfa tipi/kural filtreleri, "son taramada yeni", arama (hepsi URL'de), sıralama, kolon görünürlüğü, toplu erteleme ve yanlış alarm, CSV.
- **Bulgu detayı** (`/sites/{siteId}/findings/{id}`): problem, etki, kaynak, etkilenen sayfalar (arama, CSV), ayrıntılar, geçmiş; ertele, yanlış alarm / geri al. Görev alanları Faz 4'te.
- **Ayarlar > Kurallar** (`/sites/{siteId}/settings/rules`): kategoriye göre liste ve aç/kapa; detayda eşikler (canlı etki önizlemesi), site geneli ve sayfa tipine göre önem, kuralın bulguları ve susturulduğu sayfa tipleri.
- **Taramalar:** listede ve detayda bulgu değişimleri (yeni / çözülen / tekrar açılan).
- **Performans** (`/sites/{siteId}/performance`): sayfa tipi bazında Core Web Vitals (mobil/masaüstü, Google eşiklerine göre İyi / İyileştirilmeli / Zayıf, 28 günlük trend), performans kuralları ve bulgu sayıları, TTFB kartı ("Kesin bulgu" / "Kontrol önerisi").
- **Bulgu detayı / Etki:** Search Console bağlıysa etkilenen sayfaların son 28 günlük tıklama, gösterim, CTR ve ortalama sırası.
- **Görevler** (`/sites/{siteId}/tasks`): tablo ve pano görünümü; durum, ekip, önem filtreleri. Görev detayı bulgu detayının içindedir (önerilen çözüm, kabul kriterleri, durum/ekip, yorumlar, geri bildirim).
- **Ayarlar > Bildirimler** (`/sites/{siteId}/settings/notifications`): kanallar (panel içi, SMTP durumu, Teams/Slack webhook'u; adres tekrar gösterilmez) ve kanal başına test mesajı; olay başına kural (kanallar, e-posta alıcıları, İstanbul saatine göre sessiz saatler, tekrar aralığı, skor düşüş eşiği); günlük/haftalık/aylık yönetici özeti (saat, alıcılar, e-postanın iframe içinde önizlemesi).
- **Zil (üst bar):** son 20 panel içi bildirim, okunmamış sayısı, "Tümünü okundu say"; tıklanan bildirim okundu sayılır ve ilgili ekrana gider. Okunmamış sayısı `NotificationCountBroadcaster` ile 5 saniyede bir, değiştiğinde SignalR üzerinden canlı gelir.

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
