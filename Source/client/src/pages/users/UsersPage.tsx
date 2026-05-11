import { useMemo, useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { userService } from '../../services/userService';
import { companyService } from '../../services/companyService';
import type { User, CompanySummary } from '../../types';
import { Search, Eye, Pencil, Building2, Users, UserCheck } from 'lucide-react';
import Modal from '../../components/common/Modal';
import UserForm from './UserForm';
import { format } from 'date-fns';

function UserDetailModal({ user, onClose }: { user: User; onClose: () => void }) {
  return (
    <Modal isOpen onClose={onClose} title="User Details" size="md">
      <div className="space-y-3 text-sm">
        <div className="grid grid-cols-2 gap-3">
          <div>
            <p className="text-xs text-gray-400 mb-0.5">Full Name</p>
            <p className="font-medium text-gray-900">{user.fullName}</p>
          </div>
          <div>
            <p className="text-xs text-gray-400 mb-0.5">Role</p>
            <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-purple-100 text-purple-800">
              {user.roleName}
            </span>
          </div>
          <div>
            <p className="text-xs text-gray-400 mb-0.5">Email</p>
            <p className="text-gray-700">{user.email}</p>
          </div>
          <div>
            <p className="text-xs text-gray-400 mb-0.5">Status</p>
            <span className={user.isActive ? 'badge-active' : 'badge-inactive'}>
              {user.isActive ? 'Active' : 'Inactive'}
            </span>
          </div>
          {user.title && (
            <div>
              <p className="text-xs text-gray-400 mb-0.5">Title</p>
              <p className="text-gray-700">{user.title}</p>
            </div>
          )}
          {user.groupName && (
            <div>
              <p className="text-xs text-gray-400 mb-0.5">Security Group</p>
              <p className="text-gray-700">{user.groupName}</p>
            </div>
          )}
          <div>
            <p className="text-xs text-gray-400 mb-0.5">Last Login</p>
            <p className="text-gray-700">
              {user.lastLoginAt ? format(new Date(user.lastLoginAt), 'MMM d, yyyy · h:mm a') : 'Never'}
            </p>
          </div>
          <div>
            <p className="text-xs text-gray-400 mb-0.5">Created</p>
            <p className="text-gray-700">{format(new Date(user.createdAt), 'MMM d, yyyy')}</p>
          </div>
        </div>
      </div>
    </Modal>
  );
}

function CompanySummaryModal({ companyId, onClose }: { companyId: string; onClose: () => void }) {
  const { data, isLoading } = useQuery<CompanySummary>({
    queryKey: ['company-summary', companyId],
    queryFn: () => companyService.getSummary(companyId),
  });

  return (
    <Modal isOpen onClose={onClose} title="Company Details" size="md">
      {isLoading ? (
        <div className="space-y-3 animate-pulse">
          {Array.from({ length: 4 }).map((_, i) => (
            <div key={i} className="h-4 bg-gray-100 rounded w-3/4" />
          ))}
        </div>
      ) : data ? (
        <div className="space-y-4 text-sm">
          <div className="grid grid-cols-2 gap-3">
            <div className="col-span-2">
              <p className="text-xs text-gray-400 mb-0.5">Company Name</p>
              <p className="font-semibold text-gray-900">{data.company.companyName}</p>
            </div>
            <div>
              <p className="text-xs text-gray-400 mb-0.5">Company Code</p>
              <p className="text-gray-700">{data.company.companyCode}</p>
            </div>
            <div>
              <p className="text-xs text-gray-400 mb-0.5">Status</p>
              <span className={data.company.isActive ? 'badge-active' : 'badge-inactive'}>
                {data.company.isActive ? 'Active' : 'Inactive'}
              </span>
            </div>
            <div>
              <p className="text-xs text-gray-400 mb-0.5">Primary Email</p>
              <p className="text-gray-700">{data.company.primaryEmail}</p>
            </div>
            {data.company.phoneNumber && (
              <div>
                <p className="text-xs text-gray-400 mb-0.5">Phone</p>
                <p className="text-gray-700">{data.company.phoneNumber}</p>
              </div>
            )}
            <div className="col-span-2">
              <p className="text-xs text-gray-400 mb-0.5">Address</p>
              <p className="text-gray-700">
                {data.company.primaryAddress}, {data.company.primaryCity}, {data.company.primaryState} {data.company.primaryPostalCode}
              </p>
            </div>
          </div>
          <div className="grid grid-cols-2 gap-3 pt-3 border-t border-gray-100">
            <div className="flex items-center gap-3 bg-blue-50 rounded-lg p-3">
              <Users size={20} className="text-blue-600" />
              <div>
                <p className="text-lg font-bold text-gray-900">{data.customerCount}</p>
                <p className="text-xs text-gray-500">Customers</p>
              </div>
            </div>
            <div className="flex items-center gap-3 bg-violet-50 rounded-lg p-3">
              <UserCheck size={20} className="text-violet-600" />
              <div>
                <p className="text-lg font-bold text-gray-900">{data.employeeCount}</p>
                <p className="text-xs text-gray-500">Employees</p>
              </div>
            </div>
          </div>
        </div>
      ) : (
        <p className="text-sm text-gray-400">Company not found.</p>
      )}
    </Modal>
  );
}

export default function UsersPage() {
  const qc = useQueryClient();
  const [search, setSearch] = useState('');
  const [editUser, setEditUser] = useState<User | null>(null);
  const [viewSuperAdmin, setViewSuperAdmin] = useState<User | null>(null);
  const [viewAdminCompanyId, setViewAdminCompanyId] = useState<string | null>(null);

  const { data, isLoading } = useQuery({
    queryKey: ['users'],
    queryFn: () => userService.getAll(undefined, undefined, 1, 200),
  });

  const filtered = useMemo(() => {
    const all = (data?.items ?? []).filter(u =>
      u.roleName === 'SuperAdmin' || u.roleName === 'Admin'
    );
    if (!search.trim()) return all;
    const s = search.toLowerCase();
    return all.filter(u =>
      [u.firstName, u.lastName, u.email, u.title, u.roleName, u.groupName,
        u.isActive ? 'active' : 'inactive']
        .some(v => v?.toLowerCase().includes(s))
    );
  }, [data, search]);

  const updateMutation = useMutation({
    mutationFn: ({ id, ...payload }: User & { id: string }) => userService.update(id, payload),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['users'] }); setEditUser(null); }
  });

  const roleColors: Record<string, string> = {
    SuperAdmin: 'bg-purple-100 text-purple-800',
    Admin: 'bg-blue-100 text-blue-800',
  };

  const handleEye = (u: User) => {
    if (u.roleName === 'SuperAdmin') {
      setViewSuperAdmin(u);
    } else if (u.roleName === 'Admin' && u.userId) {
      setViewAdminCompanyId(u.userId);
    }
  };

  return (
    <div>
      <div className="flex items-center justify-between mb-1">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Users</h1>
          <p className="text-sm text-gray-500">Manage system users and their roles.</p>
        </div>
      </div>

      <div className="mt-4 mb-4 flex items-center gap-3">
        <div className="relative w-80">
          <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
          <input type="text" placeholder="Search users..." value={search}
            onChange={e => setSearch(e.target.value)}
            className="form-input pl-9" />
        </div>
        {!isLoading && (
          <span className="text-sm text-gray-500">
            Showing {filtered.length} of {(data?.items ?? []).filter(u => u.roleName === 'SuperAdmin' || u.roleName === 'Admin').length} users
          </span>
        )}
      </div>

      <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
        <table className="w-full">
          <thead className="bg-gray-50 border-b border-gray-200">
            <tr>
              <th className="table-th">Name</th>
              <th className="table-th">Email</th>
              <th className="table-th">Role</th>
              <th className="table-th">Group</th>
              <th className="table-th">Status</th>
              <th className="table-th">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-100">
            {isLoading ? (
              <tr><td colSpan={6} className="table-td text-center py-8 text-gray-400">Loading...</td></tr>
            ) : filtered.length === 0 ? (
              <tr><td colSpan={6} className="table-td text-center py-8 text-gray-400">No users found.</td></tr>
            ) : filtered.map((u) => (
              <tr key={u.id} className="hover:bg-gray-50">
                <td className="table-td font-medium text-gray-900">
                  {u.fullName}
                  {u.isForcePasswordChange && (
                    <span className="ml-2 text-xs text-amber-600 font-normal">(pw change required)</span>
                  )}
                </td>
                <td className="table-td">{u.email}</td>
                <td className="table-td">
                  <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${roleColors[u.roleName] ?? 'bg-gray-100 text-gray-800'}`}>
                    {u.roleName}
                  </span>
                </td>
                <td className="table-td text-gray-500">{u.groupName ?? '-'}</td>
                <td className="table-td">
                  <span className={u.isActive ? 'badge-active' : 'badge-inactive'}>
                    {u.isActive ? 'Active' : 'Inactive'}
                  </span>
                </td>
                <td className="table-td">
                  <div className="flex items-center gap-1">
                    <button
                      onClick={() => handleEye(u)}
                      className="p-1.5 text-blue-500 hover:bg-blue-50 rounded"
                      title={u.roleName === 'Admin' ? 'View company' : 'View user'}
                    >
                      {u.roleName === 'Admin' ? <Building2 size={15} /> : <Eye size={15} />}
                    </button>
                    <button onClick={() => setEditUser(u)} className="p-1.5 text-gray-500 hover:bg-gray-100 rounded"><Pencil size={15} /></button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {viewSuperAdmin && (
        <UserDetailModal user={viewSuperAdmin} onClose={() => setViewSuperAdmin(null)} />
      )}

      {viewAdminCompanyId && (
        <CompanySummaryModal companyId={viewAdminCompanyId} onClose={() => setViewAdminCompanyId(null)} />
      )}

      <Modal isOpen={!!editUser} onClose={() => setEditUser(null)} title="Edit User" size="lg">
        {editUser && (
          <UserForm
            defaultValues={editUser}
            isEdit
            onSubmit={(d) => updateMutation.mutate({ ...d, id: editUser.id } as unknown as User & { id: string })}
            onCancel={() => setEditUser(null)}
            isLoading={updateMutation.isPending}
            submitLabel="Save Changes"
          />
        )}
      </Modal>
    </div>
  );
}
