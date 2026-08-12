import 'package:connectivity_plus/connectivity_plus.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

/// True when the device reports any network connectivity. Not a guarantee the API is
/// reachable (captive portals, VPNs, etc.), but enough to distinguish "you're offline" from
/// a real API error in the UI — screens show a dedicated offline banner instead of a generic
/// error when this is false.
final isOnlineProvider = StreamProvider<bool>((ref) {
  final connectivity = Connectivity();
  return connectivity.onConnectivityChanged.map(
    (results) => !results.contains(ConnectivityResult.none),
  );
});
