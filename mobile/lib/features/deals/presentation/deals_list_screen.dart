import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../../core/connectivity/connectivity_service.dart';
import '../../../core/network/paged_result.dart';
import '../../../shared/widgets/async_value_view.dart';
import '../../../shared/widgets/state_views.dart';
import '../../../shared/widgets/status_chip.dart';
import '../application/deals_providers.dart';
import '../domain/deal.dart';

class DealsListScreen extends ConsumerWidget {
  const DealsListScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final deals = ref.watch(dealsListProvider);
    final isOnline = ref.watch(isOnlineProvider).valueOrNull ?? true;
    final currency = NumberFormat.currency(symbol: r'$', decimalDigits: 0);

    return Scaffold(
      appBar: AppBar(title: const Text('Deals')),
      body: Column(
        children: [
          if (!isOnline) const OfflineBanner(),
          Expanded(
            child: RefreshIndicator(
              onRefresh: () async => ref.invalidate(dealsListProvider),
              child: AsyncValueView<PagedResult<Deal>>(
                value: deals,
                onRetry: () => ref.invalidate(dealsListProvider),
                data: (context, result) {
                  if (result.items.isEmpty) {
                    return ListView(
                      physics: const AlwaysScrollableScrollPhysics(),
                      children: const [
                        SizedBox(height: 80),
                        EmptyView(
                          title: 'No deals found',
                          message:
                              'Deals you create on the web app will show up here.',
                          icon: Icons.handshake_outlined,
                        ),
                      ],
                    );
                  }

                  return ListView.separated(
                    physics: const AlwaysScrollableScrollPhysics(),
                    padding: const EdgeInsets.all(16),
                    itemCount: result.items.length,
                    separatorBuilder: (context, index) =>
                        const SizedBox(height: 8),
                    itemBuilder: (context, index) {
                      final deal = result.items[index];
                      return Card(
                        child: Padding(
                          padding: const EdgeInsets.all(14),
                          child: Row(
                            children: [
                              Expanded(
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Text(
                                      currency.format(deal.dealValue),
                                      style: const TextStyle(
                                        fontWeight: FontWeight.w700,
                                        fontSize: 15.5,
                                      ),
                                    ),
                                    const SizedBox(height: 3),
                                    Text(
                                      deal.reservationDate != null
                                          ? 'Reserved ${deal.reservationDate!.split('T').first}'
                                          : 'Not reserved yet',
                                      style: TextStyle(
                                        fontSize: 12,
                                        color: Theme.of(
                                          context,
                                        ).colorScheme.onSurfaceVariant,
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                              StatusChip(status: deal.status),
                            ],
                          ),
                        ),
                      );
                    },
                  );
                },
              ),
            ),
          ),
        ],
      ),
    );
  }
}
