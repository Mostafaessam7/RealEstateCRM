import { lazy, Suspense } from "react";
import { Navigate, Route, Routes } from "react-router-dom";
import { MainLayout } from "../layouts/MainLayout";
import { ProtectedRoute } from "./ProtectedRoute";
import { RoleRoute } from "./RoleRoute";
import { LoginPage } from "../features/auth/LoginPage";
import { PlaceholderPage } from "../pages/PlaceholderPage";
import { Roles } from "../types/auth";

// Route-level code splitting: every page below was previously a static import, so the very
// first load shipped one ~750 kB JS bundle containing every feature (billing, marketing,
// developer tools, reports, etc.) whether the user ever visits them or not. Lazy-loading means
// a user only downloads the page they're actually navigating to; React Router only renders one
// route element at a time, so Suspense's fallback is only ever visible for the split chunk
// currently loading, not a full-page flash on every navigation.
const DashboardPage = lazy(() => import("../features/dashboard/DashboardPage").then((m) => ({ default: m.DashboardPage })));
const LeadsListPage = lazy(() => import("../features/leads/LeadsListPage").then((m) => ({ default: m.LeadsListPage })));
const LeadDetailsPage = lazy(() => import("../features/leads/LeadDetailsPage").then((m) => ({ default: m.LeadDetailsPage })));
const PipelinePage = lazy(() => import("../features/leads/PipelinePage").then((m) => ({ default: m.PipelinePage })));
const ProjectsListPage = lazy(() => import("../features/projects/ProjectsListPage").then((m) => ({ default: m.ProjectsListPage })));
const UnitsListPage = lazy(() => import("../features/units/UnitsListPage").then((m) => ({ default: m.UnitsListPage })));
const UnitDetailsPage = lazy(() => import("../features/units/UnitDetailsPage").then((m) => ({ default: m.UnitDetailsPage })));
const DealsListPage = lazy(() => import("../features/deals/DealsListPage").then((m) => ({ default: m.DealsListPage })));
const TasksListPage = lazy(() => import("../features/tasks/TasksListPage").then((m) => ({ default: m.TasksListPage })));
const CommissionsListPage = lazy(() => import("../features/commissions/CommissionsListPage").then((m) => ({ default: m.CommissionsListPage })));
const ReportsPage = lazy(() => import("../features/reports/ReportsPage").then((m) => ({ default: m.ReportsPage })));
const UsersListPage = lazy(() => import("../features/users/UsersListPage").then((m) => ({ default: m.UsersListPage })));
const CompanySettingsPage = lazy(() => import("../features/companies/CompanySettingsPage").then((m) => ({ default: m.CompanySettingsPage })));
const BillingPage = lazy(() => import("../features/billing/BillingPage").then((m) => ({ default: m.BillingPage })));
const WhatsAppTemplatesPage = lazy(() => import("../features/whatsapp/WhatsAppTemplatesPage").then((m) => ({ default: m.WhatsAppTemplatesPage })));
const CampaignsPage = lazy(() => import("../features/marketing/CampaignsPage").then((m) => ({ default: m.CampaignsPage })));
const ApiKeysPage = lazy(() => import("../features/developer/ApiKeysPage").then((m) => ({ default: m.ApiKeysPage })));
const WebhooksPage = lazy(() => import("../features/developer/WebhooksPage").then((m) => ({ default: m.WebhooksPage })));
const MarketplacePage = lazy(() => import("../features/marketplace/MarketplacePage").then((m) => ({ default: m.MarketplacePage })));

function RouteFallback() {
  return <div className="card state-message" style={{ margin: 24 }} aria-live="polite">Loading…</div>;
}

export function AppRoutes() {
  return (
    <Suspense fallback={<RouteFallback />}>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/marketplace" element={<MarketplacePage />} />

        <Route element={<ProtectedRoute />}>
          <Route element={<MainLayout />}>
            <Route index element={<Navigate to="/dashboard" replace />} />
            <Route path="/dashboard" element={<DashboardPage />} />

            <Route path="/leads" element={<LeadsListPage />} />
            <Route path="/leads/:id" element={<LeadDetailsPage />} />
            <Route path="/pipeline" element={<PipelinePage />} />

            <Route path="/projects" element={<ProjectsListPage />} />
            <Route path="/units" element={<UnitsListPage />} />
            <Route path="/units/:id" element={<UnitDetailsPage />} />
            <Route path="/deals" element={<DealsListPage />} />
            <Route path="/tasks" element={<TasksListPage />} />

            <Route element={<RoleRoute roles={[Roles.CompanyAdmin, Roles.SalesManager, Roles.SuperAdmin]} />}>
              <Route path="/commissions" element={<CommissionsListPage />} />
            </Route>

            <Route path="/reports" element={<ReportsPage />} />

            <Route element={<RoleRoute roles={[Roles.CompanyAdmin, Roles.SalesManager, Roles.SuperAdmin]} />}>
              <Route path="/whatsapp-templates" element={<WhatsAppTemplatesPage />} />
              <Route path="/marketing-campaigns" element={<CampaignsPage />} />
            </Route>

            <Route element={<RoleRoute roles={[Roles.CompanyAdmin, Roles.SuperAdmin]} />}>
              <Route path="/users" element={<UsersListPage />} />
              <Route path="/company-settings" element={<CompanySettingsPage />} />
              <Route path="/billing" element={<BillingPage />} />
              <Route path="/api-keys" element={<ApiKeysPage />} />
              <Route path="/webhooks" element={<WebhooksPage />} />
            </Route>

            <Route path="*" element={<PlaceholderPage title="Not found" />} />
          </Route>
        </Route>
      </Routes>
    </Suspense>
  );
}
