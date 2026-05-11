import { useMemo, useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import { employeeService } from '../../services/employeeService';
import type { CreateEmployeePayload, UpdateEmployeePayload, RestoreEmployeePayload } from '../../services/employeeService';
import type { Employee } from '../../types';
import { Plus, Search, Pencil, Trash2, Users, Eye } from 'lucide-react';
import Modal from '../../components/common/Modal';
import ConfirmDialog from '../../components/common/ConfirmDialog';
import RestoreDialog from '../../components/common/RestoreDialog';
import Toast, { type ToastState } from '../../components/common/Toast';
import { extractErrorMessage } from '../../utils/errors';
import EmployeeForm from './EmployeeForm';

type InactiveTarget = { existingId: string; pendingData: CreateEmployeePayload; message: string };

export default function EmployeesPage() {
  const { user } = useAuth();
  const companyId = user!.userId;
  const navigate = useNavigate();
  const qc = useQueryClient();

  const [search, setSearch] = useState('');
  const [showCreate, setShowCreate] = useState(false);
  const [editEmployee, setEditEmployee] = useState<Employee | null>(null);
  const [deleteId, setDeleteId] = useState<string | null>(null);
  const [inactiveTarget, setInactiveTarget] = useState<InactiveTarget | null>(null);
  const [toast, setToast] = useState<ToastState>(null);

  const { data, isLoading } = useQuery({
    queryKey: ['employees', companyId],
    queryFn: () => employeeService.getAll(companyId, undefined, 1, 500),
  });

  const filtered = useMemo(() => {
    const all = data?.items ?? [];
    if (!search.trim()) return all;
    const s = search.toLowerCase();
    return all.filter(e =>
      [e.employeeCode, e.firstName, e.lastName, e.primaryEmail,
        e.departmentName, e.divisionName, e.districtName,
        e.phoneNumber, e.mobileNumber,
        e.primaryCity, e.primaryState, e.primaryPostalCode,
        e.isActive ? 'active' : 'inactive']
        .some(v => v?.toLowerCase().includes(s))
    );
  }, [data, search]);

  const invalidate = () => qc.invalidateQueries({ queryKey: ['employees', companyId] });

  const createMutation = useMutation({
    mutationFn: (payload: CreateEmployeePayload) => employeeService.create(payload),
    onSuccess: () => {
      invalidate();
      setShowCreate(false);
      setToast({ type: 'success', message: 'Employee added successfully' });
    },
    onError: (err: unknown, variables) => {
      const res = (err as { response?: { data?: { isInactive?: boolean; existingId?: string; message?: string } } })?.response?.data;
      if (res?.isInactive && res.existingId) {
        setShowCreate(false);
        setInactiveTarget({ existingId: res.existingId, pendingData: variables, message: res.message ?? 'This employee exists but is inactive.' });
        return;
      }
      setToast({ type: 'error', message: extractErrorMessage(err, 'Failed to add employee. Please try again.') });
    }
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateEmployeePayload }) =>
      employeeService.update(id, payload),
    onSuccess: () => {
      invalidate();
      setEditEmployee(null);
      setToast({ type: 'success', message: 'Employee updated successfully' });
    },
    onError: (err) => setToast({ type: 'error', message: extractErrorMessage(err, 'Failed to update employee. Please try again.') }),
  });

  const restoreMutation = useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: RestoreEmployeePayload }) =>
      employeeService.restore(id, payload),
    onSuccess: () => {
      invalidate();
      setInactiveTarget(null);
      setToast({ type: 'success', message: 'Employee restored successfully' });
    },
    onError: (err) => setToast({ type: 'error', message: extractErrorMessage(err, 'Failed to restore employee.') }),
  });

  const deleteMutation = useMutation({
    mutationFn: employeeService.delete,
    onSuccess: () => {
      invalidate();
      setDeleteId(null);
      setToast({ type: 'success', message: 'Employee deleted successfully' });
    },
    onError: (err) => {
      setDeleteId(null);
      setToast({ type: 'error', message: extractErrorMessage(err, 'Failed to delete employee. Please try again.') });
    },
  });

  const handleRestore = () => {
    if (!inactiveTarget) return;
    const { existingId, pendingData } = inactiveTarget;
    restoreMutation.mutate({
      id: existingId,
      payload: {
        id: existingId,
        firstName: pendingData.firstName,
        lastName: pendingData.lastName,
        secondaryEmail: pendingData.secondaryEmail,
        phoneNumber: pendingData.phoneNumber,
        mobileNumber: pendingData.mobileNumber,
        departmentId: pendingData.departmentId,
        companyDivisionId: pendingData.companyDivisionId,
        companyDistrictId: pendingData.companyDistrictId,
        primaryAddress: pendingData.primaryAddress,
        primaryCity: pendingData.primaryCity,
        primaryState: pendingData.primaryState,
        primaryPostalCode: pendingData.primaryPostalCode,
        secondaryAddress: pendingData.secondaryAddress,
        secondaryCity: pendingData.secondaryCity,
        secondaryState: pendingData.secondaryState,
        secondaryPostalCode: pendingData.secondaryPostalCode,
      }
    });
  };

  const handleCreateNew = () => {
    if (!inactiveTarget) return;
    setInactiveTarget(null);
    createMutation.mutate({ ...inactiveTarget.pendingData, skipRestoreCheck: true });
    setShowCreate(false);
  };

  const handleUpdate = (data: CreateEmployeePayload | UpdateEmployeePayload) => {
    if (!editEmployee) return;
    updateMutation.mutate({ id: editEmployee.id, payload: data as UpdateEmployeePayload });
  };

  return (
    <div>
      <div className="flex items-center justify-between mb-1">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Employees</h1>
          <p className="text-sm text-gray-500">Manage employee records and access.</p>
        </div>
        <button onClick={() => setShowCreate(true)} className="btn-primary flex items-center gap-2">
          <Plus size={16} /> Add Employee
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
            Showing {filtered.length} of {data?.items.length ?? 0} employees
          </span>
        )}
      </div>

      <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
        <table className="w-full">
          <thead className="bg-gray-50 border-b border-gray-200">
            <tr>
              <th className="table-th">Code</th>
              <th className="table-th">Name</th>
              <th className="table-th">Department</th>
              <th className="table-th">Division</th>
              <th className="table-th">District</th>
              <th className="table-th">Email</th>
              <th className="table-th">Phone</th>
              <th className="table-th">Status</th>
              <th className="table-th">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-100">
            {isLoading ? (
              <tr><td colSpan={9} className="table-td text-center py-8 text-gray-400">Loading...</td></tr>
            ) : filtered.length === 0 ? (
              <tr>
                <td colSpan={9} className="table-td text-center py-10 text-gray-400">
                  <Users size={32} className="mx-auto mb-2 opacity-30" />
                  No employees found.
                </td>
              </tr>
            ) : filtered.map((e) => (
              <tr key={e.id} className="hover:bg-gray-50">
                <td className="table-td">
                  <span className="font-mono text-xs bg-gray-100 text-gray-700 px-2 py-0.5 rounded">
                    {e.employeeCode}
                  </span>
                </td>
                <td className="table-td font-medium text-gray-900">{e.fullName}</td>
                <td className="table-td text-gray-600">{e.departmentName ?? '—'}</td>
                <td className="table-td text-gray-500">{e.divisionName ?? '—'}</td>
                <td className="table-td text-gray-500">{e.districtName ?? '—'}</td>
                <td className="table-td text-gray-600">{e.primaryEmail}</td>
                <td className="table-td text-gray-500">{e.phoneNumber ?? e.mobileNumber ?? '—'}</td>
                <td className="table-td">
                  <span className={e.isActive ? 'badge-active' : 'badge-inactive'}>
                    {e.isActive ? 'Active' : 'Inactive'}
                  </span>
                </td>
                <td className="table-td">
                  <div className="flex items-center gap-1">
                    <button onClick={() => navigate(`/employees/${e.id}`)}
                      className="p-1.5 text-blue-500 hover:bg-blue-50 rounded" title="View details">
                      <Eye size={15} />
                    </button>
                    <button onClick={() => setEditEmployee(e)}
                      className="p-1.5 text-gray-500 hover:bg-gray-100 rounded" title="Edit">
                      <Pencil size={15} />
                    </button>
                    <button onClick={() => setDeleteId(e.id)}
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
        title="Add Employee" subtitle="Create a new employee account. A welcome email with login credentials will be sent automatically." size="xl">
        <EmployeeForm
          companyId={companyId}
          onSubmit={(d) => createMutation.mutate(d as CreateEmployeePayload)}
          onCancel={() => setShowCreate(false)}
          isLoading={createMutation.isPending}
        />
      </Modal>

      <Modal isOpen={!!editEmployee} onClose={() => setEditEmployee(null)}
        title="Edit Employee" size="xl">
        {editEmployee && (
          <EmployeeForm
            companyId={companyId}
            defaultValues={editEmployee}
            isEdit
            submitLabel="Save Changes"
            onSubmit={handleUpdate}
            onCancel={() => setEditEmployee(null)}
            isLoading={updateMutation.isPending}
          />
        )}
      </Modal>

      <ConfirmDialog
        isOpen={!!deleteId}
        onClose={() => setDeleteId(null)}
        onConfirm={() => deleteId && deleteMutation.mutate(deleteId)}
        title="Delete Employee"
        message="Are you sure you want to delete this employee? Their account will be deactivated."
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
