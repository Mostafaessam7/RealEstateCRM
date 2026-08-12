/// Mirrors the backend's `PagedResult<T>` (see docs/api.md) and
/// `client/real-estate-crm-react/src/types/common.ts`'s `PagedResult<T>`.
class PagedResult<T> {
  const PagedResult({
    required this.items,
    required this.page,
    required this.pageSize,
    required this.totalCount,
    required this.totalPages,
  });

  factory PagedResult.fromJson(
    Map<String, dynamic> json,
    T Function(Map<String, dynamic>) fromJson,
  ) {
    return PagedResult(
      items: (json['items'] as List<dynamic>)
          .map((e) => fromJson(e as Map<String, dynamic>))
          .toList(),
      page: json['page'] as int,
      pageSize: json['pageSize'] as int,
      totalCount: json['totalCount'] as int,
      totalPages: json['totalPages'] as int,
    );
  }

  final List<T> items;
  final int page;
  final int pageSize;
  final int totalCount;
  final int totalPages;
}
