# Real Estate CRM — Mobile (Flutter)

A Flutter client for field agents, built against the Public API (`/api/v1`, see
[`docs/public-api.md`](../docs/public-api.md)). Same login as the web app — JWT bearer, same
roles/permissions, same tenant isolation. This replaces the earlier Expo/React Native client
(removed — see `docs/roadmap.md` for the migration record).

## Architecture

Clean, feature-based structure — mirrors the backend's Domain/Application/Infrastructure split
at a scale appropriate for a mobile client:

```text
lib/
├── app.dart                    MaterialApp.router — theme, routing
├── main.dart                   Entry point — ProviderScope
├── core/
│   ├── network/                ApiClient (Dio + auth/refresh interceptor), ApiException,
│   │                           PagedResult<T>
│   ├── storage/                TokenStorage (flutter_secure_storage; in-memory for tests)
│   ├── router/                 go_router config, auth redirect guard, bottom-tab shell
│   ├── connectivity/           Online/offline detection (connectivity_plus)
│   └── theme/                  Light/dark ThemeData, design tokens mirroring the web app
├── features/
│   ├── auth/                   domain (AuthUser) → data (AuthRepository) → application
│   │   (AuthController, a StateNotifier) → presentation (LoginScreen)
│   ├── dashboard/               same layered structure
│   ├── leads/                   same layered structure — list + detail
│   └── deals/                   same layered structure — list
└── shared/
    ├── widgets/                 StatusChip, LoadingView/ErrorView/EmptyView/OfflineBanner,
    │                            AsyncValueView (uniform AsyncValue<T> rendering)
    └── utils/                   JwtDecoder
```

Each feature follows `domain → data → application → presentation`:
- **domain**: plain Dart models (`Lead`, `Deal`, `DashboardSummary`, `AuthUser`).
- **data**: a `*Repository` — the only thing that talks to `ApiClient`.
- **application**: Riverpod providers wiring repositories to UI-consumable state
  (`FutureProvider`/`StateNotifierProvider`).
- **presentation**: screens/widgets, `ConsumerWidget`/`ConsumerStatefulWidget`.

## State management

**Riverpod** (`flutter_riverpod`) — `Provider`/`FutureProvider.autoDispose`/
`StateNotifierProvider`, sized for this app: no code generation, no global singleton services,
each provider is colocated with the feature it belongs to and composes via `ref.watch`.

## Networking

**Dio**, centralized in `ApiClient` — every repository goes through it, never a bare Dio call:
- injects `Authorization: Bearer <token>` on every request (from `TokenStorage`)
- on a 401, refreshes the access token once — concurrent 401s from simultaneous requests are
  de-duplicated into a single refresh call — then retries the original request; if the refresh
  itself fails, calls `onSessionExpired` (wired to `AuthController.handleSessionExpired`, same
  as the web app's `client.ts`)
- maps every failure to an `ApiException` (`message`, `statusCode`, `isNetworkError`) before it
  reaches a repository or screen — screens never inspect a raw `DioException`

## Auth, storage, and route protection

- Tokens live in `flutter_secure_storage` (iOS Keychain / Android Keystore) — `TokenStorage` is
  an interface with an `InMemoryTokenStorage` test double, so nothing in tests touches a real
  platform channel.
- `AuthController` (a `StateNotifier<AuthState>`) owns the three-state lifecycle: `unknown`
  (checking storage on startup) → `authenticated`/`unauthenticated`. Login/logout/session-expiry
  all flow through it.
- `go_router`'s `redirect` (a pure, unit-tested function — `computeAuthRedirect`) enforces route
  protection: unauthenticated users are bounced to `/login` from anywhere else; an authenticated
  user landing on `/login` is bounced to `/dashboard`. No redirect happens while status is
  `unknown`, so an already-logged-in user never flashes the login screen.

## UX: loading / empty / error / offline states

- `AsyncValueView<T>` renders any Riverpod `AsyncValue` with one standard loading/error/data
  treatment — no screen hand-rolls `.when(...)` UI.
- `ErrorView` distinguishes a real API error from `isNetworkError` (timeout/no-connection) and
  shows different copy/icon, plus a **Retry** button (`ref.invalidate(...)`).
- `EmptyView` gives each list its own contextual empty-state copy, not a generic "no data".
- `OfflineBanner` — a persistent banner driven by `connectivity_plus`, shown above content
  whenever the device reports no connectivity, independent of any specific failed request.
- Pull-to-refresh (`RefreshIndicator`) on every list/dashboard screen.

## Screens (feature parity with the previous Expo/React Native app)

- **Login** — email/password, client-side validation (required fields, email format),
  password-visibility toggle, server error surfaced inline.
- **Dashboard** — KPI grid (leads, conversion rate, deals, sales value, follow-ups, available
  units), pull-to-refresh.
- **Leads** — debounced search, status chip, tap-through to detail.
- **Lead detail** — full field readout, tap-to-call and tap-to-WhatsApp (`url_launcher`, native
  `tel:`/`wa.me` deep links — a capability the web app can't offer).
- **Deals** — read-only list with status and reservation date.

Read-only beyond login for this pass, same scope as before — write flows are a natural next
step (`POST`/`PUT /api/v1/leads` already exist on the backend).

## Theming

Light and dark `ThemeData` (`core/theme/app_theme.dart`), following the device's system setting
(`ThemeMode.system`) — no in-app toggle, simplest way to stay correct. Colors mirror the web
app's toned-down design tokens (`client/real-estate-crm-react/src/index.css`) so the product
reads as one system across platforms.

## RTL readiness

No Arabic/Hebrew translations are included (the product's UI language is English, same as the
web app) — but the app is structurally RTL-ready: Flutter's Material widgets are inherently
direction-aware via the ambient `Directionality`, and this codebase avoids hardcoded
left/right-specific layout (`EdgeInsets.symmetric`, `Row`/`Column`, logical alignment) in favor
of properties that flip automatically under a RTL locale. Adding a RTL locale later is a
localization/content task, not a layout rewrite.

## Run it

```bash
cd mobile
flutter pub get
flutter run   # then pick a device/emulator, or `-d chrome` for the web target
```

Point the API at your running backend via `--dart-define=API_BASE_URL=...` (defaults to
`http://10.0.2.2:5063/api`, which is how the Android emulator reaches the host machine's
`localhost` — override for a physical device or different environment).

## Validation

```bash
flutter pub get      # dependency resolution
dart format .         # formatting — clean
flutter analyze       # static analysis — 0 errors/warnings (2 optional style hints)
flutter test          # 35 tests — unit, widget, and service/repository tests — all passing
flutter build apk --debug   # Android — blocked, see below
flutter build web --release # succeeds — full production compile as a stronger substitute check
```

`.github/workflows/azure-deploy.yml`'s `test-mobile` job runs `pub get`/`dart format
--set-exit-if-changed`/`analyze`/`test` on every push to `main` and blocks deployment if any of
them fail — see `docs/deployment.md`.

- **`flutter pub get`**: resolves cleanly.
- **`dart format .`**: clean (enforced in CI via `dart format --output=none --set-exit-if-changed .`).
- **`flutter analyze`**: **0 errors, 0 warnings** — 2 `info`-level style hints
  (`use_null_aware_elements`, a newer Dart syntax preference) left as-is, not required.
- **`flutter test`**: **35/35 passing** — unit tests (`JwtDecoder`, `mapDioException`,
  `computeAuthRedirect`), service/repository tests against a fake `HttpClientAdapter`
  (`ApiClient`'s auth/refresh/retry/dedup logic, `LeadsRepository`), state tests
  (`AuthController` login/logout/bootstrap/session-expiry against a mocked repository), and
  widget tests (`LoginScreen` validation, `StatusChip`, app-boots-to-login).
- **`flutter build apk --debug`**: **fails** — `No Android SDK found. Try setting the
  ANDROID_HOME environment variable.` This sandboxed environment has no Android SDK, `adb`, or
  emulator installed (confirmed by direct inspection, not assumed) and no path to install one
  without a GUI-driven Android Studio setup. This is the one command in the list this
  environment genuinely cannot run — not a code defect.
- **`flutter build ios`**: not attempted — iOS builds require Xcode, which only runs on macOS;
  this environment is Windows. Structurally impossible here regardless of any Flutter setup.
- **`flutter build web --release`**: **succeeds** — a full production compile of the entire
  app (~2800 modules) to JavaScript/Hermes-equivalent web bundle. Not a substitute for a real
  Android/iOS device test, but strong evidence the app's code compiles and links correctly end
  to end, beyond what static analysis alone proves.
- **`npx expo-doctor`-equivalent**: N/A for Flutter; `flutter doctor -v` was run and confirms
  the Flutter SDK itself is fully functional (Chrome/web and Windows-desktop targets available)
  — Android and Visual-Studio-C++-based Windows-desktop builds are the only toolchains missing.

### Android release build note

`android/app/src/main/AndroidManifest.xml` carries `android.permission.INTERNET` — this was
missing before the QA-pass phase (only the debug/profile manifest overlays had it, which Flutter
adds automatically for hot-reload). Without it, a **release** build would have been unable to
make any network request at all — every screen is API-driven, so the app would have been
completely non-functional in production despite working fine under `flutter run` in debug.
Caught by direct manifest inspection, not a device test — verify on a real release-mode install
that network calls still work as the strongest remaining confirmation.

### What still requires a real device

Visual/layout QA, touch interaction, `tel:`/`wa.me` deep links actually opening the Phone/
WhatsApp apps, `flutter_secure_storage` against a real iOS Keychain/Android Keystore, and any
future push-notification/background work. Run `flutter run` locally with a simulator or device
to exercise these before shipping.
