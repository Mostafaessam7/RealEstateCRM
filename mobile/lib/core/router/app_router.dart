import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../features/auth/application/auth_controller.dart';
import '../../features/auth/presentation/login_screen.dart';
import '../../features/dashboard/presentation/dashboard_screen.dart';
import '../../features/deals/presentation/deals_list_screen.dart';
import '../../features/leads/presentation/lead_detail_screen.dart';
import '../../features/leads/presentation/leads_list_screen.dart';
import 'app_shell.dart';

/// Bridges Riverpod state changes into a [Listenable] go_router can use as
/// `refreshListenable` — go_router re-evaluates `redirect` whenever this notifies.
class _AuthRouterRefresh extends ChangeNotifier {
  _AuthRouterRefresh(Ref ref) {
    ref.listen<AuthState>(authControllerProvider, (previous, next) {
      if (previous?.status != next.status) {
        notifyListeners();
      }
    });
  }
}

/// Route protection, as a pure function so it's testable without spinning up a GoRouter:
/// unauthenticated users are redirected to /login for any other route; an authenticated user
/// landing on /login is redirected to /dashboard. While auth status is still
/// [AuthStatus.unknown] (checking secure storage on startup) no redirect happens, so an
/// already-logged-in user never flashes the login screen.
String? computeAuthRedirect({
  required AuthStatus status,
  required String matchedLocation,
}) {
  final isLoggingIn = matchedLocation == '/login';

  if (status == AuthStatus.unknown) {
    return null;
  }
  if (status != AuthStatus.authenticated && !isLoggingIn) {
    return '/login';
  }
  if (status == AuthStatus.authenticated && isLoggingIn) {
    return '/dashboard';
  }
  return null;
}

final routerProvider = Provider<GoRouter>((ref) {
  final refresh = _AuthRouterRefresh(ref);

  return GoRouter(
    initialLocation: '/dashboard',
    refreshListenable: refresh,
    redirect: (context, state) => computeAuthRedirect(
      status: ref.read(authControllerProvider).status,
      matchedLocation: state.matchedLocation,
    ),
    // Without this, an unmatched route (a stale deep link, a bad path typed via
    // --dart-define testing, etc.) fell through to go_router's default error screen — a raw
    // "GoException" dump, not something a real user should ever see.
    errorBuilder: (context, state) => Scaffold(
      appBar: AppBar(title: const Text('Not found')),
      body: Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Icon(Icons.search_off, size: 48),
              const SizedBox(height: 12),
              const Text("This page doesn't exist."),
              const SizedBox(height: 16),
              FilledButton(
                onPressed: () => context.go('/dashboard'),
                child: const Text('Back to Dashboard'),
              ),
            ],
          ),
        ),
      ),
    ),
    routes: [
      GoRoute(path: '/login', builder: (context, state) => const LoginScreen()),
      StatefulShellRoute.indexedStack(
        builder: (context, state, navigationShell) =>
            AppShell(navigationShell: navigationShell),
        branches: [
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: '/dashboard',
                builder: (context, state) => const DashboardScreen(),
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: '/leads',
                builder: (context, state) => const LeadsListScreen(),
                routes: [
                  GoRoute(
                    path: ':id',
                    builder: (context, state) =>
                        LeadDetailScreen(leadId: state.pathParameters['id']!),
                  ),
                ],
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: '/deals',
                builder: (context, state) => const DealsListScreen(),
              ),
            ],
          ),
        ],
      ),
    ],
  );
});
