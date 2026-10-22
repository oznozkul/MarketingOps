# MarketingOps — Backend promptları (Claude Code)

Teknoloji: .NET 10 (LTS), ASP.NET Core MVC, DDD, CQRS (MediatR), FluentValidation, EF Core + PostgreSQL, Docker.

Kullanım:
- **Prompt 0 her yeni oturumda ilk verilir.** Kuralları ve mimariyi tanımlar. Claude Code bu kuralları repoda `CLAUDE.md` dosyasına da yazacak, böylece sonraki oturumlarda kendiliğinden okunur.
- Fazları sırayla verin. Bir faza geçmeden önce önceki fazın testleri geçmiş ve `docker compose up` çalışıyor olmalı.
- Ekran promptlarıyla sıralama: Önce Backend Faz 0 ve Faz 1'i tamamlayın, sonra ekran promptlarına geçin. Ekranlar, mock veri yerine gerçek sorguları kullanacak.

---

## Prompt 0 — Mimari kurallar ve iskelet

```
MarketingOps adında bir B2B SaaS'ın backend'ini kuracağız. Önce kuralları oku, repoya CLAUDE.md olarak kaydet, sonra sadece "Bu adımda yapılacaklar" kısmını yap.

## Ürün özeti
E-ticaret siteleri için salt okunur bir pazarlama denetim platformu. Siteleri tarar; Search Console, GA4, GTM, Google Ads, Meta ve sitenin sipariş veritabanından veri okur. Teknik SEO, performans ve tracking sorunlarını bulgu (Finding) olarak çıkarır, görev (Task) olarak ekibe açar ve sonraki taramada düzelip düzelmediğini doğrular. Sistem siteye veya reklam hesaplarına ASLA yazmaz. Çok siteli ve generic: Organization > Site. Hiçbir şey belirli bir siteye göre hardcode edilmez.

## Teknoloji
- .NET 10, C#, nullable açık, implicit usings açık.
- ASP.NET Core MVC. Tek web uygulaması: MVC ekranları ve /api altındaki JSON endpoint'leri aynı host'ta. Ayrı bir API projesi YOK.
- MediatR 12.5.0. Bu sürümü sabitleyin: 13 ve sonrası ticari lisans gerektiriyor.
- FluentValidation (DI extensions ile).
- EF Core + Npgsql, EFCore.NamingConventions ile snake_case tablo ve kolon adları.
- PostgreSQL 17, Docker Compose.
- Test: xUnit, FluentAssertions yerine Shouldly (FluentAssertions da ticari lisansa geçti), Testcontainers.PostgreSql ile entegrasyon testleri.
- Central Package Management (Directory.Packages.props) ve Directory.Build.props.

## Proje yapısı
src/
  MarketingOps.Domain           -> Entity, value object, enum, domain exception. Hiçbir pakete bağımlı değil.
  MarketingOps.Application      -> MediatR command/query, handler, validator, DTO, arayüzler (IAppDbContext vb.)
  MarketingOps.Infrastructure   -> EF Core DbContext, konfigürasyonlar, migration'lar, dış servis implementasyonları
  MarketingOps.Web              -> MVC + API controller'ları, Program.cs, hata yönetimi
tests/
  MarketingOps.UnitTests        -> Domain ve handler/validator testleri
  MarketingOps.IntegrationTests -> Testcontainers ile gerçek Postgres üzerinde testler

Bağımlılık yönü: Web -> Application + Infrastructure, Infrastructure -> Application, Application -> Domain.

## DOSYA KURALI (çok önemli)
Her tip kendi .cs dosyasında olur. Bir dosyada birden fazla class/record/enum/interface OLMAZ.
- Command, Query, Handler, Validator, Response/DTO, Enum, Exception, EF konfigürasyonu ve Controller ayrı dosyalardır.
- Dosya adı tip adıyla birebir aynıdır.

Feature (dikey dilim) klasörleri:
Application/
  Sites/
    Commands/
      CreateSite/
        CreateSiteCommand.cs
        CreateSiteCommandHandler.cs
        CreateSiteCommandValidator.cs
    Queries/
      GetSiteById/
        GetSiteByIdQuery.cs
        GetSiteByIdQueryHandler.cs
        SiteDetailResponse.cs
Domain/
  Sites/
    Site.cs
    PageType.cs
    CrawlSettings.cs
    MatchCondition.cs
    MatchConditionType.cs   (enum, kendi dosyasında)
    MatchMode.cs            (enum, kendi dosyasında)

## Over-design YASAK. Şunlar yapılmayacak:
- Generic repository ve Unit of Work. Application, IAppDbContext arayüzü üzerinden DbSet'leri doğrudan kullanır. SaveChangesAsync de bu arayüzden çağrılır.
- AutoMapper ya da benzeri mapper kütüphanesi. Map'leme sorgularda LINQ Select ile, ya da elle yazılır.
- Specification pattern, Result<T> monad'ı, her şey için interface açmak.
- Domain event / outbox altyapısı. Gerçekten ihtiyaç doğan fazda, o fazın promptunda açıkça istenecek.
- Kullanılmayan base class, extension veya "ileride lazım olur" kodu.
- Her entity için ayrı strongly-typed ID tipi. Guid kullanılır (Guid.CreateVersion7()).
Bir soyutlama ancak en az iki gerçek implementasyonu varsa (ör. IOrderSource: SQL Server, Shopify) ya da dış bir sistemi test için ayırmak gerekiyorsa (ör. ICrawlerHttpClient) açılır.

## Okunabilirlik
- Kısa metotlar, açıklayıcı isimler. Yorum, kodun NE yaptığını değil NEDEN yaptığını anlatır.
- Domain dili kodda aynen kullanılır: Site, PageType, Crawl, Finding, WorkTask, Rule.
- Kod İngilizce. Kullanıcıya dönen hata mesajları Türkçe.
- Primary constructor kullanılabilir. Ama domain entity'lerde public setter olmaz, değişiklik metotlarla yapılır (site.UpdateCrawlSettings(...)).
- Magic string/number yok, ama sadece bir yerde kullanılan değer için ayrı sabit sınıfı da açılmaz.

## DDD kuralları
- Aggregate root'lar kendi tutarlılığını korur. Child entity'ler sadece root üzerinden değiştirilir (site.AddPageType(...)).
- Geçersiz durum oluşturulamaz: Factory metot (Site.Create) ve davranış metotları kuralları kontrol eder, ihlalde DomainException fırlatır.
- Aggregate'ler arası referans sadece ID ile.
- Value object'ler record ile yazılır.

## CQRS kuralları
- Command: Durum değiştirir. Aggregate'i yükler, domain metodunu çağırır, SaveChanges yapar. Sadece ID ya da minimal bir sonuç döner.
- Query: Durum değiştirmez. AsNoTracking + Select projection ile doğrudan Response DTO'ya map'ler. Aggregate yüklemez, Include kullanmaz (gerekirse projection içinde ilişki okunur). Listelerde sayfalama zorunlu.
- Response/DTO'lar sealed record olur.
- Validation: Her command/query için, girdi doğrulaması gerekiyorsa bir FluentValidation validator yazılır. Bir ValidationBehavior pipeline'ı tüm validator'ları çalıştırır. Domain kuralları (ör. aynı isimde iki sayfa tipi olamaz) validator'da değil domain'de kontrol edilir; DB gerektiren benzersizlik kontrolleri (ör. aynı domain'de iki site) handler'da yapılır.

## Hata yönetimi
- Exception tipleri (her biri ayrı dosyada): DomainException (400), ValidationException (FluentValidation, 400, alan bazlı hatalar), NotFoundException (404), ConflictException (409).
- Web'de tek bir IExceptionHandler bunları RFC 7807 ProblemDetails'e çevirir. Beklenmeyen hatalar 500 döner ve loglanır, ama detay dışarı sızmaz.

## Zaman ve kimlik
- Tüm zamanlar DateTimeOffset, UTC. Şimdiki zaman TimeProvider'dan alınır (test edilebilirlik için).
- Faz 1'de kimlik doğrulama yok. Tek bir varsayılan Organization seed edilir. Auth ileride ayrı bir faz olacak.

## Bu adımda yapılacaklar (Faz 0 — iskelet)
1. CLAUDE.md'yi bu kurallarla oluştur.
2. Solution, projeler, Directory.Build.props, Directory.Packages.props (tüm sürümler sabit), .editorconfig.
3. Domain: Entity base class (Guid Id, eşitlik ID'ye göre) ve DomainException. Başka base class yok.
4. Application: IAppDbContext (şimdilik boş, DbSet'ler fazlarda eklenecek), ValidationBehavior, NotFoundException, ConflictException, PagedResponse<T> (Items, Page, PageSize, TotalCount) ve DependencyInjection.cs (AddApplication: MediatR, validator'lar, behavior).
5. Infrastructure: AppDbContext (IAppDbContext'i uygular, snake_case), DependencyInjection.cs (AddInfrastructure).
6. Web: Program.cs, ProblemDetails exception handler, /health endpoint'i (Postgres kontrolü dahil), OpenAPI dokümanı (/openapi/v1.json, sadece Development'ta). Development'ta migration'lar başlangıçta otomatik uygulanır; Production'da uygulanmaz.
7. Docker: Web için multi-stage Dockerfile. docker-compose.yml: postgres:17 (volume ve healthcheck ile) ve web (postgres sağlıklı olunca başlar). Bağlantı bilgileri .env'den gelir; repoya .env.example eklenir.
8. README: Nasıl çalıştırılır, migration nasıl eklenir, testler nasıl koşulur.
9. UnitTests ve IntegrationTests projeleri. IntegrationTests için Testcontainers Postgres fixture'ı ve WebApplicationFactory kurulumu. Bir smoke test: /health 200 döner.
Bitti sayılır: `docker compose up` ile uygulama ayağa kalkıyor, /health sağlıklı, testler geçiyor.
```

---

## Faz 1 — Organization, Site, Sayfa Tipleri

```
CLAUDE.md kurallarına uyarak Faz 1'i yap.

## Domain
Organization (aggregate root): Name. Faz 1'de tek bir varsayılan organizasyon seed edilir.

Site (aggregate root):
- OrganizationId, Name, Domain (normalize edilmiş host: küçük harf, "www." ve şema yok, sonda "/" yok), IsOwnershipVerified, CreatedAt.
- CrawlSettings (value object, owned): Schedule (Daily, Every12Hours, Weekly), MaxRequestsPerSecond (1–10), MaxPages, ExcludedPathPatterns (liste), RespectRobotsTxt (her zaman true; ayarlanamaz, kodda sabit, sadece bilgi amaçlı gösterilir).
- ExpectedTrackingIds (value object, owned): Ga4MeasurementId, GtmContainerId, MetaPixelId, GoogleAdsConversionId. Hepsi opsiyonel, formatları kontrol edilir (G-XXXX, GTM-XXXX).
- PageTypes (child entity koleksiyonu).
- Davranışlar: Create, Rename, UpdateCrawlSettings, UpdateExpectedTrackingIds, MarkOwnershipVerified, AddPageType, UpdatePageType, RemovePageType.

PageType (Site'ın child entity'si):
- Name (site içinde benzersiz, büyük/küçük harf duyarsız), Mode (All = tüm koşullar, Any = herhangi biri), Conditions (en az 1).
- MatchCondition (value object): Type (UrlGlob, UrlRegex, SchemaType), Value. Regex, oluşturulurken derlenerek doğrulanır; geçersizse DomainException.
- Metot: bool Matches(Uri url, IReadOnlyCollection<string> schemaTypes). Glob desteği: `*` (tek path segmenti) ve `**` (birden çok segment). Bu metot Faz 2'de crawler tarafından kullanılacak, o yüzden iyi test edilmeli.
- Conditions, PostgreSQL'de jsonb olarak saklanır (EF Core ToJson).

## Application (her biri ayrı dosyalarda: Command/Query, Handler, Validator, Response)
Sites:
- CreateSiteCommand -> Guid. Aynı organizasyonda aynı domain varsa ConflictException.
- UpdateSiteCommand (ad), UpdateCrawlSettingsCommand, UpdateExpectedTrackingIdsCommand, DeleteSiteCommand.
- GetSitesQuery -> PagedResponse<SiteListItemResponse> (Id, Name, Domain, IsOwnershipVerified, PageTypeCount).
- GetSiteByIdQuery -> SiteDetailResponse (ayarlar, beklenen ID'ler, sayfa tipleri dahil).

PageTypes:
- AddPageTypeCommand, UpdatePageTypeCommand, RemovePageTypeCommand (hepsi Site aggregate'i üzerinden).
- PreviewPageTypeMatchQuery: Verilen koşullar ve örnek URL listesi için hangi URL'lerin eşleştiğini döner. DB'ye yazmaz. Ekrandaki canlı önizleme bunu kullanacak.

Validator'lar: Domain formatı, isim uzunlukları, liste boyutları, sayı aralıkları. Hata mesajları Türkçe.

## Web
- /api/sites ve /api/sites/{siteId}/page-types altında REST endpoint'leri. Controller'lar ince olur: Sadece MediatR'a gönderir ve sonucu döner.
- Development'ta seed: Varsayılan organizasyon, lastikborsasi.com ve tyresupply.com siteleri, örnek sayfa tipleri (Kategori /kategori/*, Ürün /urun/* + schema Product, Lastik ölçü /lastik/*/*, Jant /jant/*, Araç uyumluluk /arac/**, Blog /blog/* + schema Article). Seed idempotent olmalı.

## Testler
- Unit: Site domain kuralları (domain normalizasyonu, aynı isimde sayfa tipi, en az bir koşul, geçersiz regex), PageType.Matches için glob/regex/schema ve All/Any senaryoları, validator'lar.
- Integration: Site oluştur -> getir -> sayfa tipi ekle -> güncelle -> sil akışı. Duplicate domain 409, olmayan site 404, geçersiz girdi 400 + alan hataları.
Bitti sayılır: Tüm endpoint'ler OpenAPI'de görünüyor, testler geçiyor.
```

---

## Faz 2 — Tarama (Crawler)

```
CLAUDE.md kurallarına uyarak Faz 2'yi yap.

## Mimari karar
Taramalar arka planda çalışır. Hangfire + Hangfire.PostgreSql kullan (ayrı bir kuyruk sistemi kurma). Zamanlanmış taramalar her sitenin Schedule ayarına göre recurring job olarak kaydedilir. Hangfire dashboard /jobs altında, sadece Development'ta açık.
Yeni proje: MarketingOps.Worker (Hangfire server). Web sadece job ekler, Worker işler. Docker Compose'a worker servisi eklenir. Crawler kodu Infrastructure'da durur, iki host da aynı Application/Infrastructure'ı kullanır.

## Domain
Crawl (aggregate root): SiteId, Number (site içinde artan), Status (Queued, Running, Completed, PartiallyCompleted, Failed, Cancelled), TriggeredBy (Schedule, Manual), StartedAt, FinishedAt, PagesDiscovered, PagesCrawled, PagesFailed, ErrorMessage. Durum geçişleri metotlarla yapılır ve geçersiz geçiş DomainException fırlatır (Completed bir tarama tekrar Running olamaz). Aynı site için aynı anda tek bir aktif (Queued/Running) tarama olabilir; bu kontrol handler'da yapılır.

PageSnapshot (ayrı aggregate, toplu yazılır): CrawlId, SiteId, Url, NormalizedUrl, PageTypeId (nullable = eşleşmeyen), StatusCode, RedirectChain, ResponseTimeMs (TTFB), ContentEncoding, CacheControl, HtmlSizeBytes, FetchedAt ve Data (jsonb).
PageSnapshotData (owned, jsonb): Title, MetaDescription, Canonical, MetaRobots, H1 listesi, H2 listesi, Lang, Viewport, Charset, SchemaTypes, OpenGraph alanları, InternalLinks, ExternalLinks, Images (src, alt, width, height, loading), Scripts (src, async, defer, inHead), Stylesheets, Preloads, Preconnects, WordCount.
Not: Bu alanlar Faz 3'teki kurallar ve performans dokümanındaki kontroller için gerekli.

## Crawler (Infrastructure)
- Keşif: robots.txt okunur ve uyulur; sitemap.xml (sitemap index dahil) + iç linkler. ExcludedPathPatterns ve MaxPages uygulanır. URL normalizasyonu (fragment kaldırma, query parametrelerinin sıralanması, sondaki / tutarlılığı).
- İndirme: HttpClient, site ayarındaki hız limitine uyar (System.Threading.RateLimiting), User-Agent: "MarketingOpsBot/1.0 (+iletişim URL'si)", timeout, sınırlı paralellik. Redirect'ler manuel takip edilir ve zincir kaydedilir.
- Parse: AngleSharp. HTML'den PageSnapshotData çıkarılır.
- Sayfa tipi atama: Site'ın PageType.Matches metodu kullanılır; ilk eşleşen tip atanır.
- Snapshot'lar toplu (batch) yazılır, change tracker şişirilmez.
- İlerleme: Crawl kaydı periyodik güncellenir. SignalR CrawlHub üzerinden ilerleme yayınlanır (Worker -> Web: Postgres LISTEN/NOTIFY ya da SignalR Redis backplane yerine basit yöntem: Web, Crawl kaydını 2 saniyede bir okuyup hub'a yayınlar. Basit olanı seç ve gerekçesini README'ye yaz).
- Test edilebilirlik: HTTP katmanı için ICrawlerHttpClient arayüzü (gerçek + test için sahte implementasyon).
- JS render (Playwright) bu fazda YOK, Faz 6'da gelecek.

## Application
- StartCrawlCommand (manuel tetikleme), CancelCrawlCommand.
- GetCrawlsQuery (sayfalı), GetCrawlByIdQuery (aşama süreleri, hata alan URL'ler, robots ile atlananlar).
- GetPagesQuery: Bir taramadaki sayfalar; sayfa tipine, status code'a ve URL aramasına göre filtreli, sayfalı.
- Site'ın Schedule ayarı değişince recurring job güncellenir.

## Testler
- Unit: URL normalizasyonu, robots.txt kuralları, HTML parse (fixture HTML dosyalarıyla), Crawl durum geçişleri.
- Integration: Sahte HTTP client ile 20 sayfalık bir sitenin taranması; sayfa tiplerinin doğru atanması; exclude ve max pages uygulanması; aynı anda ikinci taramanın 409 dönmesi.
Bitti sayılır: Seed sitelerden biri için manuel tarama başlatılıp tamamlanabiliyor (gerçek siteyi taramadan önce hız limitini 1 istek/saniye yapın).
```

---

## Faz 3 — Kural motoru ve Bulgular

```
CLAUDE.md kurallarına uyarak Faz 3'ü yap.

## Kural motoru
- Kurallar kodda tanımlanır, her kural ayrı bir sınıf ve ayrı dosyadır. Her kural için:
  Code (ör. "seo.title.duplicate"), Category (Tracking, TechnicalSeo, Content, Performance), DefaultSeverity (Critical, High, Medium, Low), Scope (Page, Site), varsayılan eşik ayarları.
- İki arayüz: IPageRule (tek snapshot'ı değerlendirir) ve ISiteRule (taramadaki tüm snapshot'lara bakar; duplicate title, kırık iç link, sitemap tutarlılığı gibi).
- Site bazlı ayar: SiteRuleSetting (SiteId, RuleCode, IsEnabled, SeverityOverride, sayfa tipi bazında severity override, Thresholds jsonb). Ayar yoksa varsayılan geçerli.
- Taramadan sonra bir job tüm aktif kuralları çalıştırır. Sonuç: RuleViolation listesi (RuleCode, PageTypeId, Url, mevcut değer, detay).

## İlk kural paketi (her biri ayrı sınıf)
Teknik SEO: title eksik, title duplicate (site kuralı), title uzunluğu, meta description eksik, meta description duplicate, H1 eksik veya birden fazla, canonical eksik ya da başka bir domaine işaret ediyor, noindex olan ama sitemap'te bulunan sayfa, 4xx/5xx iç linkler, 3+ adımlı redirect zinciri, görsellerde alt etiketi eksik, sayfa tipi için zorunlu schema eksik (ör. Ürün sayfasında Product), zayıf içerik (eşik: kelime sayısı).
Performans (iç performans dokümanından): sıkıştırma yok, statik dosyalarda cache-control eksik, head'de bloklayan script, width/height verilmemiş görseller, ekran altındaki görsellerde lazy load eksik, preconnect eksik ya da fazla, HTML boyutu yüksek, TTFB yüksek (sayfa tipi bazında ortalama).
Her kuralın, önerilerde kullanılmak üzere bir ReferenceKey'i olur (ör. "perf-doc:p30"). Kaynak metinleri Faz 4'te bağlanacak.

## Finding (aggregate root)
- SiteId, RuleCode, PageTypeId (nullable), Fingerprint, Severity, Category, Status, FirstSeenAt, LastSeenAt, ResolvedAt, SnoozedUntil, FalsePositiveReason, AffectedCount.
- Gruplama: Aynı RuleCode + PageTypeId tek bir Finding'dir. Fingerprint = hash(SiteId, RuleCode, PageTypeId).
- FindingOccurrence (child): Url, CurrentValue, Detail, LastSeenCrawlId. Etkilenen sayfalar.
- Yaşam döngüsü (enum FindingStatus: Open, Resolved, Reopened, FalsePositive, Snoozed):
  - Taramada bulundu, kayıt yok -> Open.
  - Taramada bulunmadı, Open/Reopened idi -> Resolved (ResolvedAt).
  - Resolved idi, tekrar bulundu -> Reopened.
  - FalsePositive -> taramalar etkilemez. Kullanıcı geri alabilir.
  - Snoozed -> tarih gelene kadar durum değişmez, LastSeenAt güncellenir.
  - Kısmi tarama (PartiallyCompleted) bir bulguyu Resolved yapamaz. Bu kural önemli, test edilmeli.
- FindingHistoryEntry (child): Zaman, olay türü, açıklama. Görev detayındaki zaman çizelgesi bunu kullanır.
- Bu fazda bir domain event ihtiyacı doğuyor: FindingOpened ve FindingReopened, Faz 4'te görev üretmek ve Faz 7'de bildirim için. Basit bir yöntem kullan: Entity'de domain event listesi, SaveChanges sonrası MediatR INotification olarak publish. Outbox YOK.

## Application
- EvaluateCrawlCommand (job tarafından çağrılır): Kuralları çalıştırır, Finding'leri senkronize eder.
- GetFindingsQuery: Filtreler (severity, category, status, pageTypeId, ruleCode, sadece son taramada yeni, arama), sıralama, sayfalama. Liste ekranındaki özet sayılar için GetFindingSummaryQuery.
- GetFindingByIdQuery: Detay + occurrence'lar (sayfalı) + geçmiş.
- MarkFindingFalsePositiveCommand (gerekçe zorunlu, validator), RevertFalsePositiveCommand, SnoozeFindingCommand (gelecek bir tarih), BulkUpdateFindingsCommand (toplu ertele / yanlış alarm).
- Kural ayarları: GetRulesQuery (site için efektif ayarlarla), UpdateSiteRuleSettingCommand, PreviewRuleThresholdImpactQuery ("bu eşik değişikliği mevcut taramada X bulguyu etkiler").

## Testler
- Unit: Her kural için en az bir geçen ve bir kalan senaryo (fixture snapshot'larla). Finding yaşam döngüsünün tüm geçişleri, özellikle kısmi tarama kuralı.
- Integration: İki ardışık tarama: 1. taramada bulgu açılır, 2. taramada düzelir ve Resolved olur, 3. taramada geri gelir ve Reopened olur. Aynı sorun tekrar bulunduğunda yeni Finding açılmaz.
```

---

## Faz 4 — Görevler ve AI önerileri

```
CLAUDE.md kurallarına uyarak Faz 4'ü yap.

## WorkTask (aggregate root)
(Task adı System.Threading.Tasks ile çakıştığı için WorkTask.)
- SiteId, FindingId (bire bir), Key (site içinde artan, ör. "LB-142"; prefix site ayarından gelir), Title, Problem, Impact, Suggestion (yapılandırılmış: Kind = Text/Code/Steps, Content, örnek tablo jsonb), References, AssignedTeam (Frontend, Backend, Seo, Content), Status (Todo, InProgress, AwaitingVerification, Closed), ExternalTrackerKey, ExternalUrl.
- AcceptanceCriterion (child): Text, VerificationType (Automatic, TeamConfirmation), IsMet, MetAt.
- Kurallar: Automatic kriterler sadece sistem tarafından işaretlenir. Bağlı Finding Resolved olunca automatic kriterler sağlanır. Tüm kriterler sağlanınca görev Closed olur. Finding Reopened olursa görev tekrar Todo olur ve geçmişe not düşülür.

## Görev üretimi
- FindingOpened ve FindingReopened event'lerini dinleyen notification handler'lar görev oluşturur ya da günceller. Yeni görev açma kuralı: Finding başına tek görev.
- Görev metinleri iki aşamada üretilir:
  1. Şablon: Her kural için Title/Problem/AcceptanceCriteria şablonu (kural sınıfında ya da yanında). Somut değerler (sayfa sayısı, örnek URL'ler) yerleştirilir. AI olmadan da görev tam ve anlamlı olmalı.
  2. AI zenginleştirme (opsiyonel, Hangfire job): IAiSuggestionService arayüzü + Anthropic API implementasyonu (resmi Anthropic .NET SDK'sı). Structured output (JSON şema) ile Impact ve Suggestion üretir. Model ve API key konfigürasyondan gelir. Key yoksa adım atlanır, sistem çalışmaya devam eder.
- Bilgi tabanı: /knowledge klasöründeki iç dokümanlar (ör. web performans dokümanı) parçalara bölünüp ReferenceKey ile etiketlenir. AI'a sadece ilgili kuralın ReferenceKey'ine ait parçalar verilir. Vektör DB YOK; bu ölçekte anahtar eşleşmesi yeterli.
- AI çıktısı uydurma sayı içermemeli: Prompt'ta sadece verilen veriyi kullanması istenir; sayılar şablondan gelir.
- SuggestionFeedback: Kullanıcının "faydalı mı" geri bildirimi (iyi/kötü + not).

## Dış görev sistemi
- ITaskTracker arayüzü + JiraTaskTracker (Jira Cloud REST API v3) implementasyonu. Site bazlı ayar: Jira base URL, proje anahtarı, issue type, kimlik bilgileri (ASP.NET Core Data Protection ile şifreli saklanır).
- ExportTaskCommand: Görevi Jira'ya aktarır, ExternalTrackerKey'i kaydeder. Tekrar çağrılırsa yeni issue açmaz, mevcut issue'yu günceller.
- Jira'dan durum geri okuma bu fazda YOK.

## Application
- GetTasksQuery (durum, ekip, önem filtreleri; sayfalı), GetTaskByIdQuery, UpdateTaskStatusCommand, ConfirmAcceptanceCriterionCommand (sadece TeamConfirmation tipindeki kriterler), AssignTaskTeamCommand, AddTaskCommentCommand, SubmitSuggestionFeedbackCommand.

## Testler
- Unit: Kriter ve durum kuralları, şablon üretimi.
- Integration: Finding açılır -> görev oluşur; Finding çözülür -> automatic kriterler sağlanır; team kriteri onaylanınca görev kapanır; Reopened -> görev Todo. Jira için sahte ITaskTracker. AI için sahte IAiSuggestionService.
```

---

## Faz 5 — Google connector'ları

```
CLAUDE.md kurallarına uyarak Faz 5'i yap.

## ConnectorAccount (aggregate root)
SiteId, Type (SearchConsole, Ga4, Gtm, GoogleAds, Meta, MerchantCenter, OrderSource, TaskTracker), Status (Connected, NotConnected, PendingApproval, Expired, Error), ExternalAccountId, Scopes, encrypted credentials (Data Protection), LastSyncAt, LastError.

## Google OAuth
- Tek bir Google OAuth akışı. İstenen scope'lar salt okunurdur: webmasters.readonly, analytics.readonly, tagmanager.readonly. Refresh token şifreli saklanır.
- Bağlama akışı: Kullanıcı Google'a yönlenir, döner, hesaptaki uygun mülkler listelenir, kullanıcı siteye uygun mülkü seçer.
- Site sahipliği: Search Console'daki doğrulanmış mülk siteyle eşleşirse site.MarkOwnershipVerified(). Doğrulanmamış siteler için zamanlanmış tarama çalışmaz (Faz 2'deki job'a bu kontrolü ekle).

## Senkron job'ları (günlük, Hangfire)
- Search Console: Sayfa bazlı performans (tıklama, gösterim, CTR, pozisyon; son 28 gün), sitemap durumu, önemli sayfalar için URL Inspection (günlük kota sınırına uyarak, öncelik bulgulu sayfalarda).
- PageSpeed Insights / CrUX: Sayfa tipi başına örnek URL'ler için LCP, INP, CLS ve TTFB (mobil/masaüstü).
- GA4 Data API: Günlük e-ticaret event sayıları (view_item, add_to_cart, begin_checkout, purchase, login, sign_up, search, generate_lead) ve purchase value.
- Ham veri günlük tablolarda tutulur (SearchPerformanceDaily, WebVitalsDaily, Ga4EventDaily). Tekrar çalıştırmada upsert yapılır.
- Google istemcileri arayüz arkasında (test için sahte implementasyon). Kota ve 429 için retry (Polly ya da Microsoft.Extensions.Http.Resilience).

## Yeni kurallar (Faz 3 motoruna eklenir)
- "Fırsat" kuralı: 11–20. sırada ve gösterimi yüksek sayfalar (severity Low, kategori Content).
- Core Web Vitals eşik kuralları (sayfa tipi bazında).
- Index kuralları: URL Inspection'a göre indexlenmemiş önemli sayfalar.

## Application
- GetConnectorsQuery, StartGoogleConnectCommand, CompleteGoogleConnectCommand, SelectConnectorPropertyCommand, DisconnectConnectorCommand, TestConnectorCommand, TriggerConnectorSyncCommand.
- GetWebVitalsQuery (sayfa tipi bazında, 28 günlük trend), GetSearchPerformanceQuery.
```

---

## Faz 6 — Tracking denetimi

```
CLAUDE.md kurallarına uyarak Faz 6'yı yap.

## Tarayıcı denetimi (Playwright for .NET)
- Worker Docker imajına Playwright Chromium eklenir.
- Her sayfa tipi için temsilci URL'ler (örnek sayfalar + Ana sayfa, Sepet, Ödeme, Teşekkürler; bu son üçü site ayarında URL olarak tanımlanır) iki senaryoda açılır: onay vermeden ve onay vererek. Çerez onay butonunun seçicisi site ayarından gelir.
- Network istekleri dinlenir ve sınıflandırılır: GTM, GA4 (collect), Google Ads, Meta Pixel. Bulunan ID'ler, çağrı sayısı ve consent parametreleri (gcs/gcd) çıkarılır.
- Sonuç: TagAuditResult (CrawlId ya da AuditRunId, PageTypeKey, Vendor, Status: Ok, Missing, Duplicate, WrongId, FiresBeforeConsent; yakalanan istekler jsonb).
- Ödeme adımı simüle edilmez.

## Sipariş karşılaştırması
- IOrderSource arayüzü: GetDailyOrderStatsAsync(siteId, date) -> sipariş sayısı ve toplam tutar.
- İlk implementasyon: SqlServerOrderSource. Bağlantı cümlesi ve sorgu site ayarında tutulur, salt okunur kullanıcı beklenir, sorgu parametreli ve zaman aşımlıdır. Sorgu bir SELECT olmalı; başka komut reddedilir.
- OrderComparisonDaily: Tarih, OrderCount, OrderRevenue, Ga4PurchaseCount, Ga4PurchaseValue, GapPercent.

## Kurallar
- track.purchase.gap: Fark, site eşiğini (varsayılan %5) aşarsa (Critical).
- track.event.anomaly: Event sayısı 7 günlük ortalamaya göre eşikten fazla düştüyse ya da sıfıra indiyse.
- track.tag.missing, track.tag.duplicate, track.tag.wrong_id, track.consent.prefire.
- Meta: Pixel + CAPI aynı anda aktif mi, event_id ile dedup var mı. (Meta connector'ı bu fazda salt okunur Graph API ile; yoksa bu kurallar "veri yok" durumunda atlanır.)

## Application
- RunTrackingAuditCommand (manuel), GetTagAuditMatrixQuery, GetTagAuditCellQuery (drawer detayı), GetEventHealthQuery, GetOrderComparisonQuery (7/30 gün), GetTrackingScoreQuery.
```

---

## Faz 7 — Skorlar, bildirimler, raporlar

```
CLAUDE.md kurallarına uyarak Faz 7'yi yap.

## Skorlar
- ScoreCalculator (domain servisi, saf fonksiyon): Kategori bazında 100'den başlar, her açık bulgu severity ağırlığı (Critical 15, High 8, Medium 3, Low 1) × etkilenen sayfa oranı kadar düşürür; bulgu başına üst sınır vardır. Formül tek bir yerde durur ve testlerle sabitlenir.
- ScoreSnapshot: Her değerlendirme sonrası site + kategori + skor + tarih. Trend grafiği bunu kullanır.
- GetScoreBreakdownQuery: "Bu skor nasıl hesaplandı?" paneli için puanı en çok düşüren bulgular.

## Bildirimler
- Kanallar: E-posta (SMTP, MailKit), Teams webhook, Slack webhook. Her biri ayrı INotificationChannel implementasyonu.
- NotificationRule: Olay türü (yeni kritik bulgu, tekrar açılan bulgu, purchase farkı eşiği aştı, skor X puan düştü, tarama başarısız, connector yetkisi doldu) -> kanallar ve alıcılar. Sessiz saatler. Aynı bulgu için tekrar bildirim aralığı (spam önleme).
- Mevcut domain event'leri dinlenir. Gönderim Hangfire job'ında yapılır (istek akışını yavaşlatmaz).
- In-app bildirimler: UserNotification tablosu, okundu bilgisi, SignalR ile canlı sayaç.
- Test mesajı gönderme komutu.

## Özet raporlar
- Günlük/haftalık/aylık yönetici özeti (Razor template ile HTML e-posta): Skorlar ve değişimleri, en önemli 5 sorun, kapanan görevler, sipariş/purchase durumu.
- GetReportPreviewQuery: E-postanın HTML önizlemesi.

## Dashboard sorguları
- GetOverviewQuery: Genel Bakış ekranının ihtiyacı olan her şey tek sorguda ama okunabilir parçalar halinde (skorlar, kritik uyarı, öncelikli sorunlar, sipariş karşılaştırması, sayfa tipi durumu). Sorgu performansı için gerekli indeksleri ekle ve EXPLAIN ile kontrol et.
```

---

## Her fazın sonunda: Gözden geçirme promptu

```
Bu fazda yazılan kodu CLAUDE.md'ye göre gözden geçir:
- Her tip kendi dosyasında mı? Dosya adları tip adlarıyla aynı mı?
- Gereksiz soyutlama, kullanılmayan kod, "ileride lazım olur" kodu var mı? Varsa kaldır.
- Query'ler AsNoTracking + projection mı? Listeler sayfalı mı? N+1 sorgu var mı?
- Domain kuralları domain'de mi, yoksa handler'lara mı sızmış?
- Command'lar sadece ID ya da minimal sonuç mu dönüyor?
- Hata mesajları Türkçe ve anlamlı mı?
- Eksik test var mı? (Özellikle durum geçişleri ve sınır durumları.)
- docker compose up temiz bir ortamda çalışıyor mu?
Bulduklarını listele, sonra düzelt.
```
