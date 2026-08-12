import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';

typedef FakeResponder = ResponseBody Function(RequestOptions options);

/// A JSON response body with the content-type header Dio needs to auto-parse `data` as a
/// Map — without it, `response.data` stays a raw String and ProblemDetails-title extraction
/// (see api_exception.dart) silently no-ops.
ResponseBody jsonResponseBody(String json, int statusCode) {
  return ResponseBody.fromString(
    json,
    statusCode,
    headers: {
      Headers.contentTypeHeader: [Headers.jsonContentType],
    },
  );
}

/// A minimal, in-memory [HttpClientAdapter] for tests — no real network I/O. Responses are
/// queued per HTTP method+path via [enqueue]; each call to [fetch] consumes the next queued
/// response for that key (or falls back to a 404 if nothing was queued).
class FakeHttpClientAdapter implements HttpClientAdapter {
  final Map<String, List<FakeResponder>> _queues = {};
  final List<RequestOptions> requests = [];

  void enqueue(String method, String path, FakeResponder responder) {
    final key = '${method.toUpperCase()} $path';
    _queues.putIfAbsent(key, () => []).add(responder);
  }

  void enqueueJson(
    String method,
    String path,
    int statusCode,
    Map<String, dynamic> body,
  ) {
    enqueue(
      method,
      path,
      (options) => jsonResponseBody(jsonEncode(body), statusCode),
    );
  }

  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) async {
    requests.add(options);
    final key = '${options.method.toUpperCase()} ${options.path}';
    final queue = _queues[key];
    if (queue == null || queue.isEmpty) {
      return ResponseBody.fromString(
        '{"title":"Not found in fake adapter"}',
        404,
      );
    }
    return queue.removeAt(0)(options);
  }

  @override
  void close({bool force = false}) {}
}
