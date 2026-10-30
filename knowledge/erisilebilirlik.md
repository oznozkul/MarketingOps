# Erişilebilirlik ve mobil

Bu parçalar erişilebilirlik ve mobil kurallarının önerilerine kaynak olur. Kısa özgün özetlerdir; WCAG ve tarayıcı belgelerinin
kopyası değildir. Eşikler site ayarındadır; buradaki metin sayı vermez.

<!-- ref: a11y:links -->

Bir linkin adı, ekran okuyucunun linke geldiğinde söylediği metindir. Ad sırasıyla aria-labelledby'nin gösterdiği öğenin metninden,
aria-label'dan, linkin içindeki metinden ve içindeki görsellerin alt metninden gelir. Yalnızca ikon ya da boş alt'lı görsel taşıyan
link adsızdır ve "bağlantı" diye okunur.

İkon linke görünür metin eklenemiyorsa aria-label verilir ve ikon aria-hidden="true" ile gizlenir. Ürün görseline giden linkte
görselin alt metni ürünün adıdır. Link adı gittiği yeri anlatır; "tıkla" ya da "devamı" bağlam dışında okunduğunda anlamsızdır.

<!-- ref: a11y:headings -->

Başlıklar sayfanın içindekiler tablosudur; ekran okuyucu kullanıcıları sayfada başlıktan başlığa atlayarak gezinir. Sayfanın ana
konusu tek H1'dir, bölümler H2, alt bölümler H3 olur. Bir seviye atlanırsa (H2'den sonra H4) kullanıcı arada bir bölüm kaçırdığını
sanar.

Başlık etiketi görünüş için seçilmez: büyük yazı gereken yerde CSS sınıfı kullanılır. Metni boş başlık listede boş bir satır olarak
görünür ve kaldırılır.

<!-- ref: a11y:tables -->

Veri tablosunda her hücrenin hangi sütun ya da satıra ait olduğu başlık hücresiyle (th) bildirilir. Ekran okuyucu bir hücreye
geldiğinde başlığını da okur: "Yük endeksi: 91". Başlık hücresi olmayan tabloda yalnızca değerler sırayla okunur.

Sütun başlıkları scope="col", satır başlıkları scope="row" taşır. Yalnızca yerleşim için kullanılan tablo role="presentation"
alır; ama yerleşim için CSS grid ya da flex tercih edilir.

<!-- ref: a11y:iframes -->

Iframe sayfanın içinde ayrı bir belgedir. title özniteliği, kullanıcının çerçeveye girmeden ne olduğunu bilmesini sağlar: "Ürün
tanıtım videosu", "Mağaza konumu haritası". Başlıksız iframe adsız bir çerçeve olarak duyurulur.

<!-- ref: a11y:forms -->

Her form alanı bir etiketle adlandırılır. En sağlam yol görünür bir label öğesidir: for özniteliği alanın id'sini gösterir ya da
label alanı sarar. Görünür etiket tasarıma uymuyorsa aria-label kullanılır.

Placeholder etiket değildir: yazmaya başlayınca kaybolur, düşük kontrastla çizilir ve birçok ekran okuyucuda okunmaz. Arama
kutusu, filtreler, üyelik ve ödeme formları etiketsiz kalırsa bu kullanıcılar için alışveriş tamamlanamaz.

<!-- ref: a11y:landmarks -->

Sayfa bölgeleri (landmark) sayfanın haritasıdır: header site başlığı ve aramayı, nav menüleri, main sayfanın kendi içeriğini,
footer alt bilgiyi taşır. Ekran okuyucu kullanıcısı bu bölgeler arasında atlar; tek bir main olması "ana içeriğe geç" işlevini
mümkün kılar.

Birden fazla menü varsa her biri aria-label ile ayırt edilir: "Ana menü", "Kategori menüsü", "Alt bilgi". Aynı adla listelenen
menüler birbirinden ayrılamaz.

<!-- ref: a11y:lang -->

Kök öğedeki lang özniteliği sayfanın dilini bildirir. Ekran okuyucu telaffuzu buna göre seçer; dili tanımsız Türkçe sayfa
İngilizce telaffuzla okunabilir. Tarayıcının çeviri önerisi ve yazım denetimi de bu değeri kullanır. Değer geçerli bir dil
kodudur: tr, tr-TR, en.

<!-- ref: a11y:contrast -->

Metin ile arka planı arasındaki kontrast, metnin az gören kullanıcılar ve güneş altında telefon kullananlar tarafından okunabilmesini
belirler. Normal metin için daha yüksek, büyük ve kalın metin için daha düşük bir oran yeterlidir.

Açık gri yazı, görsel üstüne yazılan metin ve marka renginde ince yazı en sık sorunlardır. Düzeltme renk paletinde yapılır: metin
rengi koyulaştırılır ya da arka plan açılır; görsel üstündeki metne yarı saydam bir zemin eklenir.

<!-- ref: a11y:text-size -->

Çok küçük metin telefonda yakınlaştırmadan okunamaz. Gövde metni ve fiyat, stok, kargo gibi karar verdiren bilgiler okunabilir
boyutta olur. Küçük punto yalnızca gerçekten ikincil bilgide (yasal not, telif satırı) kullanılır; o da okunabilir kalır.

<!-- ref: a11y:tap-targets -->

Telefonda parmakla dokunulan link ve butonların dokunma alanı yeterince büyük olur; küçük ve birbirine yakın hedeflerde kullanıcı
yanlış öğeye dokunur. Görünen ikon küçük kalsa da dokunma alanı padding ile büyütülebilir. Metin içindeki linkler bu kuralın
dışındadır.

<!-- ref: mobile:viewport -->

Viewport meta etiketi, telefonda sayfanın ekran genişliğinde çizilmesini sağlar. Etiket yoksa tarayıcı sayfayı masaüstü
genişliğinde çizip küçültür; metin okunamaz, her şey yakınlaştırma ister. Standart değer width=device-width, initial-scale=1'dir.

Yakınlaştırmayı kapatmak (user-scalable=no ya da düşük bir maximum-scale) az gören kullanıcının metni büyütmesini engeller; bu
ayarlar kaldırılır.

<!-- ref: mobile:reflow -->

Telefonda sayfa yana kaymaz: içerik ekran genişliğine sığar, kullanıcı yalnızca aşağı kaydırır. Yana taşmanın tipik nedenleri sabit
piksel genişlik verilmiş kutular, kırılmayan uzun metinler (URL, ürün kodu), genişliği sınırlanmamış tablo ve görsellerdir.

Düzeltme genişliği sabit değer yerine max-width: 100% ile sınırlamak, tabloları yatay kaydırılan bir kapsayıcıya almak ve uzun
metinlere overflow-wrap: anywhere vermektir.
