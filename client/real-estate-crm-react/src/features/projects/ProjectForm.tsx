import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { ProjectStatus } from "../../types/project";

const optionalNumber = z
  .union([z.string(), z.number()])
  .transform((v) => (v === "" || v === undefined ? undefined : Number(v)))
  .refine((v) => v === undefined || (!Number.isNaN(v) && v >= 0), "Must be a non-negative number")
  .optional();

export const projectSchema = z.object({
  name: z.string().min(1, "Name is required").max(200),
  developer: z.string().max(200).optional().or(z.literal("")),
  location: z.string().max(200).optional().or(z.literal("")),
  description: z.string().max(2000).optional().or(z.literal("")),
  startingPrice: optionalNumber,
  status: z.enum(Object.values(ProjectStatus) as [string, ...string[]]),
});

export type ProjectFormValues = z.infer<typeof projectSchema>;

interface ProjectFormProps {
  defaultValues?: Partial<ProjectFormValues>;
  onSubmit: (values: ProjectFormValues) => Promise<void>;
  submitLabel?: string;
}

export function ProjectForm({ defaultValues, onSubmit, submitLabel = "Save" }: ProjectFormProps) {
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<ProjectFormValues>({
    resolver: zodResolver(projectSchema as never),
    defaultValues: { status: ProjectStatus.Planning, ...defaultValues },
  });

  return (
    <form onSubmit={handleSubmit(onSubmit)} noValidate>
      <div className="field">
        <label htmlFor="name">Name</label>
        <input id="name" className="input" {...register("name")} />
        {errors.name && <span className="field-error">{errors.name.message}</span>}
      </div>
      <div className="field">
        <label htmlFor="developer">Developer</label>
        <input id="developer" className="input" {...register("developer")} />
      </div>
      <div className="field">
        <label htmlFor="location">Location</label>
        <input id="location" className="input" {...register("location")} />
      </div>
      <div className="field">
        <label htmlFor="startingPrice">Starting price</label>
        <input id="startingPrice" className="input" type="number" min={0} {...register("startingPrice")} />
      </div>
      <div className="field">
        <label htmlFor="status">Status</label>
        <select id="status" className="input" {...register("status")}>
          {Object.values(ProjectStatus).map((status) => (
            <option key={status} value={status}>
              {status}
            </option>
          ))}
        </select>
      </div>
      <div className="field">
        <label htmlFor="description">Description</label>
        <textarea id="description" className="input" rows={3} {...register("description")} />
      </div>
      <button type="submit" className="btn btn-primary" disabled={isSubmitting}>
        {isSubmitting ? "Saving…" : submitLabel}
      </button>
    </form>
  );
}
