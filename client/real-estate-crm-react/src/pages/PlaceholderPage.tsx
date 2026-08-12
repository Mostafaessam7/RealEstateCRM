import { PageHeader } from "../components/PageHeader";

/** Rendered for any unmatched route (404) — see AppRoutes.tsx's catch-all `*` route. */
export function PlaceholderPage({ title }: { title: string }) {
  return (
    <>
      <PageHeader title={title} />
      <div className="card state-message">This page is coming soon.</div>
    </>
  );
}
