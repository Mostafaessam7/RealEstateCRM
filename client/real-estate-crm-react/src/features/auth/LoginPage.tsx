import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useNavigate, useLocation } from "react-router-dom";
import { Building, Lock, Mail, ArrowRight, LayoutDashboard, Users, TrendingUp } from "lucide-react";
import { useAuth } from "./AuthContext";
import { getApiErrorMessage } from "../../api/client";

const loginSchema = z.object({
  email: z.string().min(1, "Email is required").email("Enter a valid email"),
  password: z.string().min(1, "Password is required"),
});

type LoginFormValues = z.infer<typeof loginSchema>;

const highlights = [
  { icon: Users, text: "Track every lead from first contact to close" },
  { icon: LayoutDashboard, text: "One dashboard for leads, units, and deals" },
  { icon: TrendingUp, text: "Clear reporting on pipeline and performance" },
];

export function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [serverError, setServerError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginFormValues>({ resolver: zodResolver(loginSchema) });

  const from = (location.state as { from?: string } | null)?.from ?? "/dashboard";

  const onSubmit = async (values: LoginFormValues) => {
    setServerError(null);
    try {
      await login(values);
      navigate(from, { replace: true });
    } catch (error) {
      setServerError(getApiErrorMessage(error, "Invalid email or password."));
    }
  };

  return (
    <div className="auth-page">
      <div className="auth-visual">
        <div className="brand">
          <span className="brand-mark">
            <Building size={19} color="#fff" strokeWidth={2.2} />
          </span>
          <span>Real Estate CRM</span>
        </div>
        <div style={{ position: "relative", zIndex: 1, marginTop: "auto" }}>
          <h2 style={{ fontFamily: "var(--font-display)", fontSize: 28, lineHeight: 1.25, maxWidth: 380 }}>
            Manage leads, deals and units in one modern workspace.
          </h2>
          <p style={{ color: "rgba(255,255,255,0.7)", fontSize: 14, maxWidth: 360, marginTop: 10 }}>
            Built for real estate teams that need clarity across every stage of the pipeline.
          </p>
          <ul style={{ listStyle: "none", padding: 0, marginTop: 28, display: "grid", gap: 14 }}>
            {highlights.map(({ icon: Icon, text }) => (
              <li key={text} style={{ display: "flex", alignItems: "center", gap: 10, color: "rgba(255,255,255,0.85)", fontSize: 13.5 }}>
                <span
                  style={{
                    width: 28,
                    height: 28,
                    borderRadius: 8,
                    display: "grid",
                    placeItems: "center",
                    background: "rgba(255,255,255,0.08)",
                    flexShrink: 0,
                  }}
                >
                  <Icon size={14} />
                </span>
                {text}
              </li>
            ))}
          </ul>
        </div>
      </div>

      <div className="auth-form-side">
        <div className="card auth-card">
          <h1>Welcome back</h1>
          <p className="subtitle">Sign in to continue to your dashboard.</p>

          <form onSubmit={handleSubmit(onSubmit)} noValidate>
            <div className="field">
              <label htmlFor="email">Email</label>
              <div style={{ position: "relative" }}>
                <Mail size={15} color="var(--color-text-faint)" style={{ position: "absolute", left: 12, top: "50%", transform: "translateY(-50%)" }} />
                <input
                  id="email"
                  className="input"
                  type="email"
                  autoComplete="username"
                  style={{ paddingLeft: 36 }}
                  {...register("email")}
                />
              </div>
              {errors.email && <span className="field-error">{errors.email.message}</span>}
            </div>

            <div className="field">
              <label htmlFor="password">Password</label>
              <div style={{ position: "relative" }}>
                <Lock size={15} color="var(--color-text-faint)" style={{ position: "absolute", left: 12, top: "50%", transform: "translateY(-50%)" }} />
                <input
                  id="password"
                  className="input"
                  type="password"
                  autoComplete="current-password"
                  style={{ paddingLeft: 36 }}
                  {...register("password")}
                />
              </div>
              {errors.password && <span className="field-error">{errors.password.message}</span>}
            </div>

            {serverError && <p className="field-error">{serverError}</p>}

            <button
              type="submit"
              className="btn btn-primary"
              disabled={isSubmitting}
              style={{ width: "100%", display: "flex", alignItems: "center", justifyContent: "center", gap: 6 }}
            >
              {isSubmitting ? "Signing in…" : "Sign in"}
              {!isSubmitting && <ArrowRight size={15} />}
            </button>
          </form>
        </div>
      </div>
    </div>
  );
}
