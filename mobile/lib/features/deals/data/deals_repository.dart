import '../../../core/network/api_client.dart';
import '../../../core/network/paged_result.dart';
import '../domain/deal.dart';

/// GET /api/v1/deals — read-only in this client, see docs/public-api.md.
class DealsRepository {
  DealsRepository(this._apiClient);

  final ApiClient _apiClient;

  Future<PagedResult<Deal>> list({
    int page = 1,
    int pageSize = 50,
    String? status,
  }) async {
    final data = await _apiClient.run(
      (dio) => dio.get<Map<String, dynamic>>(
        '/v1/deals',
        queryParameters: {
          'page': page,
          'pageSize': pageSize,
          'status': ?status,
        },
      ),
    );
    return PagedResult.fromJson(data, Deal.fromJson);
  }
}
