import { useState } from "react";
import { toast } from "sonner";
import { PageHeader } from "../../components/PageHeader";
import { AsyncState } from "../../components/AsyncState";
import { Pagination } from "../../components/Pagination";
import { Modal } from "../../components/Modal";
import { StatusBadge } from "../../components/StatusBadge";
import { TaskForm, type TaskFormValues } from "./TaskForm";
import { useCancelTask, useCompleteTask, useCreateTask, useTasks } from "./tasksApi";
import { TaskItemStatus, type TaskItemListQuery, type TaskPriority } from "../../types/task";
import { getApiErrorMessage } from "../../api/client";

export function TasksListPage() {
  const [query, setQuery] = useState<TaskItemListQuery>({ page: 1, pageSize: 20 });
  const [showCreate, setShowCreate] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const { data, isLoading, isError } = useTasks(query);
  const createTask = useCreateTask();
  const completeTask = useCompleteTask();
  const cancelTask = useCancelTask();

  const handleCreate = async (values: TaskFormValues) => {
    setError(null);
    try {
      await createTask.mutateAsync({
        title: values.title,
        description: values.description || null,
        assignedToUserId: values.assignedToUserId,
        dueAt: values.dueAt ? new Date(values.dueAt).toISOString() : null,
        priority: values.priority as TaskPriority,
        reminderAt: values.reminderAt ? new Date(values.reminderAt).toISOString() : null,
      });
      setShowCreate(false);
      toast.success("Task created");
    } catch (err) {
      setError(getApiErrorMessage(err, "Could not create task."));
    }
  };

  return (
    <>
      <PageHeader
        title="Tasks"
        actions={
          <button type="button" className="btn btn-primary" onClick={() => setShowCreate(true)}>
            + New Task
          </button>
        }
      />

      <div className="toolbar">
        <select
          className="input"
          style={{ maxWidth: 200 }}
          value={query.status ?? ""}
          onChange={(e) =>
            setQuery((q) => ({ ...q, status: (e.target.value || undefined) as TaskItemListQuery["status"], page: 1 }))
          }
        >
          <option value="">All statuses</option>
          {Object.values(TaskItemStatus).map((status) => (
            <option key={status} value={status}>
              {status}
            </option>
          ))}
        </select>
      </div>

      {error && <p className="field-error">{error}</p>}

      <AsyncState
        isLoading={isLoading}
        isError={isError}
        errorMessage="Failed to load tasks."
        isEmpty={!isLoading && (data?.items.length ?? 0) === 0}
        emptyMessage="No tasks yet."
      >
        <div className="card table-wrap">
          <table className="table">
            <thead>
              <tr>
                <th>Title</th>
                <th>Priority</th>
                <th>Due</th>
                <th>Status</th>
                <th className="sr-only">Actions</th>
              </tr>
            </thead>
            <tbody>
              {data?.items.map((task) => (
                <tr key={task.id}>
                  <td>{task.title}</td>
                  <td>{task.priority}</td>
                  <td>{task.dueAt ? new Date(task.dueAt).toLocaleString() : "—"}</td>
                  <td>
                    <StatusBadge status={task.status} />
                  </td>
                  <td>
                    {task.status === TaskItemStatus.Pending && (
                      <>
                        <button className="btn" onClick={() => completeTask.mutate(task.id)}>
                          Complete
                        </button>{" "}
                        <button className="btn btn-danger" onClick={() => cancelTask.mutate(task.id)}>
                          Cancel
                        </button>
                      </>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <Pagination
          page={data?.page ?? 1}
          totalPages={data?.totalPages ?? 1}
          onPageChange={(page) => setQuery((q) => ({ ...q, page }))}
        />
      </AsyncState>

      {showCreate && (
        <Modal title="New Task" onClose={() => setShowCreate(false)}>
          <TaskForm onSubmit={handleCreate} />
        </Modal>
      )}
    </>
  );
}
