import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { employeeService } from '../../services/employeeService';
import { customerService } from '../../services/customerService';
import { useAuth } from '../../contexts/AuthContext';
import { ArrowLeft, Users, Plus, Trash2, Eye, Layers, BookOpen, CalendarDays } from 'lucide-react';
import Modal from '../../components/common/Modal';
import ConfirmDialog from '../../components/common/ConfirmDialog';
import type { EmployeeAssignedWork } from '../../types';

type Tab = 'details' | 'assignments';

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

function HierarchyCell({ work }: { work: EmployeeAssignedWork }) {
  const parts = [
    work.governmentEntityName,
    work.agencyName,
    work.regulationCategoryName,
    work.regulationTypeName,
    work.regulationSubtypeName,
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

function AssignmentDetailModal({ work, onClose }: { work: EmployeeAssignedWork; onClose: () => void }) {
  return (
    <Modal isOpen title="Assignment Details" onClose={onClose} size="md">
      <div className="space-y-4 text-sm">
        <div className="grid grid-cols-2 gap-4">
          <div>
            <p className="text-xs text-gray-500 uppercase font-medium mb-0.5">Customer</p>
            <p className="font-medium text-gray-900">{work.customerName}</p>
          </div>
          <div>
            <p className="text-xs text-gray-500 uppercase font-medium mb-0.5">Level</p>
            <span className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium ${LEVEL_COLORS[work.subscribingLevel] ?? 'bg-gray-100 text-gray-600'}`}>
              <Layers size={10} />
              {LEVEL_LABELS[work.subscribingLevel] ?? work.subscribingLevel}
            </span>
          </div>
          <div className="col-span-2">
            <p className="text-xs text-gray-500 uppercase font-medium mb-0.5">Subscribed Node</p>
            <p className="font-medium text-gray-900">{work.subscribedNodeName}</p>
          </div>
          <div className="col-span-2">
            <p className="text-xs text-gray-500 uppercase font-medium mb-0.5">Regulation Hierarchy</p>
            <HierarchyCell work={work} />
          </div>
          <div>
            <p className="text-xs text-gray-500 uppercase font-medium mb-0.5">Assigned At</p>
            <p>{new Date(work.assignedAt).toLocaleString('en-US', { dateStyle: 'medium', timeStyle: 'short' })}</p>
          </div>
          <div>
            <p className="text-xs text-gray-500 uppercase font-medium mb-0.5">Assigned By</p>
            <p>{work.assignedBy}</p>
          </div>
          <div>
            <p className="text-xs text-gray-500 uppercase font-medium mb-0.5">Created At</p>
            <p>{new Date(work.createdAt).toLocaleString('en-US', { dateStyle: 'medium', timeStyle: 'short' })}</p>
          </div>
          <div>
            <p className="text-xs text-gray-500 uppercase font-medium mb-0.5">Created By</p>
            <p>{work.createdBy || '—'}</p>
          </div>
        </div>
        <div className="flex justify-end pt-2 border-t border-gray-200">
          <button onClick={onClose} className="btn-secondary">Close</button>
        </div>
      </div>
    </Modal>
  );
}

function AddAssignmentModal({
  employeeId, companyId, onClose, onSuccess,
}: { employeeId: string; companyId: string; onClose: () => void; onSuccess: () => void }) {
  const [selectedCustomerId, setSelectedCustomerId] = useState('');
  const [selectedSubscriptionId, setSelectedSubscriptionId] = useState('');

  const { data: customersData } = useQuery({
    queryKey: ['customers', companyId],
    queryFn: () => customerService.getAll(companyId, undefined, 1, 500),
  });

  const { data: subscriptions = [], isLoading: subsLoading } = useQuery({
    queryKey: ['available-subscriptions', employeeId, selectedCustomerId],
    queryFn: () => employeeService.getAvailableSubscriptions(employeeId, selectedCustomerId),
    enabled: !!selectedCustomerId,
  });

  const assignMutation = useMutation({
    mutationFn: () => employeeService.assignWork(employeeId, selectedSubscriptionId, selectedCustomerId),
    onSuccess,
  });

  const customers = customersData?.items ?? [];

  return (
    <Modal isOpen title="Add Regulatory Assignment" onClose={onClose} size="md">
      <div className="space-y-4">
        <div>
          <label className="form-label">Customer <span className="text-red-500">*</span></label>
          <select
            value={selectedCustomerId}
            onChange={e => { setSelectedCustomerId(e.target.value); setSelectedSubscriptionId(''); }}
            className="form-input"
          >
            <option value="">— Select a customer —</option>
            {customers.filter(c => c.isActive).map(c => (
              <option key={c.id} value={c.id}>{c.customerName}</option>
            ))}
          </select>
        </div>

        {selectedCustomerId && (
          <div>
            <label className="form-label">Subscription <span className="text-red-500">*</span></label>
            {subsLoading ? (
              <p className="text-sm text-gray-400 mt-1">Loading subscriptions...</p>
            ) : subscriptions.length === 0 ? (
              <p className="text-xs text-gray-400 mt-1">No available subscriptions for this customer.</p>
            ) : (
              <select
                value={selectedSubscriptionId}
                onChange={e => setSelectedSubscriptionId(e.target.value)}
                className="form-input"
              >
                <option value="">— Select a subscription —</option>
                {subscriptions.map(s => (
                  <option key={s.id} value={s.id}>
                    {LEVEL_LABELS[s.subscribingLevel] ?? s.subscribingLevel} — {s.subscribedNodeName} ({s.governmentEntityName})
                  </option>
                ))}
              </select>
            )}
          </div>
        )}

        {assignMutation.isError && (
          <p className="text-sm text-red-500">Failed to assign. Please try again.</p>
        )}

        <div className="flex justify-end gap-3 pt-2 border-t border-gray-200">
          <button onClick={onClose} className="btn-secondary">Cancel</button>
          <button
            onClick={() => assignMutation.mutate()}
            disabled={!selectedCustomerId || !selectedSubscriptionId || assignMutation.isPending}
            className="btn-primary"
          >
            {assignMutation.isPending ? 'Assigning...' : 'Assign'}
          </button>
        </div>
      </div>
    </Modal>
  );
}

export default function EmployeeViewPage() {
  const { id } = useParams<{ id: string }>();
  const { user } = useAuth();
  const navigate = useNavigate();
  const qc = useQueryClient();
  const companyId = user!.userId;

  const [activeTab, setActiveTab] = useState<Tab>('details');
  const [showAddAssignment, setShowAddAssignment] = useState(false);
  const [deleteWorkId, setDeleteWorkId] = useState<string | null>(null);
  const [detailWork, setDetailWork] = useState<EmployeeAssignedWork | null>(null);

  const { data: employee, isLoading, isError } = useQuery({
    queryKey: ['employee', id],
    queryFn: () => employeeService.getById(id!),
    enabled: !!id,
  });

  const { data: assignments = [], isLoading: assignmentsLoading } = useQuery({
    queryKey: ['employee-assigned-work', id],
    queryFn: () => employeeService.getAssignedWork(id!),
    enabled: !!id && activeTab === 'assignments',
  });

  const deleteMutation = useMutation({
    mutationFn: (workId: string) => employeeService.deleteAssignedWork(workId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['employee-assigned-work', id] });
      setDeleteWorkId(null);
    },
  });

  if (isLoading) {
    return <div className="flex items-center justify-center h-64 text-gray-400">Loading employee...</div>;
  }

  if (isError || !employee) {
    return (
      <div className="flex flex-col items-center justify-center h-64 text-gray-400 gap-2">
        <Users size={40} className="opacity-30" />
        <p>Employee not found.</p>
        <button onClick={() => navigate('/employees')} className="btn-secondary mt-2">Back to Employees</button>
      </div>
    );
  }

  const tabs: { key: Tab; label: string }[] = [
    { key: 'details', label: 'Details' },
    { key: 'assignments', label: `Regulatory Assignments${assignments.length > 0 ? ` (${assignments.length})` : ''}` },
  ];

  return (
    <div>
      {/* Header */}
      <div className="flex items-center gap-3 mb-1">
        <button onClick={() => navigate('/employees')}
          className="p-1.5 text-gray-500 hover:bg-gray-100 rounded" title="Back">
          <ArrowLeft size={18} />
        </button>
        <Users size={22} className="text-blue-800" />
        <h1 className="text-2xl font-bold text-gray-900">{employee.fullName}</h1>
        <span className="ml-1 text-xs font-mono text-gray-500 bg-gray-100 px-2 py-0.5 rounded">
          {employee.employeeCode}
        </span>
        <span className={`ml-2 text-xs px-2 py-0.5 rounded-full font-medium ${employee.isActive ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'}`}>
          {employee.isActive ? 'Active' : 'Inactive'}
        </span>
      </div>
      <p className="text-sm text-gray-500 mb-4 ml-10">
        {employee.primaryEmail} · {employee.companyName}
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
          <Section title="Employee Information">
            <Field label="Employee Code" value={employee.employeeCode} />
            <Field label="Full Name" value={employee.fullName} />
            <div className="col-span-2 flex items-center gap-6 text-xs text-gray-400">
              <span className="flex items-center gap-1">
                <CalendarDays size={12} />
                Created {new Date(employee.createdAt).toLocaleDateString('en-US', { year: 'numeric', month: 'long', day: 'numeric' })}
              </span>
            </div>
          </Section>

          <Section title="Contact">
            <Field label="Primary Email" value={employee.primaryEmail} />
            <Field label="Secondary Email" value={employee.secondaryEmail} />
            <Field label="Phone" value={employee.phoneNumber} />
            <Field label="Mobile" value={employee.mobileNumber} />
          </Section>

          <Section title="Organization">
            <Field label="Department" value={employee.departmentName} />
            <Field label="Division" value={employee.divisionName} />
            <Field label="District" value={employee.districtName} />
          </Section>

          <Section title="Primary Address">
            <div className="col-span-2">
              <Field label="Street Address" value={employee.primaryAddress} />
            </div>
            <Field label="City" value={employee.primaryCity} />
            <Field label="State" value={employee.primaryState} />
            <Field label="Postal Code" value={employee.primaryPostalCode} />
          </Section>

          {(employee.secondaryAddress || employee.secondaryCity || employee.secondaryState) && (
            <Section title="Secondary Address">
              <div className="col-span-2">
                <Field label="Street Address" value={employee.secondaryAddress} />
              </div>
              <Field label="City" value={employee.secondaryCity} />
              <Field label="State" value={employee.secondaryState} />
              <Field label="Postal Code" value={employee.secondaryPostalCode} />
            </Section>
          )}
        </div>
      )}

      {/* Regulatory Assignments Tab */}
      {activeTab === 'assignments' && (
        <div>
          <div className="flex items-center justify-between mb-4">
            <p className="text-sm text-gray-500">
              Regulatory subscriptions assigned to <strong>{employee.fullName}</strong>.
            </p>
            <button onClick={() => setShowAddAssignment(true)} className="btn-primary flex items-center gap-2 text-sm">
              <Plus size={15} /> Add Regulatory Assignment
            </button>
          </div>

          <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
            {assignmentsLoading ? (
              <p className="text-center text-gray-400 p-8">Loading assignments...</p>
            ) : assignments.length === 0 ? (
              <div className="flex flex-col items-center justify-center py-16 text-gray-400 gap-3">
                <BookOpen size={36} className="opacity-30" />
                <p className="text-sm font-medium text-gray-500">No regulatory assignments</p>
                <p className="text-xs text-center max-w-xs">
                  Assign regulatory subscriptions to track this employee's compliance responsibilities.
                </p>
              </div>
            ) : (
              <table className="w-full text-sm">
                <thead className="bg-gray-50 border-b border-gray-200">
                  <tr>
                    <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide">Customer</th>
                    <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide">Level</th>
                    <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide">Subscribed Node</th>
                    <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide">Hierarchy</th>
                    <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide">Assigned</th>
                    <th className="px-4 py-3"></th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100">
                  {assignments.map(work => (
                    <tr key={work.id} className="hover:bg-gray-50 transition-colors">
                      <td className="px-4 py-3 font-medium text-gray-900">{work.customerName}</td>
                      <td className="px-4 py-3">
                        <span className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium ${LEVEL_COLORS[work.subscribingLevel] ?? 'bg-gray-100 text-gray-600'}`}>
                          <Layers size={10} />
                          {LEVEL_LABELS[work.subscribingLevel] ?? work.subscribingLevel}
                        </span>
                      </td>
                      <td className="px-4 py-3 text-gray-700 max-w-[180px] truncate" title={work.subscribedNodeName}>
                        {work.subscribedNodeName}
                      </td>
                      <td className="px-4 py-3">
                        <HierarchyCell work={work} />
                      </td>
                      <td className="px-4 py-3 text-gray-500 text-xs whitespace-nowrap">
                        {new Date(work.assignedAt).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' })}
                      </td>
                      <td className="px-4 py-3">
                        <div className="flex items-center gap-1">
                          <button onClick={() => setDetailWork(work)}
                            className="p-1.5 text-gray-400 hover:text-blue-600 hover:bg-blue-50 rounded" title="View details">
                            <Eye size={14} />
                          </button>
                          <button onClick={() => setDeleteWorkId(work.id)}
                            className="p-1.5 text-red-400 hover:text-red-600 hover:bg-red-50 rounded" title="Remove">
                            <Trash2 size={14} />
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

      {showAddAssignment && (
        <AddAssignmentModal
          employeeId={id!}
          companyId={companyId}
          onClose={() => setShowAddAssignment(false)}
          onSuccess={() => {
            qc.invalidateQueries({ queryKey: ['employee-assigned-work', id] });
            setShowAddAssignment(false);
          }}
        />
      )}

      {detailWork && (
        <AssignmentDetailModal work={detailWork} onClose={() => setDetailWork(null)} />
      )}

      <ConfirmDialog
        isOpen={!!deleteWorkId}
        onClose={() => setDeleteWorkId(null)}
        onConfirm={() => deleteWorkId && deleteMutation.mutate(deleteWorkId)}
        title="Remove Assignment"
        message="Remove this regulatory assignment from the employee?"
        isLoading={deleteMutation.isPending}
        confirmLabel="Remove"
      />
    </div>
  );
}
