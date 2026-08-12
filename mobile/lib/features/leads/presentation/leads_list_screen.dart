import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/connectivity/connectivity_service.dart';
import '../../../core/network/paged_result.dart';
import '../../../shared/widgets/async_value_view.dart';
import '../../../shared/widgets/state_views.dart';
import '../../../shared/widgets/status_chip.dart';
import '../application/leads_providers.dart';
import '../domain/lead.dart';

class LeadsListScreen extends ConsumerStatefulWidget {
  const LeadsListScreen({super.key});

  @override
  ConsumerState<LeadsListScreen> createState() => _LeadsListScreenState();
}

class _LeadsListScreenState extends ConsumerState<LeadsListScreen> {
  final _searchController = TextEditingController();
  String _search = '';
  Timer? _debounce;

  @override
  void dispose() {
    _debounce?.cancel();
    _searchController.dispose();
    super.dispose();
  }

  void _onSearchChanged(String value) {
    _debounce?.cancel();
    _debounce = Timer(const Duration(milliseconds: 300), () {
      if (mounted) setState(() => _search = value.trim());
    });
  }

  @override
  Widget build(BuildContext context) {
    final leads = ref.watch(leadsListProvider(_search));
    final isOnline = ref.watch(isOnlineProvider).valueOrNull ?? true;

    return Scaffold(
      appBar: AppBar(title: const Text('Leads')),
      body: Column(
        children: [
          if (!isOnline) const OfflineBanner(),
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 4),
            child: TextField(
              controller: _searchController,
              onChanged: _onSearchChanged,
              decoration: const InputDecoration(
                hintText: 'Search leads…',
                prefixIcon: Icon(Icons.search_rounded),
                isDense: true,
              ),
            ),
          ),
          Expanded(
            child: RefreshIndicator(
              onRefresh: () async => ref.invalidate(leadsListProvider(_search)),
              child: AsyncValueView<PagedResult<Lead>>(
                value: leads,
                onRetry: () => ref.invalidate(leadsListProvider(_search)),
                data: (context, result) {
                  if (result.items.isEmpty) {
                    return ListView(
                      physics: const AlwaysScrollableScrollPhysics(),
                      children: const [
                        SizedBox(height: 80),
                        EmptyView(
                          title: 'No leads found',
                          message:
                              'Try a different search, or check back later.',
                          icon: Icons.people_outline_rounded,
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
                      final lead = result.items[index];
                      return _LeadTile(
                        lead: lead,
                        onTap: () => context.push('/leads/${lead.id}'),
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

class _LeadTile extends StatelessWidget {
  const _LeadTile({required this.lead, required this.onTap});

  final Lead lead;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: InkWell(
        borderRadius: BorderRadius.circular(14),
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.all(14),
          child: Row(
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      lead.fullName,
                      style: const TextStyle(
                        fontWeight: FontWeight.w600,
                        fontSize: 15,
                      ),
                    ),
                    const SizedBox(height: 3),
                    Text(
                      lead.phone ?? lead.email ?? 'No contact info',
                      style: TextStyle(
                        fontSize: 12.5,
                        color: Theme.of(context).colorScheme.onSurfaceVariant,
                      ),
                    ),
                  ],
                ),
              ),
              StatusChip(status: lead.status),
            ],
          ),
        ),
      ),
    );
  }
}
