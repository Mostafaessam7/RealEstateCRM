import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/connectivity/connectivity_service.dart';
import '../../../core/theme/app_theme.dart';
import '../../../shared/widgets/async_value_view.dart';
import '../../../shared/widgets/state_views.dart';
import '../../auth/application/auth_controller.dart';
import '../application/dashboard_providers.dart';
import '../domain/dashboard_summary.dart';

class DashboardScreen extends ConsumerWidget {
  const DashboardScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final summary = ref.watch(dashboardSummaryProvider);
    final isOnline = ref.watch(isOnlineProvider).valueOrNull ?? true;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Dashboard'),
        actions: [
          IconButton(
            icon: const Icon(Icons.logout_rounded),
            tooltip: 'Sign out',
            onPressed: () => ref.read(authControllerProvider.notifier).logout(),
          ),
        ],
      ),
      body: Column(
        children: [
          if (!isOnline) const OfflineBanner(),
          Expanded(
            child: RefreshIndicator(
              onRefresh: () async => ref.invalidate(dashboardSummaryProvider),
              child: AsyncValueView<DashboardSummary>(
                value: summary,
                onRetry: () => ref.invalidate(dashboardSummaryProvider),
                data: (context, data) => ListView(
                  padding: const EdgeInsets.all(16),
                  physics: const AlwaysScrollableScrollPhysics(),
                  children: [_StatGrid(summary: data)],
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _StatGrid extends StatelessWidget {
  const _StatGrid({required this.summary});

  final DashboardSummary summary;

  @override
  Widget build(BuildContext context) {
    final stats = <_Stat>[
      _Stat(
        'Total Leads',
        summary.totalLeads.toString(),
        Icons.people_outline_rounded,
        AppColors.primary,
      ),
      _Stat(
        'New Leads (30d)',
        summary.newLeadsLast30Days.toString(),
        Icons.person_add_alt_1_outlined,
        AppColors.info,
      ),
      _Stat(
        'Conversion Rate',
        '${summary.conversionRatePercent.toStringAsFixed(0)}%',
        Icons.trending_up_rounded,
        AppColors.success,
      ),
      _Stat(
        'Total Deals',
        summary.totalDeals.toString(),
        Icons.handshake_outlined,
        AppColors.primary,
      ),
      _Stat(
        'Sales Value',
        summary.totalSalesValue.toStringAsFixed(0),
        Icons.payments_outlined,
        AppColors.success,
      ),
      _Stat(
        'Follow-ups Due',
        summary.upcomingFollowUps.toString(),
        Icons.event_available_outlined,
        AppColors.warning,
      ),
      _Stat(
        'Available Units',
        summary.totalAvailableUnits.toString(),
        Icons.door_front_door_outlined,
        AppColors.info,
      ),
    ];

    return GridView.builder(
      shrinkWrap: true,
      physics: const NeverScrollableScrollPhysics(),
      itemCount: stats.length,
      gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
        crossAxisCount: 2,
        mainAxisSpacing: 12,
        crossAxisSpacing: 12,
        childAspectRatio: 1.5,
      ),
      itemBuilder: (context, index) {
        final stat = stats[index];
        return Card(
          child: Padding(
            padding: const EdgeInsets.all(14),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Container(
                  width: 34,
                  height: 34,
                  decoration: BoxDecoration(
                    color: stat.color.withValues(alpha: 0.12),
                    borderRadius: BorderRadius.circular(9),
                  ),
                  child: Icon(stat.icon, size: 17, color: stat.color),
                ),
                const Spacer(),
                Text(
                  stat.value,
                  style: const TextStyle(
                    fontSize: 20,
                    fontWeight: FontWeight.w700,
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  stat.label,
                  style: TextStyle(
                    fontSize: 11.5,
                    color: Theme.of(context).colorScheme.onSurfaceVariant,
                  ),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
              ],
            ),
          ),
        );
      },
    );
  }
}

class _Stat {
  const _Stat(this.label, this.value, this.icon, this.color);
  final String label;
  final String value;
  final IconData icon;
  final Color color;
}
