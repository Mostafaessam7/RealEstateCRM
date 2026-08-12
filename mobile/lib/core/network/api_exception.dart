import 'package:dio/dio.dart';

/// A user-friendly, already-classified error — screens never need to inspect a raw
/// DioException. [isNetworkError] distinguishes "no connectivity/timeout" (show a retry
/// affordance) from a real API error (show the message as-is).
class ApiException implements Exception {
  const ApiException(
    this.message, {
    this.statusCode,
    this.isNetworkError = false,
  });

  final String message;
  final int? statusCode;
  final bool isNetworkError;

  bool get isUnauthorized => statusCode == 401;

  @override
  String toString() => message;
}

/// Mirrors client/real-estate-crm-react/src/api/client.ts's getApiErrorMessage: prefers the
/// backend's ProblemDetails `title`, falls back to a sensible default per failure category.
ApiException mapDioException(DioException error) {
  switch (error.type) {
    case DioExceptionType.connectionTimeout:
    case DioExceptionType.sendTimeout:
    case DioExceptionType.receiveTimeout:
      return const ApiException(
        'The request timed out. Check your connection and try again.',
        isNetworkError: true,
      );
    case DioExceptionType.connectionError:
      return const ApiException(
        'No internet connection. Check your network and try again.',
        isNetworkError: true,
      );
    case DioExceptionType.badCertificate:
      return const ApiException(
        'Could not verify the server\'s certificate.',
        isNetworkError: true,
      );
    case DioExceptionType.cancel:
      return const ApiException('Request cancelled.');
    case DioExceptionType.badResponse:
      final statusCode = error.response?.statusCode;
      final title = _extractProblemDetailsTitle(error.response?.data);
      return ApiException(
        title ?? _defaultMessageForStatus(statusCode),
        statusCode: statusCode,
      );
    case DioExceptionType.unknown:
    default:
      return const ApiException(
        'Something went wrong. Please try again.',
        isNetworkError: true,
      );
  }
}

String? _extractProblemDetailsTitle(dynamic data) {
  if (data is Map && data['title'] is String) {
    return data['title'] as String;
  }
  return null;
}

String _defaultMessageForStatus(int? statusCode) {
  switch (statusCode) {
    case 400:
      return 'That request was invalid. Please check the details and try again.';
    case 401:
      return 'Your session has expired. Please sign in again.';
    case 403:
      return 'You don\'t have permission to do that.';
    case 404:
      return 'That item could not be found.';
    case 409:
      return 'That conflicts with existing data.';
    case 429:
      return 'Too many requests. Please wait a moment and try again.';
    default:
      if (statusCode != null && statusCode >= 500) {
        return 'The server ran into a problem. Please try again shortly.';
      }
      return 'Something went wrong. Please try again.';
  }
}
