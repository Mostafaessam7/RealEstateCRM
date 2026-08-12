class Lead {
  const Lead({
    required this.id,
    required this.fullName,
    required this.phone,
    required this.email,
    required this.source,
    required this.status,
    required this.budgetMin,
    required this.budgetMax,
    required this.preferredLocation,
    required this.propertyType,
    required this.notes,
  });

  factory Lead.fromJson(Map<String, dynamic> json) => Lead(
    id: json['id'] as String,
    fullName: json['fullName'] as String,
    phone: json['phone'] as String?,
    email: json['email'] as String?,
    source: json['source'] as String,
    status: json['status'] as String,
    budgetMin: (json['budgetMin'] as num?)?.toDouble(),
    budgetMax: (json['budgetMax'] as num?)?.toDouble(),
    preferredLocation: json['preferredLocation'] as String?,
    propertyType: json['propertyType'] as String?,
    notes: json['notes'] as String?,
  );

  final String id;
  final String fullName;
  final String? phone;
  final String? email;
  final String source;
  final String status;
  final double? budgetMin;
  final double? budgetMax;
  final String? preferredLocation;
  final String? propertyType;
  final String? notes;
}
