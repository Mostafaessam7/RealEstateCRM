import type { ReactNode } from "react";
import { AlertTriangle, Inbox } from "lucide-react";
import { motion } from "framer-motion";
import { TableSkeleton } from "./Skeleton";

interface AsyncStateProps {
  isLoading: boolean;
  isError: boolean;
  errorMessage?: string;
  isEmpty?: boolean;
  emptyTitle?: string;
  emptyMessage?: string;
  skeleton?: ReactNode;
  children: ReactNode;
}

/** Every async page renders one of: loading (skeleton), error, empty, or success. */
export function AsyncState({
  isLoading,
  isError,
  errorMessage = "Something went wrong while loading this data.",
  isEmpty = false,
  emptyTitle = "Nothing here yet",
  emptyMessage = "Once you add some, they'll show up here.",
  skeleton,
  children,
}: AsyncStateProps) {
  if (isLoading) {
    return <>{skeleton ?? <TableSkeleton />}</>;
  }

  if (isError) {
    return (
      <div className="card state-message error">
        <span className="state-icon">
          <AlertTriangle size={20} />
        </span>
        <span className="state-title">Couldn't load this</span>
        <span>{errorMessage}</span>
      </div>
    );
  }

  if (isEmpty) {
    return (
      <motion.div
        initial={{ opacity: 0, y: 6 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.25 }}
        className="card state-message"
      >
        <span className="state-icon">
          <Inbox size={20} />
        </span>
        <span className="state-title">{emptyTitle}</span>
        <span>{emptyMessage}</span>
      </motion.div>
    );
  }

  return <>{children}</>;
}
