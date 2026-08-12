import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

/// Bottom-tab chrome around the three top-level sections (Dashboard/Leads/Deals) — Leads has
/// its own nested Navigator (via the ShellRoute's branch) so pushing a lead's detail page
/// keeps the tab bar visible, matching the previous React Navigation bottom-tabs + nested
/// stack structure.
class AppShell extends StatelessWidget {
  const AppShell({required this.navigationShell, super.key});

  final StatefulNavigationShell navigationShell;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: navigationShell,
      bottomNavigationBar: BottomNavigationBar(
        currentIndex: navigationShell.currentIndex,
        onTap: (index) => navigationShell.goBranch(
          index,
          initialLocation: index == navigationShell.currentIndex,
        ),
        items: const [
          BottomNavigationBarItem(
            icon: Icon(Icons.dashboard_outlined),
            activeIcon: Icon(Icons.dashboard_rounded),
            label: 'Dashboard',
          ),
          BottomNavigationBarItem(
            icon: Icon(Icons.people_outline_rounded),
            activeIcon: Icon(Icons.people_rounded),
            label: 'Leads',
          ),
          BottomNavigationBarItem(
            icon: Icon(Icons.handshake_outlined),
            activeIcon: Icon(Icons.handshake_rounded),
            label: 'Deals',
          ),
        ],
      ),
    );
  }
}
