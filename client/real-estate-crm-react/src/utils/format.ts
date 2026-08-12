/**
 * Consistent number formatting for money values shown in tables/cards across the app.
 * Found needed while visually verifying the running app: Units/Deals/Leads tables were
 * rendering raw unformatted numbers (e.g. "690000") while the Dashboard and payment history
 * already used thousands separators — this closes that inconsistency at one shared place.
 */
export function formatCurrency(value: number | null | undefined): string {
  if (value === null || value === undefined) {
    return "—";
  }
  return value.toLocaleString(undefined, { maximumFractionDigits: 0 });
}
