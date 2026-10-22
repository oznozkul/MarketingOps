# MarketingOps — Claude Code ekran promptları

Teknoloji: ASP.NET Core MVC + Razor + Tailwind + htmx + Alpine.js.

Ön koşul: Backend promptlarında Faz 0 ve Faz 1 tamamlanmış olmalı.

Kullanım: Promptları sırayla, her biri bitince bir sonrakini verin. **Prompt 0 her zaman ilk verilir.** Ortak bağlamı ve veri modelini o kurar, diğer promptlar ona dayanır. Her prompttan sonra çıkan ekranı kontrol edin, ardından devam edin.

---

## Prompt 0 — Proje bağlamı, tasarım sistemi, iskelet

```
ui-ux-pro-max skill'ini kullanarak bir B2B SaaS paneli tasarlayıp kodlayacağız. Önce bu bağlamı oku, sonra sadece aşağıdaki "Bu adımda yapılacaklar" kısmını yap.

## Ürün
Adı: MarketingOps (geçici). E-ticaret siteleri için yapay zekâ destekli bir pazarlama denetim platformu.
Sistem siteleri her gece tarar. Google Search Console, GA4, Google Tag Manager, Google Ads, Meta Pixel/CAPI ve sitenin sipariş veritabanından veri okur. Teknik SEO, performans ve tracking sorunlarını bulur. Bu sorunları gruplayıp yazılım ekibine görev olarak açar. Ekip düzeltince sonraki taramada düzeldiğini doğrulayıp görevi kapatır.

ÇOK ÖNEMLİ İLKE: Sistem SALT OKUNURDUR. Siteye ya da reklam hesaplarına hiçbir şey yazmaz, hiçbir ayarı değiştirmez. Sadece bulur, açıklar, önerir ve takip eder. Arayüzde "Uygula", "Düzelt", "Yayınla" gibi siteyi değiştiriyormuş izlenimi veren hiçbir buton olmamalı. Öneriler kopyalanabilir ve indirilebilir; uygulamayı ekip yapar.

Generic ve çok-siteli: Organizasyon > Site hiyerarşisi var. İlk iki site lastikborsasi.com ve tyresupply.com, ama hiçbir şey bu iki siteye göre hardcode edilmeyecek.

Kullanıcılar:
- Yönetici: Durumu tek bakışta görmek ister, skorlar ve trendler.
- Pazarlama/SEO sorumlusu: Bulguları inceler, önceliklendirir, yanlış alarmları işaretler.
- Yazılım ekibi: Görev detayını okur, önerilen çözümü uygular.

Arayüz dili: Türkçe. Sayı formatı tr-TR (1.206, %38, 21 Eki).

## Domain sözlüğü
- Tarama (Crawl): Bir sitenin belirli bir zamanda taranması. Numarası, başlangıç/bitiş saati, taranan sayfa sayısı, durumu var.
- Sayfa tipi (PageType): URL pattern ve/veya schema tipiyle tanımlanan sayfa grubu. Örnekler: Kategori, Ürün, Lastik ölçü, Jant, Araç uyumluluk, Blog. Kurallar sayfa tipine bağlanır. Hiçbir tipe uymayan sayfalar "Eşleşmeyen" grubuna düşer.
- Kural (Rule): Tek bir kontrol. Kodu (ör. seo.title.duplicate), kategorisi, varsayılan önemi, açık/kapalı durumu ve eşik ayarları var.
- Bulgu (Finding): Bir kuralın bir sayfa grubunda bulduğu sorun. Aynı kural + aynı sayfa tipi tek bulgu grubunda toplanır ("340 sayfada duplicate title" tek kayıttır). Her bulgunun bir parmak izi (fingerprint) var. Aynı sorun tekrar bulunursa yeni kayıt açılmaz, mevcut kayıt güncellenir.
- Görev (Task): Bulgudan üretilen iş. Başlık, problem, etki, önerilen çözüm, kabul kriterleri ve atanan ekip içerir. Jira/Azure DevOps'a aktarılabilir.
- Connector: Dış veri kaynağı bağlantısı. Search Console, GA4, GTM, Google Ads, Meta, Sipariş kaynağı, Görev hedefi (Jira vb.). Hepsi salt okunur.
- Skorlar: Tracking Sağlığı, SEO Sağlığı, Performans (0–100).

## Bulgu yaşam döngüsü
- Açık: İlk kez tespit edildi.
- Çözüldü: Sonraki taramada görülmedi. Otomatik olarak geçilir.
- Tekrar açıldı: Çözülmüştü, yeniden görüldü (regresyon). Ayrıca vurgulanmalı.
- Yanlış alarm: Kullanıcı işaretledi. Kural bu sayfa grubu için susturulur. Gerekçe zorunlu.
- Ertelendi: Kullanıcı bilinçli olarak sonraya bıraktı, bir tarih seçer.

Önem seviyeleri: Kritik, Yüksek, Orta, Düşük. Renk tek başına anlam taşımasın, her zaman etiket metniyle birlikte gösterilsin.

Kategoriler: Tracking, Teknik SEO, İçerik, Performans.

## Skor mantığı (arayüzde açıklanabilir olmalı)
Skor 100'den başlar. Her açık bulgu, önem ağırlığı × etkilenen sayfa oranı kadar puan düşürür (Kritik 15, Yüksek 8, Orta 3, Düşük 1, bulgu başına üst sınır var). Her skor kartında "Bu skor nasıl hesaplandı?" bağlantısı olmalı. Tıklanınca puanı en çok düşüren bulguları listeleyen bir panel açılır.

## Tasarım yönü
ui-ux-pro-max ile uygun bir stil seç, ama şu çerçevede kal:
- Veri yoğun, sakin, profesyonel bir operasyon paneli. Dashboard şablonu gibi değil, bir denetim aracı gibi görünsün.
- Sayılar için tabular/mono font, metin için okunaklı bir sans.
- Önem renkleri: kırmızı / turuncu / mavi / gri, erişilebilir kontrastla. Yeşil sadece "doğru/çözüldü" için kullanılsın.
- Gradient, glassmorphism ve emoji yok.
- WCAG AA kontrast, en az 44px dokunma alanı, klavyeyle gezinme, görünür focus.
- Light ve dark tema.
- 1280–1440px masaüstü öncelikli. Tablet ve telefonda menü üstte veya çekmecede; geniş tablolar yatay kaydırılsın.

## Teknik
- ASP.NET Core MVC (.NET 9), C#, Razor views. SPA framework yok.
- Stil: Tailwind CSS (standalone CLI ile build, Node zorunlu değil). Tasarım token'ları tailwind.config ve CSS değişkenleri olarak tanımlansın, dark tema `class` stratejisiyle çalışsın.
- Etkileşim: htmx (filtreleme, sayfalama, drawer ve panel yükleme, inline form gönderimi, partial yenileme) + Alpine.js (dropdown, tab, modal, tema geçişi gibi küçük istemci durumları). Ağır JavaScript yazılmasın.
- Grafikler: Chart.js. Veri, view model'den JSON olarak gelsin.
- Canlı ilerleme (tarama durumu): SignalR.
- Bu solution'da backend zaten var (CLAUDE.md'yi oku): Domain, Application (MediatR command/query), Infrastructure (EF Core + Postgres). Ekranlar MarketingOps.Web projesine eklenir.
- MVC controller'ları sadece MediatR'a query/command gönderir; iş mantığı controller'da olmaz. Ekranın ihtiyacı olan sorgu henüz yoksa, CLAUDE.md kurallarına uygun şekilde (her tip ayrı dosyada) Application'a yeni bir query eklenir.
- View model'ler Web/ViewModels altında, her biri ayrı dosyada. Application'ın Response tipleri doğrudan view'a verilebiliyorsa ayrı view model açılmaz.
- Henüz backend'i yazılmamış fazların ekranları (ör. Tracking, Performans) için: Seed verisi Infrastructure'daki Development seed'ine eklenir. Ayrı bir mock katmanı açılmaz. Veri gerçekçi ve tutarlı olsun, lastik e-ticaretine uygun sayfa tipleri ve URL'ler kullanılsın.
- Bileşenler: Tekrar eden UI parçaları ViewComponent ya da partial view olsun (SeverityBadge, StatusBadge, StatCard, ScoreCard, DataTable başlığı/sayfalama, FilterChips, EmptyState, ErrorState, Skeleton, Drawer). Mümkünse Tag Helper olarak kullanılsınlar (<severity-badge value="..."/>).
- Liste ekranlarında filtre, sıralama ve sayfalama sunucu tarafında yapılsın, query string'de tutulsun. htmx ile sadece tablo partial'ı yenilensin; URL `hx-push-url` ile güncellensin.
- Her ekranda yükleniyor (htmx indicator + skeleton), boş ve hata durumları tasarlansın.
- Türkçe kültür: Uygulama genelinde tr-TR CultureInfo kullanılsın.

## Bu adımda yapılacaklar
1. Mevcut Web projesine Tailwind standalone CLI, htmx, Alpine.js ve Chart.js'i ekle (CDN yerine wwwroot/lib altında yerel dosya). Build sırasında Tailwind çıktısının üretilmesini sağla.
2. ui-ux-pro-max ile tasarım sistemini oluştur: renk token'ları (light/dark), tipografi, spacing, radius ve yukarıdaki temel bileşenler. Hepsini gösteren bir /_design sayfası yap (sadece Development ortamında açık).
3. _Layout: Sol menüde Genel Bakış, Bulgular, Görevler, Tracking Denetimi, Performans, Taramalar ve Ayarlar (Siteler, Sayfa Tipleri, Kurallar, Bildirimler). Üst barda site seçici (organizasyonun siteleri), son tarama bilgisi, bildirim zili, tema geçişi ve kullanıcı menüsü. Mobilde menü Alpine.js ile çekmeceye dönüşsün.
4. Routing: Seçili site URL'de tutulsun: /sites/{siteId}/overview, /sites/{siteId}/findings, /sites/{siteId}/findings/{id} vb. Seçili site bir action filter ile çözülüp ViewData/base view model üzerinden layout'a aktarılsın. Site değişince aynı ekranın o siteye ait versiyonuna gidilsin.
5. Site seçici GetSitesQuery'yi kullansın.
Henüz ekran içeriklerini doldurma, sadece iskelet ve tasarım sistemi.
```

---

## Prompt 1 — Genel Bakış

```
Genel Bakış ekranını yap. Amaç: Yönetici 10 saniyede "bugün bir sorun var mı, işler iyiye mi gidiyor" sorusunun cevabını görsün.

Yukarıdan aşağıya:
1. Başlık alanı: Ekran adı, son tarama zamanı ve taranan sayfa sayısı, "Şimdi tara" butonu. Bu buton sadece taramayı kuyruğa ekler; tarama sürerken buton yerine ilerleme gösterilir.
2. Kritik uyarı bandı: Sadece Kritik önemde yeni ya da tekrar açılmış bulgu varsa görünür. En önemli tek sorunu tek cümleyle anlatır, ilgili ekrana link verir. Birden fazla kritik sorun varsa "+2 kritik sorun daha" bağlantısı gösterilir.
3. Skor kartları: Tracking Sağlığı, SEO Sağlığı, Performans, Açık görevler (önem dağılımıyla), Bu hafta kapanan görevler. Skor kartlarında önceki değere göre değişim ve "Nasıl hesaplandı?" paneli olsun.
4. Öncelikli sorunlar: En fazla 6 bulgu. Sıralama önce öneme, sonra etkilenen sayfa sayısına göre. Tekrar açılanlar etiketlenir. Satıra tıklanınca bulgu detayına gidilir.
5. Sipariş ve GA4 purchase karşılaştırması: Son 7 veya 30 günü gösteren gruplanmış bar grafik (veritabanındaki sipariş ve GA4 purchase). Her gün için fark yüzdesi; eşik (varsayılan %5) aşılırsa vurgulanır. Grafiğin altında eşiğin ne anlama geldiğini açıklayan kısa bir metin olsun.
6. Skor trendi: Son 30 günün skor çizgileri. Önemli olaylar (deploy tarihi, bulgu açılması) grafik üzerinde işaretlenebilsin.
7. Sayfa tiplerine göre durum tablosu: Sayfa tipi, sayfa sayısı, açık bulgu sayısı, SEO skoru. Satıra tıklanınca Bulgular ekranı o sayfa tipiyle filtrelenmiş olarak açılsın.

Durumlar: İlk tarama henüz yapılmadıysa tüm ekran yerine "İlk tarama bekleniyor" boş durumu ve kurulum adımlarına bağlantı. Bir connector bağlı değilse ilgili kartta "Veri yok: GA4 bağlı değil · Bağla" gösterilsin.
```

---

## Prompt 2 — Bulgular listesi

```
Bulgular ekranını yap. Amaç: SEO/pazarlama sorumlusu tüm sorunları filtreleyip önceliklendirsin ve toplu işlem yapsın.

- Üstte açıklama satırı: "Aynı kural ve sayfa tipinden gelen bulgular tek satırda gruplanır."
- Filtreler: Önem, Kategori, Durum (Açık, Tekrar açıldı, Çözüldü, Yanlış alarm, Ertelendi; varsayılan Açık + Tekrar açıldı), Sayfa tipi, Kural, "Son taramada yeni" anahtarı ve arama kutusu. Filtreler URL query'de tutulsun, böylece paylaşılabilir olsun. Aktif filtreler chip olarak gösterilip tek tıkla kaldırılabilsin.
- Tablo kolonları: Önem, Bulgu başlığı + kural kodu, Kategori, Sayfa tipi, Etkilenen sayfa sayısı, İlk tespit, Son görülme, Durum, Görev no. Kolonlar sıralanabilir. Kolon görünürlüğü ayarlanabilir.
- Satır seçimi ve toplu işlemler: Görev oluştur/aktar, Ertele, Yanlış alarm olarak işaretle (gerekçe zorunlu), CSV indir.
- Satıra tıklanınca Bulgu ve Görev Detayı açılsın.
- Üstte küçük özet şerit: Bu filtrede kaç bulgu var, kaçı kritik, kaçı son taramada yeni.
- Boş durum: Filtre sonucu boşsa "Bu filtrelerle eşleşen bulgu yok" mesajı ve filtreleri temizleme butonu. Hiç bulgu yoksa olumlu bir boş durum.
- Sunucu tarafı sayfalama (sayfa başına 25/50/100). Filtre değişince htmx ile sadece tablo partial'ı yenilensin.
```

---

## Prompt 3 — Bulgu ve Görev Detayı

```
Bulgu ve Görev Detayı ekranını yap. Amaç: Geliştirici başka hiçbir yere bakmadan sorunu anlasın ve düzeltsin.

Ana kolon:
1. Başlık, önem, kategori, durum ve atanan ekip (Frontend/Backend/SEO/İçerik) etiketleri.
2. Problem: Sorun sade bir dille, somut değerlerle anlatılır.
3. Etki: Neden önemli. Varsa veriyle (Search Console pozisyonu, CTR, kayıp sipariş sayısı). Veri yoksa genel açıklama, ama uydurma sayı kullanılmaz.
4. Önerilen çözüm: "AI tarafından üretildi · uygulamayı ekip yapar" notu. İçerik türüne göre:
   - Metin önerisi (title/meta): Önerilen kalıp ve mevcut/önerilen karşılaştırma tablosu (ilk 10 örnek), tamamını CSV indirme.
   - Kod önerisi (schema JSON-LD, script etiketi): Kopyala butonlu kod bloğu.
   - Yapılandırma önerisi (GTM tetikleyicisi, header ayarı): Adım listesi.
   Her önerinin altında "Öneri faydalı mı?" geri bildirimi (iyi/kötü + not).
5. Kaynak/referans: Önerinin dayandığı iç doküman veya resmi kaynak. Örnek: "Web Sitesi Performans Optimizasyonu, s. 30" ya da Google dokümantasyonu bağlantısı.
6. Etkilenen sayfalar: Tablo (URL, mevcut değer, son kontrol). Aranabilir, CSV indirilebilir. Her URL dışarıda yeni sekmede açılabilir.
7. Kabul kriterleri: Her kriterin yanında doğrulama türü yazar: "Otomatik · sonraki tarama" ya da "Ekip onayı". Otomatik olanlar sistem tarafından işaretlenir; ekip onayı gerekenler kullanıcı tarafından işaretlenir.

Yan kolon:
- Ayrıntılar: Site, sayfa tipi, kural kodu (tıklanınca kural ayarına gider), etkilenen sayfa sayısı, dış görev bağlantısı (Jira anahtarı), parmak izi (kısaltılmış, kopyalanabilir).
- Geçmiş (zaman çizelgesi): Tespit, görev oluşturma, her tekrar görülme ("yeni görev açılmadı, bu görev güncellendi"), durum değişiklikleri, sonraki doğrulama taramasının zamanı.
- Yorumlar: Basit yorum alanı.

Aksiyonlar: Jira'ya aktar / Jira'da aç, Ertele (tarih seçici), Yanlış alarm olarak işaretle (gerekçe zorunlu; bu sayfa tipi için kuralı susturur, geri alınabilir). "Düzelt" ya da "Uygula" butonu kesinlikle yok.

Görevler menüsü de bu yapıyı kullanır: Görevler listesi, bulgulardan farklı olarak görev durumuna (Yapılacak, Devam ediyor, Doğrulama bekliyor, Kapandı) göre kanban ya da tablo görünümü sunar.
```

---

## Prompt 4 — Tracking Denetimi

```
Tracking Denetimi ekranını yap. Amaç: "Pazarlama araçları doğru ölçüyor mu?" sorusunu cevaplamak. Sistem sayfaları gerçek bir tarayıcıyla açar, giden ağ isteklerini dinler ve GA4 verisini sipariş veritabanıyla karşılaştırır.

Bölümler:
1. Başlık: Tracking Sağlığı skoru ve son kontrol zamanı.
2. Sipariş ve purchase karşılaştırması: Genel Bakış'taki grafiğin detaylı hali. Ek olarak gelir karşılaştırması (sipariş tutarı ve GA4 purchase value), eşik ayarı ve günlük tablo.
3. Tag denetimi matrisi: Satırlar sayfa tipleri (Ana sayfa, Kategori, Ürün, Sepet, Ödeme, Teşekkürler). Kolonlar GTM, GA4, Google Ads, Meta Pixel ve Consent Mode. Hücre durumları: Doğru, Eksik, İki kez yüklü, Yanlış ID, Onay öncesi tetikleniyor. Hücreye tıklanınca drawer açılsın ve şunları göstersin: test edilen URL, yakalanan istekler (URL, parametreler, zaman), beklenen ve bulunan ID, ilgili bulgu ve görev.
4. Beklenen kimlikler: Site ayarlarından gelen GA4 Measurement ID, GTM Container ID, Meta Pixel ID ve Google Ads Conversion ID listesi. Ekranda düzenlenmez; Ayarlar'a link verilir.
5. E-ticaret event sağlığı: view_item, add_to_cart, begin_checkout, purchase, login, sign_up, search, generate_lead. Kolonlar: dün, 7 günlük ortalama, değişim yüzdesi, not, durum. Anomali kuralı: Değişim eşiğin altına düşerse ya da event sıfıra inerse işaretlenir. Satıra tıklanınca 30 günlük mini grafik açılsın.
6. Çerez onayı ve Consent Mode: İki kolon, "Onay vermeden önce" ve "Onay verildikten sonra". Her tag için giden istek türü ve Doğru/Hatalı durumu.
7. Meta özel: Pixel ve CAPI ikisi de aktif mi, duplicate event var mı (event_id eşleşmesi), Event Match Quality skoru. Meta bağlı değilse bu bölüm "Bağla" çağrılı boş durum gösterir.

Not: Ödeme adımı simüle edilmez. Purchase doğrulaması sipariş veritabanı karşılaştırmasıyla yapılır. Bu not ekranda açıkça yazsın.
```

---

## Prompt 5 — Performans

```
Performans ekranını yap. İki veri kaynağı var: CrUX/PageSpeed'den gelen gerçek kullanıcı metrikleri ve crawler'ın HTML ile header'lardan bulduğu teknik sorunlar.

1. Core Web Vitals: Sayfa tipi bazında LCP, INP, CLS ve TTFB tablosu, mobil/masaüstü sekmeleri. Değerler Google eşiklerine göre İyi / İyileştirilmeli / Zayıf olarak gösterilsin, eşik değerleri tooltip'te yazsın. 28 günlük trend mini grafikleri.
2. Performans kuralları (crawler bulguları): Kural listesi ve her birinin etkilediği sayfa sayısı:
   - Sıkıştırma yok (Content-Encoding br/gzip eksik)
   - Statik dosyalarda Cache-Control eksik veya kısa
   - <head> içinde render'ı bloklayan script (async/defer yok)
   - Width/height verilmemiş görseller (CLS riski)
   - LCP görseli lazy yükleniyor / preload ile lazy çakışıyor
   - Ekran altındaki görsellerde lazy load eksik
   - font-display: swap eksik
   - Kritik dış kaynaklar için preconnect eksik, ya da çok fazla preconnect
   - Kullanılmayan CSS oranı yüksek
   - Minify edilmemiş CSS/JS
   - Büyük inline base64 görseller / HTML boyutu yüksek
   Her satır Bulgular ekranına filtreli şekilde bağlansın.
3. TTFB yüksek sayfa tipleri: Ayrı bir kart. Sunucu tarafı sebep dışarıdan görülemediği için, öneri "kontrol edilecek hipotezler" olarak sunulur (sorgu projeksiyonu, AsNoTracking, indeks, split query, response compression) ve iç performans dokümanına referans verilir. Bu ayrım arayüzde net olsun: "Kesin bulgu" ve "Kontrol önerisi" farklı rozetlerle gösterilsin.
```

---

## Prompt 6 — Taramalar

```
Taramalar ekranını yap. Amaç: Sistemin ne zaman ne yaptığını şeffaf göstermek ve sorun giderme.

- Liste: Tarama no, başlangıç, süre, durum (Kuyrukta, Çalışıyor, Tamamlandı, Kısmen tamamlandı, Hata), taranan sayfa sayısı, yeni bulgu, çözülen bulgu, tekrar açılan bulgu.
- Çalışan tarama için canlı ilerleme: Sayfa sayısı, hız, kalan tahmini süre. Bir CrawlHub (SignalR) kurulsun; mock tarafta bir BackgroundService sahte ilerleme yayınlasın.
- Tarama detayı: Özet, aşamalar (keşif, indirme, render, kural değerlendirme, connector senkronu) ve her aşamanın süresi. Hata alan URL'ler (status code, hata mesajı). Robots.txt yüzünden atlanan sayfalar. Bu taramadaki bulgu değişimleri (yeni / çözüldü / tekrar açıldı).
- İki taramayı karşılaştırma: İki tarama seçilince bulgu farkları listelenir.
```

---

## Prompt 7 — Siteler ve Bağlantılar (+ yeni site sihirbazı)

```
Ayarlar > Siteler ekranını ve yeni site ekleme sihirbazını yap.

Site ayarları (sekmeler):
1. Genel: Domain, görünen ad, tarama sıklığı, hız limiti (istek/saniye), maksimum sayfa, hariç tutulan path'ler (pattern listesi), JS render gerekip gerekmediği (sayfa tipi bazında), robots.txt'ye uyma (varsayılan açık, kapatılamaz).
2. Bağlantılar: Connector kartları (Search Console, GA4, GTM, Google Ads, Meta Pixel/CAPI, Merchant Center, Sipariş kaynağı, Görev hedefi). Her kartta durum (Bağlı, Bağlı değil, Onay bekleniyor, Yetki süresi doldu), bağlı hesap, son senkron zamanı, verilen izinler ve yaptığı işin tek satırlık açıklaması. Kart üzerinde "Salt okunur" rozeti olsun. Sipariş kaynağı türleri: SQL Server, PostgreSQL, Shopify, WooCommerce, ikas, özel API. Bağlantı test butonu da bulunsun.
3. Beklenen kimlikler: GA4 Measurement ID, GTM Container ID, Meta Pixel ID ve Google Ads Conversion ID girişleri. Tracking denetimi bunlara göre "yanlış ID" tespiti yapar.
4. Sahiplik: Site sahipliği Search Console'daki doğrulanmış mülkle kanıtlanır. Doğrulanmamış sitede tarama başlatılamaz; bu durum açıkça gösterilir.

Yeni site sihirbazı (adımlı):
1. Domain gir
2. Search Console bağla ve sahipliği doğrula
3. Diğer connector'lar (atlanabilir)
4. Sayfa tiplerini öner: Sistem sitemap'ten örnek URL'leri çeker, AI sayfa tipleri önerir, kullanıcı onaylar veya düzenler
5. Tarama ayarları
6. İlk taramayı başlat
Her adım kaydedilip sonra devam edilebilir olsun.
```

---

## Prompt 8 — Sayfa Tipleri ve Kurallar

```
Ayarlar > Sayfa Tipleri ve Ayarlar > Kurallar ekranlarını yap.

Sayfa Tipleri:
- Liste: Ad, eşleşme kuralları, eşleşen sayfa sayısı, aktif kural sayısı. En altta "Eşleşmeyen sayfalar" satırı.
- Düzenleme: Eşleşme koşulları birden fazla olabilir ve VE/VEYA ile birleşir. Koşul türleri: URL pattern (glob), URL regex, schema @type, breadcrumb içeriği. Canlı önizleme: Koşul yazıldıkça (htmx, 400 ms debounce) eşleşen örnek URL'ler ve toplam sayı güncellensin; çakışan sayfa tipleri uyarı olarak gösterilsin.
- "AI ile tip öner": Eşleşmeyen sayfalardan öneri kartları üretir (önerilen ad, pattern, örnek URL'ler, kapsadığı sayfa sayısı). Kullanıcı kabul eder, düzenler veya reddeder.

Kurallar:
- Kategori bazlı gruplanmış liste: Tracking, Teknik SEO, İçerik, Performans. Her kural için kod, ad, açıklama, varsayılan önem, uygulandığı sayfa tipleri, açık/kapalı anahtarı.
- Kural detayı: Neyi neden kontrol ettiği, eşik ayarları (ör. thin content için kelime sayısı, title uzunluğu), sayfa tipi bazında önem değiştirme (ör. Blog'da Düşük, Ürün'de Yüksek), referans kaynak, bu kuralın şu an açtığı bulgular ve susturulduğu sayfa tipleri (yanlış alarm geçmişiyle birlikte).
- Eşik değiştirildiğinde "Bu değişiklik mevcut taramada X bulguyu etkiler" önizlemesi gösterilsin.
```

---

## Prompt 9 — Bildirimler ve Raporlar

```
Ayarlar > Bildirimler ekranını yap.

- Kanallar: E-posta, Microsoft Teams webhook, Slack webhook. Her biri için test mesajı gönderme.
- Kurallar: Hangi olay (yeni kritik bulgu, tekrar açılan bulgu, sipariş/purchase farkı eşiği aştı, skor X puan düştü, tarama başarısız, connector yetkisi doldu) hangi kanala ve kime gidecek. Sessiz saatler ayarı ve aynı bulgu için tekrar bildirim aralığı (spam önleme).
- Özet raporlar: Günlük, haftalık ve aylık yönetici özeti. Alıcılar, gönderim saati ve önizleme. Önizleme e-postanın nasıl görüneceğini göstersin: skorlar, değişimler, en önemli 5 sorun, kapanan görevler.
- Panel içi bildirim merkezi: Üst bardaki zil ikonu, okunmamış sayısı, olay listesi, tıklanınca ilgili bulguya gidilmesi.
```

---

## Son prompt — Gözden geçirme

```
Tüm ekranları ui-ux-pro-max kontrol listesiyle gözden geçir:
- Tutarlılık: Aynı kavram (önem, durum, skor) her ekranda aynı bileşenle mi gösteriliyor?
- Erişilebilirlik: Kontrast, klavye, focus, aria etiketleri, renk dışı ayırt edicilik.
- Responsive: 390px, 768px, 1280px, 1440px genişlikler.
- Light/dark tema.
- Her ekranda yükleniyor, boş ve hata durumları var mı?
- Salt okunur ilkesi: Siteyi değiştiriyormuş izlenimi veren herhangi bir buton veya metin kaldı mı?
Bulduğun sorunları listele, sonra düzelt.
```
