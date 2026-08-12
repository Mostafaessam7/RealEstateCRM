import { useState } from "react";
import { toast } from "sonner";
import { PageHeader } from "../../components/PageHeader";
import { AsyncState } from "../../components/AsyncState";
import { Pagination } from "../../components/Pagination";
import { Modal } from "../../components/Modal";
import { StatusBadge } from "../../components/StatusBadge";
import { CommissionForm, type CommissionFormValues } from "./CommissionForm";
import { useCancelCommission, useCreateCommission, useCommissions, useMarkCommissionPaid } from "./commissionsApi";
import { CommissionStatus, type CommissionListQuery } from "../../types/commission";
import { getApiErrorMessage } from "../../api/client";
import { formatCurrency } from "../../utils/format";

export function CommissionsListPage() {
  const [query, setQuery] = useState<CommissionListQuery>({ page: 1, pageSize: 20 });
  const [showCreate, setShowCreate] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const { data, isLoading, isError } = useCommissions(query);
  const createCommission = useCreateCommission();
  const markPaid = useMarkCommissionPaid();
  const cancelCommission = useCancelCommission();

  const handleCreate = async (values: CommissionFormValues) => {
    setError(null);
    try {
      await createCommission.mutateAsync(values);
      setShowCreate(false);
      toast.success("Commission created");
    } catch (err) {
      setError(getApiErrorMessage(err, "Could not create commission."));
    }
  };

  return (
    <>
      <PageHeader
        title="Commissions"
        actions={
          <button type="button" className="btn btn-primary" onClick={() => setShowCreate(true)}>
            + New Commission
          </button>
        }
      />

      {error && <p className="field-error">{error}</p>}

      <AsyncState
        isLoading={isLoading}
        isError={isError}
        errorMessage="Failed to load commissions."
        isEmpty={!isLoading && (data?.items.length ?? 0) === 0}
        emptyMessage="No commissions yet."
      >
        <div className="card table-wrap">
          <table className="table">
            <thead>
              <tr>
                <th>Agent commission</th>
                <th>Company commission</th>
                <th>Status</th>
                <th>Payment date</th>
                <th className="sr-only">Actions</th>
              </tr>
            </thead>
            <tbody>
              {data?.items.map((commission) => (
                <tr key={commission.id}>
                  <td>
                    {formatCurrency(commission.commissionAmount)} ({commission.commissionPercentage}%)
                  </td>
                  <td>{formatCurrency(commission.companyCommission)}</td>
                  <td>
                    <StatusBadge status={commission.status} />
                  </td>
                  <td>{commission.paymentDate ? new Date(commission.paymentDate).toLocaleDateString() : "—"}</td>
                  <td>
                    {commission.status === CommissionStatus.Pending && (
                      <>
                        <button className="btn" onClick={() => markPaid.mutate(commission.id)}>
                          Mark Paid
                        </button>{" "}
                        <button className="btn btn-danger" onClick={() => cancelCommission.mutate(commission.id)}>
                          Cancel
                        </button>
                      </>
                    )}
                  </td>
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
        <Modal title="New Commission" onClose={() => setShowCreate(false)}>
          <CommissionForm onSubmit={handleCreate} />
        </Modal>
      )}
    </>
  );
}
