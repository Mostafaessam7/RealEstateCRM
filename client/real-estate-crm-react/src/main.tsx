import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'

/**
 * Sentry starts only when VITE_SENTRY_DSN is set. Without it nothing initializes and nothing
 * leaves the browser, so a checkout with no DSN behaves exactly as before.
 *
 * Imported dynamically rather than statically so Sentry lands in its own chunk, fetched only by
 * deployments that actually use it. This app's main chunk is already ~449 kB, so adding an
 * always-present dependency for a feature that may never be switched on is not free.
 *
 * Guarding on the DSN also keeps development quiet: an unconfigured Sentry.init() still installs
 * global error and unhandled-rejection handlers, so every local error would detour through
 * Sentry's machinery before reaching the console where it is being read.
 */
async function startErrorReporting(): Promise<void> {
  const dsn = import.meta.env.VITE_SENTRY_DSN

  if (!dsn) {
    return
  }

  const Sentry = await import('@sentry/react')

  Sentry.init({
    dsn,
    environment: import.meta.env.MODE,

    // The default (1.0) sends a performance trace for every transaction, which exhausts the quota
    // on real traffic and then starts silently dropping the errors too -- the part actually worth
    // having.
    tracesSampleRate: 0.1,

    // This is a CRM: the data on screen belongs to the tenant's clients, not to us. No names,
    // emails or IP addresses leave the browser.
    sendDefaultPii: false,
  })
}

// Rendering does not wait on Sentry. Blocking first paint on a third-party network import would
// trade a real user-visible cost for error reporting that is only useful once something breaks --
// and if that import fails, the app should still start.
void startErrorReporting()

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
