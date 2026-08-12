import { useState } from "react";
import { toast } from "sonner";
import { Send, Mail, MessageCircle } from "lucide-react";
import { PageHeader } from "../../components/PageHeader";
import { AsyncState } from "../../components/AsyncState";
import { StatusBadge } from "../../components/StatusBadge";
import { Modal } from "../../components/Modal";
import { TableSkeleton } from "../../components/Skeleton";
import { useConfirmDialog } from "../../components/ConfirmDialog";
import { getApiErrorMessage } from "../../api/client";
import { useCampaignRecipients, useCampaigns, useCreateCampaign, useSendCampaign } from "./campaignsApi";
import { CampaignChannel, type CreateCampaignRequest } from "../../types/campaign";
import { LeadSource, LeadStatus } from "../../types/lead";

function CreateCampaignForm({ onCreated }: { onCreated: () => void }) {
  const createCampaign = useCreateCampaign();
  const [form, setForm] = useState<CreateCampaignRequest>({ name: "", channel: CampaignChannel.WhatsApp, body: "" });

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await createCampaign.mutateAsync(form);
      onCreated();
      toast.success("Campaign created as a draft");
    } catch (err) {
      toast.error(getApiErrorMessage(err, "Could not create campaign."));
    }
  };

  return (
    <form onSubmit={handleSubmit}>
      <div className="field">
        <label>Name</label>
        <input
          className="input"
          value={form.name}
          onChange={(e) => setForm({ ...form, name: e.target.value })}
          placeholder="Spring 2026 WhatsApp blast"
          required
        />
      </div>
      <div className="field">
        <label>Channel</label>
        <select
          className="input"
          value={form.channel}
          onChange={(e) => setForm({ ...form, channel: e.target.value as CampaignChannel })}
        >
          <option value={CampaignChannel.WhatsApp}>WhatsApp</option>
          <option value={CampaignChannel.Email}>Email</option>
        </select>
      </div>
      {form.channel === CampaignChannel.Email && (
        <div className="field">
          <label>Subject</label>
          <input className="input" value={form.subject ?? ""} onChange={(e) => setForm({ ...form, subject: e.target.value })} required />
        </div>
      )}
      <div className="field">
        <label>Message body</label>
        <textarea
          className="input"
          rows={4}
          value={form.body}
          onChange={(e) => setForm({ ...form, body: e.target.value })}
          placeholder="Hi {{FullName}}, check out our new units in {{PreferredLocation}}!"
          required
        />
      </div>
      <div className="field">
        <label>Target lead status (optional)</label>
        <select className="input" value={form.targetStatus ?? ""} onChange={(e) => setForm({ ...form, targetStatus: e.target.value || undefined })}>
          <option value="">Any status</option>
          {Object.values(LeadStatus).map((s) => (
            <option key={s} value={s}>{s}</option>
          ))}
        </select>
      </div>
      <div className="field">
        <label>Target lead source (optional)</label>
        <select className="input" value={form.targetSource ?? ""} onChange={(e) => setForm({ ...form, targetSource: e.target.value || undefined })}>
          <option value="">Any source</option>
          {Object.values(LeadSource).map((s) => (
            <option key={s} value={s}>{s}</option>
          ))}
        </select>
      </div>
      <button type="submit" className="btn btn-primary" disabled={createCampaign.isPending} style={{ width: "100%" }}>
        {createCampaign.isPending ? "Saving…" : "Create Draft"}
      </button>
    </form>
  );
}

function RecipientsModal({ campaignId, onClose }: { campaignId: string; onClose: () => void }) {
  const { data: recipients, isLoading } = useCampaignRecipients(campaignId);

  return (
    <Modal title="Delivery History" onClose={onClose} width={520}>
      {isLoading ? (
        <TableSkeleton columns={3} rows={4} />
      ) : recipients && recipients.length > 0 ? (
        <ul style={{ listStyle: "none", padding: 0, margin: 0 }}>
          {recipients.map((r) => (
            <li key={r.id} style={{ padding: "8px 0", borderBottom: "1px solid var(--color-border)" }}>
              <StatusBadge status={r.success ? "Sent" : "Failed"} />{" "}
              <span style={{ fontSize: 12, color: "var(--color-text-muted)" }}>{new Date(r.sentAt).toLocaleString()}</span>
              {r.errorMessage && <p className="field-error" style={{ margin: "2px 0 0" }}>{r.errorMessage}</p>}
            </li>
          ))}
        </ul>
      ) : (
        <p className="state-message">No delivery history yet.</p>
      )}
    </Modal>
  );
}

export function CampaignsPage() {
  const { data: campaigns, isLoading, isError } = useCampaigns();
  const sendCampaign = useSendCampaign();
  const [showCreate, setShowCreate] = useState(false);
  const [viewingRecipientsOf, setViewingRecipientsOf] = useState<string | null>(null);
  const { confirm, dialog: confirmDialog } = useConfirmDialog();

  const handleSend = async (id: string) => {
    if (!(await confirm({ message: "Send this campaign now? This cannot be undone.", confirmLabel: "Send", danger: false }))) return;
    try {
      await sendCampaign.mutateAsync(id);
      toast.success("Campaign sent");
    } catch (err) {
      toast.error(getApiErrorMessage(err, "Could not send campaign."));
    }
  };

  return (
    <>
      <PageHeader
        title="Marketing Campaigns"
        subtitle="Bulk-broadcast Email or WhatsApp messages to a segment of leads."
        actions={
          <button type="button" className="btn btn-primary" onClick={() => setShowCreate(true)}>
            + New Campaign
          </button>
        }
      />

      <AsyncState
        isLoading={isLoading}
        isError={isError}
        errorMessage="Failed to load campaigns."
        isEmpty={!isLoading && (campaigns?.length ?? 0) === 0}
        emptyTitle="No campaigns yet"
        emptyMessage="Create a campaign to reach a segment of your leads."
        skeleton={<TableSkeleton columns={5} />}
      >
        <div className="card table-wrap">
          <table className="table">
            <thead>
              <tr>
                <th>Name</th>
                <th>Channel</th>
                <th>Target</th>
                <th>Status</th>
                <th>Delivered</th>
                <th className="sr-only">Actions</th>
              </tr>
            </thead>
            <tbody>
              {campaigns?.map((c) => (
                <tr key={c.id}>
                  <td>{c.name}</td>
                  <td style={{ display: "flex", alignItems: "center", gap: 6 }}>
                    {c.channel === "Email" ? <Mail size={14} /> : <MessageCircle size={14} color="var(--color-success)" />}
                    {c.channel}
                  </td>
                  <td style={{ fontSize: 12.5, color: "var(--color-text-muted)" }}>
                    {c.targetStatus ?? "Any status"} · {c.targetSource ?? "Any source"}
                  </td>
                  <td>
                    <StatusBadge status={c.status} />
                  </td>
                  <td>
                    {c.status === "Sent" ? (
                      <button type="button" className="btn btn-sm" onClick={() => setViewingRecipientsOf(c.id)}>
                        {c.successCount}/{c.recipientCount} sent
                      </button>
                    ) : (
                      "—"
                    )}
                  </td>
                  <td>
                    {c.status === "Draft" && (
                      <button
                        type="button"
                        className="btn btn-primary btn-sm"
                        onClick={() => handleSend(c.id)}
                        disabled={sendCampaign.isPending}
                      >
                        <Send size={13} style={{ marginRight: 4 }} />
                        Send
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </AsyncState>

      {showCreate && (
        <Modal title="New Campaign" onClose={() => setShowCreate(false)}>
          <CreateCampaignForm onCreated={() => setShowCreate(false)} />
        </Modal>
      )}

      {viewingRecipientsOf && <RecipientsModal campaignId={viewingRecipientsOf} onClose={() => setViewingRecipientsOf(null)} />}

      {confirmDialog}
    </>
  );
}
