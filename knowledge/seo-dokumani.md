# SEO Optimizasyonu Üzerine (iç doküman)

Oğuzhan Karagüzel'in "SEO Optimizasyonu Üzerine" dokümanından kısa özgün özetler. Dokümanın kopyası değildir; ilgili kuralların
anahtarları altında Google notlarına ek bağlam olarak verilir. Sayı ve eşik site ayarından gelir.

<!-- ref: google:title-links -->

Dokümana göre title her sayfada benzersizdir ve bu, içerik girilirken doğrulanır: yönetim panelinde aynı title ikinci kez
kaydedilemez, uzunluk sınırı aşılınca editör uyarılır. Title sayfanın anahtar kelimesini taşır.

<!-- ref: google:snippets -->

Doküman her sayfada bir açıklama olmasını ister. Editörün boş bıraktığı sayfalar için sitenin genelinde kullanılacak standart bir
açıklama tanımlanır; ama sayfaya özgü açıklama her zaman tercih edilir. Açıklama uzunluk aralığında tutulur ve sayfanın anahtar
kelimelerini doğal biçimde içerir.

<!-- ref: google:headings -->

Doküman her sayfada tek ve benzersiz bir H1 ister; H1 sayfanın üst kısmında durur ve kullanıcıya sayfanın konusunu hemen söyler.
Diğer başlıklar hiyerarşiyle iner: liste sayfasında H1'den sonra her öğenin başlığı H2, öğe detayındaki iç başlıklar H3 olur.

<!-- ref: a11y:headings -->

Dokümandaki örnekte başlıklar iç içe geçen bölümler gibi kurulur: H1 ana konu, H2'ler ana bölümler, her H2'nin altındaki H3 ve
H4'ler o bölümün alt başlıkları. Seviye atlamadan ilerleyen bu yapı hem arama motoru hem ekran okuyucu için sayfanın haritasıdır.

<!-- ref: a11y:landmarks -->

Doküman, uygun yerde div yerine anlamlı HTML öğelerinin (section, article, main, nav) kullanılmasını ister. Bu öğeler sayfanın
hangi parçasının ne olduğunu hem tarayıcılara hem yardımcı teknolojilere anlatır.

<!-- ref: social:open-graph -->

Dokümana göre Open Graph etiketleri bir sayfanın sosyal medyada nasıl görüneceğini belirler ve paylaşımın daha çekici
görünmesini, daha fazla etkileşim almasını sağlar. Temel beşli og:title, og:description, og:image, og:url ve og:type'tır;
og:site_name ve og:locale tamamlayıcıdır.

Paylaşım görseli yüksek çözünürlüklü ve büyük kart boyutundadır; SVG desteklenmez, JPEG tercih edilir. Etiketler editöre
bırakılacaksa panelde görsel biçimi, boyut ve metin uzunluğu konusunda yönlendirme yapılır; bırakılmayacaksa standart değerler
belirlenir (ör. og:description meta description'dan beslenir). Etiketler yayından önce paylaşım hata ayıklayıcısıyla denenir.

<!-- ref: google:structured-data -->

Doküman yapılandırılmış veriyi arama motorunun içeriği anlaması için işaretlenmiş bilgi olarak tanımlar; zengin sonuçlar, bilgi
paneli ve sesli asistan yanıtları bundan beslenir. Sayfadaki bütün bilgiler için doldurulmaya çalışılır: organizasyon, makale,
breadcrumb, kişi, ürün. Editöre kısmen bırakılır; geri kalanı şablon üretir. Çıktı zengin sonuç test aracıyla doğrulanır.

<!-- ref: google:breadcrumbs -->

Dokümana göre breadcrumb kullanıcının sitede nerede olduğunu gösterir ve yapılandırılmış veri türleri arasında ilk sıradadır.
Sayfadaki görünen gezinme yolu ile işaretlenen yol aynı adımları taşır.

<!-- ref: google:organization -->

Doküman organizasyon işaretlemesini markanın arama motorundaki kimliği olarak anlatır: ad, adres, logo ve iletişim bilgisi bilgi
panelinde kullanılır. Belirli bir konumda hizmet veren işletmeler için yerel işletme türü seçilir.

<!-- ref: google:canonical -->

Doküman canonical'ı aynı ya da benzer içerikli adreslerden hangisinin asıl olduğunu söyleyen etiket olarak anlatır: parametreli
adresler, farklı yol yapıları ve kopyalar asıl adresi gösterir. Her sayfa kendine ya da asıl sayfasına işaret eder, tam adres
(protokol ve alan adı dahil) kullanılır, iç linkler de asıl adresi kullanır. Çok dilli sitelerde alternate ve x-default dil
sürümlerini birbirine bağlar.

<!-- ref: a11y:links -->

Doküman ARIA'yı görünür metni olmayan ya da yetersiz olan öğelere erişilebilir ad vermenin yolu olarak anlatır: "+" gibi bir
ikon butona aria-label ile "yorum yap" adı verilir; bir alan başka bir öğenin metniyle adlandırılacaksa aria-labelledby kullanılır.

<!-- ref: a11y:forms -->

Dokümandaki örnekte bir giriş alanı, yanındaki "Kullanıcı adı" metniyle aria-labelledby üzerinden ilişkilendirilir; ekran okuyucu
alana gelince bu metni de okur. Görünür etiketi olan her alan bu ilişkiyi kurar.

<!-- ref: security:links -->

Doküman güvenlik başlığında sitenin tamamen HTTPS'te olmasını ve HTTP isteklerin kalıcı yönlendirmeyle HTTPS'e gitmesini ister.
Dış sitelere giden ve yeni sekmede açılan linklerin açan sayfaya erişememesi de aynı hijyenin parçasıdır.
