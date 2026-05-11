import { useMemo, useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { companyService } from '../../services/companyService';
import type { CompanyCreatePayload } from '../../services/companyService';
import type { Company } from '../../types';
import { Plus, Search, Pencil, Trash2, Building2 } from 'lucide-react';
import Modal from '../../components/common/Modal';
import ConfirmDialog from '../../components/common/ConfirmDialog';
import RestoreDialog from '../../components/common/RestoreDialog';
import Toast, { type ToastState } from '../../components/common/Toast';
import { extractErrorMessage } from '../../utils/errors';
import CompanyForm from './CompanyForm';

type InactiveTarget = { existingId: string; pendingData: CompanyCreatePayload; message: string };

export default function CompaniesPage() {
  const qc = useQueryClient();
  const [search, setSearch] = useState('');
  const [showCreate, setShowCreate] = useState(false);
  const [editCompany, setEditCompany] = useState<Company | null>(null);
  const [deleteId, setDeleteId] = useState<string | null>(null);
  const [inactiveTarget, setInactiveTarget] = useState<InactiveTarget | null>(null);
  const [toast, setToast] = useState<ToastState>(null);

  const { data, isLoading } = useQuery({
    queryKey: ['companies'],
    queryFn: () => companyService.getAll(undefined, 1, 500),
  });

  const filtered = useMemo(() => {
    const all = data?.items ?? [];
    if (!search.trim()) return all;
    const s = search.toLowerCase();
    return all.filter(c =>
      [c.companyCode, c.companyName, c.primaryEmail, c.secondaryEmail,
        c.phoneNumber, c.primaryCity, c.primaryState, c.primaryPostalCode,
        c.websiteUrl, c.isActive ? 'active' : 'inactive']
        .some(v => v?.toLowerCase().includes(s))
    );
  }, [data, search]);

  const createMutation = useMutation({
    mutationFn: (payload: CompanyCreatePayload) => companyService.create(payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['companies'] });
      setShowCreate(false);
      setToast({ type: 'success', message: 'Company added successfully' });
    },
    onError: (err: unknown, variables) => {
      const res = (err as { response?: { data?: { isInactive?: boolean; existingId?: string; message?: string } } })?.response?.data;
      if (res?.isInactive && res.existingId) {
        setShowCreate(false);
        setInactiveTarget({ existingId: res.existingId, pendingData: variables, message: res.message ?? 'This company exists but is inactive.' });
        return;
      }
      setToast({ type: 'error', message: extractErrorMessage(err, 'Failed to add company. Please try again.') });
    }
  });

  const restoreMutation = useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: CompanyCreatePayload }) =>
      companyService.restore(id, payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['companies'] });
      setInactiveTarget(null);
      setToast({ type: 'success', message: 'Company restored successfully' });
    },
    onError: (err) => setToast({ type: 'error', message: extractErrorMessage(err, 'Failed to restore company.') }),
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, ...payload }: Company) => companyService.update(id, payload as Company),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['companies'] });
      setEditCompany(null);
      setToast({ type: 'success', message: 'Company updated successfully' });
    },
    onError: (err) => setToast({ type: 'error', message: extractErrorMessage(err, 'Failed to update company. Please try again.') }),
  });

  const deleteMutation = useMutation({
    mutationFn: companyService.delete,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['companies'] });
      setDeleteId(null);
      setToast({ type: 'success', message: 'Company deleted successfully' });
    },
    onError: (err) => {
      setDeleteId(null);
      setToast({ type: 'error', message: extractErrorMessage(err, 'Failed to delete company. Please try again.') });
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
    createMutation.mutate({ ...data, skipRestoreCheck: true });
    setShowCreate(false);
  };

  return (
    <div>
      <div className="flex items-center justify-between mb-1">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Companies</h1>
          <p className="text-sm text-gray-500">Manage company records.</p>
        </div>
        <button onClick={() => setShowCreate(true)} className="btn-primary flex items-center gap-2">
          <Plus size={16} /> Add Company
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
            Showing {filtered.length} of {data?.items.length ?? 0} companies
          </span>
        )}
      </div>

      <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
        <table className="w-full">
          <thead className="bg-gray-50 border-b border-gray-200">
            <tr>
              <th className="table-th">Code</th>
              <th className="table-th">Company Name</th>
              <th className="table-th">Primary Email</th>
              <th className="table-th">Phone</th>
              <th className="table-th">City / State</th>
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
                  <Building2 size={32} className="mx-auto mb-2 opacity-30" />
                  No companies found.
                </td>
              </tr>
            ) : filtered.map((c) => (
              <tr key={c.id} className="hover:bg-gray-50">
                <td className="table-td">
                  <span className="font-mono text-xs bg-gray-100 text-gray-700 px-2 py-0.5 rounded">{c.companyCode}</span>
                </td>
                <td className="table-td font-medium text-gray-900">{c.companyName}</td>
                <td className="table-td text-gray-600">{c.primaryEmail}</td>
                <td className="table-td text-gray-500">{c.phoneNumber ?? '—'}</td>
                <td className="table-td text-gray-500">{c.primaryCity}, {c.primaryState}</td>
                <td className="table-td">
                  <span className={c.isActive ? 'badge-active' : 'badge-inactive'}>
                    {c.isActive ? 'Active' : 'Inactive'}
                  </span>
                </td>
                <td className="table-td">
                  <div className="flex items-center gap-1">
                    {/* <button onClick={() => navigate(`/company-divisions?companyId=${c.id}`)}
                      className="p-1.5 text-blue-500 hover:bg-blue-50 rounded" title="Divisions">
                      <Network size={15} />
                    </button> */}
                    <button onClick={() => setEditCompany(c)} className="p-1.5 text-gray-500 hover:bg-gray-100 rounded" title="Edit">
                      <Pencil size={15} />
                    </button>
                    <button onClick={() => setDeleteId(c.id)} className="p-1.5 text-red-500 hover:bg-red-50 rounded" title="Delete">
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
        title="Add Company" subtitle="Enter the new company information." size="xl">
        <CompanyForm
          onSubmit={(d) => createMutation.mutate(d)}
          onCancel={() => setShowCreate(false)}
          isLoading={createMutation.isPending}
        />
      </Modal>

      <Modal isOpen={!!editCompany} onClose={() => setEditCompany(null)}
        title="Edit Company" size="xl">
        {editCompany && (
          <CompanyForm
            defaultValues={editCompany}
            isEdit
            submitLabel="Save Changes"
            onSubmit={(d) => updateMutation.mutate({ ...d, id: editCompany.id, companyCode: editCompany.companyCode, createdAt: editCompany.createdAt })}
            onCancel={() => setEditCompany(null)}
            isLoading={updateMutation.isPending}
          />
        )}
      </Modal>

      <ConfirmDialog
        isOpen={!!deleteId}
        onClose={() => setDeleteId(null)}
        onConfirm={() => deleteId && deleteMutation.mutate(deleteId)}
        title="Delete Company"
        message="Are you sure you want to delete this company? This action cannot be undone."
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
