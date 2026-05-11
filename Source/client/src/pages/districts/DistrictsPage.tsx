import { useMemo, useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useAuth } from '../../contexts/AuthContext';
import { districtService } from '../../services/districtService';
import type { District } from '../../types';
import { Plus, Search, Pencil, Network, ToggleLeft, ToggleRight } from 'lucide-react';
import Modal from '../../components/common/Modal';
import ConfirmDialog from '../../components/common/ConfirmDialog';
import DistrictForm from './DistrictForm';

export default function DistrictsPage() {
  const { user } = useAuth();
  const companyId = user!.userId;

  const qc = useQueryClient();
  const [search, setSearch] = useState('');
  const [showCreate, setShowCreate] = useState(false);
  const [editDistrict, setEditDistrict] = useState<District | null>(null);
  const [toggleTarget, setToggleTarget] = useState<District | null>(null);
  const [createError, setCreateError] = useState('');
  const [updateError, setUpdateError] = useState('');

  const { data, isLoading } = useQuery({
    queryKey: ['districts', companyId],
    queryFn: () => districtService.getAll(companyId, undefined, 1, 500),
  });

  const filtered = useMemo(() => {
    const all = data?.items ?? [];
    if (!search.trim()) return all;
    const s = search.toLowerCase();
    return all.filter(d =>
      [d.name, d.description, d.isActive ? 'active' : 'inactive']
        .some(v => v?.toLowerCase().includes(s))
    );
  }, [data, search]);

  const createMutation = useMutation({
    mutationFn: (payload: { name: string; description?: string }) =>
      districtService.create({ companyId, ...payload }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['districts', companyId] });
      setShowCreate(false);
      setCreateError('');
    },
    onError: (err: unknown) => {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message;
      setCreateError(msg ?? 'Failed to create district.');
    }
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, ...payload }: { id: string; name: string; description?: string }) =>
      districtService.update(id, payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['districts', companyId] });
      setEditDistrict(null);
      setUpdateError('');
    },
    onError: (err: unknown) => {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message;
      setUpdateError(msg ?? 'Failed to update district.');
    }
  });

  const toggleMutation = useMutation({
    mutationFn: ({ id, isActive }: { id: string; isActive: boolean }) =>
      districtService.toggleStatus(id, isActive),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['districts', companyId] });
      setToggleTarget(null);
    }
  });

  return (
    <div>
      <div className="flex items-center justify-between mb-1">
        <div>
          <div className="flex items-center gap-3">
            <Network size={22} className="text-blue-800" />
            <h1 className="text-2xl font-bold text-gray-900">Company Districts</h1>
          </div>
          <p className="text-sm text-gray-500 mt-0.5">Define and manage geographic district units for your company.</p>
        </div>
        <button onClick={() => { setCreateError(''); setShowCreate(true); }}
          className="btn-primary flex items-center gap-2">
          <Plus size={16} /> Add District
        </button>
      </div>

      <div className="mt-4 mb-4 flex items-center gap-3">
        <div className="relative w-80">
          <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
          <input type="text" placeholder="Search districts..."
            value={search} onChange={e => setSearch(e.target.value)}
            className="form-input pl-9" />
        </div>
        {!isLoading && (
          <span className="text-sm text-gray-500">
            Showing {filtered.length} of {data?.items.length ?? 0} districts
          </span>
        )}
      </div>

      <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
        <table className="w-full">
          <thead className="bg-gray-50 border-b border-gray-200">
            <tr>
              <th className="table-th">District Name</th>
              <th className="table-th">Description</th>
              <th className="table-th">Status</th>
              <th className="table-th">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-100">
            {isLoading ? (
              <tr><td colSpan={4} className="table-td text-center py-8 text-gray-400">Loading...</td></tr>
            ) : filtered.length === 0 ? (
              <tr>
                <td colSpan={4} className="table-td text-center py-10 text-gray-400">
                  <Network size={32} className="mx-auto mb-2 opacity-30" />
                  No districts found.
                </td>
              </tr>
            ) : filtered.map(d => (
              <tr key={d.id} className="hover:bg-gray-50">
                <td className="table-td font-medium text-gray-900">{d.name}</td>
                <td className="table-td text-gray-500 max-w-xs truncate">{d.description ?? '—'}</td>
                <td className="table-td">
                  <span className={d.isActive ? 'badge-active' : 'badge-inactive'}>
                    {d.isActive ? 'Active' : 'Inactive'}
                  </span>
                </td>
                <td className="table-td">
                  <div className="flex items-center gap-1">
                    <button onClick={() => { setUpdateError(''); setEditDistrict(d); }}
                      className="p-1.5 text-gray-500 hover:bg-gray-100 rounded" title="Edit">
                      <Pencil size={15} />
                    </button>
                    <button onClick={() => setToggleTarget(d)}
                      className={`p-1.5 rounded ${d.isActive ? 'text-yellow-600 hover:bg-yellow-50' : 'text-green-600 hover:bg-green-50'}`}
                      title={d.isActive ? 'Deactivate' : 'Activate'}>
                      {d.isActive ? <ToggleRight size={15} /> : <ToggleLeft size={15} />}
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <Modal isOpen={showCreate} onClose={() => setShowCreate(false)}
        title="Add District" subtitle="Create a new district for your company." size="md">
        <DistrictForm
          onSubmit={d => createMutation.mutate(d)}
          onCancel={() => setShowCreate(false)}
          isLoading={createMutation.isPending}
          serverError={createError}
        />
      </Modal>

      <Modal isOpen={!!editDistrict} onClose={() => setEditDistrict(null)}
        title="Edit District" size="md">
        {editDistrict && (
          <DistrictForm
            defaultValues={editDistrict}
            submitLabel="Save Changes"
            onSubmit={d => updateMutation.mutate({ id: editDistrict.id, ...d })}
            onCancel={() => setEditDistrict(null)}
            isLoading={updateMutation.isPending}
            serverError={updateError}
          />
        )}
      </Modal>

      <ConfirmDialog
        isOpen={!!toggleTarget}
        onClose={() => setToggleTarget(null)}
        onConfirm={() => toggleTarget && toggleMutation.mutate({ id: toggleTarget.id, isActive: !toggleTarget.isActive })}
        title={toggleTarget?.isActive ? 'Deactivate District' : 'Activate District'}
        message={toggleTarget?.isActive
          ? `Deactivate "${toggleTarget.name}"?`
          : `Activate "${toggleTarget?.name}"?`}
        isLoading={toggleMutation.isPending}
        confirmLabel={toggleTarget?.isActive ? 'Deactivate' : 'Activate'}
      />
    </div>
  );
}
