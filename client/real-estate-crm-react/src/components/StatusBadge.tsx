import { statusVariant } from "../utils/statusVariant";

export function StatusBadge({ status }: { status: string }) {
  return <span className={`badge badge-${statusVariant(status)}`}>{status}</span>;
}
