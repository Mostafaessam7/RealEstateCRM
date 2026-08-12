import 'package:flutter/material.dart';

/// Design tokens mirroring the web app's toned-down palette
/// (client/real-estate-crm-react/src/index.css) — restrained, professional SaaS look,
/// not decorative. Kept as static const so both light/dark themes and widgets can reference
/// the same brand color without duplicating hex values.
class AppColors {
  AppColors._();

  static const primary = Color(0xFF5B4FE0);
  static const primarySoft = Color(0xFFEEECFF);

  static const success = Color(0xFF17A367);
  static const successSoft = Color(0xFFE4F8EE);
  static const warning = Color(0xFFD97A06);
  static const warningSoft = Color(0xFFFEF3E2);
  static const danger = Color(0xFFE0334F);
  static const dangerSoft = Color(0xFFFDE9EC);
  static const info = Color(0xFF2F6FED);
  static const infoSoft = Color(0xFFE8F0FE);
  static const neutralSoft = Color(0xFFEEF0F5);
}

class AppTheme {
  AppTheme._();

  static ThemeData light() => _base(Brightness.light);
  static ThemeData dark() => _base(Brightness.dark);

  static ThemeData _base(Brightness brightness) {
    final isDark = brightness == Brightness.dark;
    final scheme = ColorScheme.fromSeed(
      seedColor: AppColors.primary,
      brightness: brightness,
      primary: AppColors.primary,
      error: AppColors.danger,
      surface: isDark ? const Color(0xFF15162B) : Colors.white,
    );

    return ThemeData(
      useMaterial3: true,
      brightness: brightness,
      colorScheme: scheme,
      scaffoldBackgroundColor: isDark
          ? const Color(0xFF0F1020)
          : const Color(0xFFF6F7FB),
      visualDensity: VisualDensity.standard,
      appBarTheme: AppBarTheme(
        backgroundColor: isDark ? const Color(0xFF15162B) : Colors.white,
        foregroundColor: isDark ? Colors.white : const Color(0xFF171A2B),
        elevation: 0,
        scrolledUnderElevation: 1,
        centerTitle: false,
        titleTextStyle: TextStyle(
          fontSize: 18,
          fontWeight: FontWeight.w700,
          color: isDark ? Colors.white : const Color(0xFF171A2B),
        ),
      ),
      cardTheme: CardThemeData(
        elevation: 0,
        color: isDark ? const Color(0xFF191A33) : Colors.white,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(14),
          side: BorderSide(
            color: isDark ? const Color(0xFF2A2C4A) : const Color(0xFFE6E8F0),
          ),
        ),
        margin: EdgeInsets.zero,
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: isDark ? const Color(0xFF1D1E38) : const Color(0xFFFAFBFE),
        contentPadding: const EdgeInsets.symmetric(
          horizontal: 14,
          vertical: 14,
        ),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: BorderSide(
            color: isDark ? const Color(0xFF2A2C4A) : const Color(0xFFE6E8F0),
          ),
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: BorderSide(
            color: isDark ? const Color(0xFF2A2C4A) : const Color(0xFFE6E8F0),
          ),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: const BorderSide(color: AppColors.primary, width: 1.5),
        ),
        errorBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: const BorderSide(color: AppColors.danger),
        ),
      ),
      elevatedButtonTheme: ElevatedButtonThemeData(
        style: ElevatedButton.styleFrom(
          backgroundColor: AppColors.primary,
          foregroundColor: Colors.white,
          elevation: 0,
          padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 14),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(12),
          ),
          textStyle: const TextStyle(fontSize: 15, fontWeight: FontWeight.w600),
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(12),
          ),
          side: BorderSide(
            color: isDark ? const Color(0xFF2A2C4A) : const Color(0xFFE6E8F0),
          ),
        ),
      ),
      textButtonTheme: TextButtonThemeData(
        style: TextButton.styleFrom(foregroundColor: AppColors.primary),
      ),
      dividerTheme: DividerThemeData(
        color: isDark ? const Color(0xFF2A2C4A) : const Color(0xFFE6E8F0),
        thickness: 1,
      ),
      snackBarTheme: SnackBarThemeData(
        behavior: SnackBarBehavior.floating,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
      ),
      bottomNavigationBarTheme: BottomNavigationBarThemeData(
        backgroundColor: isDark ? const Color(0xFF15162B) : Colors.white,
        selectedItemColor: AppColors.primary,
        unselectedItemColor: isDark
            ? const Color(0xFF8B8FB8)
            : const Color(0xFF9AA0B4),
        type: BottomNavigationBarType.fixed,
        elevation: 8,
      ),
    );
  }
}
