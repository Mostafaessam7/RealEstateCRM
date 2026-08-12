import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/core/network/api_exception.dart';

RequestOptions _options() => RequestOptions(path: '/test');

void main() {
  group('mapDioException', () {
    test(
      'connectionError maps to a network error with retry-friendly copy',
      () {
        final result = mapDioException(
          DioException(
            requestOptions: _options(),
            type: DioExceptionType.connectionError,
          ),
        );

        expect(result.isNetworkError, isTrue);
        expect(result.message, contains('internet connection'));
      },
    );

    test('receiveTimeout maps to a network error', () {
      final result = mapDioException(
        DioException(
          requestOptions: _options(),
          type: DioExceptionType.receiveTimeout,
        ),
      );

      expect(result.isNetworkError, isTrue);
    });

    test('badResponse prefers the ProblemDetails title when present', () {
      final response = Response(
        requestOptions: _options(),
        statusCode: 400,
        data: {'title': 'Email is required.'},
      );
      final result = mapDioException(
        DioException(
          requestOptions: _options(),
          response: response,
          type: DioExceptionType.badResponse,
        ),
      );

      expect(result.message, 'Email is required.');
      expect(result.statusCode, 400);
      expect(result.isNetworkError, isFalse);
    });

    test(
      'badResponse falls back to a status-specific message without a title',
      () {
        final response = Response(
          requestOptions: _options(),
          statusCode: 403,
          data: <String, dynamic>{},
        );
        final result = mapDioException(
          DioException(
            requestOptions: _options(),
            response: response,
            type: DioExceptionType.badResponse,
          ),
        );

        expect(result.message, contains('permission'));
        expect(result.isUnauthorized, isFalse);
      },
    );

    test('401 response is flagged isUnauthorized', () {
      final response = Response(
        requestOptions: _options(),
        statusCode: 401,
        data: <String, dynamic>{},
      );
      final result = mapDioException(
        DioException(
          requestOptions: _options(),
          response: response,
          type: DioExceptionType.badResponse,
        ),
      );

      expect(result.isUnauthorized, isTrue);
    });

    test('500 falls back to a server-error message', () {
      final response = Response(
        requestOptions: _options(),
        statusCode: 500,
        data: <String, dynamic>{},
      );
      final result = mapDioException(
        DioException(
          requestOptions: _options(),
          response: response,
          type: DioExceptionType.badResponse,
        ),
      );

      expect(result.message, contains('server ran into a problem'));
    });
  });
}
