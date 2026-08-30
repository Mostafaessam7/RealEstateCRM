import '../../../core/network/api_client.dart';
import '../../../core/network/paged_result.dart';
import '../domain/lead.dart';

/// GET /api/v1/leads[/​{id}] — see docs/public-api.md. Same pagination/search/status-filter
/// conventions as the internal API (docs/api.md).
class LeadsRepository {
  LeadsRepository(this._apiClient);

  final ApiClient _apiClient;

  Future<PagedResult<Lead>> list({
    int page = 1,
    int pageSize = 50,
    String? search,
    String? status,
  }) async {
    final data = await _apiClient.run(
      (dio) => dio.get<Map<String, dynamic>>(
        '/v1/leads',
        queryParameters: {
          'page': page,
          'pageSize': pageSize,
          if (search != null && search.isNotEmpty) 'search': search,
          'status': ?status,
        },
      ),
    );
    return PagedResult.fromJson(data, Lead.fromJson);
  }

  Future<Lead> getById(String id) async {
    final data = await _apiClient.run(
      (dio) => dio.get<Map<String, dynamic>>('/v1/leads/$id'),
    );
    return Lead.fromJson(data);
  }
}
