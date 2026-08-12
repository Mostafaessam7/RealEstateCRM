import 'dart:convert';

/// Decodes a JWT's payload client-side for routing/display only — the API independently
/// validates and enforces every claim on every request. This is never a security boundary.
class JwtDecoder {
  JwtDecoder._();

  static Map<String, dynamic> decode(String token) {
    final parts = token.split('.');
    if (parts.length != 3) {
      throw const FormatException('Invalid JWT');
    }

    final normalized = base64Url.normalize(parts[1]);
    final payload = utf8.decode(base64Url.decode(normalized));
    return jsonDecode(payload) as Map<String, dynamic>;
  }
}
