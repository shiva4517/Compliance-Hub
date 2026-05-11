import { useState } from 'react';
import { useParams, useSearchParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { customerService } from '../../services/customerService';
import { subscriptionService } from '../../services/subscriptionService';
import { regulationService } from '../../services/regulationService';
import { useAuth } from '../../contexts/AuthContext';
import { ArrowLeft, Users, CalendarDays, Hash, Trash2, BookOpen, Layers } from 'lucide-react';
import Modal from '../../components/common/Modal';
import ConfirmDialog from '../../components/common/ConfirmDialog';
import type { CustomerSubscription } from '../../types';

type Tab = 'details' | 'subscriptions';

const LEVEL_LABELS: Record<string, string> = {
  Entity: 'Gov. Entity',
  Agency: 'Agency',
  Category: 'Category',
  Type: 'Type',
  SubType: 'Subtype',
  Regulation: 'Regulation',
};

const LEVEL_COLORS: Record<string, string> = {
  Entity: 'bg-blue-100 text-blue-700',
  Agency: 'bg-purple-100 text-purple-700',
  Category: 'bg-teal-100 text-teal-700',
  Type: 'bg-orange-100 text-orange-700',
  SubType: 'bg-pink-100 text-pink-700',
  Regulation: 'bg-green-100 text-green-700',
};

function Field({ label, value }: { label: string; value?: string | null }) {
  return (
    <div>
      <p className="text-xs text-gray-500 font-medium uppercase tracking-wide mb-0.5">{label}</p>
      <p className="text-sm text-gray-900">{value || <span className="text-gray-400 italic">—</span>}</p>
    </div>
  );
}

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div>
      <h3 className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-3 pb-1 border-b border-gray-100">
        {title}
      </h3>
      <div className="grid grid-cols-2 gap-x-8 gap-y-4">{children}</div>
    </div>
  );
}

function HierarchyCell({ sub }: { sub: CustomerSubscription }) {
  const parts = [
    sub.governmentEntityName,
    sub.agencyName,
    sub.regulationCategoryName,
    sub.regulationTypeName,
    sub.regulationSubtypeName,
  ].filter(Boolean);
  return (
    <div className="text-xs text-gray-500 space-y-0.5">
      {parts.map((p, i) => (
        <div key={i} className="flex items-center gap-1">
          {i > 0 && <span className="text-gray-300 ml-1">›</span>}
          <span>{p}</span>
        </div>
      ))}
    </div>
  );
}

export default function CustomerViewPage() {
  const { id } = useParams<{ id: string }>();
  const [searchParams] = useSearchParams();
  const { user } = useAuth();
  const navigate = useNavigate();
  const qc = useQueryClient();
  const [activeTab, setActiveTab] = useState<Tab>('details');
  const [showSubscribe, setShowSubscribe] = useState(false);
  const [selectedTitleId, setSelectedTitleId] = useState('');
  const [deleteSubId, setDeleteSubId] = useState<string | null>(null);

  const companyId = searchParams.get('companyId') ?? user!.userId;

  const { data: customer, isLoading, isError } = useQuery({
    queryKey: ['customer', id, companyId],
    queryFn: () => customerService.getById(id!, companyId),
    enabled: !!id,
  });

  const { data: subscriptions = [], isLoading: subsLoading } = useQuery({
    queryKey: ['customer-subscriptions', id, companyId],
    queryFn: () => customerService.getSubscriptions(id!, companyId),
    enabled: !!id && activeTab === 'subscriptions',
  });

  const { data: titlesData } = useQuery({
    queryKey: ['regulations'],
    queryFn: () => regulationService.getAll({ pageSize: 200 }),
    enabled: showSubscribe,
  });

  const createSubMutation = useMutation({
    mutationFn: (govEntityId: string) =>
      subscriptionService.createBulk(id!, [{
        governmentEntityId: govEntityId,
        subscribingLevel: 'Entity',
        subscribedNodeName: titlesData?.items.find(t => t.id === govEntityId)?.titleName ?? '',
      }]),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['customer-subscriptions', id, companyId] });
      setShowSubscribe(false);
      setSelectedTitleId('');
    },
  });

  const deleteSubMutation = useMutation({
    mutationFn: subscriptionService.delete,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['customer-subscriptions', id, companyId] });
      setDeleteSubId(null);
    },
  });

  if (isLoading) {
    return <div className="flex items-center justify-center h-64 text-gray-400">Loading customer...</div>;
  }

  if (isError || !customer) {
    return (
      <div className="flex flex-col items-center justify-center h-64 text-gray-400 gap-2">
        <Users size={40} className="opacity-30" />
        <p>Customer not found.</p>
        <button onClick={() => navigate('/customers')} className="btn-secondary mt-2">Back to Customers</button>
      </div>
    );
  }

  const titles = titlesData?.items ?? [];
  const subscribedGovEntityIds = new Set(subscriptions.map(s => s.governmentEntityId));
  const availableTitles = titles.filter(t => !subscribedGovEntityIds.has(t.id) && t.isImported);

  const tabs: { key: Tab; label: string }[] = [
    { key: 'details', label: 'Details' },
    { key: 'subscriptions', label: `Subscriptions${subscriptions.length > 0 ? ` (${subscriptions.length})` : ''}` },
  ];

  return (
    <div>
      {/* Header */}
      <div className="flex items-center gap-3 mb-1">
        <button onClick={() => navigate('/customers')}
          className="p-1.5 text-gray-500 hover:bg-gray-100 rounded" title="Back">
          <ArrowLeft size={18} />
        </button>
        <Users size={22} className="text-blue-800" />
        <h1 className="text-2xl font-bold text-gray-900">{customer.customerName}</h1>
        <span className="ml-1 text-xs font-mono text-gray-500 bg-gray-100 px-2 py-0.5 rounded">
          {customer.customerCode}
        </span>
        <span className={`ml-2 text-xs px-2 py-0.5 rounded-full font-medium ${customer.isActive ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'}`}>
          {customer.isActive ? 'Active' : 'Inactive'}
        </span>
      </div>
      <p className="text-sm text-gray-500 mb-4 ml-10">
        {customer.primaryContactFirstName} {customer.primaryContactLastName} · {customer.primaryEmail}
      </p>

      {/* Tabs */}
      <div className="flex gap-0 border-b border-gray-200 mb-6">
        {tabs.map(tab => (
          <button key={tab.key} onClick={() => setActiveTab(tab.key)}
            className={`px-5 py-2.5 text-sm font-medium border-b-2 transition-colors ${
              activeTab === tab.key
                ? 'border-blue-700 text-blue-700'
                : 'border-transparent text-gray-500 hover:text-gray-700'
            }`}>
            {tab.label}
          </button>
        ))}
      </div>

      {/* Details Tab */}
      {activeTab === 'details' && (
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6 space-y-8">
          <Section title="Customer Information">
            <Field label="Customer Code" value={customer.customerCode} />
            <Field label="Customer Name" value={customer.customerName} />
            <div className="col-span-2 flex items-center gap-6 text-xs text-gray-400">
              <span className="flex items-center gap-1">
                <CalendarDays size={12} />
                Created {new Date(customer.createdAt).toLocaleDateString('en-US', { year: 'numeric', month: 'long', day: 'numeric' })}
              </span>
              <span className="flex items-center gap-1">
                <Hash size={12} />
                Company: {customer.companyName}
              </span>
            </div>
          </Section>

          <Section title="Primary Contact">
            <Field label="First Name" value={customer.primaryContactFirstName} />
            <Field label="Last Name" value={customer.primaryContactLastName} />
            <Field label="Primary Email" value={customer.primaryEmail} />
            <Field label="Secondary Email" value={customer.secondaryEmail} />
            <Field label="Phone Number" value={customer.phoneNumber} />
            <Field label="Mobile Number" value={customer.mobileNumber} />
          </Section>

          <Section title="Primary Address">
            <div className="col-span-2">
              <Field label="Street Address" value={customer.primaryAddress} />
            </div>
            <Field label="City" value={customer.primaryCity} />
            <Field label="State" value={customer.primaryState} />
            <Field label="Postal Code" value={customer.primaryPostalCode} />
          </Section>

          {(customer.secondaryAddress || customer.secondaryCity || customer.secondaryState) && (
            <Section title="Secondary Address">
              <div className="col-span-2">
                <Field label="Street Address" value={customer.secondaryAddress} />
              </div>
              <Field label="City" value={customer.secondaryCity} />
              <Field label="State" value={customer.secondaryState} />
              <Field label="Postal Code" value={customer.secondaryPostalCode} />
            </Section>
          )}
        </div>
      )}

      {/* Subscriptions Tab */}
      {activeTab === 'subscriptions' && (
        <div>
          <div className="flex items-center justify-between mb-4">
            <p className="text-sm text-gray-500">
              Active regulation subscriptions for <strong>{customer.customerName}</strong>.
            </p>
            {/* <button onClick={() => setShowSubscribe(true)} className="btn-primary flex items-center gap-2 text-sm">
              <Plus size={15} /> Subscribe to Title
            </button> */}
          </div>

          <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
            {subsLoading ? (
              <p className="text-center text-gray-400 p-8">Loading subscriptions...</p>
            ) : subscriptions.length === 0 ? (
              <div className="flex flex-col items-center justify-center py-16 text-gray-400 gap-3">
                <BookOpen size={36} className="opacity-30" />
                <p className="text-sm font-medium text-gray-500">No active subscriptions</p>
                <p className="text-xs text-center max-w-xs">
                  Subscribe this customer to federal regulations to start receiving change notifications.
                </p>
              </div>
            ) : (
              <table className="w-full text-sm">
                <thead className="bg-gray-50 border-b border-gray-200">
                  <tr>
                    <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide">Level</th>
                    <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide">Subscribed Node</th>
                    <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide">Hierarchy</th>
                    <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide">Status</th>
                    <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide">Subscribed</th>
                    <th className="px-4 py-3"></th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100">
                  {subscriptions.map((sub: CustomerSubscription) => (
                    <tr key={sub.id} className="hover:bg-gray-50 transition-colors">
                      <td className="px-4 py-3">
                        <span className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium ${LEVEL_COLORS[sub.subscribingLevel] ?? 'bg-gray-100 text-gray-600'}`}>
                          <Layers size={10} />
                          {LEVEL_LABELS[sub.subscribingLevel] ?? sub.subscribingLevel}
                        </span>
                      </td>
                      <td className="px-4 py-3 font-medium text-gray-900 max-w-[200px] truncate" title={sub.subscribedNodeName}>
                        {sub.subscribedNodeName || '—'}
                      </td>
                      <td className="px-4 py-3">
                        <HierarchyCell sub={sub} />
                      </td>
                      <td className="px-4 py-3">
                        <span className={`inline-flex px-2 py-0.5 rounded-full text-xs font-medium ${sub.isActive ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'}`}>
                          {sub.isActive ? 'Active' : 'Inactive'}
                        </span>
                      </td>
                      <td className="px-4 py-3 text-gray-500 text-xs whitespace-nowrap">
                        {new Date(sub.createdAt).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' })}
                      </td>
                      <td className="px-4 py-3">
                        <button
                          onClick={() => setDeleteSubId(sub.id)}
                          className="p-1.5 text-red-400 hover:text-red-600 hover:bg-red-50 rounded"
                          title="Unsubscribe"
                        >
                          <Trash2 size={14} />
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>
        </div>
      )}

      {/* Subscribe Modal */}
      <Modal
        isOpen={showSubscribe}
        onClose={() => { setShowSubscribe(false); setSelectedTitleId(''); }}
        title="Subscribe to Federal Title"
        subtitle={`Add a regulation subscription for ${customer.customerName}.`}
      >
        <div className="space-y-4">
          <div>
            <label className="form-label">Select Federal Title <span className="text-red-500">*</span></label>
            <select value={selectedTitleId} onChange={e => setSelectedTitleId(e.target.value)} className="form-input">
              <option value="">— Select a title —</option>
              {availableTitles.map(t => (
                <option key={t.id} value={t.id}>Title {t.titleNumber} — {t.titleName}</option>
              ))}
            </select>
            {availableTitles.length === 0 && titles.length > 0 && (
              <p className="text-xs text-gray-400 mt-1">All available titles are already subscribed.</p>
            )}
          </div>
          <div className="flex justify-end gap-3 pt-2 border-t border-gray-200">
            <button onClick={() => { setShowSubscribe(false); setSelectedTitleId(''); }} className="btn-secondary">
              Cancel
            </button>
            <button
              onClick={() => selectedTitleId && createSubMutation.mutate(selectedTitleId)}
              disabled={!selectedTitleId || createSubMutation.isPending}
              className="btn-primary"
            >
              {createSubMutation.isPending ? 'Subscribing...' : 'Subscribe'}
            </button>
          </div>
        </div>
      </Modal>

      <ConfirmDialog
        isOpen={!!deleteSubId}
        onClose={() => setDeleteSubId(null)}
        onConfirm={() => deleteSubId && deleteSubMutation.mutate(deleteSubId)}
        title="Unsubscribe"
        message="Remove this subscription? The customer will stop receiving change notifications."
        isLoading={deleteSubMutation.isPending}
        confirmLabel="Unsubscribe"
      />
    </div>
  );
}
