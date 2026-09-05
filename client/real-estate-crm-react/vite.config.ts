/// <reference types="vitest/config" />
import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  // VITE_API_BASE_URL lives in .env.development, which Vite loads only in development mode, and
  // .env is gitignored. So `npm run build` with nothing else set produces a bundle whose API base
  // URL is undefined -- and the app does not fail usefully on that. It renders "Loading…" forever
  // and throws "Cannot read properties of undefined (reading 'split')" from inside the minified
  // bundle, which says nothing about the real cause. That happened on 2026-09-05 and cost a while
  // to trace back to a missing environment variable.
  //
  // Failing here instead means the mistake is caught before anything is deployed, and the message
  // names the variable. Development is exempt because .env.development supplies it.
  const env = loadEnv(mode, process.cwd(), '')
  if (mode === 'production' && !env.VITE_API_BASE_URL) {
    throw new Error(
      'VITE_API_BASE_URL is not set, so this production build would ship with no API address and ' +
        'fail at runtime with an error that does not mention it.\n\n' +
        'Set it for the build, e.g.\n' +
        '  VITE_API_BASE_URL=https://api.example.com/api npm run build\n\n' +
        'or put it in .env.production for a fixed deployment target. ' +
        '.env.development already sets it for `npm run dev`.',
    )
  }

  return {
    plugins: [react()],
    test: {
      environment: 'jsdom',
      setupFiles: ['./src/test/setup.ts'],
      globals: true,
    },
  }
})
