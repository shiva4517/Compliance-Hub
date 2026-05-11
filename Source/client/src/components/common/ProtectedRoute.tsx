import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import type { UserRole } from '../../types';

interface Props {
  allowedRoles?: UserRole[];
}

function defaultHomeForRole(role: UserRole): string {
  switch (role) {
    case 'SuperAdmin': return '/dashboard';
    case 'Admin':      return '/dashboard';
    case 'Employee':   return '/employee-dashboard';
    case 'Customer':   return '/customer-details';
  }
}

export default function ProtectedRoute({ allowedRoles }: Props) {
  const { user, isLoading } = useAuth();
  const location = useLocation();

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-800" />
      </div>
    );
  }

  if (!user) return <Navigate to="/login" state={{ from: location }} replace />;

  if (user.isForcePasswordChange && location.pathname !== '/change-password') {
    return <Navigate to="/change-password" replace />;
  }

  if (allowedRoles && !allowedRoles.includes(user.role)) {
    // Redirect to the user's appropriate home, not always /dashboard
    return <Navigate to={defaultHomeForRole(user.role)} replace />;
  }

  return <Outlet />;
}
