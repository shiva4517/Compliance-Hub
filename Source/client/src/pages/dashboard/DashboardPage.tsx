import { useQuery } from '@tanstack/react-query';
import { dashboardService } from '../../services/dashboardService';
import { useAuth } from '../../contexts/AuthContext';
import {
  Users, UserCheck, BookOpen, Bell, Layers, MapPin, Building2, MessageSquare,
  FileText, RefreshCw, ShieldCheck,
} from 'lucide-react';
import { Link } from 'react-router-dom';
import { format } from 'date-fns';
import type { EntityCounts, GrievanceCounts } from '../../types';

function EntityCard({
  label,
  icon,
  counts,
  color,
}: {
  label: string;
  icon: React.ReactNode;
  counts: EntityCounts;
  color: string;
}) {
  return (
    <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-5">
      <div className="flex items-center justify-between mb-3">
        <p className="text-sm font-medium text-gray-500">{label}</p>
        <div className={`p-2 rounded-lg ${color}`}>{icon}</div>
      </div>
      <p className="text-3xl font-bold text-gray-900 mb-3">{counts.total}</p>
      <div className="flex items-center gap-4 text-xs">
        <span className="flex items-center gap-1.5 text-green-600">
          <span className="w-2 h-2 rounded-full bg-green-500 inline-block" />
          Active <span className="font-semibold">{counts.active}</span>
        </span>
        <span className="flex items-center gap-1.5 text-gray-400">
          <span className="w-2 h-2 rounded-full bg-gray-300 inline-block" />
          Inactive <span className="font-semibold">{counts.inactive}</span>
        </span>
      </div>
    </div>
  );
}

function GrievanceCard({ counts }: { counts: GrievanceCounts }) {
  return (
    <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-5">
      <div className="flex items-center justify-between mb-3">
        <div>
          <p className="text-sm font-medium text-gray-500">Grievances</p>
          <p className="text-xs text-gray-400 mt-0.5">Last 24 hours</p>
        </div>
        <div className="p-2 rounded-lg bg-amber-50">
          <MessageSquare size={18} className="text-amber-600" />
        </div>
      </div>
      <div className="grid grid-cols-3 divide-x divide-gray-100 mt-2">
        <div className="pr-3">
          <p className="text-2xl font-bold text-gray-900">{counts.received}</p>
          <p className="text-xs text-gray-500 mt-0.5">Received</p>
        </div>
        <div className="px-3">
          <p className="text-2xl font-bold text-green-600">{counts.replied}</p>
          <p className="text-xs text-gray-500 mt-0.5">Replied</p>
        </div>
        <div className="pl-3">
          <p className="text-2xl font-bold text-amber-500">{counts.pending}</p>
          <p className="text-xs text-gray-500 mt-0.5">Pending</p>
        </div>
      </div>
    </div>
  );
}

function SuperAdminDashboard({ fullName }: { fullName: string }) {
  const { data: stats, isLoading } = useQuery({
    queryKey: ['super-admin-stats'],
    queryFn: () => dashboardService.getSuperAdminStats(),
  });

  const empty: EntityCounts = { total: 0, active: 0, inactive: 0 };

  const quickLinks = [
    { label: 'Companies', desc: 'Manage registered companies.', to: '/companies', icon: <Building2 size={36} className="text-blue-600" /> },
    { label: 'Users', desc: 'Manage system users and roles.', to: '/users', icon: <Users size={36} className="text-blue-600" /> },
    { label: 'Groups', desc: 'Manage security groups.', to: '/groups', icon: <ShieldCheck size={36} className="text-blue-600" /> },
  ];

  return (
    <div>
      <h1 className="text-2xl font-bold text-gray-900">Dashboard</h1>
      <p className="text-gray-500 text-sm mt-1 mb-6">Welcome back, {fullName}.</p>

      {isLoading ? (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4 mb-6">
          {Array.from({ length: 3 }).map((_, i) => (
            <div key={i} className="bg-white rounded-xl border border-gray-200 shadow-sm p-5 animate-pulse">
              <div className="h-4 bg-gray-100 rounded w-1/2 mb-3" />
              <div className="h-8 bg-gray-100 rounded w-1/3 mb-3" />
              <div className="h-3 bg-gray-100 rounded w-2/3" />
            </div>
          ))}
        </div>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4 mb-6">
          <EntityCard
            label="Companies"
            icon={<Building2 size={18} className="text-blue-600" />}
            color="bg-blue-50"
            counts={stats?.companies ?? empty}
          />
          <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-5">
            <div className="flex items-center justify-between mb-3">
              <p className="text-sm font-medium text-gray-500">Users</p>
              <div className="p-2 rounded-lg bg-violet-50">
                <Users size={18} className="text-violet-600" />
              </div>
            </div>
            <p className="text-3xl font-bold text-gray-900 mb-3">{stats?.users.total ?? 0}</p>
            <div className="flex items-center gap-4 text-xs">
              <span className="flex items-center gap-1.5 text-purple-600">
                <span className="w-2 h-2 rounded-full bg-purple-500 inline-block" />
                SuperAdmin <span className="font-semibold">{stats?.users.superAdminCount ?? 0}</span>
              </span>
              <span className="flex items-center gap-1.5 text-blue-600">
                <span className="w-2 h-2 rounded-full bg-blue-400 inline-block" />
                Admin <span className="font-semibold">{stats?.users.adminCount ?? 0}</span>
              </span>
            </div>
          </div>
          <GrievanceCard counts={stats?.grievances ?? { received: 0, replied: 0, pending: 0 }} />
        </div>
      )}

      <h2 className="text-sm font-semibold text-gray-500 uppercase tracking-wider mb-3">Quick Access</h2>
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        {quickLinks.map(link => (
          <Link key={link.to} to={link.to} className="card hover:shadow-md transition-shadow">
            <h3 className="font-semibold text-gray-900 mb-1">{link.label}</h3>
            <p className="text-sm text-gray-500 mb-4">{link.desc}</p>
            {link.icon}
          </Link>
        ))}
      </div>

      <p className="text-xs text-gray-400 mt-6">Last updated: {format(new Date(), 'MMM d, yyyy · h:mm a')}</p>
    </div>
  );
}

function AdminDashboard({ fullName, companyId }: { fullName: string; companyId: string }) {
  const { data: stats, isLoading } = useQuery({
    queryKey: ['dashboard-stats', companyId],
    queryFn: () => dashboardService.getStats(companyId),
  });

  const empty: EntityCounts = { total: 0, active: 0, inactive: 0 };

  const entityCards = [
    {
      label: 'Customers',
      icon: <Users size={18} className="text-blue-600" />,
      color: 'bg-blue-50',
      counts: stats?.customers ?? empty,
    },
    {
      label: 'Employees',
      icon: <UserCheck size={18} className="text-violet-600" />,
      color: 'bg-violet-50',
      counts: stats?.employees ?? empty,
    },
    {
      label: 'Subscriptions',
      icon: <RefreshCw size={18} className="text-teal-600" />,
      color: 'bg-teal-50',
      counts: stats?.subscriptions ?? empty,
    },
    {
      label: 'Divisions',
      icon: <Layers size={18} className="text-orange-600" />,
      color: 'bg-orange-50',
      counts: stats?.divisions ?? empty,
    },
    {
      label: 'Districts',
      icon: <MapPin size={18} className="text-pink-600" />,
      color: 'bg-pink-50',
      counts: stats?.districts ?? empty,
    },
    {
      label: 'Departments',
      icon: <Building2 size={18} className="text-indigo-600" />,
      color: 'bg-indigo-50',
      counts: stats?.departments ?? empty,
    },
  ];

  const quickLinks = [
    { label: 'Customers', desc: 'Manage customer accounts and facilities.', to: '/customers', icon: <FileText size={36} className="text-blue-600" /> },
    { label: 'Regulations', desc: 'Browse and manage regulatory requirements.', to: '/regulations', icon: <BookOpen size={36} className="text-blue-600" /> },
    { label: 'Notifications', desc: 'View and manage pending notifications.', to: '/notifications', icon: <Bell size={36} className="text-blue-600" /> },
  ];

  return (
    <div>
      <h1 className="text-2xl font-bold text-gray-900">Dashboard</h1>
      <p className="text-gray-500 text-sm mt-1 mb-6">Welcome back, {fullName}.</p>

      {isLoading ? (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4 mb-6">
          {Array.from({ length: 7 }).map((_, i) => (
            <div key={i} className="bg-white rounded-xl border border-gray-200 shadow-sm p-5 animate-pulse">
              <div className="h-4 bg-gray-100 rounded w-1/2 mb-3" />
              <div className="h-8 bg-gray-100 rounded w-1/3 mb-3" />
              <div className="h-3 bg-gray-100 rounded w-2/3" />
            </div>
          ))}
        </div>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4 mb-6">
          {entityCards.map(card => (
            <EntityCard key={card.label} {...card} />
          ))}
          <GrievanceCard counts={stats?.grievances ?? { received: 0, replied: 0, pending: 0 }} />
        </div>
      )}

      <h2 className="text-sm font-semibold text-gray-500 uppercase tracking-wider mb-3">Quick Access</h2>
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        {quickLinks.map(link => (
          <Link key={link.to} to={link.to} className="card hover:shadow-md transition-shadow">
            <h3 className="font-semibold text-gray-900 mb-1">{link.label}</h3>
            <p className="text-sm text-gray-500 mb-4">{link.desc}</p>
            {link.icon}
          </Link>
        ))}
      </div>

      <p className="text-xs text-gray-400 mt-6">Last updated: {format(new Date(), 'MMM d, yyyy · h:mm a')}</p>
    </div>
  );
}

export default function DashboardPage() {
  const { user } = useAuth();

  if (user?.role === 'SuperAdmin') {
    return <SuperAdminDashboard fullName={user.fullName} />;
  }

  return <AdminDashboard fullName={user!.fullName} companyId={user!.userId} />;
}
