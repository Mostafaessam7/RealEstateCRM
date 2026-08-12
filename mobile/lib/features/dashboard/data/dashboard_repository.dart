import '../../../core/network/api_client.dart';
import '../domain/dashboard_summary.dart';

/// GET /api/v1/dashboard/summary — see docs/public-api.md.
class DashboardRepository {
  DashboardRepository(this._apiClient);

  final ApiClient _apiClient;

  Future<DashboardSummary> getSummary() async {
    final data = await _apiClient.run(
      (dio) => dio.get<Map<String, dynamic>>('/v1/dashboard/summary'),
    );
    return DashboardSummary.fromJson(data);
  }
}
