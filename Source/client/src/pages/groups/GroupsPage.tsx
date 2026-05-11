import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { groupService } from '../../services/groupService';
import type { SecurityGroup, UserRole } from '../../types';
import { UserRoles } from '../../types';
import { Pencil, Trash2 } from 'lucide-react';
import Modal from '../../components/common/Modal';
import ConfirmDialog from '../../components/common/ConfirmDialog';
import { useForm } from 'react-hook-form';

interface GroupFormData { groupName: string; description?: string; role: UserRole; isActive: boolean; }

function GroupForm({ defaultValues, onSubmit, onCancel, isLoading, submitLabel = 'Create Group' }:
  { defaultValues?: Partial<SecurityGroup>; onSubmit: (d: GroupFormData) => void; onCancel: () => void; isLoading?: boolean; submitLabel?: string }) {
  const { register, handleSubmit, formState: { errors } } = useForm<GroupFormData>({
    defaultValues: { groupName: defaultValues?.groupName ?? '', description: defaultValues?.description ?? '', role: defaultValues?.role ?? 'Employee', isActive: defaultValues?.isActive ?? true }
  });
  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      <div>
        <label className="form-label">Group Name <span className="text-red-500">*</span></label>
        <input {...register('groupName', { required: 'Required' })} className="form-input" />
        {errors.groupName && <p className="text-red-500 text-xs mt-1">{errors.groupName.message}</p>}
      </div>
      <div>
        <label className="form-label">Description</label>
        <textarea {...register('description')} className="form-input h-20 resize-none" />
      </div>
      <div>
        <label className="form-label">Role <span className="text-red-500">*</span></label>
        <select {...register('role', { required: 'Required' })} className="form-input">
          {UserRoles.map(r => <option key={r} value={r}>{r}</option>)}
        </select>
      </div>
      <label className="flex items-center gap-2 cursor-pointer">
        <input {...register('isActive')} type="checkbox" className="w-4 h-4 rounded" defaultChecked />
        <span className="text-sm text-gray-700">Active</span>
      </label>
      <div className="flex justify-end gap-3 pt-2 border-t border-gray-200">
        <button type="button" onClick={onCancel} className="btn-secondary">Cancel</button>
        <button type="submit" disabled={isLoading} className="btn-primary">
          {isLoading ? 'Saving...' : submitLabel}
        </button>
      </div>
    </form>
  );
}

export default function GroupsPage() {
  const qc = useQueryClient();
  const [showCreate, setShowCreate] = useState(false);
  const [editGroup, setEditGroup] = useState<SecurityGroup | null>(null);
  const [deleteId, setDeleteId] = useState<string | null>(null);

  const { data: groups = [], isLoading } = useQuery({ queryKey: ['groups'], queryFn: groupService.getAll });

  const createMutation = useMutation({
    mutationFn: groupService.create,
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['groups'] }); setShowCreate(false); }
  });
  const updateMutation = useMutation({
    mutationFn: ({ id, ...payload }: SecurityGroup) => groupService.update(id, payload as GroupFormData),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['groups'] }); setEditGroup(null); }
  });
  const deleteMutation = useMutation({
    mutationFn: groupService.delete,
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['groups'] }); setDeleteId(null); }
  });

  const roleColors: Record<string, string> = {
    SuperAdmin: 'bg-purple-100 text-purple-800',
    Admin: 'bg-blue-100 text-blue-800',
    Customer: 'bg-green-100 text-green-800',
    Employee: 'bg-orange-100 text-orange-800',
  };

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Groups</h1>
          <p className="text-sm text-gray-500">Manage security groups and roles.</p>
        </div>
        {/* <button onClick={() => setShowCreate(true)} className="btn-primary flex items-center gap-2">
          <Plus size={16} /> Add Group
        </button> */}
      </div>

      <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
        <table className="w-full">
          <thead className="bg-gray-50 border-b border-gray-200">
            <tr>
              <th className="table-th">Group Name</th>
              <th className="table-th">Description</th>
              <th className="table-th">Role</th>
              <th className="table-th">Users</th>
              <th className="table-th">Status</th>
              <th className="table-th">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-100">
            {isLoading ? (
              <tr><td colSpan={6} className="table-td text-center py-8 text-gray-400">Loading...</td></tr>
            ) : groups.map((g) => (
              <tr key={g.id} className="hover:bg-gray-50">
                <td className="table-td font-medium text-gray-900">{g.groupName}</td>
                <td className="table-td text-gray-500">{g.description ?? '-'}</td>
                <td className="table-td">
                  <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${roleColors[g.roleName] ?? 'bg-gray-100 text-gray-800'}`}>
                    {g.roleName}
                  </span>
                </td>
                <td className="table-td">{g.userCount}</td>
                <td className="table-td">
                  <span className={g.isActive ? 'badge-active' : 'badge-inactive'}>
                    {g.isActive ? 'Active' : 'Inactive'}
                  </span>
                </td>
                <td className="table-td">
                  <div className="flex items-center gap-1">
                    <button onClick={() => setEditGroup(g)} className="p-1.5 text-gray-500 hover:bg-gray-100 rounded"><Pencil size={15} /></button>
                    <button onClick={() => setDeleteId(g.id)} className="p-1.5 text-red-500 hover:bg-red-50 rounded"><Trash2 size={15} /></button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <Modal isOpen={showCreate} onClose={() => setShowCreate(false)} title="Add Group" size="md">
        <GroupForm onSubmit={(d) => createMutation.mutate(d)} onCancel={() => setShowCreate(false)} isLoading={createMutation.isPending} />
      </Modal>

      <Modal isOpen={!!editGroup} onClose={() => setEditGroup(null)} title="Edit Group" size="md">
        {editGroup && (
          <GroupForm defaultValues={editGroup} submitLabel="Save Changes"
            onSubmit={(d) => updateMutation.mutate({ ...editGroup, ...d })}
            onCancel={() => setEditGroup(null)} isLoading={updateMutation.isPending} />
        )}
      </Modal>

      <ConfirmDialog isOpen={!!deleteId} onClose={() => setDeleteId(null)}
        onConfirm={() => deleteId && deleteMutation.mutate(deleteId)}
        title="Delete Group" message="Are you sure you want to delete this group? Users in this group will be unassigned."
        isLoading={deleteMutation.isPending} />
    </div>
  );
}
