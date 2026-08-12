import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/core/storage/token_storage.dart';
import 'package:mobile/features/auth/application/auth_controller.dart';
import 'package:mobile/features/auth/data/auth_repository.dart';
import 'package:mocktail/mocktail.dart';

class _MockAuthRepository extends Mock implements AuthRepository {}

String _base64UrlEncode(String json) =>
    base64Url.encode(utf8.encode(json)).replaceAll('=', '');

String _fakeJwt(Map<String, dynamic> payload) {
  final header = _base64UrlEncode(jsonEncode({'alg': 'none', 'typ': 'JWT'}));
  final body = _base64UrlEncode(jsonEncode(payload));
  return '$header.$body.signature';
}

void main() {
  late _MockAuthRepository repository;
  late InMemoryTokenStorage tokenStorage;

  setUp(() {
    repository = _MockAuthRepository();
    tokenStorage = InMemoryTokenStorage();
  });

  test('bootstraps to unauthenticated when no token is stored', () async {
    final controller = AuthController(repository, tokenStorage);
    await Future<void>.delayed(Duration.zero);

    expect(controller.state.status, AuthStatus.unauthenticated);
  });

  test('bootstraps to authenticated from a stored valid token', () async {
    final token = _fakeJwt({
      'sub': 'user-1',
      'role': 'CompanyAdmin',
      'company_id': 'company-1',
    });
    await tokenStorage.setTokens(accessToken: token, refreshToken: 'refresh-1');

    final controller = AuthController(repository, tokenStorage);
    await Future<void>.delayed(Duration.zero);

    expect(controller.state.status, AuthStatus.authenticated);
    expect(controller.state.user?.userId, 'user-1');
    expect(controller.state.user?.companyId, 'company-1');
    expect(controller.state.user?.roles, ['CompanyAdmin']);
  });

  test(
    'bootstraps to unauthenticated and clears storage on a corrupt stored token',
    () async {
      await tokenStorage.setTokens(
        accessToken: 'not-a-jwt',
        refreshToken: 'refresh-1',
      );

      final controller = AuthController(repository, tokenStorage);
      await Future<void>.delayed(Duration.zero);

      expect(controller.state.status, AuthStatus.unauthenticated);
      expect(await tokenStorage.getAccessToken(), isNull);
    },
  );

  test('login stores tokens and moves to authenticated', () async {
    final token = _fakeJwt({'sub': 'user-2', 'role': 'SalesAgent'});
    when(
      () => repository.login(
        email: any(named: 'email'),
        password: any(named: 'password'),
      ),
    ).thenAnswer(
      (_) async => AuthTokens(accessToken: token, refreshToken: 'refresh-2'),
    );

    final controller = AuthController(repository, tokenStorage);
    await Future<void>.delayed(Duration.zero);

    await controller.login(email: 'agent@test.local', password: 'password123');

    expect(controller.state.status, AuthStatus.authenticated);
    expect(controller.state.user?.userId, 'user-2');
    expect(await tokenStorage.getAccessToken(), token);
  });

  test('logout clears tokens and moves to unauthenticated', () async {
    final token = _fakeJwt({'sub': 'user-1', 'role': 'SalesAgent'});
    await tokenStorage.setTokens(accessToken: token, refreshToken: 'refresh-1');
    when(() => repository.logout(any())).thenAnswer((_) async {});

    final controller = AuthController(repository, tokenStorage);
    await Future<void>.delayed(Duration.zero);
    expect(controller.state.status, AuthStatus.authenticated);

    await controller.logout();

    expect(controller.state.status, AuthStatus.unauthenticated);
    expect(await tokenStorage.getAccessToken(), isNull);
    verify(() => repository.logout('refresh-1')).called(1);
  });

  test(
    'handleSessionExpired clears tokens and moves to unauthenticated',
    () async {
      final token = _fakeJwt({'sub': 'user-1', 'role': 'SalesAgent'});
      await tokenStorage.setTokens(
        accessToken: token,
        refreshToken: 'refresh-1',
      );

      final controller = AuthController(repository, tokenStorage);
      await Future<void>.delayed(Duration.zero);
      expect(controller.state.status, AuthStatus.authenticated);

      await controller.handleSessionExpired();

      expect(controller.state.status, AuthStatus.unauthenticated);
      expect(await tokenStorage.getAccessToken(), isNull);
    },
  );
}
