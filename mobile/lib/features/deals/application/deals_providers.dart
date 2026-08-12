import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/paged_result.dart';
import '../../auth/application/auth_controller.dart';
import '../data/deals_repository.dart';
import '../domain/deal.dart';

final dealsRepositoryProvider = Provider<DealsRepository>((ref) {
  return DealsRepository(ref.watch(apiClientProvider));
});

final dealsListProvider = FutureProvider.autoDispose<PagedResult<Deal>>((ref) {
  return ref.watch(dealsRepositoryProvider).list();
});
