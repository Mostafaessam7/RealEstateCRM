/// Roles as defined by the backend (RealEstateCRM.Domain.Constants.Roles) — kept as raw
/// strings rather than a Dart enum so a new backend role never breaks JWT decoding here.
class Roles {
  Roles._();

  static const superAdmin = 'SuperAdmin';
  static const companyAdmin = 'CompanyAdmin';
  static const salesManager = 'SalesManager';
  static const salesAgent = 'SalesAgent';
}

class AuthUser {
  const AuthUser({
    required this.userId,
    required this.companyId,
    required this.roles,
  });

  final String userId;
  final String? companyId;
  final List<String> roles;

  bool hasRole(String role) => roles.contains(role);

  bool get isCompanyAdminOrAbove =>
      hasRole(Roles.superAdmin) || hasRole(Roles.companyAdmin);
}
