import 'package:dio/dio.dart';

import 'api_exception.dart';
import '../storage/token_storage.dart';

/// Centralized HTTP client — every feature repository goes through this, never a bare Dio.
/// Mirrors client/real-estate-crm-react/src/api/client.ts:
///  - injects `Authorization: Bearer <token>` on every request
///  - on a 401, refreshes the access token once (de-duplicated across concurrent requests)
///    and retries the original request; if the refresh itself fails, calls [onSessionExpired]
///    so the app can drop back to the login screen
///  - maps every failure to an [ApiException] before it reaches a repository/controller
class ApiClient {
  ApiClient({
    required String baseUrl,
    required this.tokenStorage,
    required this.onSessionExpired,
    Dio? dio,
    Dio? refreshDio,
  }) : dio =
           dio ??
           Dio(
             BaseOptions(
               baseUrl: baseUrl,
               connectTimeout: const Duration(seconds: 15),
               receiveTimeout: const Duration(seconds: 15),
             ),
           ),
       _refreshDio = refreshDio ?? Dio(BaseOptions(baseUrl: baseUrl)) {
    this.dio.interceptors.add(
      InterceptorsWrapper(onRequest: _onRequest, onError: _onError),
    );
  }

  final Dio dio;
  final TokenStorage tokenStorage;

  /// A separate Dio instance for POST /auth/refresh — deliberately has none of [dio]'s
  /// interceptors (an infinite loop risk if the refresh call itself ever 401s), and is
  /// injectable so tests can point it at a fake adapter independently of [dio].
  final Dio _refreshDio;

  final Future<void> Function() onSessionExpired;

  Future<String?>? _refreshInFlight;

  Future<void> _onRequest(
    RequestOptions options,
    RequestInterceptorHandler handler,
  ) async {
    final token = await tokenStorage.getAccessToken();
    if (token != null) {
      options.headers['Authorization'] = 'Bearer $token';
    }
    handler.next(options);
  }

  Future<void> _onError(
    DioException error,
    ErrorInterceptorHandler handler,
  ) async {
    final isUnauthorized = error.response?.statusCode == 401;
    final alreadyRetried = error.requestOptions.extra['retried'] == true;

    if (!isUnauthorized || alreadyRetried) {
      handler.reject(error);
      return;
    }

    final newToken = await _refreshAccessToken();
    if (newToken == null) {
      await onSessionExpired();
      handler.reject(error);
      return;
    }

    try {
      final retryOptions = error.requestOptions;
      retryOptions.extra['retried'] = true;
      retryOptions.headers['Authorization'] = 'Bearer $newToken';
      final response = await dio.fetch(retryOptions);
      handler.resolve(response);
    } on DioException catch (retryError) {
      handler.reject(retryError);
    }
  }

  /// Only one refresh runs at a time even if several requests 401 simultaneously — every
  /// caller awaits the same in-flight Future rather than each firing its own refresh call.
  Future<String?> _refreshAccessToken() {
    return _refreshInFlight ??= _doRefresh().whenComplete(
      () => _refreshInFlight = null,
    );
  }

  Future<String?> _doRefresh() async {
    final refreshToken = await tokenStorage.getRefreshToken();
    if (refreshToken == null) {
      return null;
    }

    try {
      final response = await _refreshDio.post<Map<String, dynamic>>(
        '/auth/refresh',
        data: {'refreshToken': refreshToken},
      );
      final data = response.data!;
      final accessToken = data['accessToken'] as String;
      final newRefreshToken = data['refreshToken'] as String;
      await tokenStorage.setTokens(
        accessToken: accessToken,
        refreshToken: newRefreshToken,
      );
      return accessToken;
    } catch (_) {
      return null;
    }
  }

  Future<T> run<T>(Future<Response<T>> Function(Dio dio) request) async {
    try {
      final response = await request(dio);
      return response.data as T;
    } on DioException catch (error) {
      throw mapDioException(error);
    }
  }
}
