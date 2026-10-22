# Bilgi tabanı

AI görev önerileri bu klasördeki iç dokümanlara dayanır. Her kuralın bir referans anahtarı vardır
(ör. `perf-doc:p30`, `google:title-links`); AI'a yalnızca o anahtara ait parçalar verilir.

Biçim: Markdown dosyası. Bir parçanın başına anahtarı HTML yorumu olarak yazın; parça bir sonraki işarete kadar sürer.

```markdown
<!-- ref: perf-doc:p30 -->
Görsellere width ve height öznitelikleri verilmelidir ...

<!-- ref: perf-doc:p32 -->
İlk görünümün altındaki görseller loading="lazy" ile yüklenmelidir ...
```

Kurallardaki anahtarlar: `perf-doc:p18`, `p22`, `p24`, `p26`, `p30`, `p32`, `p34` (Web Sitesi Performans Optimizasyonu dokümanının
sayfaları) ve `google:*` (Google Search Central). İşaretsiz dosyalar ve README.md dosyaları yok sayılır.
Dosyalar uygulama açılışında okunur; değişiklikten sonra web ve worker'ı yeniden başlatın.
