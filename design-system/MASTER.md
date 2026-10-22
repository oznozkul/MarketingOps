# MarketingOps tasarım sistemi

Canlı vitrin: `/_design` (yalnızca Development). Kaynak: `src/MarketingOps.Web/Styles/app.css`, `tailwind.config.js`, `TagHelpers/`, `ViewComponents/`.

Bu sistem `ui-ux-pro-max` skill'inin iş akışıyla kuruldu: `--design-system` (bkz. `skill-recommendation.md`), ardından `style`, `typography`, `color`, `ux`, `chart` alan aramaları ve `--stack html-tailwind` kuralları.

## Yön: Swiss Modernism 2.0 + Data-Dense Dashboard
Skill'in "Trust & Authority" önerisi ve "AI purple/pink gradients" anti-pattern uyarısı esas alındı. Hedef, jenerik "AI ile yapılmış dashboard" görünümünden uzak, bir denetim aracı hissi.

| Kaçınılan (jenerik AI görünümü) | Bunun yerine |
|---|---|
| İndigo/mor birincil renk, gradient | Mürekkep (siyaha yakın) birincil; tek vurgu: mavi `#0369A1` (yalnızca bağlantı ve odak) |
| Yumuşak gölgeli, çok yuvarlak kartlar | Düz yüzey + 1px çizgi; köşeler 2–4px; gölge yalnızca menü ve drawer'da |
| Hap biçimli seçili menü | Solda 2px mürekkep çizgisi |
| Yuvarlak ikon baloncukları, yuvarlak avatar | Kare ikon kutusu, kare monogram |
| Inter / varsayılan sistem fontu | IBM Plex Sans (metin) + IBM Plex Mono (etiket, sayı, kod) |
| Renkli ikonlu logo kutusu | Tipografik işaret: `MO` + MarketingOps |

## Tipografi
- Skill eşleşmesi: "Financial Trust" (IBM Plex). Yerel woff2 (`wwwroot/fonts`), `font-display: swap`.
- `page-title` 22/600, `section-title` 15/600, gövde 14/400, ikincil 12/400.
- `.eyebrow`: 11px Plex Mono etiket (kart başlıkları, menü grupları, tablo başlıkları). Büyük harf dönüşümü kullanılmaz; tr-TR'de "Tracking" → "TRACKİNG" olur.
- Skor ve KPI değerleri Plex Mono, tabular rakamlar. Biçim tr-TR: `1.206`, `%38`, `21 Eki`.

## Renk token'ları
`"R G B"` kanalları; `:root, .light` ve `.dark` altında. Tailwind'de `rgb(var(--x) / <alpha>)`.

- Nötr (mavi tonsuz): `canvas`, `surface`, `surface-2`, `surface-3`, `line`, `line-strong`, `fg`, `fg-muted`, `fg-subtle`
- Eylem: `primary` (mürekkep; koyu temada tersine döner), `primary-text` ve `focus` (mavi vurgu)
- Tonlar (`DEFAULT` metin, `soft` zemin, `line` kenar, `solid` dolgu): `critical` kırmızı, `high` turuncu, `medium` mavi, `low` gri, `ok` yeşil (yalnızca doğru/çözüldü), `warn` amber
- Durumlarda "Açık / Devam ediyor / Çalışıyor" mürekkep tonundadır; vurgu mavisi "Orta" önemle karışmasın diye durumlarda kullanılmaz.
- Grafik: `--chart-1` mürekkep, `--chart-2` vurgu mavisi, `--chart-3/4` sıcak gri. Seriler çizgi deseniyle de ayrılır (düz / kesikli / noktalı).

### Ölçülen kontrast (WCAG)
| Çift | Açık | Koyu |
|---|---|---|
| fg / canvas | 16.45 | 15.78 |
| fg-muted / canvas | 6.49 | 8.35 |
| fg-subtle / surface-2 (en kötü) | 4.95 | 5.09 |
| bağlantı / surface-2 | 5.24 | 7.22 |
| önem rozetleri (en düşük: high) | 5.24 | 8.11 |
| birincil buton | 18.13 | 15.78 |

## Ölçü
Köşe: none · sm 2 · md 3 · lg 4. Boşluk 4px ölçeği. Z-index: sticky 20 · overlay 30 · drawer 40 · modal 50 · toast 60. Dokunma alanı 44px (ince imleçte 34px).

## Bileşenler
| Bileşen | Kullanım |
|---|---|
| `<severity-badge value="@x" />` | Kritik / Yüksek / Orta / Düşük (ikon + metin) |
| `<status-badge value="@x" />` | Bulgu, görev, tarama, connector, tag denetimi durumları |
| `<category-badge value="@x" />` | Tracking / Teknik SEO / İçerik / Performans |
| `<icon name="bell" label="…" />` | Lucide; `label` yoksa dekoratif |
| `<stat-card label value delta delta-good href>` | Sayısal kart |
| `<vc:score-card site-id kind score missing-connector />` | Skor + değişim + "Bu skor nasıl hesaplandı?" |
| `<vc:filter-chips>`, `<vc:sort-header>`, `<vc:pagination>` | Sunucu tarafı filtre, sıralama, sayfalama |
| `<empty-state>`, `<error-state>`, `<skeleton variant>` | Boş, hata, yükleniyor |
| Drawer / Toast / Grafik | `hx-target="#drawer-body"`; `Response.Toast()`; `<canvas data-chart>` + tablo alternatifi |

Enum tipli Tag Helper özniteliklerinde değer `@` ile verilir (`value="@f.Severity"`). Enum → Türkçe metin: `Infrastructure/DisplayText.cs`.

## Skill kontrol listesi (teslim öncesi)
- [x] Emoji yok, tek ikon seti (Lucide)
- [x] Hover'da yerleşim kayması yok (yalnızca renk/çizgi geçişi, 150 ms)
- [x] `cursor-pointer`, görünür focus (2px vurgu mavisi)
- [x] Kontrast ≥ 4.5:1 iki temada (yukarıdaki tablo)
- [x] Renk tek başına anlam taşımıyor
- [x] `prefers-reduced-motion`
- [x] 390 / 768 / 1280 / 1440 px; geniş tablolar yatay kaydırılır
- [x] z-index ölçeği, skip link, form etiketleri
