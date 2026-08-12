import { useState } from "react";
import { toast } from "sonner";
import { PageHeader } from "../../components/PageHeader";
import { AsyncState } from "../../components/AsyncState";
import { Pagination } from "../../components/Pagination";
import { Modal } from "../../components/Modal";
import { StatusBadge } from "../../components/StatusBadge";
import { DealForm, type DealFormValues } from "./DealForm";
import { useCancelDeal, useContractDeal, useCreateDeal, useDeals, useReserveDeal } from "./dealsApi";
import { useCreateCheckout, useDealPayments } from "./paymentsApi";
import { DealStatus, type DealListQuery } from "../../types/deal";
import { getApiErrorMessage } from "../../api/client";
import { TableSkeleton } from "../../components/Skeleton";
import { formatCurrency } from "../../utils/format";

export function DealsListPage() {
  const [query, setQuery] = useState<DealListQuery>({ page: 1, pageSize: 20 });
  const [showCreate, setShowCreate] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [viewingPaymentsOf, setViewingPaymentsOf] = useState<string | null>(null);

  const { data, isLoading, isError } = useDeals(query);
  const createDeal = useCreateDeal();
  const reserveDeal = useReserveDeal();
  const contractDeal = useContractDeal();
  const cancelDeal = useCancelDeal();
  const createCheckout = useCreateCheckout();

  const handleCollectPayment = async (dealId: string) => {
    try {
      const session = await createCheckout.mutateAsync({ dealId });
      window.open(session.checkoutUrl, "_blank", "noopener,noreferrer");
    } catch (err) {
      setError(getApiErrorMessage(err, "Could not start checkout."));
    }
  };

  const handleCreate = async (values: DealFormValues) => {
    setError(null);
    try {
      await createDeal.mutateAsync({
        leadId: values.leadId,
        unitId: values.unitId,
        dealValue: values.dealValue,
        notes: values.notes || null,
      });
      setShowCreate(false);
      toast.success("Deal created");
    } catch (err) {
      setError(getApiErrorMessage(err, "Could not create deal."));
    }
  };

  const runAction = async (action: (id: string) => Promise<unknown>, id: string) => {
    setError(null);
    try {
      await action(id);
      toast.success("Deal updated");
    } catch (err) {
      setError(getApiErrorMessage(err, "Action failed."));
    }
  };

  return (
    <>
      <PageHeader
        title="Deals"
        actions={
          <button type="button" className="btn btn-primary" onClick={() => setShowCreate(true)}>
            + New Deal
          </button>
        }
      />

      <div className="toolbar">
        <select
          className="input"
          style={{ maxWidth: 200 }}
          value={query.status ?? ""}
          onChange={(e) => setQuery((q) => ({ ...q, status: (e.target.value || undefined) as DealListQuery["status"], page: 1 }))}
        >
          <option value="">All statuses</option>
          {Object.values(DealStatus).map((status) => (
            <option key={status} value={status}>
              {status}
            </option>
          ))}
        </select>
      </div>

      {error && <p className="field-error">{error}</p>}

      <AsyncState
        isLoading={isLoading}
        isError={isError}
        errorMessage="Failed to load deals."
        isEmpty={!isLoading && (data?.items.length ?? 0) === 0}
        emptyMessage="No deals yet."
      >
        <div className="card table-wrap">
          <table className="table">
            <thead>
              <tr>
                <th>Deal value</th>
                <th>Status</th>
                <th>Reservation date</th>
                <th>Contract date</th>
                <th className="sr-only">Actions</th>
              </tr>
            </thead>
            <tbody>
              {data?.items.map((deal) => (
                <tr key={deal.id}>
                  <td>{formatCurrency(deal.dealValue)}</td>
                  <td>
                    <StatusBadge status={deal.status} />
                  </td>
                  <td>{deal.reservationDate ? new Date(deal.reservationDate).toLocaleDateString() : "—"}</td>
                  <td>{deal.contractDate ? new Date(deal.contractDate).toLocaleDateString() : "—"}</td>
                  <td>
                    {deal.status === DealStatus.Pending && (
                      <button className="btn" onClick={() => runAction((id) => reserveDeal.mutateAsync(id), deal.id)}>
                        Reserve
                      </button>
                    )}{" "}
                    {deal.status === DealStatus.Reserved && (
                      <button className="btn" onClick={() => runAction((id) => contractDeal.mutateAsync(id), deal.id)}>
                        Contract
                      </button>
                    )}{" "}
                    {(deal.status === DealStatus.Pending || deal.status === DealStatus.Reserved) && (
                      <button
                        className="btn btn-danger"
                        onClick={() => runAction((id) => cancelDeal.mutateAsync(id), deal.id)}
                      >
                        Cancel
                      </button>
                    )}{" "}
                    {deal.status === DealStatus.Reserved && (
                      <button className="btn btn-sm" onClick={() => handleCollectPayment(deal.id)} disabled={createCheckout.isPending}>
                        Collect Payment
                      </button>
                    )}{" "}
                    <button className="btn btn-sm" onClick={() => setViewingPaymentsOf(deal.id)}>
                      Payments
                    </button>
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
        <Modal title="New Deal" onClose={() => setShowCreate(false)}>
          <DealForm onSubmit={handleCreate} />
        </Modal>
      )}

      {viewingPaymentsOf && <DealPaymentsModal dealId={viewingPaymentsOf} onClose={() => setViewingPaymentsOf(null)} />}
    </>
  );
}

function DealPaymentsModal({ dealId, onClose }: { dealId: string; onClose: () => void }) {
  const { data: payments, isLoading } = useDealPayments(dealId);

  return (
    <Modal title="Payments" onClose={onClose} width={480}>
      {isLoading ? (
        <TableSkeleton columns={3} rows={3} />
      ) : payments && payments.length > 0 ? (
        <ul style={{ listStyle: "none", padding: 0, margin: 0 }}>
          {payments.map((p) => (
            <li key={p.id} style={{ padding: "8px 0", borderBottom: "1px solid var(--color-border)" }}>
              <StatusBadge status={p.status} />{" "}
              <strong style={{ fontSize: 13 }}>
                {p.amount.toLocaleString()} {p.currency.toUpperCase()}
              </strong>
              <div style={{ fontSize: 11.5, color: "var(--color-text-muted)" }}>
                {new Date(p.createdAt).toLocaleString()}
                {p.paidAt && ` · paid ${new Date(p.paidAt).toLocaleString()}`}
              </div>
            </li>
          ))}
        </ul>
      ) : (
        <p className="state-message">No payments yet.</p>
      )}
    </Modal>
  );
}
