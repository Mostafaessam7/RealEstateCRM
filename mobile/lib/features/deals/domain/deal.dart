class Deal {
  const Deal({
    required this.id,
    required this.leadId,
    required this.unitId,
    required this.salesAgentId,
    required this.dealValue,
    required this.status,
    required this.reservationDate,
    required this.contractDate,
    required this.notes,
  });

  factory Deal.fromJson(Map<String, dynamic> json) => Deal(
    id: json['id'] as String,
    leadId: json['leadId'] as String,
    unitId: json['unitId'] as String,
    salesAgentId: json['salesAgentId'] as String,
    dealValue: (json['dealValue'] as num).toDouble(),
    status: json['status'] as String,
    reservationDate: json['reservationDate'] as String?,
    contractDate: json['contractDate'] as String?,
    notes: json['notes'] as String?,
  );

  final String id;
  final String leadId;
  final String unitId;
  final String salesAgentId;
  final double dealValue;
  final String status;
  final String? reservationDate;
  final String? contractDate;
  final String? notes;
}
