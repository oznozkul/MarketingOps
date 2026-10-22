/** Tasarım token'ları Styles/app.css içinde CSS değişkeni olarak tanımlıdır; burada Tailwind'e bağlanır. */
const v = (name) => `rgb(var(${name}) / <alpha-value>)`;
const tone = (name) => ({
  DEFAULT: v(`--${name}-fg`),
  soft: v(`--${name}-bg`),
  line: v(`--${name}-line`),
  solid: v(`--${name}-solid`),
});

module.exports = {
  darkMode: 'class',
  content: [
    './Views/**/*.cshtml',
    './TagHelpers/**/*.cs',
    './ViewComponents/**/*.cs',
    './Infrastructure/**/*.cs',
    './wwwroot/js/**/*.js',
  ],
  theme: {
    extend: {
      colors: {
        canvas: v('--canvas'),
        surface: { DEFAULT: v('--surface'), 2: v('--surface-2'), 3: v('--surface-3') },
        line: { DEFAULT: v('--line'), strong: v('--line-strong') },
        fg: { DEFAULT: v('--fg'), muted: v('--fg-muted'), subtle: v('--fg-subtle') },
        primary: {
          DEFAULT: v('--primary'),
          hover: v('--primary-hover'),
          fg: v('--primary-fg'),
          soft: v('--primary-soft'),
          text: v('--primary-text'),
        },
        focus: v('--focus'),
        critical: tone('critical'),
        high: tone('high'),
        medium: tone('medium'),
        low: tone('low'),
        ok: tone('ok'),
        warn: tone('warn'),
      },
      fontFamily: {
        sans: ['"IBM Plex Sans"', 'ui-sans-serif', 'system-ui', 'sans-serif'],
        mono: ['"IBM Plex Mono"', 'ui-monospace', 'SFMono-Regular', 'Menlo', 'monospace'],
      },
      fontSize: {
        '2xs': ['0.6875rem', { lineHeight: '1rem' }],
      },
      // Swiss: köşeler neredeyse keskin.
      borderRadius: {
        sm: '2px',
        DEFAULT: '3px',
        md: '3px',
        lg: '4px',
      },
      zIndex: {
        sticky: '20',
        overlay: '30',
        drawer: '40',
        modal: '50',
        toast: '60',
      },
      // Düz tasarım: yüzeylerde gölge yok; yalnızca üstte açılan katmanlar (menü, drawer) ayrışır.
      boxShadow: {
        panel: 'none',
        overlay: '0 8px 24px rgb(0 0 0 / 0.12), 0 0 0 1px rgb(0 0 0 / 0.02)',
      },
      maxWidth: {
        content: '1440px',
      },
    },
  },
  plugins: [],
};
