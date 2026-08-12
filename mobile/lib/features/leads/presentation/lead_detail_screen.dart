import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:url_launcher/url_launcher.dart';

import '../../../core/theme/app_theme.dart';
import '../../../shared/widgets/async_value_view.dart';
import '../../../shared/widgets/status_chip.dart';
import '../application/leads_providers.dart';
import '../domain/lead.dart';

class LeadDetailScreen extends ConsumerWidget {
  const LeadDetailScreen({required this.leadId, super.key});

  final String leadId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final lead = ref.watch(leadDetailProvider(leadId));

    return Scaffold(
      appBar: AppBar(title: const Text('Lead')),
      body: AsyncValueView<Lead>(
        value: lead,
        onRetry: () => ref.invalidate(leadDetailProvider(leadId)),
        data: (context, lead) => _LeadDetailBody(lead: lead),
      ),
    );
  }
}

class _LeadDetailBody extends StatelessWidget {
  const _LeadDetailBody({required this.lead});

  final Lead lead;

  String _digitsOnly(String phone) => phone.replaceAll(RegExp(r'[^\d+]'), '');

  Future<void> _call(BuildContext context, String phone) async {
    final uri = Uri(scheme: 'tel', path: _digitsOnly(phone));
    if (!await launchUrl(uri) && context.mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Could not open the phone app.')),
      );
    }
  }

  Future<void> _whatsApp(BuildContext context, String phone) async {
    final uri = Uri.parse(
      'https://wa.me/${_digitsOnly(phone).replaceAll('+', '')}',
    );
    if (!await launchUrl(uri, mode: LaunchMode.externalApplication) &&
        context.mounted) {
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(const SnackBar(content: Text('Could not open WhatsApp.')));
    }
  }

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        Row(
          children: [
            Expanded(
              child: Text(
                lead.fullName,
                style: const TextStyle(
                  fontSize: 21,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ),
            StatusChip(status: lead.status),
          ],
        ),
        if (lead.phone != null) ...[
          const SizedBox(height: 14),
          Row(
            children: [
              Expanded(
                child: ElevatedButton.icon(
                  onPressed: () => _call(context, lead.phone!),
                  icon: const Icon(Icons.call_outlined, size: 18),
                  label: const Text('Call'),
                ),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: OutlinedButton.icon(
                  onPressed: () => _whatsApp(context, lead.phone!),
                  icon: const Icon(
                    Icons.chat_outlined,
                    size: 18,
                    color: AppColors.success,
                  ),
                  label: const Text(
                    'WhatsApp',
                    style: TextStyle(color: AppColors.success),
                  ),
                  style: OutlinedButton.styleFrom(
                    side: const BorderSide(color: AppColors.successSoft),
                  ),
                ),
              ),
            ],
          ),
        ],
        const SizedBox(height: 16),
        Card(
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              children: [
                _DetailRow(label: 'Source', value: lead.source),
                _DetailRow(label: 'Phone', value: lead.phone ?? '—'),
                _DetailRow(label: 'Email', value: lead.email ?? '—'),
                _DetailRow(
                  label: 'Budget',
                  value: (lead.budgetMin != null || lead.budgetMax != null)
                      ? '${lead.budgetMin?.toStringAsFixed(0) ?? '—'} – ${lead.budgetMax?.toStringAsFixed(0) ?? '—'}'
                      : '—',
                ),
                _DetailRow(
                  label: 'Preferred location',
                  value: lead.preferredLocation ?? '—',
                ),
                _DetailRow(
                  label: 'Property type',
                  value: lead.propertyType ?? '—',
                  isLast: true,
                ),
              ],
            ),
          ),
        ),
        if (lead.notes != null && lead.notes!.isNotEmpty) ...[
          const SizedBox(height: 12),
          Card(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    'Notes',
                    style: TextStyle(
                      fontSize: 12.5,
                      fontWeight: FontWeight.w600,
                      color: Theme.of(context).colorScheme.onSurfaceVariant,
                    ),
                  ),
                  const SizedBox(height: 6),
                  Text(lead.notes!, style: const TextStyle(fontSize: 13.5)),
                ],
              ),
            ),
          ),
        ],
      ],
    );
  }
}

class _DetailRow extends StatelessWidget {
  const _DetailRow({
    required this.label,
    required this.value,
    this.isLast = false,
  });

  final String label;
  final String value;
  final bool isLast;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(vertical: 9),
      decoration: BoxDecoration(
        border: isLast
            ? null
            : Border(bottom: BorderSide(color: Theme.of(context).dividerColor)),
      ),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(
            label,
            style: TextStyle(
              fontSize: 12.5,
              fontWeight: FontWeight.w600,
              color: Theme.of(context).colorScheme.onSurfaceVariant,
            ),
          ),
          Flexible(
            child: Text(
              value,
              textAlign: TextAlign.right,
              style: const TextStyle(fontSize: 13.5),
            ),
          ),
        ],
      ),
    );
  }
}
