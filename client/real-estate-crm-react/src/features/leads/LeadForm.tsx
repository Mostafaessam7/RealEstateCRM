import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { LeadSource, LeadStatus } from "../../types/lead";

const optionalNumber = z
  .union([z.string(), z.number()])
  .transform((v) => (v === "" || v === undefined ? undefined : Number(v)))
  .refine((v) => v === undefined || (!Number.isNaN(v) && v >= 0), "Must be a non-negative number")
  .optional();

const baseSchema = {
  fullName: z.string().min(1, "Full name is required").max(200),
  phone: z.string().max(30).optional().or(z.literal("")),
  email: z.string().email("Enter a valid email").optional().or(z.literal("")),
  source: z.enum(Object.values(LeadSource) as [string, ...string[]]),
  budgetMin: optionalNumber,
  budgetMax: optionalNumber,
  preferredLocation: z.string().max(200).optional().or(z.literal("")),
  propertyType: z.string().max(100).optional().or(z.literal("")),
  notes: z.string().max(2000).optional().or(z.literal("")),
};

export const createLeadSchema = z.object(baseSchema);
export const updateLeadSchema = z.object({
  ...baseSchema,
  status: z.enum(Object.values(LeadStatus) as [string, ...string[]]),
});

export type LeadFormValues = z.infer<typeof updateLeadSchema>;

interface LeadFormProps {
  defaultValues?: Partial<LeadFormValues>;
  includeStatus?: boolean;
  onSubmit: (values: LeadFormValues) => Promise<void>;
  submitLabel?: string;
}

export function LeadForm({ defaultValues, includeStatus = false, onSubmit, submitLabel = "Save" }: LeadFormProps) {
  const schema = includeStatus ? updateLeadSchema : createLeadSchema;

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LeadFormValues>({
    resolver: zodResolver(schema as never),
    defaultValues: { source: LeadSource.Website, status: LeadStatus.New, ...defaultValues },
  });

  return (
    <form onSubmit={handleSubmit(onSubmit)} noValidate>
      <div className="field">
        <label htmlFor="fullName">Full name</label>
        <input id="fullName" className="input" {...register("fullName")} />
        {errors.fullName && <span className="field-error">{errors.fullName.message}</span>}
      </div>

      <div className="field">
        <label htmlFor="phone">Phone</label>
        <input id="phone" className="input" {...register("phone")} />
      </div>

      <div className="field">
        <label htmlFor="email">Email</label>
        <input id="email" className="input" type="email" {...register("email")} />
        {errors.email && <span className="field-error">{errors.email.message}</span>}
      </div>

      <div className="field">
        <label htmlFor="source">Source</label>
        <select id="source" className="input" {...register("source")}>
          {Object.values(LeadSource).map((source) => (
            <option key={source} value={source}>
              {source}
            </option>
          ))}
        </select>
      </div>

      {includeStatus && (
        <div className="field">
          <label htmlFor="status">Status</label>
          <select id="status" className="input" {...register("status")}>
            {Object.values(LeadStatus).map((status) => (
              <option key={status} value={status}>
                {status}
              </option>
            ))}
          </select>
        </div>
      )}

      <div className="field">
        <label htmlFor="budgetMin">Budget min</label>
        <input id="budgetMin" className="input" type="number" min={0} {...register("budgetMin")} />
      </div>

      <div className="field">
        <label htmlFor="budgetMax">Budget max</label>
        <input id="budgetMax" className="input" type="number" min={0} {...register("budgetMax")} />
      </div>

      <div className="field">
        <label htmlFor="preferredLocation">Preferred location</label>
        <input id="preferredLocation" className="input" {...register("preferredLocation")} />
      </div>

      <div className="field">
        <label htmlFor="propertyType">Property type</label>
        <input id="propertyType" className="input" {...register("propertyType")} />
      </div>

      <div className="field">
        <label htmlFor="notes">Notes</label>
        <textarea id="notes" className="input" rows={3} {...register("notes")} />
      </div>

      <button type="submit" className="btn btn-primary" disabled={isSubmitting}>
        {isSubmitting ? "Saving…" : submitLabel}
      </button>
    </form>
  );
}
