import 'package:flutter/material.dart';

import '../../core/theme/app_theme.dart';

enum StatusTone { success, warning, danger, info, neutral }

const Map<String, StatusTone> _statusTones = {
  // Leads
  'New': StatusTone.info,
  'Contacted': StatusTone.info,
  'Interested': StatusTone.warning,
  'Viewing': StatusTone.warning,
  'Negotiation': StatusTone.warning,
  'Reserved': StatusTone.warning,
  'Contracted': StatusTone.success,
  'Lost': StatusTone.danger,
  // Deals
  'Pending': StatusTone.warning,
  'Paid': StatusTone.success,
  'Cancelled': StatusTone.danger,
  'Completed': StatusTone.success,
  // Generic
  'Active': StatusTone.success,
  'Inactive': StatusTone.neutral,
};

/// One shared status -> color mapping, mirroring
/// client/real-estate-crm-react/src/utils/statusVariant.ts, so a status word reads the same
/// color on mobile as it does on the web app.
StatusTone toneForStatus(String status) =>
    _statusTones[status] ?? StatusTone.neutral;

class StatusChip extends StatelessWidget {
  const StatusChip({required this.status, super.key});

  final String status;

  @override
  Widget build(BuildContext context) {
    final tone = toneForStatus(status);
    final (bg, fg) = switch (tone) {
      StatusTone.success => (AppColors.successSoft, AppColors.success),
      StatusTone.warning => (AppColors.warningSoft, AppColors.warning),
      StatusTone.danger => (AppColors.dangerSoft, AppColors.danger),
      StatusTone.info => (AppColors.infoSoft, AppColors.info),
      StatusTone.neutral => (AppColors.neutralSoft, Colors.grey.shade700),
    };

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(
        color: bg,
        borderRadius: BorderRadius.circular(999),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Container(
            width: 6,
            height: 6,
            margin: const EdgeInsets.only(right: 6),
            decoration: BoxDecoration(color: fg, shape: BoxShape.circle),
          ),
          Text(
            status,
            style: TextStyle(
              color: fg,
              fontSize: 11.5,
              fontWeight: FontWeight.w700,
            ),
          ),
        ],
      ),
    );
  }
}
