// Render denetimi: sayfa gerçek bir tarayıcıda, kendi betikleri çalışmış haldeyken ölçülür. Playwright bu dosyayı
// page.evaluate ile tek bir fonksiyon olarak çağırır ve dönen nesneyi RenderMeasurements'a çevirir.
// Ölçüm mantığı Elevare CMS'in SEO analizöründen (elevare-seo.js v2.2) alındı; editöre özgü kısımlar (bileşen eşleme,
// canvas, Elevare işaretleri) çıkarıldı, karusel ve kırpma tespiti siteden bağımsız hale getirildi.
// Betik sayfaya hiçbir şey yazmaz; DOM'u yalnızca okur.
(options) => {
    'use strict';

    const MAX = options.maxElements || 50;
    const SCAN_CAP = options.scanCap || 5000;
    const isMobile = options.device === 'Mobile';
    const doc = document;
    const win = window;
    const vw = doc.documentElement.clientWidth;
    const vh = win.innerHeight;
    const all = Array.prototype.slice.call(doc.body ? doc.body.querySelectorAll('*') : [], 0, SCAN_CAP);

    // ---- yardımcılar -------------------------------------------------------------
    const style = (el) => win.getComputedStyle(el);
    const clip = (s, n) => { s = (s || '').replace(/\s+/g, ' ').trim(); return s.length > n ? s.slice(0, n) + '…' : s; };

    // Ekibin öğeyi kaynakta bulacağı kısa yol: en fazla üç ata, id ya da ilk iki sınıf.
    function selector(el) {
        const parts = [];
        for (let n = el; n && n.nodeType === 1 && parts.length < 4 && n !== doc.body; n = n.parentElement) {
            let part = n.tagName.toLowerCase();
            if (n.id && /^[A-Za-z][\w-]*$/.test(n.id)) { parts.unshift(part + '#' + n.id); break; }
            const classes = Array.prototype.filter.call(n.classList, (c) => /^[A-Za-z_-][\w-]*$/.test(c)).slice(0, 2);
            if (classes.length) part += '.' + classes.join('.');
            parts.unshift(part);
        }
        return clip(parts.join(' > '), 160);
    }

    const ownText = (el) => {
        const tag = el.tagName.toLowerCase();
        if (tag === 'input' || tag === 'textarea' || tag === 'select') return clip(el.value || el.placeholder || '', 40);
        let text = '';
        for (const node of el.childNodes) if (node.nodeType === 3) text += node.textContent;
        return clip(text, 40);
    };

    const visible = (el) => {
        const r = el.getBoundingClientRect();
        if (r.width <= 1 && r.height <= 1) return false;   // görünmez ya da sr-only (1×1 kırpılmış)
        const cs = style(el);
        return cs.display !== 'none' && cs.visibility !== 'hidden';
    };

    // Kendisi ya da bir atası gizli mi (display:none, visibility:hidden, opacity:0).
    const hiddenByAncestor = (el) => {
        for (let n = el; n && n !== doc.documentElement; n = n.parentElement) {
            const cs = style(n);
            if (cs.display === 'none' || cs.visibility === 'hidden' || parseFloat(cs.opacity) === 0) return true;
        }
        return false;
    };

    // html ve body dışında, taşan içeriği kırpan ya da kaydıran ilk ata. Karusel izleri ve kaydırılabilir tablolar
    // böyle bir atanın içindedir; sayfayı taşırmazlar.
    const clippingAncestor = (el) => {
        for (let n = el.parentElement; n && n !== doc.body && n !== doc.documentElement; n = n.parentElement) {
            const ox = style(n).overflowX;
            if (ox === 'hidden' || ox === 'clip' || ox === 'auto' || ox === 'scroll') return n;
        }
        return null;
    };

    // Yalnızca en içteki öğeleri tutar: taşan bir kapsayıcı ile içindeki taşan öğeden ekibe asıl sorumluyu göstermek için.
    const leaves = (els) => els.filter((el) => !els.some((o) => o !== el && el.contains(o)));

    const item = (el, value, limit) => ({ selector: selector(el), text: ownText(el) || null, value: Math.round(value * 100) / 100, limit: limit == null ? null : limit });
    const imageItem = (img, value) => ({ selector: selector(img), text: clip((img.currentSrc || img.getAttribute('src') || '').split('/').pop(), 60) || null, value: value, limit: null });
    const take = (list) => list.slice(0, MAX);

    // ---- renk ve kontrast (WCAG 2.x) ------------------------------------------------
    function parseColor(str) {
        const m = String(str || '').match(/rgba?\(\s*([\d.]+)\s*,\s*([\d.]+)\s*,\s*([\d.]+)\s*(?:,\s*([\d.]+)\s*)?\)/i);
        return m ? { r: +m[1], g: +m[2], b: +m[3], a: m[4] == null ? 1 : +m[4] } : null;
    }
    function luminance(c) {
        const f = (v) => { v /= 255; return v <= 0.03928 ? v / 12.92 : Math.pow((v + 0.055) / 1.055, 2.4); };
        return 0.2126 * f(c.r) + 0.7152 * f(c.g) + 0.0722 * f(c.b);
    }
    function contrastRatio(a, b) {
        const l1 = luminance(a), l2 = luminance(b);
        return (Math.max(l1, l2) + 0.05) / (Math.min(l1, l2) + 0.05);
    }
    function blend(top, under) {
        const a = top.a + under.a * (1 - top.a);
        if (a <= 0) return { r: 255, g: 255, b: 255, a: 0 };
        return {
            r: (top.r * top.a + under.r * under.a * (1 - top.a)) / a,
            g: (top.g * top.a + under.g * under.a * (1 - top.a)) / a,
            b: (top.b * top.a + under.b * under.a * (1 - top.a)) / a,
            a
        };
    }
    // Öğenin arkasındaki gerçek renk; arka planı görsel olan bir ataya gelinirse ölçülemez (null).
    function effectiveBg(el) {
        let acc = null;
        for (let n = el; n && n.nodeType === 1; n = n.parentElement) {
            const cs = style(n);
            if (cs.backgroundImage && cs.backgroundImage !== 'none') return null;
            const c = parseColor(cs.backgroundColor);
            if (c && c.a > 0) {
                acc = acc ? blend(acc, c) : c;
                if (acc.a >= 0.999) return acc;
            }
        }
        const base = { r: 255, g: 255, b: 255, a: 1 };
        return acc ? blend(acc, base) : base;
    }

    // ---- ölçümler ----------------------------------------------------------------
    const contrast = [], smallText = [];
    let textChecked = 0;
    for (const el of all) {
        if (!ownText(el) || !visible(el)) continue;
        if (el.closest('[aria-hidden="true"], :disabled, [aria-disabled="true"]')) continue;
        const cs = style(el);
        const fs = parseFloat(cs.fontSize) || 16;
        if (fs < 16) smallText.push(item(el, fs));
        const fg = parseColor(cs.color);
        const bg = fg && effectiveBg(el);
        if (!fg || !bg) continue;
        textChecked++;
        const weight = parseInt(cs.fontWeight, 10) || 400;
        const needed = fs >= 24 || (fs >= 18.66 && weight >= 700) ? 3 : 4.5;
        const ratio = contrastRatio(fg.a < 1 ? blend(fg, bg) : fg, bg);
        if (ratio < needed) contrast.push(item(el, ratio, needed));
    }

    const tapTargets = [];
    if (isMobile) {
        for (const el of doc.querySelectorAll('a[href], button, [role="button"], input[type="checkbox"], input[type="radio"], input[type="submit"], input[type="button"], select')) {
            if (!visible(el) || hiddenByAncestor(el)) continue;
            if (style(el).display === 'inline') continue;   // metin içi link (WCAG 2.5.8 istisnası)
            if (!(el.textContent || '').trim() && !el.querySelector('*') && el.tagName !== 'INPUT' && el.tagName !== 'SELECT') continue;
            const r = el.getBoundingClientRect();
            const side = Math.min(r.width, r.height);
            if (side < 48) tapTargets.push(item(el, side));
        }
    }

    // İlk ekran: görsel hem dikeyde hem yatayda viewport'ta, hiçbir atası gizlemiyor ve kırpan bir atanın dışında kalmıyor.
    // Karuselde yalnızca görünen slayt ilk ekrandadır; diğerleri kırpan atanın dışında kalır.
    const inFirstScreen = (img) => {
        const r = img.getBoundingClientRect();
        if (r.width <= 1 || r.height <= 1) return false;
        if (!(r.bottom > 0 && r.top < vh && r.right > 0 && r.left < vw)) return false;
        if (hiddenByAncestor(img)) return false;
        const clipper = clippingAncestor(img);
        if (clipper) {
            const c = clipper.getBoundingClientRect();
            if (r.right <= c.left + 1 || r.left >= c.right - 1) return false;
        }
        return true;
    };
    const lazyAbove = [], eagerBelow = [];
    for (const img of doc.querySelectorAll('img')) {
        const r = img.getBoundingClientRect();
        const pixel = r.width <= 1 && r.height <= 1 && (img.naturalWidth <= 1 || !img.complete);
        if (pixel) continue;   // izleme pikselleri
        const loading = (img.getAttribute('loading') || '').toLowerCase();
        if (inFirstScreen(img)) { if (loading === 'lazy') lazyAbove.push(imageItem(img, 0)); }
        else if (loading !== 'lazy') eagerBelow.push(imageItem(img, 0));
    }

    // Doğal orandan farklı çizilen görseller (Lighthouse'un %2 toleransı); object-fit fill dışı ve SVG bozulmaz.
    const distorted = [];
    for (const img of doc.querySelectorAll('img')) {
        if (!img.complete || !img.naturalWidth || !img.naturalHeight) continue;
        if (/\.svg(\?|#|$)|^data:image\/svg/i.test(img.currentSrc || img.getAttribute('src') || '')) continue;
        const r = img.getBoundingClientRect();
        if (r.width < 8 || r.height < 8) continue;
        const fit = style(img).objectFit;
        if (fit && fit !== 'fill') continue;
        const natural = img.naturalWidth / img.naturalHeight;
        const deviation = Math.abs((r.width / r.height) - natural) / natural;
        if (deviation > 0.02) distorted.push(imageItem(img, Math.round(deviation * 100)));
    }

    // Yatay taşma: belge viewport'tan geniş mi; taşanlardan kırpan bir atası olmayan en içtekiler sorumlu.
    const docWidth = doc.documentElement.scrollWidth;
    const overflow = docWidth > vw + 4
        ? leaves(all.filter((el) => { const r = el.getBoundingClientRect(); return r.width > 0 && (r.right > vw + 4 || r.left < -4) && !clippingAncestor(el) && style(el).position !== 'fixed'; }))
            .map((el) => item(el, el.getBoundingClientRect().width))
        : [];

    // Ekrandan geniş öğe: body/html taşmayı gizlese de içerik kesilir. Kendisi kaydırılabilir olan ya da kırpan bir atanın
    // içindeki öğe (karusel izi) sayılmaz.
    const wide = isMobile
        ? leaves(all.filter((el) => {
            const r = el.getBoundingClientRect();
            if (r.width <= vw + 4 || !visible(el) || clippingAncestor(el)) return false;
            const ox = style(el).overflowX;
            return ox !== 'auto' && ox !== 'scroll';
        })).map((el) => item(el, el.getBoundingClientRect().width))
        : [];

    // Ekranı kaplayan sabit katman (çoğunlukla çerez penceresi): ilk ekran ve kontrast o katmanı ölçer.
    const obstructed = all.some((el) => {
        const cs = style(el);
        if (cs.position !== 'fixed' || !visible(el) || parseFloat(cs.opacity) === 0) return false;
        const r = el.getBoundingClientRect();
        const w = Math.max(0, Math.min(r.right, vw) - Math.max(r.left, 0));
        const h = Math.max(0, Math.min(r.bottom, vh) - Math.max(r.top, 0));
        return w * h > vw * vh * 0.3;
    });

    const bySmallest = (a, b) => a.value - b.value;
    return {
        obstructed,
        measurements: {
            viewportWidth: vw,
            documentWidth: docWidth,
            overflowOffenders: take(overflow),
            wideElements: take(wide),
            contrastFailures: take(contrast.sort(bySmallest)),
            smallText: take(smallText.sort(bySmallest)),
            smallTapTargets: take(tapTargets.sort(bySmallest)),
            lazyAboveFold: take(lazyAbove),
            eagerBelowFold: take(eagerBelow),
            distortedImages: take(distorted.sort((a, b) => b.value - a.value)),
            elementsScanned: all.length,
            textElementsChecked: textChecked
        }
    };
}
