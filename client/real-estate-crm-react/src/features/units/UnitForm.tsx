import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { UnitStatus } from "../../types/unit";
import { useAllProjects } from "../projects/projectsApi";

const optionalNumber = z
  .union([z.string(), z.number()])
  .transform((v) => (v === "" || v === undefined ? undefined : Number(v)))
  .refine((v) => v === undefined || !Number.isNaN(v), "Must be a number")
  .optional();

export const unitSchema = z.object({
  projectId: z.string().min(1, "Project is required"),
  unitCode: z.string().min(1, "Unit code is required").max(50),
  propertyType: z.string().max(100).optional().or(z.literal("")),
  price: z.union([z.string(), z.number()]).transform(Number).refine((v) => v > 0, "Price must be greater than 0"),
  area: optionalNumber,
  bedrooms: optionalNumber,
  bathrooms: optionalNumber,
  floor: z.string().max(30).optional().or(z.literal("")),
  location: z.string().max(200).optional().or(z.literal("")),
  status: z.enum(Object.values(UnitStatus) as [string, ...string[]]),
  downPayment: optionalNumber,
  installmentYears: optionalNumber,
  description: z.string().max(2000).optional().or(z.literal("")),
  isPubliclyListed: z.boolean().optional(),
});

export type UnitFormValues = z.infer<typeof unitSchema>;

interface UnitFormProps {
  defaultValues?: Partial<UnitFormValues>;
  onSubmit: (values: UnitFormValues) => Promise<void>;
  submitLabel?: string;
}

export function UnitForm({ defaultValues, onSubmit, submitLabel = "Save" }: UnitFormProps) {
  const { data: projects } = useAllProjects();

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<UnitFormValues>({
    resolver: zodResolver(unitSchema as never),
    defaultValues: { status: UnitStatus.Available, ...defaultValues },
  });

  return (
    <form onSubmit={handleSubmit(onSubmit)} noValidate>
      <div className="field">
        <label htmlFor="projectId">Project</label>
        <select id="projectId" className="input" {...register("projectId")}>
          <option value="">Select a project…</option>
          {projects?.map((project) => (
            <option key={project.id} value={project.id}>
              {project.name}
            </option>
          ))}
        </select>
        {errors.projectId && <span className="field-error">{errors.projectId.message}</span>}
      </div>

      <div className="field">
        <label htmlFor="unitCode">Unit code</label>
        <input id="unitCode" className="input" {...register("unitCode")} />
        {errors.unitCode && <span className="field-error">{errors.unitCode.message}</span>}
      </div>

      <div className="field">
        <label htmlFor="price">Price</label>
        <input id="price" className="input" type="number" min={0} {...register("price")} />
        {errors.price && <span className="field-error">{errors.price.message}</span>}
      </div>

      <div className="field">
        <label htmlFor="propertyType">Property type</label>
        <input id="propertyType" className="input" {...register("propertyType")} />
      </div>

      <div className="field">
        <label htmlFor="area">Area (sqm)</label>
        <input id="area" className="input" type="number" min={0} {...register("area")} />
      </div>

      <div className="field">
        <label htmlFor="bedrooms">Bedrooms</label>
        <input id="bedrooms" className="input" type="number" min={0} {...register("bedrooms")} />
      </div>

      <div className="field">
        <label htmlFor="bathrooms">Bathrooms</label>
        <input id="bathrooms" className="input" type="number" min={0} {...register("bathrooms")} />
      </div>

      <div className="field">
        <label htmlFor="floor">Floor</label>
        <input id="floor" className="input" {...register("floor")} />
      </div>

      <div className="field">
        <label htmlFor="location">Location</label>
        <input id="location" className="input" {...register("location")} />
      </div>

      <div className="field">
        <label htmlFor="status">Status</label>
        <select id="status" className="input" {...register("status")}>
          {Object.values(UnitStatus).map((status) => (
            <option key={status} value={status}>
              {status}
            </option>
          ))}
        </select>
      </div>

      <div className="field">
        <label htmlFor="downPayment">Down payment</label>
        <input id="downPayment" className="input" type="number" min={0} {...register("downPayment")} />
      </div>

      <div className="field">
        <label htmlFor="installmentYears">Installment years</label>
        <input id="installmentYears" className="input" type="number" min={0} {...register("installmentYears")} />
      </div>

      <div className="field">
        <label htmlFor="description">Description</label>
        <textarea id="description" className="input" rows={3} {...register("description")} />
      </div>

      <label style={{ display: "flex", alignItems: "center", gap: 8, fontSize: 13, marginBottom: 16 }}>
        <input type="checkbox" {...register("isPubliclyListed")} />
        List on the public marketplace
      </label>

      <button type="submit" className="btn btn-primary" disabled={isSubmitting}>
        {isSubmitting ? "Saving…" : submitLabel}
      </button>
    </form>
  );
}
