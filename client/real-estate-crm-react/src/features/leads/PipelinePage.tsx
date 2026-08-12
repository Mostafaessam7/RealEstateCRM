import { useQueryClient } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import { PageHeader } from "../../components/PageHeader";
import { AsyncState } from "../../components/AsyncState";
import { useLeads } from "./leadsApi";
import { apiClient } from "../../api/client";
import { LeadStatus, type Lead } from "../../types/lead";

const columns: LeadStatus[] = [
  LeadStatus.New,
  LeadStatus.Contacted,
  LeadStatus.Interested,
  LeadStatus.Viewing,
  LeadStatus.Negotiation,
  LeadStatus.Reserved,
  LeadStatus.Contracted,
  LeadStatus.Lost,
];

/**
 * No drag/drop — docs/frontend.md says not to build complex DnD until backend business
 * rules for stage transitions are established. A "move to next stage" button is enough for now.
 */
export function PipelinePage() {
  const { data, isLoading, isError } = useLeads({ page: 1, pageSize: 200 });
  const queryClient = useQueryClient();

  const moveToNextStage = async (lead: Lead) => {
    const currentIndex = columns.indexOf(lead.status);
    const nextStatus = columns[currentIndex + 1];
    if (!nextStatus) return;

    await apiClient.put(`/leads/${lead.id}`, {
      fullName: lead.fullName,
      phone: lead.phone,
      email: lead.email,
      source: lead.source,
      status: nextStatus,
      budgetMin: lead.budgetMin,
      budgetMax: lead.budgetMax,
      preferredLocation: lead.preferredLocation,
      propertyType: lead.propertyType,
      notes: lead.notes,
    });

    await queryClient.invalidateQueries({ queryKey: ["leads"] });
  };

  return (
    <>
      <PageHeader title="Pipeline" />
      <AsyncState isLoading={isLoading} isError={isError} errorMessage="Failed to load pipeline.">
        <div style={{ display: "flex", gap: 12, overflowX: "auto", paddingBottom: 8 }}>
          {columns.map((status) => {
            const leads = data?.items.filter((lead) => lead.status === status) ?? [];
            return (
              <div key={status} className="card" style={{ minWidth: 220, flex: "0 0 220px" }}>
                <div className="toolbar" style={{ marginBottom: 8 }}>
                  <strong>{status}</strong>
                  <span className="spacer" />
                  <span className="badge">{leads.length}</span>
                </div>
                <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
                  {leads.map((lead) => (
                    <div key={lead.id} className="card" style={{ padding: 10 }}>
                      <Link to={`/leads/${lead.id}`}>{lead.fullName}</Link>
                      <div style={{ fontSize: 12, color: "var(--color-text-muted)" }}>{lead.source}</div>
                      {columns.indexOf(status) < columns.length - 1 && (
                        <button
                          type="button"
                          className="btn"
                          style={{ marginTop: 6, fontSize: 12, padding: "4px 8px" }}
                          onClick={() => moveToNextStage(lead)}
                        >
                          Move to {columns[columns.indexOf(status) + 1]} →
                        </button>
                      )}
                    </div>
                  ))}
                </div>
              </div>
            );
          })}
        </div>
      </AsyncState>
    </>
  );
}
