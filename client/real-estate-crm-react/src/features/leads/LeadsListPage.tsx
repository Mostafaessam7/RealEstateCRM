import { useState } from "react";
import { Link } from "react-router-dom";
import { toast } from "sonner";
import { PageHeader } from "../../components/PageHeader";
import { AsyncState } from "../../components/AsyncState";
import { Pagination } from "../../components/Pagination";
import { Modal } from "../../components/Modal";
import { StatusBadge } from "../../components/StatusBadge";
import { LeadForm, type LeadFormValues } from "./LeadForm";
import { useCreateLead, useLeads } from "./leadsApi";
import { LeadSource, LeadStatus, type LeadListQuery } from "../../types/lead";
import { getApiErrorMessage } from "../../api/client";
import { formatCurrency } from "../../utils/format";

export function LeadsListPage() {
  const [query, setQuery] = useState<LeadListQuery>({ page: 1, pageSize: 20 });
  const [searchInput, setSearchInput] = useState("");
  const [showCreate, setShowCreate] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  const { data, isLoading, isError } = useLeads(query);
  const createLead = useCreateLead();

  const submitSearch = (e: React.FormEvent) => {
    e.preventDefault();
    setQuery((q) => ({ ...q, search: searchInput, page: 1 }));
  };

  const handleCreate = async (values: LeadFormValues) => {
    setFormError(null);
    try {
      await createLead.mutateAsync({
        fullName: values.fullName,
        phone: values.phone || null,
        email: values.email || null,
        source: values.source as LeadSource,
        budgetMin: values.budgetMin ?? null,
        budgetMax: values.budgetMax ?? null,
        preferredLocation: values.preferredLocation || null,
        propertyType: values.propertyType || null,
        notes: values.notes || null,
      });
      setShowCreate(false);
      toast.success("Lead created");
    } catch (error) {
      setFormError(getApiErrorMessage(error, "Could not create lead."));
    }
  };

  return (
    <>
      <PageHeader
        title="Leads"
        actions={
          <button type="button" className="btn btn-primary" onClick={() => setShowCreate(true)}>
            + New Lead
          </button>
        }
      />

      <form className="toolbar" onSubmit={submitSearch}>
        <input
          className="input"
          style={{ maxWidth: 260 }}
          placeholder="Search name, phone, email…"
          value={searchInput}
          onChange={(e) => setSearchInput(e.target.value)}
        />
        <select
          className="input"
          style={{ maxWidth: 180 }}
          value={query.status ?? ""}
          onChange={(e) =>
            setQuery((q) => ({ ...q, status: (e.target.value || undefined) as LeadListQuery["status"], page: 1 }))
          }
        >
          <option value="">All statuses</option>
          {Object.values(LeadStatus).map((status) => (
            <option key={status} value={status}>
              {status}
            </option>
          ))}
        </select>
        <select
          className="input"
          style={{ maxWidth: 180 }}
          value={query.source ?? ""}
          onChange={(e) =>
            setQuery((q) => ({ ...q, source: (e.target.value || undefined) as LeadListQuery["source"], page: 1 }))
          }
        >
          <option value="">All sources</option>
          {Object.values(LeadSource).map((source) => (
            <option key={source} value={source}>
              {source}
            </option>
          ))}
        </select>
        <button type="submit" className="btn">
          Search
        </button>
      </form>

      <AsyncState
        isLoading={isLoading}
        isError={isError}
        errorMessage="Failed to load leads."
        isEmpty={!isLoading && (data?.items.length ?? 0) === 0}
        emptyMessage="No leads found."
      >
        <div className="card table-wrap">
          <table className="table">
            <thead>
              <tr>
                <th>Name</th>
                <th>Phone</th>
                <th>Status</th>
                <th>Source</th>
                <th>Budget</th>
                <th>Created</th>
              </tr>
            </thead>
            <tbody>
              {data?.items.map((lead) => (
                <tr key={lead.id}>
                  <td>
                    <Link to={`/leads/${lead.id}`}>{lead.fullName}</Link>
                  </td>
                  <td>{lead.phone ?? "—"}</td>
                  <td>
                    <StatusBadge status={lead.status} />
                  </td>
                  <td>{lead.source}</td>
                  <td>
                    {formatCurrency(lead.budgetMin)} - {formatCurrency(lead.budgetMax)}
                  </td>
                  <td>{new Date(lead.createdAt).toLocaleDateString()}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <Pagination
          page={data?.page ?? 1}
          totalPages={data?.totalPages ?? 1}
          onPageChange={(page) => setQuery((q) => ({ ...q, page }))}
        />
      </AsyncState>

      {showCreate && (
        <Modal title="New Lead" onClose={() => setShowCreate(false)}>
          {formError && <p className="field-error">{formError}</p>}
          <LeadForm onSubmit={handleCreate} submitLabel="Create Lead" />
        </Modal>
      )}
    </>
  );
}
