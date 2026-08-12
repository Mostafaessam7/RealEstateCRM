import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useContractedDeals } from "./commissionsApi";
import { formatCurrency } from "../../utils/format";

export const commissionSchema = z.object({
  dealId: z.string().min(1, "Deal is required"),
  commissionPercentage: z
    .union([z.string(), z.number()])
    .transform(Number)
    .refine((v) => v > 0 && v <= 100, "Must be between 0 and 100"),
  companyCommissionPercentage: z
    .union([z.string(), z.number()])
    .transform(Number)
    .refine((v) => v >= 0 && v <= 100, "Must be between 0 and 100"),
});

export type CommissionFormValues = z.infer<typeof commissionSchema>;

interface CommissionFormProps {
  onSubmit: (values: CommissionFormValues) => Promise<void>;
}

export function CommissionForm({ onSubmit }: CommissionFormProps) {
  const { data: deals } = useContractedDeals();

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<CommissionFormValues>({ resolver: zodResolver(commissionSchema as never) });

  return (
    <form onSubmit={handleSubmit(onSubmit)} noValidate>
      <div className="field">
        <label htmlFor="dealId">Deal (Contracted only)</label>
        <select id="dealId" className="input" {...register("dealId")}>
          <option value="">Select a deal…</option>
          {deals?.map((deal) => (
            <option key={deal.id} value={deal.id}>
              ${formatCurrency(deal.dealValue)} — {deal.id.slice(0, 8)}
            </option>
          ))}
        </select>
        {errors.dealId && <span className="field-error">{errors.dealId.message}</span>}
      </div>

      <div className="field">
        <label htmlFor="commissionPercentage">Agent commission %</label>
        <input
          id="commissionPercentage"
          className="input"
          type="number"
          min={0}
          max={100}
          step={0.1}
          {...register("commissionPercentage")}
        />
        {errors.commissionPercentage && <span className="field-error">{errors.commissionPercentage.message}</span>}
      </div>

      <div className="field">
        <label htmlFor="companyCommissionPercentage">Company commission %</label>
        <input
          id="companyCommissionPercentage"
          className="input"
          type="number"
          min={0}
          max={100}
          step={0.1}
          {...register("companyCommissionPercentage")}
        />
        {errors.companyCommissionPercentage && (
          <span className="field-error">{errors.companyCommissionPercentage.message}</span>
        )}
      </div>

      <button type="submit" className="btn btn-primary" disabled={isSubmitting}>
        {isSubmitting ? "Creating…" : "Create Commission"}
      </button>
    </form>
  );
}
