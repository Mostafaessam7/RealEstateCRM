import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'core/router/app_router.dart';
import 'core/theme/app_theme.dart';

class RealEstateCrmApp extends ConsumerWidget {
  const RealEstateCrmApp({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final router = ref.watch(routerProvider);

    return MaterialApp.router(
      title: 'Real Estate CRM',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.light(),
      darkTheme: AppTheme.dark(),
      // Follows the device's light/dark setting — no separate in-app toggle, same as most
      // system-integrated mobile apps and simplest to keep correct.
      themeMode: ThemeMode.system,
      routerConfig: router,
    );
  }
}
