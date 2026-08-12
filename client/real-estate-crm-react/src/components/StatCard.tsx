import { useEffect, useState, type ReactNode } from "react";
import { motion } from "framer-motion";

interface StatCardProps {
  label: string;
  value: number;
  icon: ReactNode;
  accent?: "primary" | "success" | "warning" | "info";
  suffix?: string;
  prefix?: string;
  format?: (value: number) => string;
  index?: number;
}

const ACCENTS: Record<string, string> = {
  primary: "var(--color-primary)",
  success: "var(--color-success)",
  warning: "var(--color-warning)",
  info: "var(--color-info)",
};

const ACCENT_SOFT: Record<string, string> = {
  primary: "var(--color-primary-soft)",
  success: "var(--color-success-soft)",
  warning: "var(--color-warning-soft)",
  info: "var(--color-info-soft)",
};

/**
 * Animates from 0 to the target value once, on mount/value change — a small "alive" touch.
 *
 * This must never be the only path to showing the real number: requestAnimationFrame is
 * throttled or fully paused by the browser for a backgrounded/non-visible tab, which without a
 * fallback left this stuck at 0 indefinitely — a real bug (business KPIs silently reading zero,
 * not just "reserved" as a loading affordance) caught while verifying the running app, not by
 * a build/type check. Two guards: skip the animation entirely under prefers-reduced-motion
 * (accessibility — it was never respecting this before either), and a setTimeout safety net
 * (unlike rAF, timers still fire — just coarsely throttled — in a backgrounded tab) that forces
 * the correct value once the animation's duration has elapsed regardless of whether any rAF
 * frame actually ran.
 */
function useCountUp(target: number, durationMs = 700) {
  const prefersReducedMotion =
    typeof window !== "undefined" && window.matchMedia?.("(prefers-reduced-motion: reduce)").matches;
  const [value, setValue] = useState(prefersReducedMotion ? target : 0);

  useEffect(() => {
    if (prefersReducedMotion) {
      setValue(target);
      return;
    }

    let frame: number;
    let settled = false;
    const start = performance.now();
    const from = 0;

    const settle = () => {
      if (settled) return;
      settled = true;
      setValue(target);
    };

    const tick = (now: number) => {
      const progress = Math.min((now - start) / durationMs, 1);
      const eased = 1 - Math.pow(1 - progress, 3);
      setValue(from + (target - from) * eased);
      if (progress < 1) {
        frame = requestAnimationFrame(tick);
      } else {
        settle();
      }
    };

    frame = requestAnimationFrame(tick);
    const fallbackTimer = window.setTimeout(settle, durationMs + 250);

    return () => {
      cancelAnimationFrame(frame);
      window.clearTimeout(fallbackTimer);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [target, durationMs, prefersReducedMotion]);

  return value;
}

export function StatCard({ label, value, icon, accent = "primary", suffix, prefix, format, index = 0 }: StatCardProps) {
  const animated = useCountUp(value);
  const display = format ? format(animated) : Math.round(animated).toLocaleString();

  return (
    <motion.div
      className="card"
      initial={{ opacity: 0, y: 14 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.35, delay: index * 0.05, ease: [0.22, 1, 0.36, 1] }}
      whileHover={{ y: -3 }}
      // A slim top-accent bar (not a border on every edge, not a gradient) so each metric's
      // category reads at a glance while scanning the row — one small, restrained addition
      // rather than a heavier redesign of the card itself.
      style={{ borderTop: `3px solid ${ACCENTS[accent]}` }}
    >
      <div
        style={{
          width: 40,
          height: 40,
          borderRadius: 12,
          display: "grid",
          placeItems: "center",
          background: ACCENT_SOFT[accent],
          color: ACCENTS[accent],
        }}
      >
        {icon}
      </div>
      <div className="value" style={{ marginTop: 14 }}>
        {prefix}
        {display}
        {suffix}
      </div>
      <div className="label">{label}</div>
    </motion.div>
  );
}
