import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:mobile/app.dart';
import 'package:mobile/core/storage/token_storage.dart';
import 'package:mobile/features/auth/application/auth_controller.dart';

void main() {
  testWidgets('boots to the login screen when no token is stored', (
    tester,
  ) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          tokenStorageProvider.overrideWithValue(InMemoryTokenStorage()),
        ],
        child: const RealEstateCrmApp(),
      ),
    );
    // Auth bootstrap (reading local storage) settles asynchronously — pump past it.
    await tester.pumpAndSettle();

    expect(find.text('Welcome back'), findsOneWidget);
    expect(find.widgetWithText(ElevatedButton, 'Sign in'), findsOneWidget);
  });
}
