// Chart.js teması ve grafik kurucuları. Renkler CSS değişkenlerinden okunur; tema değişince grafikler yeniden çizilir.
// Kullanım: <canvas data-chart="order-comparison" data-source="#orders-json" data-threshold="5"></canvas>
(function () {
  'use strict';

  var builders = {};
  var instances = new Map();

  function cssVar(name) {
    return getComputedStyle(document.documentElement).getPropertyValue(name).trim().split(/\s+/).join(',');
  }
  function color(name, alpha) {
    return 'rgba(' + cssVar(name) + ',' + (alpha === undefined ? 1 : alpha) + ')';
  }

  function theme() {
    return {
      text: color('--fg-muted'),
      strong: color('--fg'),
      grid: color('--chart-grid'),
      surface: color('--surface'),
      line: color('--line-strong'),
      series: [color('--chart-1'), color('--chart-2'), color('--chart-3'), color('--chart-4')],
      critical: color('--critical-solid'),
      criticalSoft: color('--critical-solid', 0.18),
      ok: color('--ok-solid'),
      color: color
    };
  }

  function applyDefaults(t) {
    var d = window.Chart.defaults;
    d.font.family = '"IBM Plex Sans", ui-sans-serif, system-ui, sans-serif';
    d.font.size = 12;
    d.color = t.text;
    d.borderColor = t.grid;
    d.animation = window.matchMedia('(prefers-reduced-motion: reduce)').matches ? false : { duration: 250 };
    d.maintainAspectRatio = false;
    d.plugins.legend.labels.boxWidth = 10;
    d.plugins.legend.labels.boxHeight = 10;
    d.plugins.legend.labels.usePointStyle = true;
    d.elements.point.pointStyle = 'rect';
    d.elements.bar.borderRadius = 0;
    d.plugins.tooltip.backgroundColor = t.surface;
    d.plugins.tooltip.titleColor = t.strong;
    d.plugins.tooltip.bodyColor = t.text;
    d.plugins.tooltip.borderColor = t.line;
    d.plugins.tooltip.borderWidth = 1;
    d.plugins.tooltip.padding = 10;
    d.plugins.tooltip.bodyFont = { family: '"IBM Plex Mono", ui-monospace, monospace', size: 12 };
    d.plugins.tooltip.cornerRadius = 2;
    d.plugins.tooltip.boxPadding = 4;
  }

  var nf = new Intl.NumberFormat('tr-TR');
  function pct(v) { return (v < 0 ? '−' : v > 0 ? '+' : '') + '%' + Math.abs(v).toLocaleString('tr-TR', { maximumFractionDigits: 1 }); }

  // Dikey olay işaretleri (deploy, bulgu açılması) için küçük eklenti.
  var markerPlugin = {
    id: 'moMarkers',
    afterDatasetsDraw: function (chart, args, opts) {
      if (!opts || !opts.items || !opts.items.length) return;
      var ctx = chart.ctx, x = chart.scales.x, area = chart.chartArea;
      ctx.save();
      opts.items.forEach(function (m) {
        var idx = chart.data.labels.indexOf(m.label);
        if (idx < 0) return;
        var px = x.getPixelForValue(idx);
        ctx.strokeStyle = m.kind === 'finding' ? opts.findingColor : opts.deployColor;
        ctx.setLineDash([4, 3]);
        ctx.lineWidth = 1;
        ctx.beginPath(); ctx.moveTo(px, area.top); ctx.lineTo(px, area.bottom); ctx.stroke();
        ctx.setLineDash([]);
        ctx.fillStyle = ctx.strokeStyle;
        ctx.beginPath();
        if (m.kind === 'finding') { ctx.moveTo(px, area.top); ctx.lineTo(px + 5, area.top + 6); ctx.lineTo(px, area.top + 12); ctx.lineTo(px - 5, area.top + 6); }
        else { ctx.rect(px - 4, area.top, 8, 8); }
        ctx.fill();
      });
      ctx.restore();
    }
  };

  builders['order-comparison'] = function (data, t, el) {
    var threshold = parseFloat(el.dataset.threshold || '5');
    var over = data.map(function (d) { return d.diff !== null && d.diff !== undefined && Math.abs(d.diff) > threshold; });
    return {
      type: 'bar',
      data: {
        labels: data.map(function (d) { return d.label; }),
        datasets: [
          { label: 'Sipariş (veritabanı)', data: data.map(function (d) { return d.orders; }), backgroundColor: t.series[0], maxBarThickness: 16 },
          {
            label: 'GA4 purchase', data: data.map(function (d) { return d.purchases; }),
            backgroundColor: over.map(function (o, i) { return data[i].diff === null || data[i].diff === undefined ? t.series[1] : o ? t.critical : t.series[1]; }), maxBarThickness: 16
          }
        ]
      },
      options: {
        interaction: { mode: 'index', intersect: false },
        scales: {
          x: { grid: { display: false } },
          y: { beginAtZero: true, ticks: { callback: function (v) { return nf.format(v); } } }
        },
        plugins: {
          legend: { position: 'bottom', align: 'start' },
          tooltip: {
            callbacks: {
              label: function (c) { return ' ' + c.dataset.label + ': ' + nf.format(c.parsed.y); },
              afterBody: function (items) {
                var d = data[items[0].dataIndex];
                if (d.diff === null || d.diff === undefined) return ['Fark: —'];
                return ['Fark: ' + pct(d.diff) + (Math.abs(d.diff) > threshold ? ' (eşik aşıldı)' : '')];
              }
            }
          }
        }
      }
    };
  };

  builders['event-series'] = function (data, t) {
    return {
      type: 'line',
      data: {
        labels: data.map(function (d) { return d.label; }),
        datasets: [{ label: 'Sayı', data: data.map(function (d) { return d.count; }), borderColor: t.series[0], backgroundColor: t.series[0], borderWidth: 1.75, pointRadius: 0, pointHoverRadius: 3, tension: 0 }]
      },
      options: {
        interaction: { mode: 'index', intersect: false },
        scales: {
          x: { grid: { display: false }, ticks: { maxRotation: 0, autoSkip: true, maxTicksLimit: 6 } },
          y: { beginAtZero: true, ticks: { callback: function (v) { return nf.format(v); } } }
        },
        plugins: { legend: { display: false } }
      }
    };
  };

  builders['score-trend'] = function (payload, t) {
    var points = payload.points;
    return {
      type: 'line',
      data: {
        labels: points.map(function (p) { return p.label; }),
        datasets: [
          { label: 'Tracking', data: points.map(function (p) { return p.tracking; }), borderColor: t.series[1], backgroundColor: t.series[1] },
          { label: 'SEO', data: points.map(function (p) { return p.seo; }), borderColor: t.series[2], backgroundColor: t.series[2], borderDash: [6, 3] },
          { label: 'Performans', data: points.map(function (p) { return p.performance; }), borderColor: t.series[3], backgroundColor: t.series[3], borderDash: [2, 2] }
        ].map(function (ds) { return Object.assign(ds, { borderWidth: 1.75, pointRadius: 0, pointHoverRadius: 3, tension: 0 }); })
      },
      options: {
        interaction: { mode: 'index', intersect: false },
        layout: { padding: { top: 14 } },
        scales: {
          x: { grid: { display: false }, ticks: { maxRotation: 0, autoSkip: true, maxTicksLimit: 8 } },
          y: { min: 0, max: 100, ticks: { stepSize: 25 } }
        },
        plugins: {
          legend: { position: 'bottom', align: 'start' },
          moMarkers: { items: payload.events || [], deployColor: t.color('--fg-subtle'), findingColor: t.critical },
          tooltip: {
            callbacks: {
              afterBody: function (items) {
                var label = points[items[0].dataIndex].label;
                return (payload.events || []).filter(function (e) { return e.label === label; }).map(function (e) { return '• ' + e.text; });
              }
            }
          }
        }
      },
      plugins: [markerPlugin]
    };
  };

  function readData(el) {
    var src = document.querySelector(el.dataset.source);
    return src ? JSON.parse(src.textContent) : [];
  }

  function mount(el) {
    var build = builders[el.dataset.chart];
    if (!build || !window.Chart) return;
    if (instances.has(el)) instances.get(el).destroy();
    var t = theme();
    applyDefaults(t);
    instances.set(el, new window.Chart(el, build(readData(el), t, el)));
  }

  function mountAll(root) {
    (root || document).querySelectorAll('canvas[data-chart]').forEach(mount);
    // DOM'dan kaldırılmış grafiklerin örneklerini temizle.
    instances.forEach(function (chart, el) { if (!document.body.contains(el)) { chart.destroy(); instances.delete(el); } });
  }

  window.MOCharts = { register: function (name, fn) { builders[name] = fn; }, mountAll: mountAll };

  document.addEventListener('DOMContentLoaded', function () { mountAll(); });
  document.addEventListener('mo:themechange', function () { instances.forEach(function (_, el) { mount(el); }); });
  document.addEventListener('htmx:afterSettle', function (e) { mountAll(e.detail.elt); });
})();
