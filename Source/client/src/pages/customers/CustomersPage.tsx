import { useMemo, useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import { customerService } from '../../services/customerService';
import type { Customer } from '../../types';
import type { CustomerFormData } from '../../services/customerService';
import { Plus, Search, Eye, Pencil, Trash2, Users, ToggleLeft, ToggleRight } from 'lucide-react';
import Modal from '../../components/common/Modal';
import ConfirmDialog from '../../components/common/ConfirmDialog';
import RestoreDialog from '../../components/common/RestoreDialog';
import Toast, { type ToastState } from '../../components/common/Toast';
import { extractErrorMessage } from '../../utils/errors';
import CustomerForm from './CustomerForm';

type InactiveTarget = { existingId: string; pendingData: CustomerFormData; message: string };

export default function CustomersPage() {
  const { user } = useAuth();
  const companyId = user!.userId;
  const navigate = useNavigate();
  const qc = useQueryClient();

  const [search, setSearch] = useState('');
  const [showCreate, setShowCreate] = useState(false);
  const [editCustomer, setEditCustomer] = useState<Customer | null>(null);
  const [deleteId, setDeleteId] = useState<string | null>(null);
  const [toggleTarget, setToggleTarget] = useState<Customer | null>(null);
  const [createError, setCreateError] = useState('');
  const [updateError, setUpdateError] = useState('');
  const [inactiveTarget, setInactiveTarget] = useState<InactiveTarget | null>(null);
  const [toast, setToast] = useState<ToastState>(null);

  const { data, isLoading } = useQuery({
    queryKey: ['customers', companyId],
    queryFn: () => customerService.getAll(companyId, undefined, 1, 500),
  });

  const filtered = useMemo(() => {
    const all = data?.items ?? [];
    if (!search.trim()) return all;
    const s = search.toLowerCase();
    return all.filter(c =>
      [c.customerName, c.customerCode, c.primaryContactFirstName, c.primaryContactLastName,
       c.primaryEmail, c.secondaryEmail, c.phoneNumber, c.mobileNumber,
       c.primaryAddress, c.primaryCity, c.primaryState, c.primaryPostalCode,
       c.isActive ? 'active' : 'inactive']
        .some(v => v?.toLowerCase().includes(s))
    );
  }, [data, search]);

  const createMutation = useMutation({
    mutationFn: (payload: CustomerFormData) => customerService.create(payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['customers', companyId] });
      setShowCreate(false);
      setCreateError('');
      setToast({ type: 'success', message: 'Customer added successfully' });
    },
    onError: (err: unknown, variables) => {
      const res = (err as { response?: { data?: { isInactive?: boolean; existingId?: string; message?: string } } })?.response?.data;
      if (res?.isInactive && res.existingId) {
        setShowCreate(false);
        setCreateError('');
        setInactiveTarget({ existingId: res.existingId, pendingData: variables, message: res.message ?? 'This customer exists but is inactive.' });
        return;
      }
      const msg = extractErrorMessage(err, 'Failed to add customer. Please try again.');
      setCreateError(msg);
      setToast({ type: 'error', message: msg });
    }
  });

  const restoreMutation = useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: CustomerFormData }) =>
      customerService.restore(id, payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['customers', companyId] });
      setInactiveTarget(null);
      setToast({ type: 'success', message: 'Customer restored successfully' });
    },
    onError: (err) => setToast({ type: 'error', message: extractErrorMessage(err, 'Failed to restore customer.') }),
  });

  const updateMutation = useMutation({
    mutationFn: (payload: CustomerFormData & { id: string }) =>
      customerService.update(payload.id, payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['customers', companyId] });
      setEditCustomer(null);
      setUpdateError('');
      setToast({ type: 'success', message: 'Customer updated successfully' });
    },
    onError: (err: unknown) => {
      const msg = extractErrorMessage(err, 'Failed to update customer. Please try again.');
      setUpdateError(msg);
      setToast({ type: 'error', message: msg });
    }
  });

  const toggleMutation = useMutation({
    mutationFn: ({ id, isActive }: { id: string; isActive: boolean }) =>
      customerService.toggleStatus(id, companyId, isActive),
    onSuccess: (_, vars) => {
      qc.invalidateQueries({ queryKey: ['customers', companyId] });
      setToggleTarget(null);
      setToast({ type: 'success', message: vars.isActive ? 'Customer activated successfully' : 'Customer deactivated successfully' });
    },
    onError: (err) => {
      setToggleTarget(null);
      setToast({ type: 'error', message: extractErrorMessage(err, 'Failed to update customer status.') });
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => customerService.delete(id, companyId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['customers', companyId] });
      setDeleteId(null);
      setToast({ type: 'success', message: 'Customer deleted successfully' });
    },
    onError: (err) => {
      setDeleteId(null);
      setToast({ type: 'error', message: extractErrorMessage(err, 'Failed to delete customer. Please try again.') });
    },
  });

  const handleRestore = () => {
    if (!inactiveTarget) return;
    restoreMutation.mutate({ id: inactiveTarget.existingId, payload: inactiveTarget.pendingData });
  };

  const handleCreateNew = () => {
    if (!inactiveTarget) return;
    const data = inactiveTarget.pendingData;
    setInactiveTarget(null);
    createMutation.mutate({ ...data, skipRestoreCheck: true } as CustomerFormData);
    setShowCreate(false);
  };

  return (
    <div>
      <div className="flex items-center justify-between mb-1">
        <div>
          <div className="flex items-center gap-3">
            <Users size={22} className="text-blue-800" />
            <h1 className="text-2xl font-bold text-gray-900">Customers</h1>
          </div>
          <p className="text-sm text-gray-500 mt-0.5">Manage customer accounts and their portal access.</p>
        </div>
        <button onClick={() => { setCreateError(''); setShowCreate(true); }}
          className="btn-primary flex items-center gap-2">
          <Plus size={16} /> Add Customer
        </button>
      </div>

      <div className="mt-4 mb-4 flex items-center gap-3">
        <div className="relative w-80">
          <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
          <input type="text" placeholder="Search across all fields..."
            value={search} onChange={e => setSearch(e.target.value)}
            className="form-input pl-9" />
        </div>
        {!isLoading && (
          <span className="text-sm text-gray-500">
            Showing {filtered.length} of {data?.items.length ?? 0} customers
          </span>
        )}
      </div>

      <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
        <table className="w-full">
          <thead className="bg-gray-50 border-b border-gray-200">
            <tr>
              <th className="table-th">Code</th>
              <th className="table-th">Customer Name</th>
              <th className="table-th">Contact</th>
              <th className="table-th">Primary Email</th>
              <th className="table-th">Phone</th>
              <th className="table-th">Status</th>
              <th className="table-th">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-100">
            {isLoading ? (
              <tr><td colSpan={7} className="table-td text-center py-8 text-gray-400">Loading...</td></tr>
            ) : filtered.length === 0 ? (
              <tr>
                <td colSpan={7} className="table-td text-center py-10 text-gray-400">
                  <Users size={32} className="mx-auto mb-2 opacity-30" />
                  No customers found.
                </td>
              </tr>
            ) : filtered.map(c => (
              <tr key={c.id} className="hover:bg-gray-50">
                <td className="table-td text-gray-500 font-mono text-xs">{c.customerCode}</td>
                <td className="table-td font-medium text-gray-900">{c.customerName}</td>
                <td className="table-td text-gray-600">
                  {c.primaryContactFirstName} {c.primaryContactLastName}
                </td>
                <td className="table-td text-gray-600">{c.primaryEmail}</td>
                <td className="table-td text-gray-500">{c.phoneNumber ?? c.mobileNumber ?? '—'}</td>
                <td className="table-td">
                  <span className={c.isActive ? 'badge-active' : 'badge-inactive'}>
                    {c.isActive ? 'Active' : 'Inactive'}
                  </span>
                </td>
                <td className="table-td">
                  <div className="flex items-center gap-1">
                    <button onClick={() => navigate(`/customers/${c.id}?companyId=${companyId}`)}
                      className="p-1.5 text-blue-500 hover:bg-blue-50 rounded" title="View">
                      <Eye size={15} />
                    </button>
                    <button onClick={() => { setUpdateError(''); setEditCustomer(c); }}
                      className="p-1.5 text-gray-500 hover:bg-gray-100 rounded" title="Edit">
                      <Pencil size={15} />
                    </button>
                    <button onClick={() => setToggleTarget(c)}
                      className={`p-1.5 rounded ${c.isActive ? 'text-yellow-600 hover:bg-yellow-50' : 'text-green-600 hover:bg-green-50'}`}
                      title={c.isActive ? 'Deactivate' : 'Activate'}>
                      {c.isActive ? <ToggleRight size={15} /> : <ToggleLeft size={15} />}
                    </button>
                    <button onClick={() => setDeleteId(c.id)}
                      className="p-1.5 text-red-500 hover:bg-red-50 rounded" title="Delete">
                      <Trash2 size={15} />
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <Modal isOpen={showCreate} onClose={() => setShowCreate(false)}
        title="Add Customer" subtitle="Create a new customer account. A portal login will be emailed automatically." size="xl">
        <CustomerForm
          companyId={companyId}
          onSubmit={d => createMutation.mutate(d)}
          onCancel={() => setShowCreate(false)}
          isLoading={createMutation.isPending}
          serverError={createError}
        />
      </Modal>

      <Modal isOpen={!!editCustomer} onClose={() => setEditCustomer(null)}
        title="Edit Customer" size="xl">
        {editCustomer && (
          <CustomerForm
            companyId={companyId}
            defaultValues={editCustomer}
            submitLabel="Save Changes"
            onSubmit={d => updateMutation.mutate({ ...d, id: editCustomer.id })}
            onCancel={() => setEditCustomer(null)}
            isLoading={updateMutation.isPending}
            serverError={updateError}
          />
        )}
      </Modal>

      <ConfirmDialog
        isOpen={!!toggleTarget}
        onClose={() => setToggleTarget(null)}
        onConfirm={() => toggleTarget && toggleMutation.mutate({ id: toggleTarget.id, isActive: !toggleTarget.isActive })}
        title={toggleTarget?.isActive ? 'Deactivate Customer' : 'Activate Customer'}
        message={toggleTarget?.isActive
          ? `Deactivate "${toggleTarget.customerName}"? Their portal login will also be disabled.`
          : `Activate "${toggleTarget?.customerName}"?`}
        isLoading={toggleMutation.isPending}
        confirmLabel={toggleTarget?.isActive ? 'Deactivate' : 'Activate'}
      />

      <ConfirmDialog
        isOpen={!!deleteId}
        onClose={() => setDeleteId(null)}
        onConfirm={() => deleteId && deleteMutation.mutate(deleteId)}
        title="Delete Customer"
        message="Are you sure you want to delete this customer? Their portal account will also be removed."
        isLoading={deleteMutation.isPending}
      />

      <Toast toast={toast} onClose={() => setToast(null)} />

      <RestoreDialog
        isOpen={!!inactiveTarget}
        message={inactiveTarget?.message ?? ''}
        onRestore={handleRestore}
        onCreateNew={handleCreateNew}
        onClose={() => setInactiveTarget(null)}
        isLoading={restoreMutation.isPending}
      />
    </div>
  );
}
