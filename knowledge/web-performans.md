# Web Sitesi Performans Optimizasyonu

Bu parçalar, performans kurallarının önerilerine kaynak olur. Eşik değerleri site ayarındadır; buradaki metin sayı vermez.

<!-- ref: perf-doc:p18 -->

İlk görünümün (ekranın açılışta görünen bölümünün) altındaki görseller hemen indirilmemelidir. `loading="lazy"` yalnızca ekranın altında kalan görsellere verilir. İlk görünümdeki görsel, özellikle en büyük içerik görseli, tembel yüklenmez; aksi halde ilk görüntü gecikir.

Liste, kategori ve ürün galerisi şablonlarında ekran altına düşen görseller `loading="lazy"` ile işaretlenir. Boyut öznitelikleri tembel yüklemeyle birlikte durur; biri diğerinin yerine geçmez.

<!-- ref: perf-doc:p22 -->

`<head>` içindeki harici script, `async` veya `defer` olmadan sayfanın çizilmesini bekletir. Tarayıcı dosyayı indirip çalıştırana kadar HTML'in geri kalanını işleyemez.

Üçüncü taraf ve uygulama script'leri `defer` ile yüklenir. Sıra önemli değilse `async` kullanılabilir. Kritik olmayan script `<head>` dışına, gövdenin sonuna alınabilir. Satır içi küçük bir başlatma kodu harici dosyanın önünü kesmemelidir.

<!-- ref: perf-doc:p24 -->

Kritik bir üçüncü taraf kaynağına (font, CDN, etiket yöneticisinin barındırıldığı köken) erken bağlantı, `preconnect` ile kurulur. Bağlantı kurulumu indirme başlamadan biter.

`preconnect` yalnızca gerçekten ilk yüklemede kullanılan kökenlere verilir. Her köken için ayrı bağlantı açmak tarayıcının bağlantı bütçesini doldurur; kullanılmayan preconnect'ler kaldırılır. Aynı kökene birden fazla preconnect yazılmaz.

<!-- ref: perf-doc:p26 -->

Görselin yer ayırması için `width` ve `height` (ya da eşdeğer en-boy oranı) HTML'de bulunmalıdır. Boyut bilinmezse görsel inene kadar sayfa kayar; kullanıcı başka bir öğeye tıklayabilir.

Şablondaki her içerik görseli gerçek en ve boy ile işaretlenir. CSS ile sonradan ölçeklenseler bile öznitelikler durur. Boyut bilinmeyen bir görsel için yer tutucu oran verilir.

<!-- ref: perf-doc:p30 -->

HTML, CSS ve JavaScript yanıtları sunucu ya da CDN üzerinde sıkıştırılarak gönderilir. Tercih Brotli (`Content-Encoding: br`); yoksa gzip. Sıkıştırma kapalıysa aynı işaretleme daha büyük aktarılır ve özellikle yavaş bağlantıda ilk görüntü gecikir.

Sıkıştırma metin yanıtlarına uygulanır. Zaten sıkışık görseller (JPEG, WebP, AVIF) tekrar sıkıştırılmaz. Doğrulama, yanıtta `Content-Encoding` başlığının döndüğünü görmektir.

<!-- ref: perf-doc:p32 -->

HTML yanıtı küçük tutulur. Büyük satır içi script, stil, SVG ve gömülü JSON ilk indirmenin ve ayrıştırmanın süresini uzatır. Tekrar eden işaretleme (uzun listelerin tamamının HTML'e basılması) boyutu büyütür.

Büyük satır içi içerik harici dosyaya taşınır. Sayfanın açılışta ihtiyacı olmayan veri sonra yüklenir. Liste şablonunda ilk ekranın ötesindeki öğeler ayrı istek ya da sayfalama ile gelir.

<!-- ref: perf-doc:p34 -->

Parmak izi ya da sürüm taşıyan CSS ve JavaScript dosyaları tarayıcıda uzun süre önbelleğe alınır. `Cache-Control` içinde `max-age` yoksa ya da süre kısaysa dosya her ziyarette yeniden indirilir.

Dosya adı içerik değişince değişiyorsa (parmak izi) uzun önbellek güvenlidir. HTML belgesi uzun önbelleğe alınmaz; aksi halde ziyaretçi eski işaretlemeyi görür. Önbellek süresi site eşiğinin altındaysa başlık güncellenir.

<!-- ref: perf-doc:p36 -->

TTFB, sunucunun HTML yanıtının ilk baytını göndermesine kadar geçen süredir. Ölçüm sayfa tipi bazında ortalama alınır. CSS ve JavaScript bu sürenin içinde değildir; onlar ilk bayttan sonra inen ayrı GET istekleridir.

Bulgu, crawler'ın yaptığı her GET'i ve o yanıtta ölçülen alanları yazar: adres, süre, durum kodu, içerik türü, gövde boyutu, Content-Encoding, Cache-Control. Ölçülmeyen alan yazılmaz. Sunucunun sayfayı üretirken içeride çağırdığı API bu taramada görünmez; verilmeyen adres yazılmaz.

İlk bayt için bakılacak yer HTML'in önbelleği, boyutu ve sayfa üretimidir. Script ve stil süreleri TTFB nedeni olarak yazılmaz; ilk çizimi uzatan ayrı kanıttır. Şablondaki adres ve sayılar olduğu gibi kalır, yenisi eklenmez.
