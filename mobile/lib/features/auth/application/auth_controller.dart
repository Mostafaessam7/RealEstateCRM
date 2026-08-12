import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/storage/token_storage.dart';
import '../../../shared/utils/jwt_decoder.dart';
import '../data/auth_repository.dart';
import '../domain/auth_user.dart';

enum AuthStatus {
  /// Bootstrapping — checking for a previously-stored token. The router must not redirect
  /// while in this state, or an already-logged-in user would flash the login screen.
  unknown,
  authenticated,
  unauthenticated,
}

class AuthState {
  const AuthState({required this.status, this.user, this.error});

  const AuthState.unknown() : this(status: AuthStatus.unknown);
  const AuthState.authenticated(AuthUser user)
    : this(status: AuthStatus.authenticated, user: user);
  const AuthState.unauthenticated({String? error})
    : this(status: AuthStatus.unauthenticated, error: error);

  final AuthStatus status;
  final AuthUser? user;
  final String? error;

  bool get isAuthenticated => status == AuthStatus.authenticated;
}

class AuthController extends StateNotifier<AuthState> {
  AuthController(this._repository, this._tokenStorage)
    : super(const AuthState.unknown()) {
    _bootstrap();
  }

  final AuthRepository _repository;
  final TokenStorage _tokenStorage;

  Future<void> _bootstrap() async {
    final token = await _tokenStorage.getAccessToken();
    if (token == null) {
      state = const AuthState.unauthenticated();
      return;
    }

    try {
      state = AuthState.authenticated(_userFromToken(token));
    } catch (_) {
      await _tokenStorage.clear();
      state = const AuthState.unauthenticated();
    }
  }

  Future<void> login({required String email, required String password}) async {
    try {
      final tokens = await _repository.login(email: email, password: password);
      await _tokenStorage.setTokens(
        accessToken: tokens.accessToken,
        refreshToken: tokens.refreshToken,
      );
      state = AuthState.authenticated(_userFromToken(tokens.accessToken));
    } on ApiException catch (error) {
      state = AuthState.unauthenticated(error: error.message);
      rethrow;
    }
  }

  Future<void> logout() async {
    final refreshToken = await _tokenStorage.getRefreshToken();
    await _tokenStorage.clear();
    state = const AuthState.unauthenticated();
    if (refreshToken != null) {
      unawaited(_repository.logout(refreshToken));
    }
  }

  /// Called by ApiClient when a token refresh fails — the session is over.
  Future<void> handleSessionExpired() async {
    await _tokenStorage.clear();
    if (state.status != AuthStatus.unauthenticated) {
      state = const AuthState.unauthenticated();
    }
  }

  AuthUser _userFromToken(String token) {
    final claims = JwtDecoder.decode(token);
    final roleClaim = claims['role'];
    final roles = switch (roleClaim) {
      List<dynamic> list => list.map((e) => e.toString()).toList(),
      String single => [single],
      _ => <String>[],
    };

    return AuthUser(
      userId: claims['sub'] as String,
      companyId: claims['company_id'] as String?,
      roles: roles,
    );
  }
}

final tokenStorageProvider = Provider<TokenStorage>(
  (ref) => SecureTokenStorage(),
);

final apiBaseUrlProvider = Provider<String>((ref) {
  // Android emulator reaches the host machine via 10.0.2.2, not localhost. Override with
  // `--dart-define=API_BASE_URL=...` for a device or a different environment.
  return const String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'http://10.0.2.2:5063/api',
  );
});

final Provider<ApiClient> apiClientProvider = Provider<ApiClient>((ref) {
  final tokenStorage = ref.watch(tokenStorageProvider);
  final baseUrl = ref.watch(apiBaseUrlProvider);
  return ApiClient(
    baseUrl: baseUrl,
    tokenStorage: tokenStorage,
    onSessionExpired: () =>
        ref.read(authControllerProvider.notifier).handleSessionExpired(),
  );
});

final Provider<AuthRepository> authRepositoryProvider =
    Provider<AuthRepository>((ref) {
      return AuthRepository(ref.watch(apiClientProvider));
    });

final StateNotifierProvider<AuthController, AuthState> authControllerProvider =
    StateNotifierProvider<AuthController, AuthState>((ref) {
      return AuthController(
        ref.watch(authRepositoryProvider),
        ref.watch(tokenStorageProvider),
      );
    });
