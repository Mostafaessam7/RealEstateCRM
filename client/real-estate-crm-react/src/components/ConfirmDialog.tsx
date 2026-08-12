import { useCallback, useState, type ReactNode } from "react";
import { AlertTriangle } from "lucide-react";
import { Modal } from "./Modal";

interface ConfirmOptions {
  title?: string;
  message: ReactNode;
  confirmLabel?: string;
  /** Defaults to true — renders the confirm button as destructive (red). Set false for a
   * non-destructive confirmation (e.g. "send this campaign now?"). */
  danger?: boolean;
}

/**
 * Replaces the browser's blocking `confirm()` with an in-app modal that matches the rest of
 * the design system. Usage: `const { confirm, dialog } = useConfirmDialog();` then
 * `if (!(await confirm("Delete this?"))) return;` and render `{dialog}` once in the page.
 */
export function useConfirmDialog() {
  const [options, setOptions] = useState<ConfirmOptions | null>(null);
  const [resolver, setResolver] = useState<((value: boolean) => void) | null>(null);

  const confirm = useCallback((opts: ConfirmOptions | string) => {
    const normalized = typeof opts === "string" ? { message: opts } : opts;
    return new Promise<boolean>((resolve) => {
      setOptions(normalized);
      setResolver(() => resolve);
    });
  }, []);

  const close = (result: boolean) => {
    resolver?.(result);
    setOptions(null);
    setResolver(null);
  };

  const dialog = options ? (
    <Modal title={options.title ?? "Are you sure?"} onClose={() => close(false)} width={420}>
      <div style={{ display: "flex", gap: 10, alignItems: "flex-start" }}>
        <AlertTriangle
          size={18}
          color={options.danger === false ? "var(--color-warning)" : "var(--color-danger)"}
          style={{ flexShrink: 0, marginTop: 2 }}
        />
        <p style={{ margin: 0, fontSize: 13.5, lineHeight: 1.5 }}>{options.message}</p>
      </div>
      <div style={{ display: "flex", justifyContent: "flex-end", gap: 8, marginTop: 22 }}>
        <button type="button" className="btn" onClick={() => close(false)}>
          Cancel
        </button>
        <button
          type="button"
          className={options.danger === false ? "btn btn-primary" : "btn btn-danger"}
          onClick={() => close(true)}
        >
          {options.confirmLabel ?? "Confirm"}
        </button>
      </div>
    </Modal>
  ) : null;

  return { confirm, dialog };
}
