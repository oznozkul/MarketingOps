# Tracking denetimi

Bu parçalar tracking kurallarının önerilerine kaynak olur. Eşikler site ayarındadır.

<!-- ref: tracking:tags -->

Sayfadaki ölçüm tag'i, sitede beklenen kimlikle bir kez ve çerez onayından sonra yüklenir. GTM'in kendisi onay sayılmaz; GA4, Google Ads ve Meta istekleri onaydan önce gitmemelidir.

Beklenen kimlik Ayarlar'da tanımlı değilse yanlış kimlik kontrolü yapılmaz. Aynı satıcının iki farklı kimliği event'i çift sayar. Onay butonu seçicisi yoksa veya tıklanamazsa onay öncesi iddia edilmez.

<!-- ref: tracking:orders -->

GA4 purchase sayısı, sipariş veritabanındaki günlük sipariş sayısıyla karşılaştırılır. Fark, sipariş sayısına göre hesaplanır. Sipariş yok ve purchase varsa fark tamdır; ikisi de sıfırsa fark yoktur. GA4 satırı yoksa purchase boş kalır ve fark kuralı çalışmaz.

İzlenen event'lerin dünkü sayısı, önceki günlerin ortalamasına göre düşerse ölçümün bir sayfada durduğu düşünülür. Sıfıra inen bir event, ortalaması varken ayrıca işaretlenir.

<!-- ref: tracking:meta -->

Meta Pixel ile Conversions API aynı pixel üzerinde birlikte durmalıdır. Tarayıcı ve sunucu event'leri aynı event kimliğiyle tekilleşir; aksi halde dönüşüm iki kez sayılabilir.

Token yoksa ya da Graph bu kanıtı döndürmezse kural çalışmaz ve mevcut bulgu kapatılmaz. Denetim reklam hesabına yazmaz.
