import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/core/router/app_router.dart';
import 'package:mobile/features/auth/application/auth_controller.dart';

void main() {
  group('computeAuthRedirect', () {
    test('does not redirect while auth status is still unknown', () {
      expect(
        computeAuthRedirect(
          status: AuthStatus.unknown,
          matchedLocation: '/dashboard',
        ),
        isNull,
      );
      expect(
        computeAuthRedirect(
          status: AuthStatus.unknown,
          matchedLocation: '/login',
        ),
        isNull,
      );
    });

    test(
      'redirects an unauthenticated user to /login from a protected route',
      () {
        expect(
          computeAuthRedirect(
            status: AuthStatus.unauthenticated,
            matchedLocation: '/dashboard',
          ),
          '/login',
        );
        expect(
          computeAuthRedirect(
            status: AuthStatus.unauthenticated,
            matchedLocation: '/leads',
          ),
          '/login',
        );
      },
    );

    test('does not redirect an unauthenticated user already on /login', () {
      expect(
        computeAuthRedirect(
          status: AuthStatus.unauthenticated,
          matchedLocation: '/login',
        ),
        isNull,
      );
    });

    test('redirects an authenticated user away from /login to /dashboard', () {
      expect(
        computeAuthRedirect(
          status: AuthStatus.authenticated,
          matchedLocation: '/login',
        ),
        '/dashboard',
      );
    });

    test('does not redirect an authenticated user on a protected route', () {
      expect(
        computeAuthRedirect(
          status: AuthStatus.authenticated,
          matchedLocation: '/dashboard',
        ),
        isNull,
      );
      expect(
        computeAuthRedirect(
          status: AuthStatus.authenticated,
          matchedLocation: '/leads/123',
        ),
        isNull,
      );
    });
  });
}
