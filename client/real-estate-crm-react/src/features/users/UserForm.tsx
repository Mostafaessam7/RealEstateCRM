import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Roles } from "../../types/auth";

export const userSchema = z.object({
  fullName: z.string().min(1, "Full name is required").max(200),
  email: z.string().min(1, "Email is required").email("Enter a valid email"),
  password: z.string().min(8, "Password must be at least 8 characters"),
  role: z.enum(Object.values(Roles) as [string, ...string[]]),
});

export type UserFormValues = z.infer<typeof userSchema>;

interface UserFormProps {
  onSubmit: (values: UserFormValues) => Promise<void>;
}

export function UserForm({ onSubmit }: UserFormProps) {
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<UserFormValues>({ resolver: zodResolver(userSchema), defaultValues: { role: Roles.SalesAgent } });

  return (
    <form onSubmit={handleSubmit(onSubmit)} noValidate>
      <div className="field">
        <label htmlFor="fullName">Full name</label>
        <input id="fullName" className="input" {...register("fullName")} />
        {errors.fullName && <span className="field-error">{errors.fullName.message}</span>}
      </div>
      <div className="field">
        <label htmlFor="email">Email</label>
        <input id="email" className="input" type="email" {...register("email")} />
        {errors.email && <span className="field-error">{errors.email.message}</span>}
      </div>
      <div className="field">
        <label htmlFor="password">Temporary password</label>
        <input id="password" className="input" type="password" {...register("password")} />
        {errors.password && <span className="field-error">{errors.password.message}</span>}
      </div>
      <div className="field">
        <label htmlFor="role">Role</label>
        <select id="role" className="input" {...register("role")}>
          {Object.values(Roles).map((role) => (
            <option key={role} value={role}>
              {role}
            </option>
          ))}
        </select>
      </div>
      <button type="submit" className="btn btn-primary" disabled={isSubmitting}>
        {isSubmitting ? "Creating…" : "Create User"}
      </button>
    </form>
  );
}
