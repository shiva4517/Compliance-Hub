import { BrowserRouter, Routes, Route, Navigate, useLocation } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { useEffect } from 'react';
import { AuthProvider } from './contexts/AuthContext';
import ProtectedRoute from './components/common/ProtectedRoute';
import AppLayout from './components/layout/AppLayout';
import { getPageTitle } from './utils/pageMeta';

import LoginPage from './pages/auth/LoginPage';
import ChangePasswordPage from './pages/auth/ChangePasswordPage';
import DashboardPage from './pages/dashboard/DashboardPage';
import PlaceholderPage from './pages/PlaceholderPage';

// SuperAdmin
import CompaniesPage from './pages/companies/CompaniesPage';
import GroupsPage from './pages/groups/GroupsPage';
import UsersPage from './pages/users/UsersPage';
import SyncRegulationsPage from './pages/syncRegulations/SyncRegulationsPage';
import SchedulerPage from './pages/scheduler/SchedulerPage';

// Admin
import CustomersPage from './pages/customers/CustomersPage';
import CustomerViewPage from './pages/customers/CustomerViewPage';
import EmployeesPage from './pages/employees/EmployeesPage';
import EmployeeViewPage from './pages/employees/EmployeeViewPage';
import CompanyDivisionsPage from './pages/companyDivisions/CompanyDivisionsPage';
import DepartmentsPage from './pages/departments/DepartmentsPage';
import DistrictsPage from './pages/districts/DistrictsPage';
import AiProviderSettingsPage from './pages/aiProvider/AiProviderSettingsPage';
import NotificationsPage from './pages/notifications/NotificationsPage';
import NotificationDetailPage from './pages/notifications/NotificationDetailPage';

// Customer
import CustomerDetailsPage from './pages/customerDetails/CustomerDetailsPage';

// Employee
import MyProfilePage from './pages/myProfile/MyProfilePage';
import RegulatoryAssignmentsPage from './pages/regulatoryAssignments/RegulatoryAssignmentsPage';
import RegulatoryAssignmentDetailPage from './pages/regulatoryAssignments/RegulatoryAssignmentDetailPage';

// Shared across multiple roles (behaviour differs inside the component)
import RegulationsPage from './pages/regulations/RegulationsPage';
import SubscriptionsPage from './pages/subscriptions/SubscriptionsPage';
import ChangeNoticesPage from './pages/changeNotices/ChangeNoticesPage';
import ChangeNoticeDetailPage from './pages/changeNotices/ChangeNoticeDetailPage';
import GrievancesPage from './pages/grievances/GrievancesPage';
import AgentPage from './pages/agent/AgentPage';
import AdminAgentPage from './pages/agent/AdminAgentPage';

const queryClient = new QueryClient({
  defaultOptions: { queries: { staleTime: 30_000, retry: 1 } },
});

function DocumentMeta() {
  const location = useLocation();

  useEffect(() => {
    document.title = getPageTitle(location.pathname);
  }, [location.pathname]);

  return null;
}

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <BrowserRouter>
          <DocumentMeta />
          <Routes>
            {/* ── Public ──────────────────────────────────────────────────── */}
            <Route path="/login" element={<LoginPage />} />
            <Route path="/" element={<Navigate to="/dashboard" replace />} />

            {/* Force password change — all authenticated users */}
            <Route element={<ProtectedRoute />}>
              <Route path="/change-password" element={<ChangePasswordPage />} />
            </Route>

            {/* ── Authenticated shell ──────────────────────────────────────── */}
            <Route element={<ProtectedRoute />}>
              <Route element={<AppLayout />}>

                {/* Dashboard — SuperAdmin, Admin see this; Employee goes to /employee-dashboard; Customer to /customer-details */}
                <Route element={<ProtectedRoute allowedRoles={['SuperAdmin', 'Admin']} />}>
                  <Route path="/dashboard" element={<DashboardPage />} />
                </Route>

                {/* ── SuperAdmin only ──────────────────────────────────────── */}
                <Route element={<ProtectedRoute allowedRoles={['SuperAdmin']} />}>
                  <Route path="/companies" element={<CompaniesPage />} />
                  <Route path="/groups" element={<GroupsPage />} />
                  <Route path="/users" element={<UsersPage />} />
                  <Route path="/sync-regulations" element={<SyncRegulationsPage />} />
                  <Route path="/scheduler" element={<SchedulerPage />} />
                  <Route path="/agent" element={<AgentPage />} />
                </Route>

                {/* ── Admin only ───────────────────────────────────────────── */}
                <Route element={<ProtectedRoute allowedRoles={['Admin']} />}>
                  <Route path="/admin-agent" element={<AdminAgentPage />} />
                  <Route path="/customers" element={<CustomersPage />} />
                  <Route path="/customers/:id" element={<CustomerViewPage />} />
                  <Route path="/employees" element={<EmployeesPage />} />
                  <Route path="/employees/:id" element={<EmployeeViewPage />} />
                  <Route path="/departments" element={<DepartmentsPage />} />
                  <Route path="/company-divisions" element={<CompanyDivisionsPage />} />
                  <Route path="/districts" element={<DistrictsPage />} />
                  <Route path="/notifications" element={<NotificationsPage />} />
                  <Route path="/notifications/:id" element={<NotificationDetailPage />} />
                  <Route path="/reports" element={<PlaceholderPage title="Reports" />} />
                </Route>

                {/* ── AI Provider Settings — Admin + SuperAdmin ─────────────── */}
                <Route element={<ProtectedRoute allowedRoles={['SuperAdmin', 'Admin']} />}>
                  <Route path="/ai-provider-settings" element={<AiProviderSettingsPage />} />
                </Route>

                {/* ── Customer only ────────────────────────────────────────── */}
                <Route element={<ProtectedRoute allowedRoles={['Customer']} />}>
                  <Route path="/customer-details" element={<CustomerDetailsPage />} />
                </Route>

                {/* ── Employee only ────────────────────────────────────────── */}
                <Route element={<ProtectedRoute allowedRoles={['Employee']} />}>
                  <Route path="/employee-dashboard" element={<MyProfilePage />} />
                  <Route path="/regulatory-assignments" element={<RegulatoryAssignmentsPage />} />
                  <Route path="/regulatory-assignments/:id" element={<RegulatoryAssignmentDetailPage />} />
                </Route>

                {/* ── Shared: SuperAdmin + Admin see all regulations ─────────  */}
                <Route element={<ProtectedRoute allowedRoles={['SuperAdmin', 'Admin', 'Employee','Customer']} />}>
                  <Route path="/regulations" element={<RegulationsPage />} />
                </Route>

                {/* ── Shared: Admin + Customer see subscriptions ────────────── */}
                <Route element={<ProtectedRoute allowedRoles={['Admin', 'Customer']} />}>
                  <Route path="/subscriptions" element={<SubscriptionsPage />} />
                </Route>

                {/* ── Shared: Admin + Customer + Employee see change notices ── */}
                <Route element={<ProtectedRoute allowedRoles={['Admin', 'Customer', 'Employee']} />}>
                  <Route path="/change-notices" element={<ChangeNoticesPage />} />
                  <Route path="/change-notices/:id" element={<ChangeNoticeDetailPage />} />
                </Route>

                {/* ── Shared: All roles see grievances (behaviour differs) ───── */}
                <Route element={<ProtectedRoute allowedRoles={['SuperAdmin', 'Admin', 'Customer', 'Employee']} />}>
                  <Route path="/grievances" element={<GrievancesPage />} />
                </Route>

              </Route>
            </Route>

            {/* Fallback */}
            <Route path="*" element={<Navigate to="/dashboard" replace />} />
          </Routes>
        </BrowserRouter>
      </AuthProvider>
    </QueryClientProvider>
  );
}
