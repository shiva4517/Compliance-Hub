import { useEffect, useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { Eye, RotateCcw, Search } from 'lucide-react';
import { notificationHistoryService } from '../../services/notificationHistoryService';
import type { NotificationLog } from '../../types';
import { useAuth } from '../../contexts/AuthContext';

function StatusBadge({ status }: { status: NotificationLog['status'] }) {
  const styles: Record<string, string> = {
    Sent: 'bg-green-100 text-green-800',
    Failed: 'bg-red-100 text-red-800',
    Pending: 'bg-yellow-100 text-yellow-800',
  };
  return (
    <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ${styles[status] ?? 'bg-gray-100 text-gray-700'}`}>
      {status}
    </span>
  );
}

export default function NotificationsPage() {
  const navigate = useNavigate();
  const qc = useQueryClient();
  const { user } = useAuth();
  const companyId = user!.userId;

  const [search, setSearch] = useState('');
  const [searchInput, setSearchInput] = useState('');
  const [page, setPage] = useState(1);
  const [toast, setToast] = useState<{ type: 'success' | 'error'; message: string } | null>(null);

  const { data, isLoading } = useQuery({
    queryKey: ['notification-logs', companyId, search, page],
    queryFn: () => notificationHistoryService.getAll(companyId, search || undefined, page),
    enabled: !!companyId,
  });

  useEffect(() => {
    if (!companyId) return;
    notificationHistoryService.markSeen(companyId).then(() => {
      qc.invalidateQueries({ queryKey: ['notifications-unseen-count'] });
    });
  }, [companyId]);

  const retryMutation = useMutation({
    mutationFn: (id: string) => notificationHistoryService.retry(id, companyId),
    onSuccess: (response) => {
      setToast({
        type: response.data?.success ? 'success' : 'error',
        message: response.data?.message || response.message || 'Retry completed.',
      });
      qc.invalidateQueries({ queryKey: ['notification-logs'] });
    },
    onError: (error: any) => {
      setToast({
        type: 'error',
        message: error?.response?.data?.message || 'Retry failed. Please try again.',
      });
    },
  });

  const notifications = data?.items ?? [];

  const handleSearch = () => {
    setSearch(searchInput);
    setPage(1);
  };

  useEffect(() => {
    if (!toast) {
      return;
    }

    const timeoutId = window.setTimeout(() => {
      setToast(null);
    }, 4000);

    return () => window.clearTimeout(timeoutId);
  }, [toast]);

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between flex-wrap gap-3">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Notification History</h1>
          <p className="text-sm text-gray-500 mt-0.5">Email audit log for regulation change notifications.</p>
        </div>
        <div className="flex items-center gap-2">
          <div className="relative">
            <Search size={16} className="absolute left-2.5 top-1/2 -translate-y-1/2 text-gray-400" />
            <input
              type="text"
              placeholder="Search customer, regulation, status..."
              value={searchInput}
              onChange={e => setSearchInput(e.target.value)}
              onKeyDown={e => e.key === 'Enter' && handleSearch()}
              className="pl-8 pr-3 py-1.5 text-sm border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 w-72"
            />
          </div>
          <button onClick={handleSearch} className="btn-primary py-1.5 px-3 text-sm">
            Search
          </button>
        </div>
      </div>

      <div className="card overflow-hidden">
        {toast && (
          <div className={`mx-4 mt-4 rounded-lg border px-4 py-3 text-sm ${
            toast.type === 'success'
              ? 'border-green-200 bg-green-50 text-green-700'
              : 'border-red-200 bg-red-50 text-red-700'
          }`}>
            {toast.message}
          </div>
        )}
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="bg-gray-50 border-b border-gray-200">
                <th className="text-left px-4 py-3 font-medium text-gray-600">Customer</th>
                <th className="text-left px-4 py-3 font-medium text-gray-600">Previous Regulation</th>
                <th className="text-left px-4 py-3 font-medium text-gray-600">Present Regulation</th>
                <th className="text-left px-4 py-3 font-medium text-gray-600">Status</th>
                <th className="text-left px-4 py-3 font-medium text-gray-600">Sent At</th>
                <th className="text-right px-4 py-3 font-medium text-gray-600">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {isLoading ? (
                <tr>
                  <td colSpan={6} className="text-center text-gray-400 py-10">Loading...</td>
                </tr>
              ) : notifications.length === 0 ? (
                <tr>
                  <td colSpan={6} className="text-center text-gray-400 py-10">No notifications found.</td>
                </tr>
              ) : (
                notifications.map((n: NotificationLog) => (
                  <tr key={n.id} className="hover:bg-gray-50 transition-colors">
                    <td className="px-4 py-3">
                      <div className="font-medium text-gray-900">{n.customerName ?? '—'}</div>
                      {n.recipientEmail && (
                        <div className="text-xs text-gray-400">{n.recipientEmail}</div>
                      )}
                    </td>
                    <td className="px-4 py-3 text-gray-600">{n.previousRegulationName ?? '—'}</td>
                    <td className="px-4 py-3 text-gray-800 font-medium">{n.presentRegulationName ?? '—'}</td>
                    <td className="px-4 py-3">
                      <StatusBadge status={n.status} />
                      {n.retryCount > 0 && (
                        <span className="ml-1 text-xs text-gray-400">({n.retryCount} retries)</span>
                      )}
                    </td>
                    <td className="px-4 py-3 text-gray-500 text-xs whitespace-nowrap">
                      {n.createdAt ? new Date(n.  createdAt).toLocaleString() : '—'}
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex items-center justify-end gap-2">
                        <button
                          onClick={() => navigate(`/notifications/${n.id}`)}
                          className="text-blue-600 hover:text-blue-800 p-1 rounded"
                          title="View details"
                        >
                          <Eye size={16} />
                        </button>
                        {n.status !== 'Sent' && (
                          <button
                            onClick={() => retryMutation.mutate(n.id)}
                            disabled={retryMutation.isPending}
                            className="text-orange-600 hover:text-orange-800 p-1 rounded disabled:opacity-40"
                            title="Retry sending"
                          >
                            <RotateCcw size={16} />
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      {data && data.totalPages > 1 && (
        <div className="flex items-center justify-between text-sm text-gray-600">
          <span>Page {page} of {data.totalPages} ({data.totalCount} total)</span>
          <div className="flex gap-2">
            <button
              onClick={() => setPage(p => Math.max(1, p - 1))}
              disabled={!data.hasPreviousPage}
              className="btn-primary py-1 px-3 disabled:opacity-50"
            >
              Prev
            </button>
            <button
              onClick={() => setPage(p => p + 1)}
              disabled={!data.hasNextPage}
              className="btn-primary py-1 px-3 disabled:opacity-50"
            >
              Next
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
