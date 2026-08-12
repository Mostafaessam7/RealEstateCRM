import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/shared/utils/jwt_decoder.dart';

String _base64UrlEncode(String json) {
  final bytes = utf8.encode(json);
  return base64Url.encode(bytes).replaceAll('=', '');
}

String _fakeJwt(Map<String, dynamic> payload) {
  final header = _base64UrlEncode(jsonEncode({'alg': 'none', 'typ': 'JWT'}));
  final body = _base64UrlEncode(jsonEncode(payload));
  return '$header.$body.signature';
}

void main() {
  group('JwtDecoder', () {
    test('decodes a simple payload', () {
      final token = _fakeJwt({'sub': 'user-1', 'role': 'CompanyAdmin'});
      final claims = JwtDecoder.decode(token);

      expect(claims['sub'], 'user-1');
      expect(claims['role'], 'CompanyAdmin');
    });

    test('decodes an array claim and unicode characters', () {
      final token = _fakeJwt({
        'role': ['CompanyAdmin', 'SalesAgent'],
        'name': 'Nour Ünïcode',
      });
      final claims = JwtDecoder.decode(token);

      expect(claims['role'], ['CompanyAdmin', 'SalesAgent']);
      expect(claims['name'], 'Nour Ünïcode');
    });

    test('throws on a malformed token', () {
      expect(() => JwtDecoder.decode('not-a-jwt'), throwsFormatException);
    });
  });
}
