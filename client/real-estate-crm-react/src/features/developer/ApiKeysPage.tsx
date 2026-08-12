import { useState } from "react";
import { toast } from "sonner";
import { Key, Copy, ShieldAlert } from "lucide-react";
import { PageHeader } from "../../components/PageHeader";
import { AsyncState } from "../../components/AsyncState";
import { StatusBadge } from "../../components/StatusBadge";
import { Modal } from "../../components/Modal";
import { TableSkeleton } from "../../components/Skeleton";
import { useConfirmDialog } from "../../components/ConfirmDialog";
import { getApiErrorMessage } from "../../api/client";
import { useApiKeys, useCreateApiKey, useRevokeApiKey } from "./apiKeysApi";
import type { CreatedApiKey } from "../../types/apiKey";

function CreateKeyForm({ onCreated }: { onCreated: (key: CreatedApiKey) => void }) {
  const createKey = useCreateApiKey();
  const [name, setName] = useState("");
  const [scopes, setScopes] = useState("read");

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const created = await createKey.mutateAsync({ name, scopes });
      onCreated(created);
    } catch (err) {
      toast.error(getApiErrorMessage(err, "Could not create API key."));
    }
  };

  return (
    <form onSubmit={handleSubmit}>
      <div className="field">
        <label>Name</label>
        <input className="input" value={name} onChange={(e) => setName(e.target.value)} placeholder="Mobile app — production" required />
      </div>
      <div className="field">
        <label>Scopes</label>
        <select className="input" value={scopes} onChange={(e) => setScopes(e.target.value)}>
          <option value="read">Read only</option>
          <option value="read,write">Read + write</option>
        </select>
      </div>
      <button type="submit" className="btn btn-primary" disabled={createKey.isPending} style={{ width: "100%" }}>
        {createKey.isPending ? "Creating…" : "Create Key"}
      </button>
    </form>
  );
}

export function ApiKeysPage() {
  const { data: keys, isLoading, isError } = useApiKeys();
  const revokeKey = useRevokeApiKey();
  const [showCreate, setShowCreate] = useState(false);
  const [newKey, setNewKey] = useState<CreatedApiKey | null>(null);
  const { confirm, dialog: confirmDialog } = useConfirmDialog();

  const handleRevoke = async (id: string) => {
    if (!(await confirm({ message: "Revoke this API key? Any integration using it will stop working immediately.", confirmLabel: "Revoke" }))) return;
    try {
      await revokeKey.mutateAsync(id);
      toast.success("API key revoked");
    } catch (err) {
      toast.error(getApiErrorMessage(err, "Could not revoke key."));
    }
  };

  return (
    <>
      <PageHeader
        title="API Keys"
        subtitle="Credentials for the Public API (/api/v1) — for mobile apps and integrations."
        actions={
          <button type="button" className="btn btn-primary" onClick={() => setShowCreate(true)}>
            + New Key
          </button>
        }
      />

      <AsyncState
        isLoading={isLoading}
        isError={isError}
        errorMessage="Failed to load API keys."
        isEmpty={!isLoading && (keys?.length ?? 0) === 0}
        emptyTitle="No API keys yet"
        emptyMessage="Create a key to authenticate mobile apps or integrations against /api/v1."
        skeleton={<TableSkeleton columns={5} />}
      >
        <div className="card table-wrap">
          <table className="table">
            <thead>
              <tr>
                <th>Name</th>
                <th>Key</th>
                <th>Scopes</th>
                <th>Status</th>
                <th>Last used</th>
                <th className="sr-only">Actions</th>
              </tr>
            </thead>
            <tbody>
              {keys?.map((k) => (
                <tr key={k.id}>
                  <td style={{ display: "flex", alignItems: "center", gap: 8 }}>
                    <Key size={14} color="var(--color-primary)" />
                    {k.name}
                  </td>
                  <td style={{ fontFamily: "monospace", fontSize: 12.5 }}>{k.keyPrefix}…</td>
                  <td>{k.scopes}</td>
                  <td>
                    <StatusBadge status={k.isActive ? "Active" : "Inactive"} />
                  </td>
                  <td style={{ fontSize: 12.5, color: "var(--color-text-muted)" }}>
                    {k.lastUsedAt ? new Date(k.lastUsedAt).toLocaleString() : "Never"}
                  </td>
                  <td>
                    {k.isActive && (
                      <button type="button" className="btn btn-sm" onClick={() => handleRevoke(k.id)}>
                        Revoke
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
        <Modal title="New API Key" onClose={() => setShowCreate(false)}>
          <CreateKeyForm
            onCreated={(key) => {
              setShowCreate(false);
              setNewKey(key);
            }}
          />
        </Modal>
      )}

      {newKey && (
        <Modal title="API key created" onClose={() => setNewKey(null)} width={480}>
          <div className="state-message" style={{ display: "flex", gap: 8, alignItems: "flex-start", textAlign: "left" }}>
            <ShieldAlert size={16} color="var(--color-warning)" style={{ flexShrink: 0, marginTop: 2 }} />
            <span>Copy this key now — it will not be shown again.</span>
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
            {newKey.plaintextKey}
            <button
              type="button"
              className="icon-btn"
              style={{ flexShrink: 0 }}
              onClick={() => {
                navigator.clipboard.writeText(newKey.plaintextKey);
                toast.success("Copied to clipboard");
              }}
            >
              <Copy size={14} />
            </button>
          </div>
        </Modal>
      )}

      {confirmDialog}
    </>
  );
}
