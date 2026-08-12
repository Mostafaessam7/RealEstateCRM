import { PageHeader } from "../../components/PageHeader";
import { AsyncState } from "../../components/AsyncState";
import { StatusBadge } from "../../components/StatusBadge";
import { useCurrentCompany } from "./companiesApi";

/**
 * Read-only for now — no update-company endpoint exists on the backend yet
 * (see docs/roadmap.md Phase 11 notes). This page will grow an edit form once that lands.
 */
export function CompanySettingsPage() {
  const { data: company, isLoading, isError } = useCurrentCompany();

  return (
    <>
      <PageHeader title="Company Settings" />
      <AsyncState isLoading={isLoading} isError={isError} errorMessage="Failed to load company settings.">
        {company && (
          <div className="card" style={{ maxWidth: 480 }}>
            <p>
              <strong>Name:</strong> {company.name}
            </p>
            <p>
              <strong>Slug:</strong> {company.slug}
            </p>
            <p>
              <strong>Phone:</strong> {company.phone ?? "—"}
            </p>
            <p>
              <strong>Email:</strong> {company.email ?? "—"}
            </p>
            <p>
              <strong>Status:</strong>{" "}
              <StatusBadge status={company.isActive ? "Active" : "Inactive"} />
            </p>
          </div>
        )}
      </AsyncState>
    </>
  );
}
