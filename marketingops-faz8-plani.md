# Faz 8 — HTML denetimi, sıkı structured data, Render denetimi (erişilebilirlik ve mobil)

Durum: **Tamamlandı (2026-10-31).** Adım 1 `c928933`; adım 2 bu dosyayla birlikte commit'lendi. Kurallar: [CLAUDE.md](CLAUDE.md). Bu dosya konuşmada alınan kararların tek kaynağıdır;
uygulama sırasında karar değişirse önce burası güncellenir.

Kaynak fikir: Elevare CMS'teki SEO analizörü
(`Elevare/src/cms/Presentation/Wasm/Wasm/wwwroot/js/grapesjs/custom-plugins/elevare-seo.js`, v2.2). Dosya kopyalanmaz; ölçüm
mantığı alınır, MarketingOps'un kural → bulgu → görev → doğrulama akışına bağlanır. Elevare'in 100'den düşen puan formülü, rapor
penceresi, "Bul" düğmesi, canvas'a düşme, önizleme/postMessage hattı ve Elevare'e özel işaretler (`.el-slides`, `data-elevare-*`,
bağlı şablon notu, odak kelime, sayfa adresi alanı) alınmaz.

---

## 0. Temel ilkeler (bütün faz için)

### 0.1 Sıkılık
Kullanıcı kararı: **sistem sıkı olacak.** Somut karşılığı:

1. **Tek sayfa yeter.** Kural bir sayfada bile ihlal görürse bulgu açılır. "Sayfaların %X'inde" gibi taban eşik yok.
   (Bulgu yine sayfa tipi başına tektir: aynı kural + aynı sayfa tipi = tek bulgu. Etkilenen sayfa sayısı bulguda yazar.)
2. **Varsayılan açık.** Yeni kuralların hepsi her sitede açık başlar. Kapatmak Ayarlar > Kurallar'dan bilinçli bir işlemdir.
3. **Kapsam geniş.** SEO kuralları indekslenebilir sayfalara (`PageChecks.IsIndexable`), erişilebilirlik ve mobil kuralları
   indekslenebilir olsun olmasın **bütün HTML sayfalara** (`PageChecks.IsHtml`) bakar. Ziyaretçi noindex sayfayı da görür.
4. **Structured data her indekslenebilir sayfada zorunlu.** Sayfa tipine "beklenen schema" tanımlanmamış olsa bile hiç
   structured data'sı olmayan tek bir indekslenebilir sayfa bile bulgu açar (bkz. 2.3).
5. **Önem dereceleri yüksek tutulur.** Elevare'de ✕ (fail) olan kontroller High, ! (warn) olanlar en az Medium başlar.
   Sıkılık bulgunun açılmasındadır; skor cezası 0.4'teki formülle orantılı kalır.
6. **Kısmi veri bulgu çözmez.** Mevcut ilke korunur: kısmi tarama ya da eksik render verisi hiçbir bulguyu "çözüldü" yapmaz.
7. **Eski veriyle yanlış alarm yok.** Yeni alanlar eski snapshot'larda yoktur; yeni kurallar eski snapshot'lara bakmaz
   (bkz. 1.1 `Version`).

### 0.2 Mimari kurallar (CLAUDE.md'den, burada tekrar)
- Her tip ayrı dosya; dosya adı = tip adı. Kurallar `Domain/Rules/Definitions` altında, her biri ayrı sınıf, `RuleCatalog`'a eklenir.
- Over-design yok: yeni soyutlama yalnızca dış sistem test için ayrılacaksa (`IRenderAuditor`, Playwright).
- Query'ler `AsNoTracking` + projection; command'lar ID döner; hata mesajları Türkçe.
- Arayüz Türkçe, enum → metin yalnızca `Web/Infrastructure/DisplayText.cs`.
- Her backend parçası aynı adımda ekrana bağlanır ve tarayıcıda akış olarak doğrulanır.
- Salt okunur ilke: render denetimi siteye yalnızca GET atar, form göndermez, tıklama yalnızca çerez onayı içindir.

### 0.3 Yeni kategori
- `FindingCategory.Accessibility` eklenir. Arayüz metni: **"Erişilebilirlik ve mobil"**.
- `ScoreCategory.Accessibility` eklenir. Arayüz metni: **"Erişilebilirlik ve Mobil Sağlığı"**.
- `ScoreCategoryMap`: `Accessibility` ↔ `FindingCategory.Accessibility`.
- Enum'lar veritabanında string tutuluyor (`HasConversion<string>()`, en fazla 20 karakter; "Accessibility" 13), migration gerekmez.
- Kategori **1. adımda** eklenir; çünkü HTML'den yapılan erişilebilirlik kuralları da bu kategoriye girer.

### 0.4 Skor formülü (uygulama sırasında değişti, kullanıcı onayı 2026-10-31)
Lastik Borsası doğrulamasında SEO skoru 74'ten 0'a düştü. Sebep Faz 7 formülünün bulguyu **sayfa tipi başına** cezalandırması:
şablondan gelen tek sorun 7 sayfa tipinde 7 kez sayılıyordu (Performans skoru Faz 8'den önce de bu yüzden 20'ydi).
Yeni formül (`ScoreCalculator`, testli):
- Site skorunda oran = **sitede etkilenen sayfa / sitenin bütün HTML (2xx) sayfaları** (sayfa tipine uymayanlar dahil).
- Bir kuralın bütün sayfa tiplerindeki bulguları birlikte en fazla o kuralın en yüksek önem ağırlığı kadar düşürür; aşarsa
  bulguların payı orantılı küçülür.
- Sayfa tipi skoru (Genel Bakış'taki tip tablosu) oranı o tipin kendi sayfa sayısından hesaplamaya devam eder.
- Oran yalnızca taranan sayfalara dayanan kurallarda uygulanır (veri kaynağı Crawl, SearchConsole, UrlInspection); sayfa
  tipine uymayan tek sayfa da kendi payı kadar düşürür. Örnek sayfa ölçen kaynaklar (Tracking, PageSpeed) ve sayfaya
  dayanmayan bulgular (purchase farkı) tam ağırlık düşürür, kural sınırı yine geçerlidir; Render ve PageSpeed 2.7'de
  örnek oranına geçer.
Aynı veriyle: SEO 0 → 74, Erişilebilirlik 0 → 78, Performans 6 → 84. URL bazında skor yok (kullanıcı: gerek yok).

---

## 1. Adım 1 — HTML denetimi (crawler, bütün sayfalar)

Crawler sayfanın HTML'ini indirir ama saklamaz; `HtmlSnapshotParser` belirli alanları `PageSnapshotData`'ya (jsonb) yazar.
Bu yüzden her yeni kontrol iki parçadır: **parser veriyi çıkarır, kural değerlendirir.** `PageSnapshotData` jsonb olduğu için
yeni alanlar migration istemez.

### 1.1 Parser ve snapshot verisi

**Sürüm:**
- `PageSnapshotData.Version` (int). Parser yeni alanları yazınca `Version = 2`. Eski kayıtlarda 0 gelir.
- `PageChecks.HasMarkupFacts(page)` → `page.Data.Version >= 2`. Yeni kuralların hepsi önce buna bakar; değilse çalışmaz.
  Böylece "Kuralları çalıştır" ile eski bir tarama değerlendirilince eksik alan yüzünden yanlış bulgu açılmaz.

**Örnek kayıtları:** Her sorun için sayının yanında en fazla **5 örnek** saklanır (snapshot büyümesin). Örnek, ekipte
anlaşılır kısa bir tanımdır (uygulamada sayı ve örnekler tek tipte: `ElementIssue { Count, Samples }`) → ör. `a[href="#"] "Sepete ekle"`,
`input[name=email][type=email]`, `iframe src=https://www.youtube.com/embed/...`. En fazla 160 karakter.

**Genel dışlamalar (HTML kurallarının hepsinde):** `<template>`, `<noscript>`, `hidden` öznitelikli öğeler ve
`aria-hidden="true"` alt ağaçları sayılmaz. CSS ile gizlenen öğe HTML'den bilinemez; bunun yanlış alarmı render adımında
düzelir (bkz. 2.9).

**Yeni tipler (her biri ayrı dosya, `Domain/Crawls/`):**

| Tip | Alanlar |
|---|---|
| `ElementSample` | `Description` |
| `OpenGraphInfo` (genişler) | mevcut `Title, Description, Image, Type, Url` + `ImageWidth (string?)`, `ImageHeight (string?)`, `ImageAlt (string?)` |
| `StructuredDataInfo` | `JsonLdBlocks`, `InvalidJsonLdBlocks`, `InvalidSamples (list<ElementSample>)`, `Nodes (list<SchemaNode>)`, `MicrodataTypes (list<string>)` |
| `SchemaNode` | `Type` (string), `Properties` (düzleştirilmiş, dolu olan yollar: `name`, `offers.price`, `offers.priceCurrency`, `itemListElement.item` …) |
| `LinkFacts` | `NoText`, `NoTextSamples`, `EmptyHref`, `EmptyHrefSamples`, `BlankWithoutNoopener`, `BlankSamples`, `ShortTexts (list<LinkTextCount>)` |
| `LinkTextCount` | `Text` (küçük harf, kırpılmış), `Count` |
| `HeadingFacts` | `Levels (list<int>)` belge sırasıyla, en fazla 300; `Empty` (metni boş başlık sayısı), `EmptySamples` |
| `FormFacts` | `Fields`, `Unlabeled`, `UnlabeledSamples` |
| `TableFacts` | `Tables`, `WithoutHeader`, `Samples` |
| `IframeFacts` | `Iframes`, `WithoutTitle`, `WithoutTitleSamples`, `NotLazy`, `NotLazySamples` |
| `LandmarkFacts` | `Main`, `Header`, `Nav`, `Footer`, `UnlabeledNavs` |

`PageSnapshotData`'ya eklenen alanlar: `Version`, `StructuredData`, `Links`, `Headings`, `Forms`, `Tables`, `Iframes`,
`Landmarks`. Mevcut `SchemaTypes` korunur (sayfa tipi eşleşmesi kullanıyor), `StructuredData` ondan bağımsız doldurulur.

**Çıkarma kuralları (parser):**
- **og:** `og:image:width`, `og:image:height`, `og:image:alt` meta'ları da okunur.
- **JSON-LD:** her `script[type=application/ld+json]` bloğu ayrı ayrı parse edilir. Bozuk blok `InvalidJsonLdBlocks`'a sayılır,
  örneğine ilk 80 karakteri yazılır. Geçerli bloklarda dizi, `@graph` ve iç içe düğümler gezilir; `@type` dizisi her tip için
  ayrı `SchemaNode` üretir. Özellik yolları en fazla 3 seviye derinlikte düzleştirilir; değeri boş string/boş dizi/null olan
  özellik "dolu" sayılmaz. Bir sayfada en fazla 40 düğüm saklanır.
- **Microdata:** üst düzey `itemtype`'lar `MicrodataTypes`'a yazılır (varlık sayılır; özellik doğrulaması yalnızca JSON-LD'de).
- **Link metni:** erişilebilir ad sırası: `aria-labelledby` (id'lerin metni) → `aria-label` → görünür metin (iç `img[alt]`,
  `svg > title` dahil) → `title`. Ad boşsa `NoText`.
- **Boş hedef:** `href` yok, boş, `#`, `javascript:` ile başlayan. İstisna: `aria-current="page"` olup `href`'i olmayan
  (breadcrumb'ın son adımı) ve `role="button"` olanlar (buton gibi davranan link ayrı bir konu; Elevare de saymıyor).
- **`_blank`:** `target="_blank"` olup `rel` içinde `noopener` ya da `noreferrer` geçmeyen.
- **Kısa link metinleri:** 40 karakterden kısa link metinleri küçük harfe çevrilip sayılır, en fazla 100 farklı metin.
  Jenerik metin listesi Domain'dedir (kural), parser liste bilmez.
- **Başlıklar:** `h1`–`h6` ve `role="heading"`+`aria-level` belge sırasıyla.
- **Form alanı etiketi:** `input` (hidden, submit, button, reset, image hariç), `select`, `textarea`. Etiketli sayılır:
  `label[for=id]`, saran `label`, `aria-label`, `aria-labelledby`, `title`. Placeholder etiket **sayılmaz**.
- **Tablo:** `role="presentation"`/`role="none"` olanlar hariç; içinde `th` yoksa `WithoutHeader`.
- **Iframe:** `title` boşsa `WithoutTitle`; `loading="lazy"` değilse `NotLazy`.
- **Landmark:** `main`/`[role=main]`, `header`, `nav`/`[role=navigation]`, `footer` sayıları; birden fazla nav varsa
  `aria-label`/`aria-labelledby`'sı olmayanlar `UnlabeledNavs`.

### 1.2 Yeni kurallar (HTML, `RuleDataSource.Crawl`)

Önem kolonundaki değer varsayılandır; site ve sayfa tipi bazında Ayarlar > Kurallar'dan değişir (mevcut altyapı).

| # | Kod | Ad (arayüz) | Kategori | Önem | Kapsam | Ne zaman ihlal | Eşik |
|---|---|---|---|---|---|---|---|
| 1 | `seo.schema.missing` | Structured data yok | TechnicalSeo | **High** | indekslenebilir | Geçerli JSON-LD düğümü de microdata da yok | — |
| 2 | `seo.schema.invalid` | Structured data bozuk | TechnicalSeo | **High** | HTML | En az bir JSON-LD bloğu parse edilemiyor | — |
| 3 | `seo.schema.properties` | Structured data'da zorunlu alan eksik | TechnicalSeo | **High** | indekslenebilir | Bilinen tiplerden birinde zorunlu alan eksik (tablo 1.3) | — |
| 4 | `seo.schema.site_identity` | Ana sayfada site kimliği yok | TechnicalSeo | Medium | site (ana sayfa) | Ana sayfada `Organization` (ya da alt tipi: `OnlineStore`, `LocalBusiness`, `Corporation`) **ve** `WebSite` yok | — |
| 5 | `seo.schema.breadcrumb` | BreadcrumbList yok | TechnicalSeo | Medium | indekslenebilir, ana sayfa hariç | `BreadcrumbList` düğümü yok | — |
| 6 | `social.og.incomplete` | Paylaşım etiketleri eksik | TechnicalSeo | Medium | indekslenebilir | `og:title`, `og:description`, `og:image`, `og:url`, `og:type`'tan biri boş ya da `og:image`/`og:url` mutlak http(s) adres değil | — |
| 7 | `social.og.image_meta` | Paylaşım görselinin bilgisi eksik | TechnicalSeo | Low | indekslenebilir, og:image dolu | `og:image:width`/`height` yok, eşiğin altında ya da `og:image:alt` boş | min genişlik 1200, min yükseklik 630 |
| 8 | `social.og.url_mismatch` | Paylaşım adresi canonical'la uyuşmuyor | TechnicalSeo | Medium | indekslenebilir, ikisi de dolu | `og:url` ≠ canonical (fragment ve sondaki `/` yok sayılarak, küçük harf) | — |
| 9 | `seo.meta_description.length` | Meta description uzunluğu | Content | Medium | indekslenebilir, meta dolu | Eşik dışında | min 70, max 160 |
| 10 | `a11y.link.no_text` | Erişilebilir adı olmayan link | Accessibility | **High** | HTML | `LinkFacts.NoText > 0` | — |
| 11 | `seo.link.generic_text` | Jenerik link metni | Content | Medium | indekslenebilir | Kısa metinlerden biri jenerik listede | — |
| 12 | `seo.link.empty_href` | Hedefi olmayan link | TechnicalSeo | Medium | HTML | `EmptyHref > 0` | — |
| 13 | `sec.link.blank_noopener` | `_blank` link `noopener`'sız | TechnicalSeo | Low | HTML | `BlankWithoutNoopener > 0` | — |
| 14 | `a11y.heading.skip` | Başlık seviyesi atlanıyor ya da boş başlık | Accessibility | Medium | HTML | Sırada bir seviye atlanıyor (H2 → H4) ya da metni boş başlık var | — |
| 15 | `a11y.table.no_th` | Başlık hücresi olmayan tablo | Accessibility | Medium | HTML | `WithoutHeader > 0` | — |
| 16 | `a11y.iframe.no_title` | Başlığı olmayan iframe | Accessibility | Medium | HTML | `WithoutTitle > 0` | — |
| 17 | `a11y.form.no_label` | Etiketi olmayan form alanı | Accessibility | **High** | HTML | `Unlabeled > 0` | — |
| 18 | `a11y.landmarks.missing` | Sayfa bölgeleri tanımsız | Accessibility | Medium | HTML | `main` tam olarak 1 değil, ya da `header`/`nav`/`footer`'dan hiçbiri yok | — |
| 19 | `a11y.nav.unlabeled` | Birden fazla menü etiketsiz | Accessibility | Low | HTML | Birden fazla nav var ve en az biri etiketsiz | — |
| 20 | `mobile.viewport.invalid` | Mobil viewport eksik ya da yakınlaştırmayı engelliyor | Accessibility | **High** | HTML | `meta viewport` yok, `width=device-width` içermiyor, `user-scalable=no` ya da `maximum-scale` < 2 | — |
| 21 | `a11y.lang.missing` | Sayfa dili tanımsız | Accessibility | Medium | HTML | `<html lang>` yok ya da geçerli BCP 47 kodu değil (`tr`, `tr-TR`, `en` …) | — |
| 22 | `perf.iframe.lazy_missing` | Iframe'lerde lazy load eksik | Performance | Medium | HTML | `NotLazy > 0` | — |

**Jenerik link metni listesi** (kuralın içinde sabit; sayfa dili `tr` ise ikisi birden, değilse İngilizce):
- tr: tıkla, tıklayın, buraya tıklayın, buraya tıkla, devamı, devamını oku, daha fazla, daha fazlası, detay, detaylar, incele,
  link, burada, burası, git, tümü, hepsi
- en: click here, here, read more, more, link, this, learn more, details, click, go, page

**Kurallara yazılacak somut değerler (bulgudaki "mevcut değer / ayrıntı"):**
- `seo.schema.missing`: mevcut "Structured data yok", ayrıntı sayfa tipi.
- `seo.schema.invalid`: mevcut "N bozuk blok", ayrıntı örnekler.
- `seo.schema.properties`: mevcut "Product: offers eksik", ayrıntı eksik alanların tam listesi.
- Link/form/iframe/tablo kuralları: mevcut "N öğe", ayrıntı en fazla 5 örnek.
- `a11y.heading.skip`: mevcut "H2 → H4", ayrıntı seviye dizisi (ilk 20).

### 1.3 Zorunlu structured data alanları (`seo.schema.properties`)

Tablo Domain'de tek yerde durur (`SchemaRequirements`, ayrı dosya). Kaynak: Google'ın zengin sonuç belgeleri. "veya" grubu:
alanlardan biri yeter.

| Tip | Zorunlu |
|---|---|
| `Product` | `name`, `image`; ve (`offers` veya `review` veya `aggregateRating`) |
| `Product.offers` (`Offer`) | `price` (veya `priceSpecification.price`), `priceCurrency`, `availability` |
| `Product.offers` (`AggregateOffer`) | `lowPrice`, `priceCurrency` |
| `BreadcrumbList` | `itemListElement`, `itemListElement.position`, (`itemListElement.name` veya `itemListElement.item.name`) — Lastik Borsası doğrulamasında adı `item.name`'de veren geçerli işaretleme yanlış alarm verdi, düzeltildi. Yollar dizideki öğelerin birleşimi olduğundan "son öğe dışında item" denetlenmez. |
| `Organization` (ve alt tipleri) | `name`, `url`, `logo` |
| `WebSite` | `name`, `url` |
| `Article` / `NewsArticle` / `BlogPosting` | `headline`, `image`, `datePublished`, `author` |
| `FAQPage` | `mainEntity`; her `Question`'da `name` ve `acceptedAnswer.text` |
| `ItemList` | `itemListElement` |
| `LocalBusiness` | `name`, `address` |

Tabloda olmayan tiplerin alanı denetlenmez (yalnızca varlıkları `seo.schema.missing`'i karşılar).

### 1.4 Mevcut kuralların sıkılaştırılması

| Kural | Bugün | Yeni | Neden |
|---|---|---|---|
| `content.image.alt_missing` | Low | **Medium** | Elevare'de fail; ekran okuyucu için temel |
| `seo.title.length` | Low, 30–60 | **Medium**, 30–60 | Sıkılık |
| `seo.meta_description.duplicate` | Low | **Medium** | Sıkılık |
| `perf.image.lazy_missing` | Low | **Medium** | Sıkılık; render verisiyle ilişkisi 2.8'de |
| `content.thin` | Medium, 250 kelime | Medium, **300** kelime | Elevare eşiği |
| `seo.h1.missing_or_multiple` | Medium | değişmedi | Parser boş H1'i zaten saymıyor (H1 yok sayılır); boş başlıklar ayrıca `a11y.heading.skip`'te |
| `seo.schema.required_missing` | High | aynı | Sayfa tipine tanımlı beklenen tipler için kalır; genel zorunluluk `seo.schema.missing`'te |

Not: Elevare'deki "3'ten fazla eager görsel" kontrolünün karşılığı mevcut `perf.image.lazy_missing`'dir (ilk N görselden
sonrası lazy değilse ihlal, N=3); ayrı kural açılmaz.

### 1.5 Skor ve ekranlar (adım 1)
- `ScoreCategory.Accessibility` ve eşlemesi (0.3).
- `RecordScoreSnapshotsCommandHandler`, `SummaryReportFactory`, bildirimler `Enum.GetValues<ScoreCategory>()` ile döndüğü için
  dördüncü kategori otomatik girer; `NotificationText` ve `DisplayText` switch'leri güncellenir.
- **Genel Bakış:** skor kartları 4'e çıkar (`xl:grid-cols-4`); `OverviewTrendPoint`'e `Accessibility` alanı, trend grafiğine
  dördüncü çizgi (tasarım sistemine uygun tek ek renk, `design-system/MASTER.md`'ye bakılarak); "Bu skor nasıl hesaplandı?"
  paneli dört kategori için çalışır.
- **Bulgular:** kategori filtresine "Erişilebilirlik ve mobil" gelir (enum'dan geldiği için `DisplayText` yeterli; doğrulanır).
- **Ayarlar > Kurallar:** yeni kategori bölümü; yeni kuralların eşik alanları (og görsel boyutu, meta uzunluğu).
- **Tasarım vitrini** (`/_design`): yeni kategori rozeti.

### 1.6 Bilgi tabanı (AI önerileri)
- Yeni anahtarlar ve dosyalar:
  - `knowledge/erisilebilirlik.md`: `a11y:links`, `a11y:forms`, `a11y:headings`, `a11y:tables`, `a11y:iframes`,
    `a11y:landmarks`, `a11y:lang`, `a11y:contrast`, `a11y:text-size`, `a11y:tap-targets`, `mobile:viewport`, `mobile:reflow`.
  - `knowledge/google-search.md` genişler: `social:open-graph`, `google:breadcrumbs`, `google:organization`.
- **Kullanıcının SEO dokümanı** (`Dokümanlarım/Seo Optimizasyonu Üzerine.pdf`) bilgi tabanına işlenir: Open Graph, Structured
  Data, Canonical/Alternate/x-default, ARIA-Label, sayfa içi SEO bölümleri özgün kısa özetler halinde, ilgili kuralların
  anahtarları altına (`social:open-graph`, `google:structured-data`, `google:canonical`, `a11y:links` …) ek parça olarak yazılır.
  Kopya metin değil, kısa özgün özet; sayı içermez. Dosya: `knowledge/seo-dokumani.md`.
- Her yeni kural `ReferenceKey`, `Impact`, `Suggestion` (metin/kod/adımlar), `Team`, `TeamCriterion` taşır.

### 1.7 Testler (adım 1)
- `HtmlSnapshotParserTests`: her yeni alan için fixture parçası (`Fixtures/` altına `markup-page.html`): bozuk JSON-LD, `@graph`,
  iç içe offers, link adı kaynakları (aria-labelledby, img alt, svg title), `aria-hidden`, `template`, placeholder'lı input,
  `role=presentation` tablo, birden fazla nav, viewport varyantları.
- Her kural için `RuleTests` desenine uygun birim testi: ihlal var / yok / sınır değer / eski sürüm snapshot (Version 0) → bulgu yok.
- `SchemaRequirements` için tablo testleri (her tip, "veya" grupları, AggregateOffer).
- `ScoreCategoryMap` ve `ScoreCalculator`: dördüncü kategori.
- Entegrasyon: test sitesi fixture'ında bilinen ihlallerle tarama → beklenen bulgu kodları açılıyor, düzeltilmiş ikinci taramada
  çözülüyor; eski snapshot'la "Kuralları çalıştır" yeni kuralları tetiklemiyor.
- Ekran testi (entegrasyon): Genel Bakış'ta 4 skor kartı ve "Erişilebilirlik ve Mobil Sağlığı" metni.

### 1.8 Adım 1 doğrulama
- `dotnet build`, `dotnet test` (birim + entegrasyon) yeşil.
- Lastik Borsası yerel taramasıyla tarayıcıda: Genel Bakış 4 skor, Bulgular'da yeni kategori ve kurallar, bir bulgu detayında
  örnekler, Ayarlar > Kurallar'da yeni kurallar ve eşikler.

---

## 2. Adım 2 — Render denetimi (Playwright, örnek sayfalar)

Tarayıcı gerektiren kontroller. Projede boş duran `CrawlStage.Render` aşaması doldurulur. Elevare'in ölçüm fonksiyonları
(`classifyImages`, `overflowOffenders`, kontrast hesapları, `distorted`, tap target, küçük yazı) arayüzsüz bir betiğe taşınır.

### 2.1 Ne zaman, hangi sayfalar
- Tarama indirme aşaması bitince, kural değerlendirmesinden **önce** `Render` aşaması çalışır:
  Discovery → Download → **Render** → RuleEvaluation.
- **Örnekleme:** her sayfa tipinden `RenderSamplesPerPageType` kadar sayfa (varsayılan **3**, 1–10), ek olarak ana sayfa her zaman.
  Seçim: o taramada 2xx dönen, yönlendirmesiz HTML sayfalar; URL uzunluğuna göre sıralanıp eşit aralıklarla seçilir
  (hep en kısa URL'ler değil, tipin farklı örnekleri). Eşleşmeyen sayfalar render edilmez.
- **Cihazlar** (her sayfa 3 kez açılır, ayrı tarayıcı bağlamıyla — iframe genişletme yerine gerçek cihaz öykünmesi):
  - Masaüstü 1366×900
  - Tablet 768×1024
  - Mobil 375×812, `IsMobile = true`, `HasTouch = true`, `DeviceScaleFactor = 2`
- Hız: sitenin `MaxRequestsPerSecond` ayarı render'a da uygulanır; aynı anda tek sayfa. robots.txt'ye uyulur (render edilen
  sayfa crawler'ın zaten indirdiği sayfadır). User-Agent: `MarketingOpsBot/1.0 (render)`.
- Çerez onayı: sitenin `TrackingJourney.ConsentButtonSelector` ayarı varsa tıklanır (tracking denetimiyle aynı). Yoksa
  tıklanmaz; sayfanın üstünde viewport'un %30'undan fazlasını kaplayan `position: fixed` katman varsa örnek
  "ölçüm engellenmiş olabilir" diye işaretlenir ve fold/kontrast sonuçları o örnek için kullanılmaz.
- Zaman aşımı: sayfa başına 30 sn (`DOMContentLoaded` + en fazla 5 sn ağ sakinleşmesi + 300 ms yerleşme, fontlar hazır).
- Render başarısız olursa tarama **başarısız olmaz**: aşama "tamamlanamadı" olarak işaretlenir, Render kaynaklı kurallar bu
  taramada çalışmaz (bulgular korunur, çözülmez).

### 2.2 Ayarlar
`CrawlSettings`'e (owned, migration ile kolon):
- `RenderEnabled` (bool, varsayılan **true**) — sıkılık ilkesi.
- `RenderSamplesPerPageType` (int, varsayılan 3, 1–10; dışında `DomainException`, Türkçe mesaj).
- Ayarlar > Siteler > Genel'de iki alan; tahmini süre metni ((sayfa tipi × örnek + ana sayfa) × 3 cihaz × ~7 sn; Lastik Borsası ölçümüne göre).

### 2.3 Veri
- Yeni entity `RenderSample` (aggregate root değil; tarama çıktısı, `PageSnapshot` gibi): `Id`, `CrawlId`, `SiteId`, `Url`,
  `PageTypeId`, `Device` (`RenderDevice` enum: Desktop, Tablet, Mobile), `MeasuredAt`, `Status` (`RenderStatus`: Measured,
  Obstructed, Failed), `Error`, `Measurements` (jsonb, `RenderMeasurements`).
- `RenderMeasurements` (her biri ayrı record dosyası):
  - `ViewportWidth`, `DocumentWidth` (yatay taşma), `OverflowOffenders (list<RenderElement>)`
  - `ContrastFailures (list<ContrastFailure>)`: seçici, oran, gereken oran, metin örneği (40 karakter)
  - `SmallText (list<RenderElement>)` + font boyutu
  - `SmallTapTargets (list<RenderElement>)` + genişlik/yükseklik
  - `LazyAboveFold`, `EagerBelowFold` (list<RenderElement>, görsel src)
  - `DistortedImages (list<RenderElement>)` + doğal/görünen oran
  - `WideElements (list<RenderElement>)` (mobilde viewport'tan geniş öğeler)
- `RenderElement`: `Selector` (kısa CSS yolu, en fazla 3 ata + id/class, 160 karakter), `Text` (40 karakter), `Value` (ölçü).
- Listeler en fazla 20 öğe; sayılar ayrıca tutulur.
- Tablo `render_samples`; indeks `(crawl_id)`, `(site_id, crawl_id, page_type_id)`.
- Saklama: `Crawling:KeepSnapshotsForCrawls` ile aynı; günlük temizlik işi render örneklerini de siler.
- `RuleDataSource.Render` eklenir. `ExternalData`'ya `RenderSamples (list<RenderSampleData>)` eklenir; taramanın `Measured`
  örneği yoksa `Available` içinde `Render` olmaz → render kuralları çalışmaz.

### 2.4 Betik (`render-audit.js`)
- Yer: `src/MarketingOps.Infrastructure/Rendering/render-audit.js`, gömülü kaynak (embedded resource).
- Tek fonksiyon: `(options) => RenderMeasurements` JSON; Playwright `EvaluateAsync` ile çağrılır. Sayfanın kendi
  betikleri çalışmış haldedir (gerçek ziyaretçi görünümü).
- Elevare'den taşınanlar ve değişiklikler:
  - **Kontrast:** `parseColor`, `luminance`, `contrastRatio`, `blend`, `effectiveBg` aynen. WCAG AA: normal 4.5, büyük (≥24 px ya
    da ≥18.66 px ve kalın) 3. Arka planı görsel olan öğe atlanır. Tarama sınırı 1500 → **5000**. `disabled` öğeler ve
    görünmez (sr-only: 1×1 clip) öğeler atlanır. Koyu tema yalnızca sitede `data-theme` varsa denenir (genelde yok).
  - **Küçük yazı:** < eşik px (varsayılan 12). Her cihazda.
  - **Tap target:** yalnızca mobil cihazda; `display:inline` linkler (metin içi) hariç; < eşik px (varsayılan **24**, WCAG 2.5.8).
  - **Fold:** `classifyImages` mantığı; `.el-slides` yerine genel karusel tespiti: atası `overflow: hidden|clip` olan ve
    yatayda viewport dışında kalan görsel "ilk ekranda değil" sayılır; `opacity:0`/`visibility:hidden`/`display:none` ataları
    zaten dışlanıyor. `loading` yok = eager (Elevare düzeltmesi aynen).
  - **Yatay taşma:** `documentElement.scrollWidth > clientWidth + 4`; suçlu öğelerden, atası kırpan (`overflow` hidden/clip)
    olanlar çıkarılır (karusel izleri yanlış suçlanmasın).
  - **Geniş öğe:** mobilde hesaplanan genişliği viewport'tan büyük öğeler (Elevare'in "inline style 500px+" kontrolünün
    gerçek ölçüme dayalı hali).
  - **En-boy bozulması:** `distorted` aynen (%2 tolerans, `object-fit` fill dışı ve SVG hariç).
  - **Alınmayan:** `@media` kontrolü (başka origin'deki CSS okunamaz; gerçek sorunu taşma kontrolü yakalar).
- Betik siteye hiçbir şey yazmaz; DOM'u değiştirmez (yalnızca okur).

### 2.5 Application / Infrastructure
- `Application/Rendering/IRenderAuditor` (dış sistem; testte sahte uygulanır): `Task<IReadOnlyList<RenderResult>>
  AuditAsync(IReadOnlyList<RenderTarget> targets, RenderOptions options, CancellationToken)`.
- `Infrastructure/Rendering/PlaywrightRenderAuditor`: tek Chromium süreci, cihaz başına bağlam, sayfa sayfa; betiği gömülü
  kaynaktan okur; hız limitini uygular.
- `Application/Crawls/Commands/RenderCrawl/RenderCrawlCommand` + Handler: örnekleri seçer, auditor'ı çağırır, `RenderSample`
  yazar, aşama zamanlarını işler.
- `Infrastructure/Jobs/RenderCrawlJob` (Hangfire, `crawls` kuyruğu): tarama bitince `EnqueueEvaluation` yerine
  render açıksa `EnqueueRender` → render bitince `EnqueueEvaluation`. Render kapalıysa doğrudan değerlendirme.
- Kesinti: iş yeniden çalışırsa ölçülmüş (crawl, url, cihaz) atlanır.
- `EvaluateCrawlCommandHandler`: `ExternalData`'ya render örneklerini yükler.

### 2.6 Render kuralları (`RuleDataSource.Render`, `ISiteRule`, sayfa tipi başına bulgu)

| # | Kod | Ad | Kategori | Önem | İhlal | Eşik |
|---|---|---|---|---|---|---|
| R1 | `mobile.overflow` | Mobilde yatay kaydırma | Accessibility | **High** | Mobilde belge genişliği viewport'u aşıyor | tolerans 4 px |
| R2 | `mobile.wide_element` | Mobilde ekrandan geniş öğe | Accessibility | Medium | Mobilde viewport'tan geniş öğe var | — |
| R3 | `a11y.contrast` | Yetersiz renk kontrastı | Accessibility | **High** | WCAG AA altı metin var | normal 4.5, büyük 3.0 |
| R4 | `a11y.text.small` | Çok küçük yazı | Accessibility | Medium | Eşikten küçük metin var | 12 px |
| R5 | `mobile.tap_target.small` | Küçük dokunma alanı | Accessibility | Medium | Mobilde eşikten küçük link/buton | 24 px |
| R6 | `perf.image.lazy_above_fold` | İlk ekrandaki görsel lazy | Performance | **High** | Herhangi bir cihazda ilk ekranda görünen görsel `loading=lazy` | — |
| R7 | `perf.image.eager_below_fold` | Ekran altındaki görsel hemen yükleniyor | Performance | Medium | Herhangi bir cihazda ekran altındaki görsel lazy değil | — |
| R8 | `content.image.distorted` | Görsel orantısız gösteriliyor | Content | Medium | Doğal orandan %2'den fazla sapma | %2 |

Bulgudaki değerler: mevcut değer = "375 px'de 412 px (mobil)", "2,9:1 (gereken 4,5:1)", "11 px", "20×18 px"; ayrıntı =
cihaz(lar) + en fazla 5 seçici ve metin örneği. Etkilenen sayfalar listesinde her örnek URL ayrı satır.

### 2.7 Skor: örneklenmiş veri
Sorun: skor oranı "etkilenen sayfa / sayfa tipinin sayfa sayısı". Render 3 örnek ölçer; 10.000 sayfalık tipte 3/10.000 oranı
skoru neredeyse hiç düşürmez, oysa örneklenen sorun şablon sorunudur.
Karar: **örneklenmiş kaynaklarda (Render, PageSpeed) oran = etkilenen örnek / ölçülen örnek** (aynı sayfa tipinde).
0.4'teki site oranına uyarlanışı: örneklenen tipin bulgusu sitede `(etkilenen örnek / ölçülen örnek) × tipin sayfa sayısı`
kadar sayfayı temsil eder; `ScoreReader` bu kaynakların bulgularında etkilenen sayıyı buna çevirir. Mevcut Core Web Vitals bulguları da aynı düzeltmeden yararlanır (bugün aynı sorunu taşıyorlar). Formül tek yerde kalır,
testle sabitlenir.

### 2.8 `perf.image.lazy_missing` ile ilişki
Sezgisel kural ("ilk 3 görselden sonrası lazy olmalı") bütün sayfalarda çalışmaya devam eder. Aynı sayfa tipi için o taramada
render verisi varsa sezgisel kural o sayfa tipine bakmaz; yerine R6/R7 kesin ölçümü geçerlidir. Böylece aynı sorun için iki
görev açılmaz.

### 2.9 HTML kurallarının render ile düzeltilmesi (yanlış alarm azaltma)
HTML'den yapılan kontroller CSS ile gizli öğeleri (ör. mobil menü kopyası) bilemez. Render verisi olan sayfalarda
`a11y.link.no_text` ve `a11y.form.no_label` örnekleri betikte görünürlükle ayrıca doğrulanabilir — **bu adımda yapılmaz**,
ihtiyaç görülürse ayrı iş olarak eklenir (not düşüldü).

### 2.10 Ekranlar (adım 2)
- **Tarama detayı:** "Render" aşaması süresiyle; kaç sayfa × cihaz ölçüldü, engellenen/başarısız örnekler ve nedenleri.
  "Bu taramada çalışmadı" metni yalnızca render kapalıysa.
- **Ayarlar > Siteler > Genel:** render açık/kapalı, sayfa tipi başına örnek sayısı, tahmini süre.
- **Bulgu detayı:** render bulgularında cihaz kolonu; ayrıntıda seçici ve metin örnekleri.
- **Genel Bakış:** dördüncü skor (adım 1'de geldi) render bulgularıyla dolar.
- Ekran görüntüsü kaydı **bu adımda yok** (ileride: bulgu kanıtı olarak).

### 2.11 Ortam
- Worker Docker imajında Chromium zaten kuruluyor (tracking denetimi). Ek iş yok; doğrulanır.
- Yerel Windows: `pwsh src/MarketingOps.Worker/bin/Debug/net10.0/playwright.ps1 install chromium` (README'ye eklenir).
- Avast HTTPS taraması Docker imaj derlemesini bozuyor (bilinen yerel sorun); yerel doğrulama `dotnet run` ile yapılır.

### 2.12 Testler (adım 2)
- `render-audit.js`: entegrasyon testi, Playwright ile yerel statik test sayfaları (`tests/.../Fixtures/render/`):
  taşan sayfa, düşük kontrast, 10 px yazı, 18 px buton, ilk ekranda lazy görsel, ekran altında eager görsel, karusel, bozuk oranlı
  görsel, çerez katmanı. Chromium yoksa test atlanır değil **başarısız olur** (CI'da kurulu olmalı).
- Render kuralları: sahte `ExternalData` ile birim testleri (ihlal / yok / eşik / `Obstructed` örnek kullanılmaz).
- Skor: örneklenmiş kaynakta oran testi; Core Web Vitals regresyon testi.
- Pipeline: `IRenderAuditor` sahte uygulamasıyla tarama → render → değerlendirme sırası; render hata verince tarama tamamlanıyor,
  render kuralları çalışmıyor, mevcut bulgular çözülmüyor.
- `CrawlSettings`: yeni alanların sınırları.

### 2.13 Adım 2 doğrulama
- `dotnet build`, `dotnet test` yeşil (Chromium kurulu).
- Lastik Borsası yerel taramasıyla: tarama detayında Render aşaması, Erişilebilirlik ve Mobil skorunun render bulgularıyla
  değişmesi, bir kontrast ve bir taşma bulgusunun detayında seçici/cihaz bilgisi.

---

## 3. Kapsam dışı (bilinçli olarak sonraya)
- Canonical'ın gösterdiği sayfanın durumu (404, yönlendirme, noindex).
- hreflang karşılıklılığı ve dil kodları.
- Yönlendirmeye giden iç linkler; yetim sayfalar (sitemap'te olup iç link almayan).
- Karışık içerik (HTTPS sayfada HTTP kaynak); `X-Robots-Tag` header'ı.
- `prefers-color-scheme` (`@media`) ile gelen koyu tema kontrastı, ekran görüntüsü kanıtı. (`data-theme` olan sitelerde koyu tema kontrastı yapıldı, bkz. 7.)
- Odak kelime kontrolleri (taranan sitede odak kelime alanı yok).
- Render ile HTML erişilebilirlik bulgularının görünürlük doğrulaması (2.9).

## 4. Uygulama sırası (her madde sonunda build + ilgili testler)
1. Domain: `FindingCategory.Accessibility`, `ScoreCategory.Accessibility`, `ScoreCategoryMap`, `DisplayText`, `NotificationText`.
2. Domain: yeni snapshot tipleri, `PageSnapshotData.Version`, `PageChecks.HasMarkupFacts`.
3. Infrastructure: `HtmlSnapshotParser` genişletmesi + parser testleri.
4. Domain: `SchemaRequirements` + testleri.
5. Domain: 22 yeni kural (1.2) + `RuleCatalog` + birim testleri.
6. Domain: mevcut kuralların sıkılaştırılması (1.4) + testlerin güncellenmesi.
7. Application/Web: Genel Bakış 4 skor, trend, breakdown, Kurallar ekranı, tasarım vitrini.
8. Bilgi tabanı: yeni anahtarlar + SEO dokümanı özetleri + `KnowledgeBaseTests`.
9. Entegrasyon testleri (adım 1) + tarayıcıda doğrulama + README "Faz 8 — HTML denetimi" bölümü. **Ara commit.**
10. Domain: `RenderDevice`, `RenderStatus`, `RenderSample`, ölçüm record'ları, `RuleDataSource.Render`, `ExternalData` genişlemesi,
    `CrawlSettings` alanları; Infrastructure: EF konfigürasyonu + migration `RenderSamples`.
11. Infrastructure: `render-audit.js` + `PlaywrightRenderAuditor` + betik entegrasyon testleri.
12. Application: `RenderCrawlCommand`, iş zinciri (Download → Render → Evaluation), temizlik işi.
13. Domain: 8 render kuralı + testler; skor oranı düzeltmesi (2.7); `lazy_missing` ilişkisi (2.8).
14. Web: tarama detayı, site ayarları, bulgu detayı.
15. Entegrasyon testleri (adım 2) + tarayıcıda doğrulama + README "Faz 8 — Render denetimi". **Commit.**
16. Faz sonu gözden geçirme (backend promptlarındaki kontrol listesi).

## 5. Kararlar (2026-10-31, kullanıcı onayı)
1. Erişilebilirlik ve mobil **tek kategori**: "Erişilebilirlik ve mobil", skor "Erişilebilirlik ve Mobil Sağlığı".
2. 1.2 ve 2.6 tablolarındaki önem dereceleri **onaylandı**.
3. 1.4'teki mevcut kuralların sıkılaştırılması **onaylandı**.
4. 2.7 örneklenmiş veride oran = etkilenen örnek / ölçülen örnek **onaylandı** (Core Web Vitals bulguları dahil).

## 6. Uygulama notları (adım 2)
- Ölçüm tipleri sadeleşti: `ContrastFailure` ayrı tip olmadı; bütün listeler `RenderElement(Selector, Text, Value, Limit)`. Kontrastta Value oran, Limit gereken oran.
- Eşiği ayarlanabilen ölçümler betikte gevşek sınırla toplanır (yazı < 16 px, dokunma alanı < 48 px), kural kendi eşiğiyle süzer. Listeler en fazla 50 öğe.
- Taşma ve geniş öğe tespitinde html/body dışındaki kırpan ya da kaydıran atanın içindeki öğe (karusel izi, kaydırılabilir tablo) sayılmaz; taşma kaynağı olarak en içteki öğeler gösterilir.
- Render tarama içinde değil, tarama bittikten sonra ayrı iş (`RenderCrawlJob`) olarak çalışır; tarama durumu "Tamamlandı" olduktan sonra aşama eklenir, ardından değerlendirme kuyruğa girer. "Kuralları çalıştır" render'ı yeniden çalıştırmaz.
- `crawl_render_enabled` için veritabanı varsayılanı modelde yok (EF bool'da false'u "atanmamış" sayardı); mevcut siteler migration'da true/3 aldı.
- Lastik Borsası doğrulaması (300 sayfa): 16 sayfa × 3 cihaz = 48 ölçüm, render 5 dk 26 sn, hepsi ölçüldü. Erişilebilirlik ve mobil skoru 79 → 68 (kontrast, küçük yazı, dokunma alanı bulguları), mobilde yatay taşma yok.

## 7. Faz sonu gözden geçirme (2026-10-31)
Backend promptlarındaki kontrol listesi bu fazın değişikliklerine uygulandı:
- Her tip kendi dosyasında, dosya adı = tip adı. İç içe özel yardımcılar (`RenderRule.Run`, test sahteleri) kodun mevcut kalıbıyla aynı.
- Kullanılmayan kod: `RenderMeasurements.ElementsScanned/TextElementsChecked` hiçbir yerde okunmuyordu, kaldırıldı.
- Yeni sorguların hepsi `AsNoTracking` + projection; yeni liste sorgusu yok, N+1 yok.
- Domain kuralları domain'de (ayar sınırları, `Crawl.RecordRender` koruması); handler'lar aynı kontrolü yalnızca erken çıkış için yapar.
- Hata mesajları Türkçe.
- Eklenen testler: render ayarlarının kaydı/doğrulaması, eski sürüm snapshot'la değerlendirme, render başarısız olunca bulgunun açık kalması, koyu tema kontrastı, iş kilitleri.
- Eksik kalan maddeler tamamlandı: render süre tahmini, bulgu detayında cihaz kolonu (`FindingOccurrence.Devices`; JSON kolon olduğu için `OccurrenceDevices` migration'ı yalnızca model snapshot'ını günceller), `data-theme` olan sitelerde iki temada kontrast.
- Eşzamanlı değerlendirme çakışması: `EvaluateCrawlJob` ve `RenderCrawlJob` tarama başına `DisableConcurrentExecution` kilidi taşır.
- `docker compose up` temiz ortamda doğrulanamadı: bu makinede Avast HTTPS taraması imaj içindeki NuGet bağlantısını kesiyor (ortam sorunu).
