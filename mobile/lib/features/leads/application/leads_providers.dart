import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/paged_result.dart';
import '../../auth/application/auth_controller.dart';
import '../data/leads_repository.dart';
import '../domain/lead.dart';

final leadsRepositoryProvider = Provider<LeadsRepository>((ref) {
  return LeadsRepository(ref.watch(apiClientProvider));
});

/// Keyed by the current search text so typing a new query fetches fresh results while the
/// previous query's result stays cached (autoDispose still evicts unused entries).
final leadsListProvider = FutureProvider.autoDispose
    .family<PagedResult<Lead>, String>((ref, search) {
      return ref
          .watch(leadsRepositoryProvider)
          .list(search: search.isEmpty ? null : search);
    });

final leadDetailProvider = FutureProvider.autoDispose.family<Lead, String>((
  ref,
  id,
) {
  return ref.watch(leadsRepositoryProvider).getById(id);
});
