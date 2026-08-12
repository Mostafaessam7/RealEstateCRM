import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/core/network/api_client.dart';
import 'package:mobile/core/network/api_exception.dart';
import 'package:mobile/core/storage/token_storage.dart';

import 'fake_http_adapter.dart';

void main() {
  late InMemoryTokenStorage tokenStorage;
  late FakeHttpClientAdapter mainAdapter;
  late FakeHttpClientAdapter refreshAdapter;
  late Dio dio;
  late Dio refreshDio;
  late bool sessionExpiredCalled;
  late ApiClient client;

  setUp(() {
    tokenStorage = InMemoryTokenStorage();
    mainAdapter = FakeHttpClientAdapter();
    refreshAdapter = FakeHttpClientAdapter();
    dio = Dio(BaseOptions(baseUrl: 'https://api.test'))
      ..httpClientAdapter = mainAdapter;
    refreshDio = Dio(BaseOptions(baseUrl: 'https://api.test'))
      ..httpClientAdapter = refreshAdapter;
    sessionExpiredCalled = false;
    client = ApiClient(
      baseUrl: 'https://api.test',
      tokenStorage: tokenStorage,
      // Mirrors what AuthController.handleSessionExpired actually does in production —
      // ApiClient itself never touches tokenStorage.clear(), it only notifies the callback.
      onSessionExpired: () async {
        sessionExpiredCalled = true;
        await tokenStorage.clear();
      },
      dio: dio,
      refreshDio: refreshDio,
    );
  });

  test('injects the Bearer token from storage on every request', () async {
    await tokenStorage.setTokens(
      accessToken: 'access-1',
      refreshToken: 'refresh-1',
    );
    mainAdapter.enqueueJson('GET', '/v1/leads/1', 200, {
      'id': '1',
      'fullName': 'Test',
    });

    await client.run((dio) => dio.get<Map<String, dynamic>>('/v1/leads/1'));

    expect(
      mainAdapter.requests.single.headers['Authorization'],
      'Bearer access-1',
    );
  });

  test(
    'maps a failed request to an ApiException, preferring the ProblemDetails title',
    () async {
      mainAdapter.enqueue(
        'GET',
        '/v1/leads/missing',
        (options) => jsonResponseBody('{"title":"Not found."}', 404),
      );

      await expectLater(
        client.run((dio) => dio.get<Map<String, dynamic>>('/v1/leads/missing')),
        throwsA(
          isA<ApiException>().having((e) => e.message, 'message', 'Not found.'),
        ),
      );
    },
  );

  test(
    'on a 401, refreshes the token once and retries the original request',
    () async {
      await tokenStorage.setTokens(
        accessToken: 'expired',
        refreshToken: 'refresh-1',
      );

      var attempt = 0;
      ResponseBody responder(RequestOptions options) {
        attempt++;
        if (attempt == 1) {
          return jsonResponseBody('{"title":"Unauthorized"}', 401);
        }
        expect(options.headers['Authorization'], 'Bearer fresh-access');
        return jsonResponseBody('{"id":"1","fullName":"Test"}', 200);
      }

      // Queued twice: the original attempt (401) and the interceptor's retry (200) — the
      // fake adapter is a one-shot-per-enqueue queue, not a sticky stub.
      mainAdapter.enqueue('GET', '/v1/leads/1', responder);
      mainAdapter.enqueue('GET', '/v1/leads/1', responder);
      refreshAdapter.enqueueJson('POST', '/auth/refresh', 200, {
        'accessToken': 'fresh-access',
        'refreshToken': 'fresh-refresh',
      });

      final data = await client.run(
        (dio) => dio.get<Map<String, dynamic>>('/v1/leads/1'),
      );

      expect(data['fullName'], 'Test');
      expect(attempt, 2);
      expect(await tokenStorage.getAccessToken(), 'fresh-access');
      expect(sessionExpiredCalled, isFalse);
    },
  );

  test(
    'calls onSessionExpired and clears tokens when the refresh itself fails',
    () async {
      await tokenStorage.setTokens(
        accessToken: 'expired',
        refreshToken: 'bad-refresh',
      );

      mainAdapter.enqueue(
        'GET',
        '/v1/leads/1',
        (options) => jsonResponseBody('{"title":"Unauthorized"}', 401),
      );
      refreshAdapter.enqueue(
        'POST',
        '/auth/refresh',
        (options) => jsonResponseBody('{"title":"Invalid refresh token"}', 401),
      );

      await expectLater(
        client.run((dio) => dio.get<Map<String, dynamic>>('/v1/leads/1')),
        throwsA(isA<ApiException>()),
      );

      expect(sessionExpiredCalled, isTrue);
      expect(await tokenStorage.getAccessToken(), isNull);
    },
  );

  test(
    'deduplicates concurrent refreshes into a single refresh call',
    () async {
      await tokenStorage.setTokens(
        accessToken: 'expired',
        refreshToken: 'refresh-1',
      );

      mainAdapter.enqueue(
        'GET',
        '/v1/leads/1',
        (options) => jsonResponseBody('{"title":"Unauthorized"}', 401),
      );
      mainAdapter.enqueue(
        'GET',
        '/v1/leads/1',
        (options) => jsonResponseBody('{"id":"1"}', 200),
      );
      mainAdapter.enqueue(
        'GET',
        '/v1/deals/1',
        (options) => jsonResponseBody('{"title":"Unauthorized"}', 401),
      );
      mainAdapter.enqueue(
        'GET',
        '/v1/deals/1',
        (options) => jsonResponseBody('{"id":"1"}', 200),
      );
      refreshAdapter.enqueueJson('POST', '/auth/refresh', 200, {
        'accessToken': 'fresh-access',
        'refreshToken': 'fresh-refresh',
      });

      await Future.wait([
        client.run((dio) => dio.get<Map<String, dynamic>>('/v1/leads/1')),
        client.run((dio) => dio.get<Map<String, dynamic>>('/v1/deals/1')),
      ]);

      expect(refreshAdapter.requests.length, 1);
    },
  );
}
