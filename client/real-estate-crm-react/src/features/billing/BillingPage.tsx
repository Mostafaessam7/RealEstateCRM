import { motion } from "framer-motion";
import { Check, CreditCard, Users, Contact, DoorOpen } from "lucide-react";
import { toast } from "sonner";
import { PageHeader } from "../../components/PageHeader";
import { AsyncState } from "../../components/AsyncState";
import { StatusBadge } from "../../components/StatusBadge";
import { CardGridSkeleton } from "../../components/Skeleton";
import { useConfirmDialog } from "../../components/ConfirmDialog";
import { getApiErrorMessage } from "../../api/client";
import { usePlans, useCurrentSubscription, useChangePlan, useCancelSubscription } from "./billingApi";

function UsageBar({ icon, label, used, limit }: { icon: React.ReactNode; label: string; used: number; limit: number }) {
  const percent = limit > 0 ? Math.min(100, Math.round((used / limit) * 100)) : 0;
  const nearLimit = percent >= 90;

  return (
    <div style={{ marginBottom: 14 }}>
      <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", marginBottom: 6 }}>
        <span style={{ display: "flex", alignItems: "center", gap: 6, fontSize: 13, fontWeight: 500 }}>
          {icon}
          {label}
        </span>
        <span style={{ fontSize: 12.5, color: "var(--color-text-muted)" }}>
          {used.toLocaleString()} / {limit.toLocaleString()}
        </span>
      </div>
      <div className="progress-track">
        <div
          className="progress-fill"
          style={{ width: `${percent}%`, background: nearLimit ? "var(--color-danger)" : undefined }}
        />
      </div>
    </div>
  );
}

export function BillingPage() {
  const { data: plans, isLoading: plansLoading, isError: plansError } = usePlans();
  const { data: subscription, isLoading, isError } = useCurrentSubscription();
  const changePlan = useChangePlan();
  const cancelSubscription = useCancelSubscription();
  const { confirm, dialog: confirmDialog } = useConfirmDialog();

  const handleChoosePlan = async (code: string) => {
    try {
      await changePlan.mutateAsync({ planCode: code });
      toast.success("Plan updated");
    } catch (err) {
      toast.error(getApiErrorMessage(err, "Could not change plan."));
    }
  };

  const handleCancel = async () => {
    if (!(await confirm({ message: "Cancel the current subscription? You'll lose access at the end of the current billing period.", confirmLabel: "Cancel subscription" }))) return;
    try {
      await cancelSubscription.mutateAsync();
      toast.success("Subscription cancelled");
    } catch (err) {
      toast.error(getApiErrorMessage(err, "Could not cancel subscription."));
    }
  };

  return (
    <>
      <PageHeader
        title="Billing & Subscription"
        subtitle="Manage your plan, usage, and billing period. Creating leads, units, or users beyond your plan's limit is blocked until you upgrade."
      />

      <AsyncState
        isLoading={isLoading || plansLoading}
        isError={isError || plansError}
        errorMessage="Failed to load subscription details."
        skeleton={<CardGridSkeleton count={3} />}
      >
        {subscription && (
          <div className="card" style={{ marginBottom: 22 }}>
            <div style={{ display: "flex", alignItems: "flex-start", justifyContent: "space-between", flexWrap: "wrap", gap: 12 }}>
              <div>
                <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
                  <span
                    style={{
                      width: 40,
                      height: 40,
                      borderRadius: 12,
                      display: "grid",
                      placeItems: "center",
                      background: "var(--color-primary-soft)",
                      color: "var(--color-primary)",
                    }}
                  >
                    <CreditCard size={19} />
                  </span>
                  <div>
                    <div style={{ fontFamily: "var(--font-display)", fontSize: 19, fontWeight: 700 }}>{subscription.plan.name} plan</div>
                    <div style={{ fontSize: 12.5, color: "var(--color-text-muted)" }}>
                      ${subscription.plan.monthlyPrice.toLocaleString()}/mo
                    </div>
                  </div>
                </div>
              </div>

              <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
                <StatusBadge status={subscription.status} />
                {subscription.status !== "Cancelled" && (
                  <button type="button" className="btn btn-ghost btn-sm" onClick={handleCancel} disabled={cancelSubscription.isPending}>
                    Cancel subscription
                  </button>
                )}
              </div>
            </div>

            <div style={{ marginTop: 20 }}>
              <UsageBar icon={<Users size={14} />} label="Users" used={subscription.usage.userCount} limit={subscription.plan.maxUsers} />
              <UsageBar icon={<Contact size={14} />} label="Leads" used={subscription.usage.leadCount} limit={subscription.plan.maxLeads} />
              <UsageBar icon={<DoorOpen size={14} />} label="Units" used={subscription.usage.unitCount} limit={subscription.plan.maxUnits} />
            </div>

            <p className="subtitle" style={{ marginTop: 4 }}>
              {subscription.status === "Trialing"
                ? `Trial ends ${new Date(subscription.trialEndsAt).toLocaleDateString()}`
                : `Current period ends ${new Date(subscription.currentPeriodEnd).toLocaleDateString()}`}
            </p>
          </div>
        )}

        <div className="section-title">Available plans</div>
        <div className="kpi-grid">
          {plans?.map((plan, index) => {
            const isCurrent = subscription?.plan.code === plan.code;
            return (
              <motion.div
                key={plan.id}
                className="card"
                initial={{ opacity: 0, y: 14 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ duration: 0.3, delay: index * 0.05, ease: [0.22, 1, 0.36, 1] }}
                style={isCurrent ? { borderColor: "var(--color-primary)", boxShadow: "0 0 0 3px var(--color-primary-soft)" } : undefined}
              >
                <div style={{ fontFamily: "var(--font-display)", fontSize: 17, fontWeight: 700 }}>{plan.name}</div>
                <div style={{ fontSize: 24, fontWeight: 700, marginTop: 4 }}>
                  ${plan.monthlyPrice.toLocaleString()}
                  <span style={{ fontSize: 12.5, color: "var(--color-text-muted)", fontWeight: 500 }}> /mo</span>
                </div>

                <ul style={{ listStyle: "none", padding: 0, margin: "14px 0", fontSize: 12.5, color: "var(--color-text-muted)" }}>
                  <li style={{ display: "flex", alignItems: "center", gap: 6, marginBottom: 6 }}>
                    <Check size={13} color="var(--color-success)" /> {plan.maxUsers.toLocaleString()} users
                  </li>
                  <li style={{ display: "flex", alignItems: "center", gap: 6, marginBottom: 6 }}>
                    <Check size={13} color="var(--color-success)" /> {plan.maxLeads.toLocaleString()} leads
                  </li>
                  <li style={{ display: "flex", alignItems: "center", gap: 6 }}>
                    <Check size={13} color="var(--color-success)" /> {plan.maxUnits.toLocaleString()} units
                  </li>
                </ul>

                <button
                  type="button"
                  className={isCurrent ? "btn" : "btn btn-primary"}
                  style={{ width: "100%" }}
                  disabled={isCurrent || changePlan.isPending}
                  onClick={() => handleChoosePlan(plan.code)}
                >
                  {isCurrent ? "Current plan" : "Choose plan"}
                </button>
              </motion.div>
            );
          })}
        </div>
      </AsyncState>

      {confirmDialog}
    </>
  );
}
