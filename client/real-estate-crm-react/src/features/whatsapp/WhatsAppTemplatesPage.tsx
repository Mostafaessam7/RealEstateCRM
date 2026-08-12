import { useState } from "react";
import { toast } from "sonner";
import { MessageCircle } from "lucide-react";
import { PageHeader } from "../../components/PageHeader";
import { AsyncState } from "../../components/AsyncState";
import { StatusBadge } from "../../components/StatusBadge";
import { Modal } from "../../components/Modal";
import { TableSkeleton } from "../../components/Skeleton";
import { getApiErrorMessage } from "../../api/client";
import { useCreateWhatsAppTemplate, useUpdateWhatsAppTemplate, useWhatsAppTemplates } from "./whatsappApi";
import type { WhatsAppTemplate } from "../../types/whatsapp";

function TemplateForm({
  defaultValues,
  onSubmit,
  submitLabel,
}: {
  defaultValues?: { name: string; body: string };
  onSubmit: (values: { name: string; body: string }) => Promise<void>;
  submitLabel: string;
}) {
  const [name, setName] = useState(defaultValues?.name ?? "");
  const [body, setBody] = useState(defaultValues?.body ?? "");
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    try {
      await onSubmit({ name, body });
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <form onSubmit={handleSubmit}>
      <div className="field">
        <label>Name</label>
        <input className="input" value={name} onChange={(e) => setName(e.target.value)} required />
      </div>
      <div className="field">
        <label>Message body</label>
        <textarea
          className="input"
          rows={4}
          value={body}
          onChange={(e) => setBody(e.target.value)}
          placeholder="Hi {{FullName}}, we have new units in {{PreferredLocation}}!"
          required
        />
        <span style={{ fontSize: 11.5, color: "var(--color-text-faint)", marginTop: 4 }}>
          Placeholders: {"{{FullName}}"}, {"{{PreferredLocation}}"}, {"{{PropertyType}}"}
        </span>
      </div>
      <button type="submit" className="btn btn-primary" disabled={submitting} style={{ width: "100%" }}>
        {submitting ? "Saving…" : submitLabel}
      </button>
    </form>
  );
}

export function WhatsAppTemplatesPage() {
  const { data: templates, isLoading, isError } = useWhatsAppTemplates();
  const createTemplate = useCreateWhatsAppTemplate();
  const [showCreate, setShowCreate] = useState(false);
  const [editing, setEditing] = useState<WhatsAppTemplate | null>(null);

  const handleCreate = async (values: { name: string; body: string }) => {
    try {
      await createTemplate.mutateAsync(values);
      setShowCreate(false);
      toast.success("Template created");
    } catch (err) {
      toast.error(getApiErrorMessage(err, "Could not create template."));
    }
  };

  return (
    <>
      <PageHeader
        title="WhatsApp Templates"
        subtitle="Reusable message templates for lead outreach."
        actions={
          <button type="button" className="btn btn-primary" onClick={() => setShowCreate(true)}>
            + New Template
          </button>
        }
      />

      <AsyncState
        isLoading={isLoading}
        isError={isError}
        errorMessage="Failed to load templates."
        isEmpty={!isLoading && (templates?.length ?? 0) === 0}
        emptyTitle="No templates yet"
        emptyMessage="Create a template to speed up lead outreach on WhatsApp."
        skeleton={<TableSkeleton columns={3} />}
      >
        <div className="card table-wrap">
          <table className="table">
            <thead>
              <tr>
                <th>Name</th>
                <th>Body</th>
                <th>Status</th>
                <th className="sr-only">Actions</th>
              </tr>
            </thead>
            <tbody>
              {templates?.map((template) => (
                <tr key={template.id}>
                  <td style={{ display: "flex", alignItems: "center", gap: 8 }}>
                    <MessageCircle size={14} color="var(--color-success)" />
                    {template.name}
                  </td>
                  <td style={{ maxWidth: 380, color: "var(--color-text-muted)" }}>{template.body}</td>
                  <td>
                    <StatusBadge status={template.isActive ? "Active" : "Inactive"} />
                  </td>
                  <td>
                    <button type="button" className="btn btn-sm" onClick={() => setEditing(template)}>
                      Edit
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </AsyncState>

      {showCreate && (
        <Modal title="New WhatsApp Template" onClose={() => setShowCreate(false)}>
          <TemplateForm onSubmit={handleCreate} submitLabel="Create Template" />
        </Modal>
      )}

      {editing && (
        <EditTemplateModal template={editing} onClose={() => setEditing(null)} />
      )}
    </>
  );
}

function EditTemplateModal({ template, onClose }: { template: WhatsAppTemplate; onClose: () => void }) {
  const updateTemplate = useUpdateWhatsAppTemplate(template.id);
  const [isActive, setIsActive] = useState(template.isActive);

  const handleSubmit = async (values: { name: string; body: string }) => {
    try {
      await updateTemplate.mutateAsync({ ...values, isActive });
      onClose();
      toast.success("Template updated");
    } catch (err) {
      toast.error(getApiErrorMessage(err, "Could not update template."));
    }
  };

  return (
    <Modal title="Edit Template" onClose={onClose}>
      <TemplateForm defaultValues={template} onSubmit={handleSubmit} submitLabel="Save Changes" />
      <label style={{ display: "flex", alignItems: "center", gap: 8, marginTop: 12, fontSize: 13 }}>
        <input type="checkbox" checked={isActive} onChange={(e) => setIsActive(e.target.checked)} />
        Active
      </label>
    </Modal>
  );
}
