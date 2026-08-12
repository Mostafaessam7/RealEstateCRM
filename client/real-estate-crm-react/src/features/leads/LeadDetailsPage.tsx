import { useState } from "react";
import { useParams } from "react-router-dom";
import { Link } from "react-router-dom";
import { toast } from "sonner";
import { MessageCircle, Sparkles, Bot, Wand2 } from "lucide-react";
import { PageHeader } from "../../components/PageHeader";
import { AsyncState } from "../../components/AsyncState";
import { Modal } from "../../components/Modal";
import { StatusBadge } from "../../components/StatusBadge";
import { LeadForm, type LeadFormValues } from "./LeadForm";
import {
  useAssignLead,
  useLead,
  useLeadTimeline,
  useTransferLead,
  useUpdateLead,
  useAddLeadActivity,
  useLeadRecommendations,
  useGenerateAiInsight,
} from "./leadsApi";
import { useLeadWhatsAppMessages, useSendWhatsApp, useWhatsAppTemplates } from "../whatsapp/whatsappApi";
import { LeadActivityType, type LeadSource, type LeadStatus } from "../../types/lead";
import { getApiErrorMessage } from "../../api/client";
import { formatCurrency } from "../../utils/format";

export function LeadDetailsPage() {
  const { id } = useParams<{ id: string }>();
  const { data: lead, isLoading, isError } = useLead(id);
  const { data: timeline } = useLeadTimeline(id);
  const updateLead = useUpdateLead(id ?? "");
  const assignLead = useAssignLead(id ?? "");
  const transferLead = useTransferLead(id ?? "");
  const addActivity = useAddLeadActivity(id ?? "");
  const { data: waTemplates } = useWhatsAppTemplates();
  const { data: waMessages } = useLeadWhatsAppMessages(id);
  const sendWhatsApp = useSendWhatsApp(id ?? "");
  const { data: recommendations } = useLeadRecommendations(id);
  const generateInsight = useGenerateAiInsight(id ?? "");

  const [showEdit, setShowEdit] = useState(false);
  const [assignAgentId, setAssignAgentId] = useState("");
  const [activityNote, setActivityNote] = useState("");
  const [waTemplateId, setWaTemplateId] = useState("");
  const [waBody, setWaBody] = useState("");
  const [error, setError] = useState<string | null>(null);

  const handleUpdate = async (values: LeadFormValues) => {
    setError(null);
    try {
      await updateLead.mutateAsync({
        fullName: values.fullName,
        phone: values.phone || null,
        email: values.email || null,
        source: values.source as LeadSource,
        status: values.status as LeadStatus,
        budgetMin: values.budgetMin ?? null,
        budgetMax: values.budgetMax ?? null,
        preferredLocation: values.preferredLocation || null,
        propertyType: values.propertyType || null,
        notes: values.notes || null,
      });
      setShowEdit(false);
    } catch (err) {
      setError(getApiErrorMessage(err, "Could not update lead."));
    }
  };

  const handleAssign = async () => {
    if (!assignAgentId || !lead) return;
    setError(null);
    try {
      if (lead.assignedAgentId) {
        await transferLead.mutateAsync({ agentId: assignAgentId });
      } else {
        await assignLead.mutateAsync({ agentId: assignAgentId });
      }
      setAssignAgentId("");
    } catch (err) {
      setError(getApiErrorMessage(err, "Could not assign lead."));
    }
  };

  const handleAddNote = async () => {
    if (!activityNote.trim()) return;
    try {
      await addActivity.mutateAsync({ type: LeadActivityType.Note, description: activityNote });
      setActivityNote("");
    } catch (err) {
      setError(getApiErrorMessage(err, "Could not add activity."));
    }
  };

  const handleSendWhatsApp = async () => {
    if (!waTemplateId && !waBody.trim()) return;
    try {
      await sendWhatsApp.mutateAsync(
        waTemplateId ? { templateId: waTemplateId } : { body: waBody },
      );
      setWaTemplateId("");
      setWaBody("");
      toast.success("WhatsApp message sent");
    } catch (err) {
      toast.error(getApiErrorMessage(err, "Could not send WhatsApp message."));
    }
  };

  return (
    <>
      <PageHeader
        title={lead?.fullName ?? "Lead Details"}
        actions={
          lead && (
            <button type="button" className="btn" onClick={() => setShowEdit(true)}>
              Edit
            </button>
          )
        }
      />

      <AsyncState isLoading={isLoading} isError={isError} errorMessage="Failed to load lead.">
        {lead && (
          <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 16 }}>
            <div className="card">
              <h2 style={{ marginTop: 0 }}>Details</h2>
              <p>
                <strong>Status:</strong> <StatusBadge status={lead.status} />
              </p>
              <p>
                <strong>Source:</strong> {lead.source}
              </p>
              <p>
                <strong>Phone:</strong> {lead.phone ?? "—"}
              </p>
              <p>
                <strong>Email:</strong> {lead.email ?? "—"}
              </p>
              <p>
                <strong>Budget:</strong> {formatCurrency(lead.budgetMin)} - {formatCurrency(lead.budgetMax)}
              </p>
              <p>
                <strong>Preferred location:</strong> {lead.preferredLocation ?? "—"}
              </p>
              <p>
                <strong>Notes:</strong> {lead.notes ?? "—"}
              </p>

              <h3>Assignment</h3>
              {lead.assignedAgentId ? (
                <p>Assigned agent: {lead.assignedAgentId}</p>
              ) : (
                <p className="state-message">Not yet assigned.</p>
              )}
              <div className="toolbar">
                <input
                  className="input"
                  placeholder="Agent user ID"
                  value={assignAgentId}
                  onChange={(e) => setAssignAgentId(e.target.value)}
                />
                <button type="button" className="btn" onClick={handleAssign} disabled={!assignAgentId}>
                  {lead.assignedAgentId ? "Transfer" : "Assign"}
                </button>
              </div>
              {error && <p className="field-error">{error}</p>}
            </div>

            <div className="card">
              <h2 style={{ marginTop: 0 }}>Activity Timeline</h2>
              <div className="toolbar">
                <input
                  className="input"
                  placeholder="Add a note…"
                  value={activityNote}
                  onChange={(e) => setActivityNote(e.target.value)}
                />
                <button type="button" className="btn" onClick={handleAddNote} disabled={!activityNote.trim()}>
                  Add
                </button>
              </div>
              {timeline && timeline.length > 0 ? (
                <ul style={{ listStyle: "none", padding: 0, margin: 0 }}>
                  {timeline.map((activity) => (
                    <li key={activity.id} style={{ padding: "8px 0", borderBottom: "1px solid var(--color-border)" }}>
                      <span className="badge">{activity.type}</span>{" "}
                      <span style={{ color: "var(--color-text-muted)", fontSize: 12 }}>
                        {new Date(activity.activityDate).toLocaleString()}
                      </span>
                      <p style={{ margin: "4px 0 0" }}>{activity.description}</p>
                    </li>
                  ))}
                </ul>
              ) : (
                <p className="state-message">No activity yet.</p>
              )}
            </div>

            <div className="card" style={{ gridColumn: "1 / -1" }}>
              <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
                <h2 style={{ margin: 0, display: "flex", alignItems: "center", gap: 8 }}>
                  <Bot size={17} color="var(--color-primary)" /> AI Assistant
                </h2>
                <button
                  type="button"
                  className="btn btn-sm"
                  onClick={() => generateInsight.mutate()}
                  disabled={generateInsight.isPending}
                >
                  <Wand2 size={13} style={{ marginRight: 4 }} />
                  {generateInsight.isPending ? "Thinking…" : generateInsight.data ? "Regenerate" : "Generate insight"}
                </button>
              </div>

              {generateInsight.data ? (
                <div style={{ marginTop: 14, display: "grid", gap: 12 }}>
                  <div>
                    <div className="section-title" style={{ marginBottom: 4 }}>Summary</div>
                    <p style={{ margin: 0, fontSize: 13.5 }}>{generateInsight.data.summary}</p>
                  </div>
                  <div>
                    <div className="section-title" style={{ marginBottom: 4 }}>Next best action</div>
                    <p style={{ margin: 0, fontSize: 13.5 }}>{generateInsight.data.nextBestAction}</p>
                  </div>
                  <div className="card-flat">
                    <div className="section-title" style={{ marginBottom: 4 }}>Suggested follow-up message</div>
                    <p style={{ margin: "0 0 10px", fontSize: 13.5 }}>{generateInsight.data.suggestedFollowUpMessage}</p>
                    <button
                      type="button"
                      className="btn btn-sm"
                      onClick={() => {
                        setWaTemplateId("");
                        setWaBody(generateInsight.data!.suggestedFollowUpMessage);
                        toast.success("Copied into the WhatsApp message box below");
                      }}
                    >
                      Use as WhatsApp message
                    </button>
                  </div>
                </div>
              ) : (
                <p className="state-message">Generate a summary, next best action, and a draft follow-up message.</p>
              )}
            </div>

            <div className="card" style={{ gridColumn: "1 / -1" }}>
              <h2 style={{ marginTop: 0, display: "flex", alignItems: "center", gap: 8 }}>
                <Sparkles size={17} color="var(--color-primary)" /> Recommended Units
              </h2>
              {recommendations && recommendations.length > 0 ? (
                <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fill, minmax(220px, 1fr))", gap: 12 }}>
                  {recommendations.map((rec) => (
                    <Link
                      key={rec.unitId}
                      to={`/units/${rec.unitId}`}
                      className="card-flat"
                      style={{ display: "block", textDecoration: "none", color: "inherit" }}
                    >
                      <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
                        <strong style={{ fontSize: 13.5 }}>{rec.unitCode}</strong>
                        <span className="badge badge-info">{rec.score}% match</span>
                      </div>
                      <div style={{ fontSize: 13, marginTop: 4 }}>${rec.price.toLocaleString()}</div>
                      <div style={{ fontSize: 12, color: "var(--color-text-muted)" }}>{rec.location ?? "—"}</div>
                      {rec.matchReasons.length > 0 && (
                        <div style={{ fontSize: 11, color: "var(--color-text-faint)", marginTop: 6 }}>
                          {rec.matchReasons.join(" · ")}
                        </div>
                      )}
                      {rec.conversionLikelihood != null && (
                        <div style={{ display: "flex", alignItems: "center", gap: 5, fontSize: 11, color: "var(--color-primary)", marginTop: 6 }}>
                          <Sparkles size={11} /> {Math.round(rec.conversionLikelihood * 100)}% predicted conversion (ML)
                        </div>
                      )}
                    </Link>
                  ))}
                </div>
              ) : (
                <p className="state-message">No available units match this lead yet.</p>
              )}
            </div>

            <div className="card" style={{ gridColumn: "1 / -1" }}>
              <h2 style={{ marginTop: 0, display: "flex", alignItems: "center", gap: 8 }}>
                <MessageCircle size={17} color="var(--color-success)" /> WhatsApp
              </h2>

              {lead.phone ? (
                <>
                  <div className="toolbar">
                    <select
                      className="input"
                      style={{ maxWidth: 220 }}
                      value={waTemplateId}
                      onChange={(e) => setWaTemplateId(e.target.value)}
                    >
                      <option value="">Custom message…</option>
                      {waTemplates?.filter((t) => t.isActive).map((t) => (
                        <option key={t.id} value={t.id}>
                          {t.name}
                        </option>
                      ))}
                    </select>
                    {!waTemplateId && (
                      <input
                        className="input"
                        placeholder="Type a message…"
                        value={waBody}
                        onChange={(e) => setWaBody(e.target.value)}
                      />
                    )}
                    <button
                      type="button"
                      className="btn btn-primary"
                      onClick={handleSendWhatsApp}
                      disabled={sendWhatsApp.isPending || (!waTemplateId && !waBody.trim())}
                    >
                      Send
                    </button>
                  </div>

                  {waMessages && waMessages.length > 0 ? (
                    <ul style={{ listStyle: "none", padding: 0, margin: "10px 0 0" }}>
                      {waMessages.map((message) => (
                        <li key={message.id} style={{ padding: "8px 0", borderBottom: "1px solid var(--color-border)" }}>
                          <StatusBadge status={message.status} />{" "}
                          <span style={{ color: "var(--color-text-muted)", fontSize: 12 }}>
                            {new Date(message.createdAt).toLocaleString()}
                          </span>
                          <p style={{ margin: "4px 0 0" }}>{message.body}</p>
                          {message.errorMessage && <p className="field-error" style={{ margin: "2px 0 0" }}>{message.errorMessage}</p>}
                        </li>
                      ))}
                    </ul>
                  ) : (
                    <p className="state-message">No WhatsApp messages yet.</p>
                  )}
                </>
              ) : (
                <p className="state-message">Add a phone number to this lead to send WhatsApp messages.</p>
              )}
            </div>
          </div>
        )}
      </AsyncState>

      {showEdit && lead && (
        <Modal title="Edit Lead" onClose={() => setShowEdit(false)}>
          <LeadForm
            includeStatus
            defaultValues={{
              fullName: lead.fullName,
              phone: lead.phone ?? "",
              email: lead.email ?? "",
              source: lead.source,
              status: lead.status,
              budgetMin: lead.budgetMin ?? undefined,
              budgetMax: lead.budgetMax ?? undefined,
              preferredLocation: lead.preferredLocation ?? "",
              propertyType: lead.propertyType ?? "",
              notes: lead.notes ?? "",
            }}
            onSubmit={handleUpdate}
            submitLabel="Save Changes"
          />
        </Modal>
      )}
    </>
  );
}
