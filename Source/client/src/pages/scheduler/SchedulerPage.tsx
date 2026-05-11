import { useMemo, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { ArrowDownUp, CalendarClock, Eye, RefreshCw, Search } from 'lucide-react';
import Modal from '../../components/common/Modal';
import { schedulerService } from '../../services/schedulerService';
import type { RegulationSyncSchedulerHistory } from '../../types';

function formatDateTime(value: string) {
  return new Date(value).toLocaleString('en-US', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
  });
}

function triggerSourceClassName(source: string) {
  return source.toLowerCase() === 'manual'
    ? 'bg-indigo-100 text-indigo-700'
    : 'bg-gray-100 text-gray-600';
}

function buildSearchValue(row: RegulationSyncSchedulerHistory) {
  return [
    row.governmentEntityName,
    row.titleNumber,
    row.operationType,
    row.status,
    row.triggerSource,
    row.importedRecordsCount,
    row.changedRecordsCount,
    row.deactivatedRecordsCount,
    row.outboxEventsQueuedCount,
    row.details ?? '',
    formatDateTime(row.startedAt),
    formatDateTime(row.completedAt),
  ]
    .join(' ')
    .toLowerCase();
}

function statusClassName(status: string) {
  switch (status.toLowerCase()) {
    case 'completed':
      return 'bg-green-100 text-green-700';
    case 'nochangesdetected':
      return 'bg-amber-100 text-amber-700';
    case 'failed':
      return 'bg-red-100 text-red-700';
    case 'skipped':
      return 'bg-gray-100 text-gray-700';
    default:
      return 'bg-blue-100 text-blue-700';
  }
}

export default function SchedulerPage() {
  const [sortDir, setSortDir] = useState<'asc' | 'desc'>('desc');
  const [filter, setFilter] = useState('');
  const [selectedHistory, setSelectedHistory] = useState<RegulationSyncSchedulerHistory | null>(null);

  const { data, isLoading, isFetching, refetch } = useQuery({
    queryKey: ['scheduler-history', sortDir],
    queryFn: () =>
      schedulerService.getHistory({
        sortBy: 'CompletedAt',
        sortDir,
        pageNumber: 1,
        pageSize: 0,
      }),
    refetchOnMount: 'always',
    refetchOnWindowFocus: true,
    refetchInterval: 30000,
  });

  const items = data?.items ?? [];
  const normalizedFilter = filter.trim().toLowerCase();

  const filteredItems = useMemo(() => {
    if (!normalizedFilter) {
      return items;
    }

    return items.filter((row) => buildSearchValue(row).includes(normalizedFilter));
  }, [items, normalizedFilter]);

  const toggleDateSort = () => {
    setSortDir((current) => (current === 'asc' ? 'desc' : 'asc'));
  };

  return (
    <div className="space-y-4">
      <div className="flex items-start justify-between gap-3 flex-wrap">
        <div>
          <div className="flex items-center gap-3 mb-1">
            <CalendarClock size={22} className="text-blue-800" />
            <h1 className="text-2xl font-bold text-gray-900">Scheduler Run History</h1>
          </div>
          <p className="text-sm text-gray-500">
            Live execution history for regulation imports and sync runs.
          </p>
        </div>

        <button
          onClick={() => void refetch()}
          className="btn-secondary flex items-center gap-2"
          disabled={isFetching}
        >
          <RefreshCw size={14} className={isFetching ? 'animate-spin' : ''} />
          Refresh
        </button>
      </div>

      <div className="card p-4">
        <div className="relative max-w-md">
          <Search size={15} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
          <input
            type="text"
            value={filter}
            onChange={(e) => setFilter(e.target.value)}
            placeholder="Filter across all columns"
            className="form-input pl-9"
          />
        </div>
      </div>

      <div className="card overflow-hidden p-0">
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="bg-gray-50 border-b border-gray-200">
                <th className="table-th">Title</th>
                <th className="table-th">Operation</th>
                <th className="table-th">Status</th>
                <th className="table-th">Trigger</th>
                <th className="table-th">Started At</th>
                <th
                  className="table-th cursor-pointer select-none hover:text-gray-800"
                  onClick={toggleDateSort}
                >
                  <span className="inline-flex items-center gap-1">
                    Executed At
                    <ArrowDownUp size={13} className="text-blue-700" />
                  </span>
                </th>
                <th className="table-th text-center">View</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {isLoading ? (
                <tr>
                  <td colSpan={7} className="table-td text-center py-10 text-gray-400">
                    Loading scheduler history...
                  </td>
                </tr>
              ) : filteredItems.length === 0 ? (
                <tr>
                  <td colSpan={7} className="table-td text-center py-10 text-gray-400">
                    {items.length === 0 ? 'No scheduler history found.' : 'No rows match the current filter.'}
                  </td>
                </tr>
              ) : (
                filteredItems.map((row) => (
                  <tr key={row.id} className="hover:bg-gray-50 transition-colors">
                    <td className="table-td font-medium text-gray-900 whitespace-nowrap">
                      Title {row.titleNumber}
                    </td>
                    <td className="table-td text-gray-600 whitespace-nowrap">{row.operationType}</td>
                    <td className="table-td">
                      <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${statusClassName(row.status)}`}>
                        {row.status}
                      </span>
                    </td>
                    <td className="table-td">
                      <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${triggerSourceClassName(row.triggerSource)}`}>
                        {row.triggerSource}
                      </span>
                    </td>
                    <td className="table-td text-gray-600 whitespace-nowrap">
                      {formatDateTime(row.startedAt)}
                    </td>
                    <td className="table-td text-gray-600 whitespace-nowrap">{formatDateTime(row.completedAt)}</td>
                    <td className="table-td text-center">
                      <button
                        type="button"
                        onClick={() => setSelectedHistory(row)}
                        className="inline-flex h-9 w-9 items-center justify-center rounded-md border border-gray-200 text-gray-500 transition-colors hover:border-blue-200 hover:bg-blue-50 hover:text-blue-700"
                        title="View full run details"
                        aria-label={`View details for title ${row.titleNumber}`}
                      >
                        <Eye size={16} />
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      <div className="text-sm text-gray-500">
        Showing {filteredItems.length} of {items.length} loaded run history record(s).
      </div>

      <Modal
        isOpen={selectedHistory !== null}
        onClose={() => setSelectedHistory(null)}
        title={selectedHistory ? `Scheduler Run Details - Title ${selectedHistory.titleNumber}` : 'Scheduler Run Details'}
        subtitle={selectedHistory?.governmentEntityName}
        size="lg"
      >
        {selectedHistory && (
          <div className="space-y-6">
            <div className="grid gap-4 sm:grid-cols-2">
              <div className="rounded-lg border border-gray-200 p-4">
                <div className="text-xs font-medium uppercase tracking-wide text-gray-400">Status</div>
                <div className="mt-2">
                  <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${statusClassName(selectedHistory.status)}`}>
                    {selectedHistory.status}
                  </span>
                </div>
              </div>
              <div className="rounded-lg border border-gray-200 p-4">
                <div className="text-xs font-medium uppercase tracking-wide text-gray-400">Operation</div>
                <div className="mt-2 text-sm font-medium text-gray-900">{selectedHistory.operationType}</div>
              </div>
              <div className="rounded-lg border border-gray-200 p-4">
                <div className="text-xs font-medium uppercase tracking-wide text-gray-400">Trigger Source</div>
                <div className="mt-2">
                  <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${triggerSourceClassName(selectedHistory.triggerSource)}`}>
                    {selectedHistory.triggerSource}
                  </span>
                </div>
              </div>
              <div className="rounded-lg border border-gray-200 p-4">
                <div className="text-xs font-medium uppercase tracking-wide text-gray-400">Started At</div>
                <div className="mt-2 text-sm text-gray-900">{formatDateTime(selectedHistory.startedAt)}</div>
              </div>
              <div className="rounded-lg border border-gray-200 p-4">
                <div className="text-xs font-medium uppercase tracking-wide text-gray-400">Completed At</div>
                <div className="mt-2 text-sm text-gray-900">{formatDateTime(selectedHistory.completedAt)}</div>
              </div>
            </div>

            <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
              <div className="rounded-lg border border-gray-200 p-4">
                <div className="text-xs font-medium uppercase tracking-wide text-gray-400">Imported</div>
                <div className="mt-2 text-2xl font-semibold text-gray-900">{selectedHistory.importedRecordsCount}</div>
              </div>
              <div className="rounded-lg border border-gray-200 p-4">
                <div className="text-xs font-medium uppercase tracking-wide text-gray-400">Changed</div>
                <div className="mt-2 text-2xl font-semibold text-gray-900">{selectedHistory.changedRecordsCount}</div>
              </div>
              <div className="rounded-lg border border-gray-200 p-4">
                <div className="text-xs font-medium uppercase tracking-wide text-gray-400">Deactivated</div>
                <div className="mt-2 text-2xl font-semibold text-gray-900">{selectedHistory.deactivatedRecordsCount}</div>
              </div>
              <div className="rounded-lg border border-gray-200 p-4">
                <div className="text-xs font-medium uppercase tracking-wide text-gray-400">Outbox Events</div>
                <div className="mt-2 text-2xl font-semibold text-gray-900">{selectedHistory.outboxEventsQueuedCount}</div>
              </div>
            </div>

            <div className="grid gap-4 sm:grid-cols-2">
              <div className="rounded-lg border border-gray-200 p-4">
                <div className="text-xs font-medium uppercase tracking-wide text-gray-400">Government Entity</div>
                <div className="mt-2 text-sm text-gray-900">{selectedHistory.governmentEntityName}</div>
              </div>
              <div className="rounded-lg border border-gray-200 p-4">
                <div className="text-xs font-medium uppercase tracking-wide text-gray-400">Title Number</div>
                <div className="mt-2 text-sm text-gray-900">{selectedHistory.titleNumber}</div>
              </div>
            </div>

            <div className="rounded-lg border border-gray-200 p-4">
              <div className="text-xs font-medium uppercase tracking-wide text-gray-400">Details</div>
              <div className="mt-2 whitespace-pre-wrap break-words text-sm text-gray-700">
                {selectedHistory.details?.trim() ? selectedHistory.details : 'No additional details available for this run.'}
              </div>
            </div>
          </div>
        )}
      </Modal>
    </div>
  );
}
