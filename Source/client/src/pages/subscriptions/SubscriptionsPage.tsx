import { useMemo, useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Eye, Pencil, Plus, Search, Trash2 } from 'lucide-react';
import { useAuth } from '../../contexts/AuthContext';
import { subscriptionService } from '../../services/subscriptionService';
import { regulationService } from '../../services/regulationService';
import { customerService } from '../../services/customerService';
import type { Subscription, SubscribingLevel } from '../../types';
import ConfirmDialog from '../../components/common/ConfirmDialog';
import SubscriptionHierarchyPicker, { buildTreeNodes, getSubscriptionItems } from './SubscriptionHierarchyPicker';
import SubscriptionDetailsModal from './SubscriptionDetailsModal';
import SubscriptionEditTaskModal from './SubscriptionEditTaskModal';

const levelColors: Record<SubscribingLevel, string> = {
  Entity: 'bg-purple-100 text-purple-700',
  Agency: 'bg-blue-100 text-blue-700',
  Category: 'bg-cyan-100 text-cyan-700',
  Type: 'bg-green-100 text-green-700',
  SubType: 'bg-yellow-100 text-yellow-700',
  Regulation: 'bg-orange-100 text-orange-700',
};

export default function SubscriptionsPage() {
  const qc = useQueryClient();
  const { user } = useAuth();
  const isCustomer = user?.role === 'Customer';
  const companyId = user?.role === 'Admin' ? user.userId : undefined;
  const scopedCustomerId = isCustomer ? user?.userId : undefined;

  const [tab, setTab] = useState<'add' | 'list'>('list');
  const [pickerCustomerId, setPickerCustomerId] = useState(scopedCustomerId ?? '');
  const [pickerEntityId, setPickerEntityId] = useState('');
  const [selectedKeys, setSelectedKeys] = useState<Set<string>>(new Set());
  const [saveResult, setSaveResult] = useState<{ saved: number; duplicates: string[] } | null>(null);
  const [filterCustomerId, setFilterCustomerId] = useState('');
  const [search, setSearch] = useState('');
  const [deleteTarget, setDeleteTarget] = useState<Subscription | null>(null);
  const [detailsId, setDetailsId] = useState<string | null>(null);
  const [editTaskId, setEditTaskId] = useState<string | null>(null);

  const activeCustomerId = scopedCustomerId ?? (filterCustomerId || undefined);
  const subscriptionParams = isCustomer
    ? { customerId: activeCustomerId }
    : { companyId: companyId, customerId: filterCustomerId || undefined };

  const { data: customersData } = useQuery({
    queryKey: ['customers-all', companyId],
    queryFn: () => customerService.getAll(companyId!, undefined, 1, 500),
    enabled: !isCustomer && !!companyId,
  });

  const { data: currentProfile } = useQuery({
    queryKey: ['customer-profile-picker', scopedCustomerId],
    queryFn: () => customerService.getCurrentProfile(),
    enabled: !!scopedCustomerId,
  });

  const { data: titlesData } = useQuery({
    queryKey: ['regulations', { isImported: true }],
    queryFn: async () => {
      const res = await regulationService.getAll({ isImported: true, pageSize: 200 });
      // Debug: confirm the dropdown sees only imported regulations end-to-end.
      const nonImported = (res?.items ?? []).filter(t => t.isImported === false);
      console.debug('[Subscriptions/AddModal] titles fetched', {
        total: res?.totalCount,
        returned: res?.items?.length ?? 0,
        nonImportedLeaks: nonImported.length,
      });
      return res;
    },
    enabled: !isCustomer && tab === 'add',
  });

  const { data: subscriptionsData, isLoading: loadingSubs } = useQuery({
    queryKey: ['subscriptions', subscriptionParams],
    queryFn: () => subscriptionService.getAll(subscriptionParams, 1, 200),
  });

  const { data: pickerHierarchy } = useQuery({
    queryKey: ['regulation-hierarchy', pickerEntityId],
    queryFn: () => regulationService.getHierarchy(pickerEntityId),
    enabled: !isCustomer && !!pickerEntityId,
  });

  const createMutation = useMutation({
    mutationFn: ({ customerId, items }: { customerId: string; items: ReturnType<typeof getSubscriptionItems> }) =>
      subscriptionService.createBulk(customerId, items),
    onSuccess: result => {
      qc.invalidateQueries({ queryKey: ['subscriptions'] });
      setSaveResult(result);
      setSelectedKeys(new Set());
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => subscriptionService.delete(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['subscriptions'] });
      setDeleteTarget(null);
    },
  });

  const customers = customersData?.items ?? [];
  // Server already filters to IsImported=true. This .filter is a defensive guard
  // so a stale React Query cache from a prior unfiltered call can never leak a
  // non-imported title into the dropdown.
  const titles = (titlesData?.items ?? []).filter(t => t.isImported === true);
  const allSubs = subscriptionsData?.items ?? [];

  const filteredSubs = useMemo(() => {
    if (!search.trim()) return allSubs;
    const q = search.toLowerCase();
    return allSubs.filter(sub =>
      sub.subscribedNodeName.toLowerCase().includes(q) ||
      sub.subscribingLevel.toLowerCase().includes(q) ||
      (sub.isActive ? 'active' : 'inactive').includes(q) ||
      (!isCustomer && sub.customerName.toLowerCase().includes(q))
    );
  }, [allSubs, isCustomer, search]);

  const selectedEntity = titles.find(t => t.id === pickerEntityId);
  const customerPageSubtitle = currentProfile?.customer.customerName
    ? `Subscription details for ${currentProfile.customer.customerName}.`
    : 'Your subscription details.';

  const handleSave = () => {
    if (!pickerCustomerId || !pickerEntityId || !pickerHierarchy || selectedKeys.size === 0) return;
    const treeNodes = buildTreeNodes(pickerHierarchy);
    const items = getSubscriptionItems(selectedKeys, pickerHierarchy, treeNodes);
    if (items.length === 0) return;
    setSaveResult(null);
    createMutation.mutate({ customerId: pickerCustomerId, items });
  };

  const tabClass = (value: 'add' | 'list') =>
    `px-4 py-2 text-sm font-medium border-b-2 transition-colors ${tab === value ? 'border-blue-600 text-blue-600' : 'border-transparent text-gray-500 hover:text-gray-700'}`;

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between flex-wrap gap-3">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Subscriptions</h1>
          <p className="text-sm text-gray-500 mt-0.5">
            {isCustomer ? customerPageSubtitle : 'Manage customer regulation subscriptions.'}
          </p>
        </div>
      </div>

      {!isCustomer && (
        <div className="flex border-b border-gray-200 gap-2">
          <button className={tabClass('list')} onClick={() => setTab('list')}>Subscriptions List</button>
          <button className={tabClass('add')} onClick={() => { setTab('add'); setSaveResult(null); }}>
            <span className="flex items-center gap-1"><Plus size={14} /> Add Subscription</span>
          </button>
        </div>
      )}

      {!isCustomer && tab === 'add' && (
        <div className="card space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Customer <span className="text-red-500">*</span></label>
              <select
                value={pickerCustomerId}
                onChange={e => { setPickerCustomerId(e.target.value); setSaveResult(null); setSelectedKeys(new Set()); }}
                className="form-input"
              >
                <option value="">-- Select customer --</option>
                {customers.map(customer => (
                  <option key={customer.id} value={customer.id}>{customer.customerName}</option>
                ))}
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Federal Title <span className="text-red-500">*</span></label>
              <select
                value={pickerEntityId}
                onChange={e => { setPickerEntityId(e.target.value); setSaveResult(null); setSelectedKeys(new Set()); }}
                className="form-input"
                disabled={titles.length === 0}
              >
                {titles.length === 0 ? (
                  <option value="">There are no active government entities</option>
                ) : (
                  <>
                    <option value="">-- Select title --</option>
                    {titles.map(title => (
                      <option key={title.id} value={title.id}>
                        Title {title.titleNumber} - {title.titleName}
                      </option>
                    ))}
                  </>
                )}
              </select>
            </div>
          </div>

          {pickerCustomerId && pickerEntityId && selectedEntity && (
            <>
              <SubscriptionHierarchyPicker
                governmentEntityId={pickerEntityId}
                governmentEntityName={`Title ${selectedEntity.titleNumber} - ${selectedEntity.titleName}`}
                selected={selectedKeys}
                onSelectionChange={setSelectedKeys}
              />

              {saveResult && (
                <div className="space-y-2">
                  {saveResult.saved > 0 && (
                    <div className="bg-green-50 border border-green-200 rounded-lg px-4 py-2 text-sm text-green-700">
                      {saveResult.saved} subscription{saveResult.saved !== 1 ? 's' : ''} saved successfully.
                    </div>
                  )}
                  {saveResult.duplicates.length > 0 && (
                    <div className="bg-amber-50 border border-amber-200 rounded-lg px-4 py-2 space-y-1">
                      <p className="text-sm font-medium text-amber-800">The following were already subscribed:</p>
                      {saveResult.duplicates.map((message, index) => (
                        <p key={index} className="text-sm text-amber-700">- {message}</p>
                      ))}
                    </div>
                  )}
                </div>
              )}

              <div className="flex items-center justify-between pt-2 border-t">
                <span className="text-sm text-gray-500">
                  {selectedKeys.size > 0 ? `${selectedKeys.size} node(s) selected` : 'Select nodes to subscribe'}
                </span>
                <button
                  onClick={handleSave}
                  disabled={selectedKeys.size === 0 || createMutation.isPending}
                  className="btn-primary px-6"
                >
                  {createMutation.isPending ? 'Saving...' : 'Save Subscriptions'}
                </button>
              </div>
            </>
          )}
        </div>
      )}

      {(isCustomer || tab === 'list') && (
        <div className="space-y-3">
          <div className="flex gap-3 items-center flex-wrap">
            <div className="relative flex-1 min-w-64 max-w-md">
              <Search size={15} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
              <input
                value={search}
                onChange={e => setSearch(e.target.value)}
                placeholder={isCustomer ? 'Search regulation title, level, status...' : 'Search customer, regulation title, level...'}
                className="form-input pl-9 py-2 text-sm"
              />
            </div>

            {!isCustomer && (
              <select
                value={filterCustomerId}
                onChange={e => setFilterCustomerId(e.target.value)}
                className="form-input text-sm max-w-xs"
              >
                <option value="">All Customers</option>
                {customers.map(customer => (
                  <option key={customer.id} value={customer.id}>{customer.customerName}</option>
                ))}
              </select>
            )}
          </div>

          <div className="card overflow-hidden">
            {loadingSubs ? (
              <p className="text-center text-gray-400 p-8">Loading...</p>
            ) : filteredSubs.length === 0 ? (
              <p className="text-center text-gray-400 p-8">No subscriptions found.</p>
            ) : (
              <table className="w-full text-sm">
                <thead>
                  <tr className="bg-gray-50 border-b">
                    {!isCustomer && <th className="text-left p-3 font-medium text-gray-600">Customer</th>}
                    <th className="text-left p-3 font-medium text-gray-600">Subscribing Level</th>
                    <th className="text-left p-3 font-medium text-gray-600">Subscribed To</th>
                    <th className="text-left p-3 font-medium text-gray-600">Status</th>
                    <th className="text-left p-3 font-medium text-gray-600">Since</th>
                    <th className="p-3 text-right font-medium text-gray-600">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredSubs.map(subscription => (
                    <tr key={subscription.id} className="border-b hover:bg-gray-50">
                      {!isCustomer && <td className="p-3 font-medium text-gray-800">{subscription.customerName}</td>}
                      <td className="p-3">
                        <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${levelColors[subscription.subscribingLevel] ?? 'bg-gray-100 text-gray-600'}`}>
                          {subscription.subscribingLevel}
                        </span>
                      </td>
                      <td className="p-3 text-gray-700 max-w-xs truncate" title={subscription.subscribedNodeName}>
                        {subscription.subscribedNodeName}
                      </td>
                      <td className="p-3">
                        <span className={`text-xs px-2 py-0.5 rounded-full ${subscription.isActive ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'}`}>
                          {subscription.isActive ? 'Active' : 'Inactive'}
                        </span>
                      </td>
                      <td className="p-3 text-gray-500 whitespace-nowrap">{new Date(subscription.createdAt).toLocaleDateString()}</td>
                      <td className="p-3 text-right">
                        <div className="flex items-center justify-end gap-2">
                          <button
                            onClick={() => setDetailsId(subscription.id)}
                            className="flex items-center gap-1 text-xs px-2 py-1 border border-gray-300 rounded hover:bg-gray-50 text-gray-600"
                          >
                            <Eye size={13} /> Details
                          </button>
                          {!isCustomer && (
                            <button
                              onClick={() => setEditTaskId(subscription.id)}
                              title="Edit Task Detail"
                              aria-label="Edit Task Detail"
                              className="flex items-center gap-1 text-xs px-2 py-1 border border-indigo-200 rounded hover:bg-indigo-50 text-indigo-700"
                            >
                              <Pencil size={13} /> Edit Task
                            </button>
                          )}
                          <button
                            onClick={() => setDeleteTarget(subscription)}
                            className="flex items-center gap-1 text-xs px-2 py-1 border border-red-200 rounded hover:bg-red-50 text-red-600"
                          >
                            <Trash2 size={13} /> Delete
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>
        </div>
      )}

      {detailsId && <SubscriptionDetailsModal subscriptionId={detailsId} onClose={() => setDetailsId(null)} />}
      {editTaskId && <SubscriptionEditTaskModal subscriptionId={editTaskId} onClose={() => setEditTaskId(null)} />}

      <ConfirmDialog
        isOpen={!!deleteTarget}
        onClose={() => setDeleteTarget(null)}
        onConfirm={() => deleteTarget && deleteMutation.mutate(deleteTarget.id)}
        title="Delete Subscription"
        message={`Are you sure you want to delete the subscription to "${deleteTarget?.subscribedNodeName}"? This will be soft-deleted and the customer will no longer receive notifications for this subscription.`}
        isLoading={deleteMutation.isPending}
        confirmLabel="Unsubscribe"
      />
    </div>
  );
}
