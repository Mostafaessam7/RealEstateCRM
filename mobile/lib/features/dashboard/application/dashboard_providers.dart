import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../auth/application/auth_controller.dart';
import '../data/dashboard_repository.dart';
import '../domain/dashboard_summary.dart';

final dashboardRepositoryProvider = Provider<DashboardRepository>((ref) {
  return DashboardRepository(ref.watch(apiClientProvider));
});

final dashboardSummaryProvider = FutureProvider.autoDispose<DashboardSummary>((
  ref,
) {
  return ref.watch(dashboardRepositoryProvider).getSummary();
});
