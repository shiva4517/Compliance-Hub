import { useQuery } from '@tanstack/react-query';
import { Building2, Bell, Mail, MapPin, Phone, RefreshCw, UserCircle } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import { customerService } from '../../services/customerService';
import { subscriptionService } from '../../services/subscriptionService';
import { changeNoticeService } from '../../services/changeNoticeService';

function InfoRow({ label, value }: { label: string; value?: string | null }) {
  return (
    <div>
      <p className="text-xs font-semibold uppercase tracking-wide text-gray-400">{label}</p>
      <p className="mt-1 text-sm text-gray-700">{value?.trim() ? value : '—'}</p>
    </div>
  );
}

function formatAddress(parts: Array<string | null | undefined>) {
  return parts.filter(part => !!part?.trim()).join(', ');
}

export default function CustomerDetailsPage() {
  const navigate = useNavigate();
  const { user } = useAuth();
  const customerId = user?.userId;

  const { data, isLoading, isError } = useQuery({
    queryKey: ['customer-profile', customerId],
    queryFn: () => customerService.getCurrentProfile(),
    enabled: !!customerId,
  });

  const { data: subscriptionsCount = 0 } = useQuery({
    queryKey: ['customer-subscriptions-count', customerId],
    queryFn: async () => {
      const result = await subscriptionService.getAll({ customerId }, 1, 1);
      return result.totalCount;
    },
    enabled: !!customerId,
  });

  const { data: changeNoticesCount = 0 } = useQuery({
    queryKey: ['customer-change-notices-count', customerId],
    queryFn: async () => {
      const result = await changeNoticeService.getNotifications({ customerId, pageNumber: 1, pageSize: 1 });
      return result.totalCount;
    },
    enabled: !!customerId,
  });

  if (isLoading) {
    return <div className="flex items-center justify-center h-64 text-gray-400">Loading your details...</div>;
  }

  if (isError || !data) {
    return <div className="flex items-center justify-center h-64 text-gray-400">Unable to load your profile.</div>;
  }

  const { customer, company } = data;
  const customerAddress = formatAddress([
    customer.primaryAddress,
    customer.primaryCity,
    customer.primaryState,
    customer.primaryPostalCode,
  ]);
  const companyAddress = formatAddress([
    company.primaryAddress,
    company.primaryCity,
    company.primaryState,
    company.primaryPostalCode,
  ]);

  return (
    <div className="space-y-6">
      <div className="flex items-start justify-between gap-4 flex-wrap">
        <div>
          <div className="flex items-center gap-3 mb-1">
            <UserCircle size={22} className="text-blue-800" />
            <h1 className="text-2xl font-bold text-gray-900">My Details</h1>
          </div>
          <p className="text-sm text-gray-500">View your account profile, company information, and regulatory activity.</p>
        </div>
        <span className={`text-xs px-3 py-1 rounded-full font-medium ${customer.isActive ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-600'}`}>
          {customer.isActive ? 'Active Account' : 'Inactive Account'}
        </span>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <button
          type="button"
          onClick={() => navigate('/subscriptions')}
          className="card text-left hover:border-blue-200 hover:shadow-sm transition-all"
        >
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-500">Subscriptions</p>
              <p className="mt-2 text-3xl font-bold text-gray-900">{subscriptionsCount}</p>
              <p className="mt-2 text-sm text-blue-700">View my subscription details</p>
            </div>
            <RefreshCw size={24} className="text-blue-600" />
          </div>
        </button>

        <button
          type="button"
          onClick={() => navigate('/change-notices')}
          className="card text-left hover:border-blue-200 hover:shadow-sm transition-all"
        >
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-500">Change Notices Received</p>
              <p className="mt-2 text-3xl font-bold text-gray-900">{changeNoticesCount}</p>
              <p className="mt-2 text-sm text-blue-700">Open my change notices</p>
            </div>
            <Bell size={24} className="text-blue-600" />
          </div>
        </button>
      </div>

      <div className="grid grid-cols-1 xl:grid-cols-2 gap-6">
        <section className="card space-y-5">
          <div className="flex items-center gap-2">
            <UserCircle size={18} className="text-blue-700" />
            <h2 className="text-lg font-semibold text-gray-900">Personal Details</h2>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <InfoRow label="Customer Code" value={customer.customerCode} />
            <InfoRow label="Customer Name" value={customer.customerName} />
            <InfoRow label="Primary Contact" value={`${customer.primaryContactFirstName} ${customer.primaryContactLastName}`} />
            <InfoRow label="Primary Email" value={customer.primaryEmail} />
            <InfoRow label="Secondary Email" value={customer.secondaryEmail} />
            <InfoRow label="Phone Number" value={customer.phoneNumber} />
            <InfoRow label="Mobile Number" value={customer.mobileNumber} />
            <InfoRow label="Address" value={customerAddress} />
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-3 pt-2 border-t border-gray-100">
            <div className="flex items-center gap-2 text-sm text-gray-600">
              <Mail size={15} className="text-gray-400" />
              <span>{customer.primaryEmail}</span>
            </div>
            <div className="flex items-center gap-2 text-sm text-gray-600">
              <Phone size={15} className="text-gray-400" />
              <span>{customer.mobileNumber || customer.phoneNumber || '—'}</span>
            </div>
            <div className="flex items-center gap-2 text-sm text-gray-600">
              <MapPin size={15} className="text-gray-400" />
              <span>{customerAddress || '—'}</span>
            </div>
          </div>
        </section>

        <section className="card space-y-5">
          <div className="flex items-center gap-2">
            <Building2 size={18} className="text-blue-700" />
            <h2 className="text-lg font-semibold text-gray-900">Company Information</h2>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <InfoRow label="Company Code" value={company.companyCode} />
            <InfoRow label="Company Name" value={company.companyName} />
            <InfoRow label="Primary Email" value={company.primaryEmail} />
            <InfoRow label="Secondary Email" value={company.secondaryEmail} />
            <InfoRow label="Phone Number" value={company.phoneNumber} />
            <InfoRow label="Website" value={company.websiteUrl} />
            <InfoRow label="Primary Address" value={companyAddress} />
            <InfoRow
              label="Secondary Address"
              value={formatAddress([
                company.secondaryAddress,
                company.secondaryCity,
                company.secondaryState,
                company.secondaryPostalCode,
              ])}
            />
          </div>
        </section>
      </div>
    </div>
  );
}
