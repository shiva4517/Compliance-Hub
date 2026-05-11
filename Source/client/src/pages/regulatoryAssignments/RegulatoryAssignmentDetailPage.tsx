import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import {
  ArrowLeft, BookOpen, Building2, UserCircle,
} from 'lucide-react';
import { employeeService } from '../../services/employeeService';
import type { SubscribingLevel } from '../../types';

function InfoRow({ label, value }: { label: string; value?: string | null }) {
  return (
    <div>
      <p className="text-xs font-semibold uppercase tracking-wide text-gray-400">{label}</p>
      <p className="mt-1 text-sm text-gray-700">{value?.trim() ? value : '—'}</p>
    </div>
  );
}

function levelBadge(level: SubscribingLevel) {
  const map: Record<SubscribingLevel, { label: string; cls: string }> = {
    Entity: { label: 'Entity', cls: 'bg-purple-100 text-purple-700' },
    Agency: { label: 'Agency', cls: 'bg-blue-100 text-blue-700' },
    Category: { label: 'Category', cls: 'bg-teal-100 text-teal-700' },
    Type: { label: 'Type', cls: 'bg-orange-100 text-orange-700' },
    SubType: { label: 'SubType', cls: 'bg-pink-100 text-pink-700' },
    Regulation: { label: 'Regulation', cls: 'bg-indigo-100 text-indigo-700' },
  };
  const { label, cls } = map[level] ?? { label: level, cls: 'bg-gray-100 text-gray-600' };
  return <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${cls}`}>{label}</span>;
}

function formatDate(value: string) {
  return new Date(value).toLocaleDateString('en-US', {
    day: '2-digit', month: 'short', year: 'numeric',
  });
}

type Tab = 'customer' | 'regulation';

export default function RegulatoryAssignmentDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [tab, setTab] = useState<Tab>('customer');

  const { data: detail, isLoading, isError } = useQuery({
    queryKey: ['my-assignment-detail', id],
    queryFn: () => employeeService.getMyAssignmentDetail(id!),
    enabled: !!id,
  });

  if (isLoading) {
    return <div className="flex items-center justify-center h-64 text-gray-400">Loading assignment details...</div>;
  }

  if (isError || !detail) {
    return <div className="flex items-center justify-center h-64 text-gray-400">Assignment not found.</div>;
  }

  const customerAddress = [detail.primaryAddress, detail.primaryCity, detail.primaryState, detail.primaryPostalCode]
    .filter(Boolean).join(', ');

  const hasRegulationDetail = !!(detail.regulationDescription || detail.regulationCondition || detail.suggestedTask || detail.frequencyTypeName || detail.dueDateTypeName);

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-3">
        <button
          onClick={() => navigate('/regulatory-assignments')}
          className="text-gray-400 hover:text-gray-600 p-1 rounded"
        >
          <ArrowLeft size={20} />
        </button>
        <div>
          <h1 className="text-xl font-bold text-gray-900">{detail.subscribedNodeName}</h1>
          <p className="text-sm text-gray-500">Assignment Detail · {detail.customerName}</p>
        </div>
        <div className="ml-auto flex items-center gap-2">
          {levelBadge(detail.subscribingLevel)}
          <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${detail.isActive ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'}`}>
            {detail.isActive ? 'Active' : 'Inactive'}
          </span>
        </div>
      </div>

      {/* Tabs */}
      <div className="border-b border-gray-200">
        <nav className="flex gap-1">
          {([
            { key: 'customer', label: 'Customer Information', icon: <UserCircle size={15} /> },
            { key: 'regulation', label: 'Regulation Details', icon: <BookOpen size={15} /> },
          ] as { key: Tab; label: string; icon: React.ReactNode }[]).map(t => (
            <button
              key={t.key}
              onClick={() => setTab(t.key)}
              className={`flex items-center gap-1.5 px-4 py-2.5 text-sm font-medium border-b-2 -mb-px transition-colors ${
                tab === t.key
                  ? 'border-blue-700 text-blue-700'
                  : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
              }`}
            >
              {t.icon}
              {t.label}
            </button>
          ))}
        </nav>
      </div>

      {tab === 'customer' && (
        <div className="grid grid-cols-1 xl:grid-cols-2 gap-6">
          <section className="card space-y-4">
            <div className="flex items-center gap-2">
              <UserCircle size={18} className="text-blue-700" />
              <h2 className="text-base font-semibold text-gray-900">Customer Information</h2>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <InfoRow label="Customer Code" value={detail.customerCode} />
              <InfoRow label="Customer Name" value={detail.customerName} />
              <InfoRow label="Primary Contact" value={`${detail.primaryContactFirstName} ${detail.primaryContactLastName}`} />
              <InfoRow label="Email" value={detail.customerEmail} />
              <InfoRow label="Phone" value={detail.customerPhone} />
              <InfoRow label="Mobile" value={detail.customerMobileNumber} />
              <InfoRow label="Address" value={customerAddress || undefined} />
            </div>
          </section>

          <section className="card space-y-4">
            <div className="flex items-center gap-2">
              <Building2 size={18} className="text-blue-700" />
              <h2 className="text-base font-semibold text-gray-900">Assignment Metadata</h2>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <InfoRow label="Assigned At" value={formatDate(detail.assignedAt)} />
              <InfoRow label="Assigned By" value={detail.assignedBy} />
              <InfoRow label="Created At" value={formatDate(detail.createdAt)} />
              <InfoRow label="Created By" value={detail.createdBy} />
            </div>
          </section>
        </div>
      )}

      {tab === 'regulation' && (
        <div className="space-y-6">
          <section className="card space-y-4">
            <div className="flex items-center gap-2">
              <BookOpen size={18} className="text-blue-700" />
              <h2 className="text-base font-semibold text-gray-900">Regulation Hierarchy</h2>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <InfoRow label="Subscribing Level" value={detail.subscribingLevel} />
              <InfoRow label="Subscribed Node" value={detail.subscribedNodeName} />
              <InfoRow label="Government Entity" value={detail.governmentEntityName} />
              <InfoRow label="Agency" value={detail.agencyName} />
              <InfoRow label="Category" value={detail.regulationCategoryName} />
              <InfoRow label="Type" value={detail.regulationTypeName} />
              <InfoRow label="Subtype" value={detail.regulationSubtypeName} />
            </div>
          </section>

          {hasRegulationDetail && (
            <section className="card space-y-4">
              <div className="flex items-center gap-2">
                <BookOpen size={18} className="text-indigo-600" />
                <h2 className="text-base font-semibold text-gray-900">Regulation Details</h2>
              </div>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {detail.regulationDescription && (
                  <div className="md:col-span-2">
                    <InfoRow label="Description" value={detail.regulationDescription} />
                  </div>
                )}
                {detail.regulationCondition && (
                  <div className="md:col-span-2">
                    <InfoRow label="Condition" value={detail.regulationCondition} />
                  </div>
                )}
                {detail.suggestedTask && (
                  <div className="md:col-span-2">
                    <InfoRow label="Suggested Task" value={detail.suggestedTask} />
                  </div>
                )}
                <InfoRow label="Frequency Type" value={detail.frequencyTypeName} />
                <InfoRow label="Due Date Type" value={detail.dueDateTypeName} />
              </div>
            </section>
          )}

          {!hasRegulationDetail && (
            <div className="card flex flex-col items-center justify-center py-12 text-gray-400 gap-2">
              <BookOpen size={36} />
              <p className="text-sm">No additional regulation details available for this subscription level.</p>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
