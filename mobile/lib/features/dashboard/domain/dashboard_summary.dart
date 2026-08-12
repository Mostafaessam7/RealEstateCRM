class DashboardSummary {
  const DashboardSummary({
    required this.totalLeads,
    required this.newLeadsLast30Days,
    required this.conversionRatePercent,
    required this.totalDeals,
    required this.totalSalesValue,
    required this.upcomingFollowUps,
    required this.totalAvailableUnits,
  });

  factory DashboardSummary.fromJson(Map<String, dynamic> json) =>
      DashboardSummary(
        totalLeads: json['totalLeads'] as int? ?? 0,
        newLeadsLast30Days: json['newLeadsLast30Days'] as int? ?? 0,
        conversionRatePercent:
            (json['conversionRatePercent'] as num?)?.toDouble() ?? 0,
        totalDeals: json['totalDeals'] as int? ?? 0,
        totalSalesValue: (json['totalSalesValue'] as num?)?.toDouble() ?? 0,
        upcomingFollowUps: json['upcomingFollowUps'] as int? ?? 0,
        totalAvailableUnits: json['totalAvailableUnits'] as int? ?? 0,
      );

  final int totalLeads;
  final int newLeadsLast30Days;
  final double conversionRatePercent;
  final int totalDeals;
  final double totalSalesValue;
  final int upcomingFollowUps;
  final int totalAvailableUnits;
}
