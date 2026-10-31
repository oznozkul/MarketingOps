# Arama görünürlüğü notları

Kısa özgün özetler. Resmi kaynak bağlantıları ekranda ayrıca gösterilir; bu metinler o sayfaların kopyası değildir.

<!-- ref: google:title-links -->

Title, arama sonucundaki başlık bağlantısının ana kaynağıdır. İndekslenebilir her sayfada, o sayfayı anlatan bir title bulunur. Title yoksa arama motoru içerikten kendi başlığını üretir.

Aynı title'ı paylaşan sayfalar sonuçlarda birbirinden ayırt edilemez ve aynı sorgu için yarışır. Title sayfaya özgüdür: ürün adı, kategori adı ya da ebat gibi sayfayı tanımlayan bilgi başlığa girer. Çok uzun title sonuçta kesilir; çok kısa title sayfayı tanımlamaz. Aralık site eşiğindedir.

<!-- ref: google:snippets -->

Meta description sıralamayı doğrudan belirlemez. Arama sonucundaki açıklamayı ve dolayısıyla tıklamayı etkiler. İndekslenebilir sayfada, o sayfanın vaadini anlatan bir açıklama bulunur. Açıklama yoksa arama motoru sayfadaki cümlelerden birini seçebilir.

Aynı açıklamayı paylaşan sayfalar sonuçlarda birbirine benzer. Her sayfanın açıklaması kendi içeriğine aittir; şablon cümlesi bütün kataloğa kopyalanmaz.

<!-- ref: google:headings -->

Sayfada tek bir H1 bulunur ve sayfanın ana konusunu söyler. H1 yoksa konu belirsiz kalır. Birden fazla H1, sayfanın hangi başlıkla anılacağını dağıtır. Alt başlıklar H2 ve sonrası ile sıralanır. H1, title ile aynı cümle olmak zorunda değildir; ikisi de aynı konuyu anlatır.

<!-- ref: google:canonical -->

Canonical, aynı içeriğin asıl adresini bildirir. İndekslenebilir sayfada canonical bulunur ve sitenin kendi alanında bir adrese işaret eder. Parametreli kopyalar (sıralama, filtre, oturum) asıl adresi göstermelidir.

Başka bir domaine işaret eden canonical, sayfanın bu sitede indekslenmemesine yol açabilir. Canonical, yönlendirme hedefiyle ve sitemap'teki adresle çelişmemelidir.

<!-- ref: google:hreflang -->

hreflang, bir sayfanın dil ve bölge sürümlerinin adreslerini eşleştirir. Her kod (dil ya da dil-bölge) kümede tek bir adresi gösterir; aynı kodun iki farklı adresi olmaz. Kod geçerli olmalıdır: iki harfli dil (tr, en) ve isteğe bağlı iki harfli bölge (tr-TR, en-US); dil sürümü belli değilse x-default.

Küme karşılıklıdır: bir sürüm diğerini işaret ettiğinde diğeri de onu işaret eder; her sayfa kendi adresini de kümede taşır (kendine referans). İşaret edilen adres 200 döner, yönlenmez ve noindex değildir. Karşılıklılık bozuksa arama motoru bölgesel eşlemeyi güvenilir bulmaz.

<!-- ref: google:robots -->

noindex, arama motoruna sayfayı dizine almamasını söyler. Sayfa kaynağındaki meta robots'ta verilebileceği gibi sunucu ya da CDN'in eklediği X-Robots-Tag yanıt başlığıyla da verilir. Başlıktaki noindex kaynakta görünmez; genel bir sunucu kuralı yüzünden sayfa sessizce dizinden düşebilir. İki sinyal tutarlı olmalıdır: indekslenmesi istenen sayfada hiçbir yerde noindex olmaz, istenmeyen sayfada ise izlenebilir olması için sinyal kaynakta da bulunur.

<!-- ref: google:redirects -->

Kalıcı taşınan bir adres, hedefe tek yönlendirmeyle gider. Zincir (A → B → C) her adımda gecikme ekler ve hedefin bulunmasını zorlaştırır. Eski adres, güncel adrese doğrudan bağlanır. Yönlendirme döngüsü olmaz.

<!-- ref: google:sitemaps -->

Sitemap, indekslenmesi istenen adresleri bildirir. `noindex` dönen ya da robots ile kapatılan bir sayfa sitemap'te durmaz. Listede duran adres 200 döner, canonical'ı kendisidir ve yönlendirme zincirinin ortasında değildir. Sitemap ile sayfanın verdiği sinyal çelişirse tarama boşa harcanır ve hatalı bir noindex fark edilmeyebilir.

<!-- ref: google:images -->

İçerik görselinin `alt` özniteliği, görselin ne olduğunu yazar. Bu metin görsel aramada ve ekran okuyucuda kullanılır. Süs amaçlı görselde öznitelik boş bırakılabilir (`alt=""`); özniteliğin hiç olmaması, görselin içeriğinin bilinmediği anlamına gelir. Dosya adı ve çevresindeki metin alt metnin yerine geçmez.

<!-- ref: google:structured-data -->

Yapılandırılmış veri, sayfadaki öğenin türünü (örneğin ürün) arama motoruna açıkça söyler. Sayfa tipinin beklediği tür sayfada yoksa fiyat, stok ya da puan gibi zengin sonuçlar üretilemez. İşaretleme, sayfada görünen bilgiyle aynı olmalıdır. Beklenen tür, sayfa tipinin eşleşme koşullarında tanımlıdır.

<!-- ref: google:helpful-content -->

Sayfanın ana metni, o sayfanın konusunu anlatır. Ürün, kategori ya da ebat sayfasında yalnızca şablon cümlesi ve filtre listesi varsa sayfa benzerlerinden ayrışamaz. Kelime eşiği site ayarındadır. Metin, sayfadaki ürüne ya da kategoriye özgüdür; aynı paragraf kataloğun tamamına yapıştırılmaz.

<!-- ref: google:links -->

İç bağlantı, taranabilir bir adres olmalıdır: gerçek bir `href`, javascript: veya boş hedef değil. Hedef 4xx ya da 5xx dönüyorsa ya da yanıt vermiyorsa bağlantı kırık sayılır. Kırık bağlantı ziyaretçiyi hata sayfasına götürür ve tarama bütçesini harcar. Kaldırılan bir sayfanın bağlantısı güncel adrese çevrilir ya da şablondan çıkarılır.

<!-- ref: social:open-graph -->

Open Graph etiketleri bir bağlantının sosyal ağlarda ve mesajlaşma uygulamalarında nasıl görüneceğini belirler. og:title ve
og:description kartın başlığı ve metnidir; og:image görselidir; og:url paylaşımların toplanacağı asıl adrestir ve canonical ile
aynıdır; og:type sayfanın türüdür (website, article, product).

Görsel ve adres mutlak (https ile başlayan) yazılır. Görselin boyutu og:image:width ve og:image:height ile bildirilirse ilk
paylaşımda da görünür; og:image:alt görselin ne olduğunu anlatır.

<!-- ref: google:breadcrumbs -->

BreadcrumbList, sayfanın site hiyerarşisindeki yerini adım adım anlatır. Arama sonucunda adres yerine bu yol gösterilebilir. Her
adım sırası (position), adı (name) ve son adım dışında adresi (item) ile yazılır; sayfada görünen gezinme yoluyla aynı adımları
taşır.

<!-- ref: google:organization -->

Organization (e-ticarette OnlineStore) ve WebSite, sitenin sahibini ve adını anlatır. Ana sayfada bir kez tanımlanır; diğer
düğümler @id ile bunlara bağlanır. Logo, ad ve adres arama sonucunda ve bilgi panelinde kullanılır.
