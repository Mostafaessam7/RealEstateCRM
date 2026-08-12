import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/shared/widgets/status_chip.dart';

void main() {
  testWidgets('renders the status label', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(body: StatusChip(status: 'Contracted')),
      ),
    );

    expect(find.text('Contracted'), findsOneWidget);
  });

  testWidgets('falls back to a neutral tone for an unknown status', (
    tester,
  ) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(body: StatusChip(status: 'SomeFutureStatus')),
      ),
    );

    expect(find.text('SomeFutureStatus'), findsOneWidget);
    expect(toneForStatus('SomeFutureStatus'), StatusTone.neutral);
  });

  test('maps known statuses to the expected tone', () {
    expect(toneForStatus('Contracted'), StatusTone.success);
    expect(toneForStatus('Lost'), StatusTone.danger);
    expect(toneForStatus('New'), StatusTone.info);
    expect(toneForStatus('Reserved'), StatusTone.warning);
  });
}
