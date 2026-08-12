import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useQuery } from "@tanstack/react-query";
import { apiClient } from "../../api/client";
import type { PagedResult } from "../../types/common";
import type { Lead } from "../../types/lead";
import type { Unit } from "../../types/unit";

export const dealSchema = z.object({
  leadId: z.string().min(1, "Lead is required"),
  unitId: z.string().min(1, "Unit is required"),
  dealValue: z.union([z.string(), z.number()]).transform(Number).refine((v) => v > 0, "Deal value must be greater than 0"),
  notes: z.string().max(2000).optional().or(z.literal("")),
});

export type DealFormValues = z.infer<typeof dealSchema>;

interface DealFormProps {
  onSubmit: (values: DealFormValues) => Promise<void>;
}

export function DealForm({ onSubmit }: DealFormProps) {
  const { data: leads } = useQuery({
    queryKey: ["leads", "all"],
    queryFn: async () => {
      const response = await apiClient.get<PagedResult<Lead>>("/leads", { params: { page: 1, pageSize: 100 } });
      return response.data.items;
    },
  });

  const { data: units } = useQuery({
    queryKey: ["units", "available", "all"],
    queryFn: async () => {
      const response = await apiClient.get<Unit[]>("/units/available");
      return response.data;
    },
  });

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<DealFormValues>({ resolver: zodResolver(dealSchema as never) });

  return (
    <form onSubmit={handleSubmit(onSubmit)} noValidate>
      <div className="field">
        <label htmlFor="leadId">Lead</label>
        <select id="leadId" className="input" {...register("leadId")}>
          <option value="">Select a lead…</option>
          {leads?.map((lead) => (
            <option key={lead.id} value={lead.id}>
              {lead.fullName}
            </option>
          ))}
        </select>
        {errors.leadId && <span className="field-error">{errors.leadId.message}</span>}
      </div>

      <div className="field">
        <label htmlFor="unitId">Unit (available only)</label>
        <select id="unitId" className="input" {...register("unitId")}>
          <option value="">Select a unit…</option>
          {units?.map((unit) => (
            <option key={unit.id} value={unit.id}>
              {unit.unitCode} — {unit.price}
            </option>
          ))}
        </select>
        {errors.unitId && <span className="field-error">{errors.unitId.message}</span>}
      </div>

      <div className="field">
        <label htmlFor="dealValue">Deal value</label>
        <input id="dealValue" className="input" type="number" min={0} {...register("dealValue")} />
        {errors.dealValue && <span className="field-error">{errors.dealValue.message}</span>}
      </div>

      <div className="field">
        <label htmlFor="notes">Notes</label>
        <textarea id="notes" className="input" rows={3} {...register("notes")} />
      </div>

      <button type="submit" className="btn btn-primary" disabled={isSubmitting}>
        {isSubmitting ? "Creating…" : "Create Deal"}
      </button>
    </form>
  );
}
