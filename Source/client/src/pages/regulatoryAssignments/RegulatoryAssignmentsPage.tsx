import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { Briefcase, Eye } from 'lucide-react';
import { employeeService } from '../../services/employeeService';
import type { SubscribingLevel } from '../../types';

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

export default function RegulatoryAssignmentsPage() {
  const navigate = useNavigate();

  const { data: assignments = [], isLoading } = useQuery({
    queryKey: ['my-assigned-work'],
    queryFn: () => employeeService.getMyAssignedWork(),
  });

  return (
    <div className="space-y-4">
      <div>
        <div className="flex items-center gap-3 mb-1">
          <Briefcase size={20} className="text-blue-800" />
          <h1 className="text-2xl font-bold text-gray-900">Regulatory Assignments</h1>
        </div>
        <p className="text-sm text-gray-500">Your assigned regulatory monitoring responsibilities.</p>
      </div>

      <div className="card overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="bg-gray-50 border-b border-gray-200">
                <th className="text-left px-3 py-3 font-medium text-gray-600">Customer</th>
                <th className="text-left px-3 py-3 font-medium text-gray-600">Level</th>
                <th className="text-left px-3 py-3 font-medium text-gray-600">Subscribed To</th>
                <th className="text-left px-3 py-3 font-medium text-gray-600">Gov. Entity</th>
                <th className="text-left px-3 py-3 font-medium text-gray-600">Status</th>
                <th className="text-left px-3 py-3 font-medium text-gray-600">Assigned Since</th>
                <th className="text-right px-3 py-3 font-medium text-gray-600">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {isLoading ? (
                <tr>
                  <td colSpan={7} className="text-center text-gray-400 py-10">Loading assignments...</td>
                </tr>
              ) : assignments.length === 0 ? (
                <tr>
                  <td colSpan={7} className="text-center text-gray-400 py-10">No regulatory assignments found.</td>
                </tr>
              ) : (
                assignments.map(a => (
                  <tr key={a.id} className="hover:bg-gray-50 transition-colors">
                    <td className="px-3 py-3 text-gray-800 font-medium">{a.customerName}</td>
                    <td className="px-3 py-3">{levelBadge(a.subscribingLevel)}</td>
                    <td className="px-3 py-3 text-gray-700 max-w-[200px] truncate" title={a.subscribedNodeName}>
                      {a.subscribedNodeName}
                    </td>
                    <td className="px-3 py-3 text-gray-600 max-w-[160px] truncate" title={a.governmentEntityName}>
                      {a.governmentEntityName}
                    </td>
                    <td className="px-3 py-3">
                      <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${a.isActive ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'}`}>
                        {a.isActive ? 'Active' : 'Inactive'}
                      </span>
                    </td>
                    <td className="px-3 py-3 text-gray-500 text-xs whitespace-nowrap">
                      {formatDate(a.assignedAt)}
                    </td>
                    <td className="px-3 py-3">
                      <div className="flex justify-end">
                        <button
                          onClick={() => navigate(`/regulatory-assignments/${a.id}`)}
                          className="text-blue-600 hover:text-blue-800 p-1 rounded"
                          title="View details"
                        >
                          <Eye size={16} />
                        </button>
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
