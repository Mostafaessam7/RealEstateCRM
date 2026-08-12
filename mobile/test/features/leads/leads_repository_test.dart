import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/core/network/api_client.dart';
import 'package:mobile/core/storage/token_storage.dart';
import 'package:mobile/features/leads/data/leads_repository.dart';

import '../../core/fake_http_adapter.dart';

void main() {
  late FakeHttpClientAdapter adapter;
  late LeadsRepository repository;

  setUp(() {
    adapter = FakeHttpClientAdapter();
    final dio = Dio(BaseOptions(baseUrl: 'https://api.test'))
      ..httpClientAdapter = adapter;
    final client = ApiClient(
      baseUrl: 'https://api.test',
      tokenStorage: InMemoryTokenStorage(),
      onSessionExpired: () async {},
      dio: dio,
    );
    repository = LeadsRepository(client);
  });

  test(
    'list() requests /v1/leads with pagination and search parameters',
    () async {
      adapter.enqueueJson('GET', '/v1/leads', 200, {
        'items': [
          {
            'id': 'lead-1',
            'fullName': 'Nour',
            'phone': '+201000000000',
            'email': null,
            'source': 'Website',
            'status': 'New',
            'budgetMin': 500000,
            'budgetMax': 800000,
            'preferredLocation': 'New Cairo',
            'propertyType': 'Apartment',
            'notes': null,
          },
        ],
        'page': 1,
        'pageSize': 50,
        'totalCount': 1,
        'totalPages': 1,
      });

      final result = await repository.list(search: 'Nour');

      expect(result.items, hasLength(1));
      expect(result.items.first.fullName, 'Nour');
      expect(result.items.first.budgetMin, 500000);
      final sentRequest = adapter.requests.single;
      expect(sentRequest.queryParameters['search'], 'Nour');
      expect(sentRequest.queryParameters['page'], 1);
    },
  );

  test('list() omits the search parameter when empty', () async {
    adapter.enqueueJson('GET', '/v1/leads', 200, {
      'items': <Map<String, dynamic>>[],
      'page': 1,
      'pageSize': 50,
      'totalCount': 0,
      'totalPages': 0,
    });

    await repository.list(search: '');

    expect(
      adapter.requests.single.queryParameters.containsKey('search'),
      isFalse,
    );
  });

  test('getById() requests /v1/leads/{id}', () async {
    adapter.enqueueJson('GET', '/v1/leads/lead-1', 200, {
      'id': 'lead-1',
      'fullName': 'Nour',
      'phone': null,
      'email': 'nour@test.local',
      'source': 'Referral',
      'status': 'Contacted',
      'budgetMin': null,
      'budgetMax': null,
      'preferredLocation': null,
      'propertyType': null,
      'notes': 'Follow up next week',
    });

    final lead = await repository.getById('lead-1');

    expect(lead.id, 'lead-1');
    expect(lead.status, 'Contacted');
    expect(lead.notes, 'Follow up next week');
  });
}
