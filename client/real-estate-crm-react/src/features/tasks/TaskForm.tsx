import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { TaskPriority } from "../../types/task";
import { useUsers } from "../users/usersApi";

export const taskSchema = z.object({
  title: z.string().min(1, "Title is required").max(200),
  description: z.string().max(2000).optional().or(z.literal("")),
  assignedToUserId: z.string().min(1, "Assignee is required"),
  dueAt: z.string().optional().or(z.literal("")),
  priority: z.enum(Object.values(TaskPriority) as [string, ...string[]]),
  reminderAt: z.string().optional().or(z.literal("")),
});

export type TaskFormValues = z.infer<typeof taskSchema>;

interface TaskFormProps {
  onSubmit: (values: TaskFormValues) => Promise<void>;
}

export function TaskForm({ onSubmit }: TaskFormProps) {
  const { data: users } = useUsers();

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<TaskFormValues>({ resolver: zodResolver(taskSchema), defaultValues: { priority: TaskPriority.Medium } });

  return (
    <form onSubmit={handleSubmit(onSubmit)} noValidate>
      <div className="field">
        <label htmlFor="title">Title</label>
        <input id="title" className="input" {...register("title")} />
        {errors.title && <span className="field-error">{errors.title.message}</span>}
      </div>

      <div className="field">
        <label htmlFor="assignedToUserId">Assigned to</label>
        <select id="assignedToUserId" className="input" {...register("assignedToUserId")}>
          <option value="">Select a user…</option>
          {users?.map((user) => (
            <option key={user.id} value={user.id}>
              {user.fullName}
            </option>
          ))}
        </select>
        {errors.assignedToUserId && <span className="field-error">{errors.assignedToUserId.message}</span>}
      </div>

      <div className="field">
        <label htmlFor="priority">Priority</label>
        <select id="priority" className="input" {...register("priority")}>
          {Object.values(TaskPriority).map((priority) => (
            <option key={priority} value={priority}>
              {priority}
            </option>
          ))}
        </select>
      </div>

      <div className="field">
        <label htmlFor="dueAt">Due date</label>
        <input id="dueAt" className="input" type="datetime-local" {...register("dueAt")} />
      </div>

      <div className="field">
        <label htmlFor="reminderAt">Reminder</label>
        <input id="reminderAt" className="input" type="datetime-local" {...register("reminderAt")} />
      </div>

      <div className="field">
        <label htmlFor="description">Description</label>
        <textarea id="description" className="input" rows={3} {...register("description")} />
      </div>

      <button type="submit" className="btn btn-primary" disabled={isSubmitting}>
        {isSubmitting ? "Creating…" : "Create Task"}
      </button>
    </form>
  );
}
