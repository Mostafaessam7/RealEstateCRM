import '../../../core/network/api_client.dart';

class AuthTokens {
  const AuthTokens({required this.accessToken, required this.refreshToken});

  final String accessToken;
  final String refreshToken;
}

/// Talks to /api/auth — the same endpoints the web app and the Public API's Bearer-auth
/// path use (see docs/public-api.md and docs/auth.md).
class AuthRepository {
  AuthRepository(this._apiClient);

  final ApiClient _apiClient;

  Future<AuthTokens> login({
    required String email,
    required String password,
  }) async {
    final data = await _apiClient.run(
      (dio) => dio.post<Map<String, dynamic>>(
        '/auth/login',
        data: {'email': email, 'password': password},
      ),
    );

    return AuthTokens(
      accessToken: data['accessToken'] as String,
      refreshToken: data['refreshToken'] as String,
    );
  }

  /// Best-effort — the caller has already cleared local tokens regardless of the outcome,
  /// mirroring the web app's logout behavior.
  Future<void> logout(String refreshToken) async {
    try {
      await _apiClient.run(
        (dio) => dio.post('/auth/logout', data: {'refreshToken': refreshToken}),
      );
    } catch (_) {
      // Deliberately swallowed — the user is logged out locally no matter what.
    }
  }
}
