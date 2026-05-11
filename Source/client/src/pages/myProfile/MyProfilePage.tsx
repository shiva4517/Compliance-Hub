import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import {
  Bell, Briefcase, Building2, Mail, MapPin, Phone, UserCircle,
} from 'lucide-react';
import { employeeService } from '../../services/employeeService';

function InfoRow({ label, value }: { label: string; value?: string | null }) {
  return (
    <div>
      <p className="text-xs font-semibold uppercase tracking-wide text-gray-400">{label}</p>
      <p className="mt-1 text-sm text-gray-700">{value?.trim() ? value : '—'}</p>
    </div>
  );
}

function formatAddress(parts: Array<string | null | undefined>) {
  return parts.filter(p => !!p?.trim()).join(', ');
}

export default function MyProfilePage() {
  const navigate = useNavigate();

  const { data: profile, isLoading, isError } = useQuery({
    queryKey: ['my-profile'],
    queryFn: () => employeeService.getMyProfile(),
  });

  const { data: stats } = useQuery({
    queryKey: ['my-stats'],
    queryFn: () => employeeService.getMyStats(),
  });

  if (isLoading) {
    return <div className="flex items-center justify-center h-64 text-gray-400">Loading your profile...</div>;
  }

  if (isError || !profile) {
    return <div className="flex items-center justify-center h-64 text-gray-400">Unable to load your profile.</div>;
  }

  const primaryAddress = formatAddress([
    profile.primaryAddress,
    profile.primaryCity,
    profile.primaryState,
    profile.primaryPostalCode,
  ]);

  const companyAddress = formatAddress([
    profile.companyAddress,
    profile.companyCity,
    profile.companyState,
    profile.companyPostalCode,
  ]);

  return (
    <div className="space-y-6">
      <div className="flex items-start justify-between gap-4 flex-wrap">
        <div>
          <div className="flex items-center gap-3 mb-1">
            <UserCircle size={22} className="text-blue-800" />
            <h1 className="text-2xl font-bold text-gray-900">My Profile</h1>
          </div>
          <p className="text-sm text-gray-500">View your employee profile, company information, and regulatory activity.</p>
        </div>
        <span className={`text-xs px-3 py-1 rounded-full font-medium ${profile.isActive ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-600'}`}>
          {profile.isActive ? 'Active' : 'Inactive'}
        </span>
      </div>

      {/* Summary cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <button
          type="button"
          onClick={() => navigate('/regulatory-assignments')}
          className="card text-left hover:border-blue-200 hover:shadow-sm transition-all"
        >
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-500">Regulatory Assignments</p>
              <p className="mt-2 text-3xl font-bold text-gray-900">{stats?.activeAssignments ?? '—'}</p>
              <p className="mt-2 text-sm text-blue-700">View my assignments</p>
            </div>
            <Briefcase size={24} className="text-blue-600" />
          </div>
        </button>

        <button
          type="button"
          onClick={() => navigate('/change-notices')}
          className="card text-left hover:border-blue-200 hover:shadow-sm transition-all"
        >
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-500">Change Notices</p>
              <p className="mt-2 text-3xl font-bold text-gray-900">{stats?.totalChangeNotices ?? '—'}</p>
              <p className="mt-2 text-sm text-blue-700">Open my change notices</p>
            </div>
            <Bell size={24} className="text-blue-600" />
          </div>
        </button>
      </div>

      <div className="grid grid-cols-1 xl:grid-cols-2 gap-6">
        {/* Employee info */}
        <section className="card space-y-5">
          <div className="flex items-center gap-2">
            <UserCircle size={18} className="text-blue-700" />
            <h2 className="text-lg font-semibold text-gray-900">Employee Details</h2>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <InfoRow label="Employee Code" value={profile.employeeCode} />
            <InfoRow label="Full Name" value={profile.fullName} />
            <InfoRow label="Primary Email" value={profile.primaryEmail} />
            <InfoRow label="Secondary Email" value={profile.secondaryEmail} />
            <InfoRow label="Phone Number" value={profile.phoneNumber} />
            <InfoRow label="Mobile Number" value={profile.mobileNumber} />
            <InfoRow label="Department" value={profile.departmentName} />
            <InfoRow label="Division" value={profile.divisionName} />
            <InfoRow label="District" value={profile.districtName} />
            <InfoRow label="Primary Address" value={primaryAddress} />
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-3 pt-2 border-t border-gray-100">
            <div className="flex items-center gap-2 text-sm text-gray-600">
              <Mail size={15} className="text-gray-400" />
              <span className="truncate">{profile.primaryEmail}</span>
            </div>
            <div className="flex items-center gap-2 text-sm text-gray-600">
              <Phone size={15} className="text-gray-400" />
              <span>{profile.mobileNumber || profile.phoneNumber || '—'}</span>
            </div>
            <div className="flex items-center gap-2 text-sm text-gray-600">
              <MapPin size={15} className="text-gray-400" />
              <span className="truncate">{primaryAddress || '—'}</span>
            </div>
          </div>
        </section>

        {/* Company info */}
        <section className="card space-y-5">
          <div className="flex items-center gap-2">
            <Building2 size={18} className="text-blue-700" />
            <h2 className="text-lg font-semibold text-gray-900">Company Information</h2>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <InfoRow label="Company Name" value={profile.companyName} />
            <InfoRow label="Email" value={profile.companyEmail} />
            <InfoRow label="Phone" value={profile.companyPhone} />
            <InfoRow label="Website" value={profile.companyWebsite} />
            <InfoRow label="Address" value={companyAddress} />
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-3 pt-2 border-t border-gray-100">
            <div className="flex items-center gap-2 text-sm text-gray-600">
              <Mail size={15} className="text-gray-400" />
              <span className="truncate">{profile.companyEmail}</span>
            </div>
            <div className="flex items-center gap-2 text-sm text-gray-600">
              <Phone size={15} className="text-gray-400" />
              <span>{profile.companyPhone || '—'}</span>
            </div>
            <div className="flex items-center gap-2 text-sm text-gray-600">
              <MapPin size={15} className="text-gray-400" />
              <span className="truncate">{companyAddress || '—'}</span>
            </div>
          </div>
        </section>
      </div>
    </div>
  );
}
