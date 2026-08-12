import { useState } from "react";
import { toast } from "sonner";
import { Webhook, Copy, ShieldAlert } from "lucide-react";
import { PageHeader } from "../../components/PageHeader";
import { AsyncState } from "../../components/AsyncState";
import { StatusBadge } from "../../components/StatusBadge";
import { Modal } from "../../components/Modal";
import { TableSkeleton } from "../../components/Skeleton";
import { useConfirmDialog } from "../../components/ConfirmDialog";
import { getApiErrorMessage } from "../../api/client";
import { useCreateWebhook, useDeleteWebhook, useWebhookDeliveries, useWebhookEventTypes, useWebhooks } from "./webhooksApi";
import type { CreatedWebhookSubscription } from "../../types/webhook";

function CreateWebhookForm({ onCreated }: { onCreated: (webhook: CreatedWebhookSubscription) => void }) {
  const createWebhook = useCreateWebhook();
  const { data: eventTypes } = useWebhookEventTypes();
  const [url, setUrl] = useState("");
  const [selected, setSelected] = useState<string[]>([]);

  const toggle = (type: string) => {
    setSelected((prev) => (prev.includes(type) ? prev.filter((t) => t !== type) : [...prev, type]));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const created = await createWebhook.mutateAsync({ url, eventTypes: selected });
      onCreated(created);
    } catch (err) {
      toast.error(getApiErrorMessage(err, "Could not create webhook."));
    }
  };

  return (
    <form onSubmit={handleSubmit}>
      <div className="field">
        <label>Endpoint URL</label>
        <input className="input" type="url" value={url} onChange={(e) => setUrl(e.target.value)} placeholder="https://your-app.com/webhooks/crm" required />
      </div>
      <div className="field">
        <label>Events</label>
        <div style={{ display: "flex", flexDirection: "column", gap: 6 }}>
          {eventTypes?.map((type) => (
            <label key={type} style={{ display: "flex", alignItems: "center", gap: 8, fontSize: 13 }}>
              <input type="checkbox" checked={selected.includes(type)} onChange={() => toggle(type)} />
              {type}
            </label>
          ))}
        </div>
      </div>
      <button type="submit" className="btn btn-primary" disabled={createWebhook.isPending || selected.length === 0} style={{ width: "100%" }}>
        {createWebhook.isPending ? "Creating…" : "Create Webhook"}
      </button>
    </form>
  );
}

function DeliveriesModal({ subscriptionId, onClose }: { subscriptionId: string; onClose: () => void }) {
  const { data: deliveries, isLoading } = useWebhookDeliveries(subscriptionId);

  return (
    <Modal title="Delivery History" onClose={onClose} width={560}>
      {isLoading ? (
        <TableSkeleton columns={3} rows={4} />
      ) : deliveries && deliveries.length > 0 ? (
        <ul style={{ listStyle: "none", padding: 0, margin: 0 }}>
          {deliveries.map((d) => (
            <li key={d.id} style={{ padding: "8px 0", borderBottom: "1px solid var(--color-border)" }}>
              <StatusBadge status={d.success ? "Sent" : "Failed"} /> <strong style={{ fontSize: 12.5 }}>{d.eventType}</strong>{" "}
              <span style={{ fontSize: 11.5, color: "var(--color-text-muted)" }}>
                attempt {d.attemptNumber} · {new Date(d.createdAt).toLocaleString()}
                {d.responseStatusCode ? ` · HTTP ${d.responseStatusCode}` : ""}
              </span>
              {d.errorMessage && <p className="field-error" style={{ margin: "2px 0 0" }}>{d.errorMessage}</p>}
            </li>
          ))}
        </ul>
      ) : (
        <p className="state-message">No deliveries yet.</p>
      )}
    </Modal>
  );
}

export function WebhooksPage() {
  const { data: webhooks, isLoading, isError } = useWebhooks();
  const deleteWebhook = useDeleteWebhook();
  const [showCreate, setShowCreate] = useState(false);
  const [newWebhook, setNewWebhook] = useState<CreatedWebhookSubscription | null>(null);
  const [viewingDeliveriesOf, setViewingDeliveriesOf] = useState<string | null>(null);
  const { confirm, dialog: confirmDialog } = useConfirmDialog();

  const handleDelete = async (id: string) => {
    if (!(await confirm({ message: "Delete this webhook subscription? This cannot be undone.", confirmLabel: "Delete" }))) return;
    try {
      await deleteWebhook.mutateAsync(id);
      toast.success("Webhook deleted");
    } catch (err) {
      toast.error(getApiErrorMessage(err, "Could not delete webhook."));
    }
  };

  return (
    <>
      <PageHeader
        title="Webhooks"
        subtitle="Receive signed HTTP callbacks when important events happen — lead created, deal contracted, and more."
        actions={
          <button type="button" className="btn btn-primary" onClick={() => setShowCreate(true)}>
            + New Webhook
          </button>
        }
      />

      <AsyncState
        isLoading={isLoading}
        isError={isError}
        errorMessage="Failed to load webhooks."
        isEmpty={!isLoading && (webhooks?.length ?? 0) === 0}
        emptyTitle="No webhooks yet"
        emptyMessage="Register an endpoint to receive event notifications."
        skeleton={<TableSkeleton columns={4} />}
      >
        <div className="card table-wrap">
          <table className="table">
            <thead>
              <tr>
                <th>Endpoint</th>
                <th>Events</th>
                <th>Status</th>
                <th className="sr-only">Actions</th>
              </tr>
            </thead>
            <tbody>
              {webhooks?.map((w) => (
                <tr key={w.id}>
                  <td style={{ display: "flex", alignItems: "center", gap: 8, wordBreak: "break-all" }}>
                    <Webhook size={14} color="var(--color-primary)" />
                    {w.url}
                  </td>
                  <td style={{ fontSize: 12, color: "var(--color-text-muted)" }}>{w.eventTypes.join(", ")}</td>
                  <td>
                    <StatusBadge status={w.isActive ? "Active" : "Inactive"} />
                  </td>
                  <td style={{ display: "flex", gap: 6 }}>
                    <button type="button" className="btn btn-sm" onClick={() => setViewingDeliveriesOf(w.id)}>
                      Deliveries
                    </button>
                    <button type="button" className="btn btn-sm" onClick={() => handleDelete(w.id)}>
                      Delete
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </AsyncState>

      {showCreate && (
        <Modal title="New Webhook" onClose={() => setShowCreate(false)}>
          <CreateWebhookForm
            onCreated={(webhook) => {
              setShowCreate(false);
              setNewWebhook(webhook);
            }}
          />
        </Modal>
      )}

      {newWebhook && (
        <Modal title="Webhook created" onClose={() => setNewWebhook(null)} width={480}>
          <div className="state-message" style={{ display: "flex", gap: 8, alignItems: "flex-start", textAlign: "left" }}>
            <ShieldAlert size={16} color="var(--color-warning)" style={{ flexShrink: 0, marginTop: 2 }} />
            <span>Copy this signing secret now — it will not be shown again. Use it to verify the X-Webhook-Signature header (HMAC-SHA256).</span>
          </div>
          <div
            style={{
              display: "flex",
              alignItems: "center",
              gap: 8,
              marginTop: 12,
              padding: "10px 12px",
              background: "var(--color-bg)",
              borderRadius: "var(--radius-md)",
              fontFamily: "monospace",
              fontSize: 12.5,
              wordBreak: "break-all",
            }}
          >
            {newWebhook.secret}
            <button
              type="button"
              className="icon-btn"
              style={{ flexShrink: 0 }}
              onClick={() => {
                navigator.clipboard.writeText(newWebhook.secret);
                toast.success("Copied to clipboard");
              }}
            >
              <Copy size={14} />
            </button>
          </div>
        </Modal>
      )}

      {viewingDeliveriesOf && <DeliveriesModal subscriptionId={viewingDeliveriesOf} onClose={() => setViewingDeliveriesOf(null)} />}

      {confirmDialog}
    </>
  );
}
