// MarketingOps istemci davranışları: tema, drawer, toast, kopyalama ve htmx entegrasyonu.
// Alpine store'ları alpine:init sırasında kaydedilir; bu dosya Alpine'den önce yüklenmelidir.
(function () {
  'use strict';

  var THEME_KEY = 'mo-theme';
  var media = window.matchMedia('(prefers-color-scheme: dark)');

  function storedTheme() {
    try { return localStorage.getItem(THEME_KEY) || 'system'; } catch (e) { return 'system'; }
  }

  function applyTheme(mode) {
    var dark = mode === 'dark' || (mode === 'system' && media.matches);
    document.documentElement.classList.toggle('dark', dark);
    document.dispatchEvent(new CustomEvent('mo:themechange', { detail: { dark: dark } }));
  }

  media.addEventListener('change', function () {
    if (storedTheme() === 'system') applyTheme('system');
  });

  document.addEventListener('alpine:init', function () {
    var Alpine = window.Alpine;

    Alpine.store('theme', {
      mode: storedTheme(),
      set: function (mode) {
        this.mode = mode;
        try { localStorage.setItem(THEME_KEY, mode); } catch (e) { /* gizli pencere */ }
        applyTheme(mode);
      },
      label: function () {
        return { light: 'Açık', dark: 'Koyu', system: 'Sistem' }[this.mode];
      }
    });

    Alpine.store('drawer', {
      open: false,
      title: '',
      show: function (title) { this.title = title || ''; this.open = true; },
      close: function () { this.open = false; }
    });

    // Canlı tarama ilerlemesi. Sunucudan 2 sn'de bir gelen değerlerden hız ve kalan süre burada hesaplanır.
    var stageNames = { Discovery: 'Keşif', Download: 'İndirme', Render: 'Render', RuleEvaluation: 'Kural değerlendirme', ConnectorSync: 'Connector senkronu' };
    var nf = new Intl.NumberFormat('tr-TR');
    Alpine.data('crawlLive', function (initial) {
      // Getter'lı nesne hedef olmalı: Object.assign kaynaktaki getter'ları bir kez çalıştırıp değerini sabitler.
      return Object.assign({
        currentStage: null,
        get done() { return this.pagesCrawled + this.pagesFailed; },
        get percent() { return this.pagesDiscovered ? Math.min(100, Math.round(this.done * 100 / this.pagesDiscovered)) : 0; },
        get stageName() { return this.status === 'Queued' ? 'Kuyrukta, işçi bekleniyor' : (stageNames[this.currentStage] || 'Başlıyor'); },
        get speed() {
          if (!this.startedAt) return null;
          var seconds = (Date.now() - new Date(this.startedAt).getTime()) / 1000;
          return seconds > 1 && this.done > 0 ? this.done / seconds : null;
        },
        get speedText() { return this.speed ? this.speed.toLocaleString('tr-TR', { maximumFractionDigits: 1 }) + ' sayfa/sn' : '—'; },
        get etaText() {
          var remaining = this.pagesDiscovered - this.done;
          if (!this.speed || remaining <= 0) return '—';
          var s = Math.round(remaining / this.speed);
          return s < 60 ? s + ' sn' : Math.round(s / 60) + ' dk';
        },
        n: function (v) { return nf.format(v || 0); },
        onProgress: function (e) {
          if (e.detail.crawlId !== this.crawlId) return;
          Object.assign(this, e.detail);
        },
        onFinished: function (e) {
          // Bitmiş taramanın sonuçları sunucuda; sayfa yenilenir.
          if (e.detail.crawlId === this.crawlId) setTimeout(function () { location.reload(); }, 600);
        }
      }, initial);
    });

    // Bulgu listesi: satır seçimi (toplu işlem) ve kolon görünürlüğü. Kolon tercihi tarayıcıda saklanır.
    var columnsKey = 'mo.findings.columns';
    Alpine.data('findingsList', function () {
      var saved = {};
      try { saved = JSON.parse(localStorage.getItem(columnsKey) || '{}'); } catch (e) { saved = {}; }
      function rowBoxes() { return Array.from(document.querySelectorAll('#findings-results input[data-row]')); }
      return {
        selected: [],
        cols: Object.assign({ category: true, pageType: true, affected: true, firstSeen: true, lastSeen: true, status: true, task: true }, saved),
        get allSelected() {
          var boxes = rowBoxes();
          var selected = this.selected;
          return boxes.length > 0 && boxes.every(function (b) { return selected.indexOf(b.value) >= 0; });
        },
        toggleAll: function (checked) { this.selected = checked ? rowBoxes().map(function (b) { return b.value; }) : []; },
        saveCols: function () { try { localStorage.setItem(columnsKey, JSON.stringify(this.cols)); } catch (e) { /* gizli pencere */ } }
      };
    });

    Alpine.store('toasts', {
      items: [],
      seq: 0,
      push: function (message, kind) {
        var id = ++this.seq;
        this.items.push({ id: id, message: message, kind: kind || 'ok' });
        var self = this;
        setTimeout(function () { self.dismiss(id); }, kind === 'error' ? 7000 : 4000);
      },
      dismiss: function (id) {
        this.items = this.items.filter(function (t) { return t.id !== id; });
      }
    });
  });

  // Yönlendirmeden sonra sunucunun bıraktığı tek seferlik başarı mesajı (TempData → #flash-message).
  document.addEventListener('alpine:initialized', function () {
    var flash = document.getElementById('flash-message');
    if (flash) toast(JSON.parse(flash.textContent), 'ok');
  });

  function toast(message, kind) {
    if (window.Alpine) window.Alpine.store('toasts').push(message, kind);
  }
  window.MOToast = toast;

  // Sunucu HX-Trigger: {"toast": {"message": "...", "kind": "ok|error"}}
  document.body.addEventListener('toast', function (e) {
    toast(e.detail.message, e.detail.kind);
  });

  // Drawer: hedefi #drawer-body olan her htmx isteği drawer'ı iskeletle açar.
  var drawerSkeleton = document.getElementById('drawer-skeleton');
  document.body.addEventListener('htmx:beforeRequest', function (e) {
    var target = e.detail.target;
    if (!target || target.id !== 'drawer-body') return;
    var title = e.detail.elt.getAttribute('data-drawer-title') || '';
    if (drawerSkeleton) target.innerHTML = drawerSkeleton.innerHTML;
    window.Alpine.store('drawer').show(title);
  });

  document.body.addEventListener('htmx:responseError', function (e) {
    var status = e.detail.xhr ? e.detail.xhr.status : 0;
    toast(status === 404 ? 'İstenen kayıt bulunamadı.' : 'İstek tamamlanamadı (' + status + '). Tekrar deneyin.', 'error');
  });
  document.body.addEventListener('htmx:sendError', function () {
    toast('Sunucuya ulaşılamadı. Bağlantınızı kontrol edin.', 'error');
  });

  // [data-copy="metin"] ya da [data-copy-target="#id"] ile panoya kopyalama.
  document.addEventListener('click', function (e) {
    var btn = e.target.closest('[data-copy], [data-copy-target]');
    if (!btn) return;
    var text = btn.getAttribute('data-copy');
    if (text === null) {
      var el = document.querySelector(btn.getAttribute('data-copy-target'));
      text = el ? el.innerText : '';
    }
    navigator.clipboard.writeText(text).then(
      function () { toast(btn.getAttribute('data-copy-message') || 'Panoya kopyalandı.', 'ok'); },
      function () { toast('Kopyalanamadı. Metni elle seçip kopyalayın.', 'error'); }
    );
  });

  // SignalR: açık sitenin tarama ilerlemesini ve okunmamış bildirim sayısını dinle; olaylar DOM'a yayılır, Alpine bileşenleri kendini günceller.
  var liveSiteId = document.body.dataset.siteId;
  if (liveSiteId && window.signalR) {
    var hub = new signalR.HubConnectionBuilder().withUrl('/hubs/crawls').withAutomaticReconnect().build();
    hub.on('crawlProgress', function (p) { window.dispatchEvent(new CustomEvent('mo:crawl-progress', { detail: p })); });
    hub.on('crawlFinished', function (p) { window.dispatchEvent(new CustomEvent('mo:crawl-finished', { detail: p })); });
    hub.on('notificationCount', function (p) { window.dispatchEvent(new CustomEvent('mo:notification-count', { detail: p })); });
    var join = function () { return hub.invoke('JoinSite', liveSiteId); };
    hub.onreconnected(join);
    hub.start().then(join).catch(function () { /* canlı ilerleme olmadan da sayfa çalışır */ });
  }

  // htmx isteklerine antiforgery token'ı ekle.
  document.body.addEventListener('htmx:configRequest', function (e) {
    var token = document.querySelector('meta[name="request-verification-token"]');
    if (token && e.detail.verb !== 'get') e.detail.headers['RequestVerificationToken'] = token.content;
  });
})();
