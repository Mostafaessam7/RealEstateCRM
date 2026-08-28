import mecodex from './design-system/tailwind-preset.js';
import animate from 'tailwindcss-animate';

/**
 * Tailwind is introduced alongside the existing hand-written CSS, not as a
 * replacement for it. The app has ~30 pages of working styles; rewriting them
 * all at once would be a large, untestable change with no user-visible benefit
 * on day one. New and reworked UI uses Tailwind utilities; existing CSS keeps
 * working and is migrated screen by screen.
 *
 * `preflight` is disabled deliberately. Tailwind's CSS reset would strip the
 * base styling the existing stylesheets rely on (headings, lists, form
 * controls), breaking every current page the moment this config is added. The
 * app already has its own reset in index.css.
 */
export default {
  presets: [mecodex],
  content: ['./index.html', './src/**/*.{ts,tsx}'],
  corePlugins: {
    preflight: false,
  },
  plugins: [animate],
};
