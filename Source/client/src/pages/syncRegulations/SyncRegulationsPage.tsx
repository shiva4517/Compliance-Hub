import { useEffect, useRef, useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { DatabaseZap, FlaskConical, RefreshCw, TriangleAlert } from 'lucide-react';
import { regulationService } from '../../services/regulationService';
import { api } from '../../services/api';
import { useAuth } from '../../contexts/AuthContext';
import type { GovernmentEntity } from '../../types';
import SimulateRegulationChangesModal from './SimulateRegulationChangesModal';

interface SyncSummary {
  sectionsAdded: number;
  sectionsUpdated: number;
  sectionsDeactivated: number;
  outboxEventsQueued: number;
  duration: string;
  error?: string;
}

export default function SyncRegulationsPage() {
  const qc = useQueryClient();
  const { user } = useAuth();
  const isSuperAdmin = user?.role === 'SuperAdmin';
  const [selectedId, setSelectedId] = useState<string>('');
  const [seeded, setSeeded] = useState(false);
  const [showSimulateModal, setShowSimulateModal] = useState(false);
  const [simulateToast, setSimulateToast] = useState<string | null>(null);

  interface LogEntry {
    message: string;
    level: 'info' | 'success' | 'warning' | 'error';
  }

  type LogFilter = 'all' | LogEntry['level'];

  // Live log state
  const [logs, setLogs] = useState<LogEntry[]>([]);
  const [logFilter, setLogFilter] = useState<LogFilter>('all');
  const [isStreaming, setIsStreaming] = useState(false);
  const [summary, setSummary] = useState<SyncSummary | null>(null);
  const [streamError, setStreamError] = useState<string | null>(null);
  const logContainerRef = useRef<HTMLDivElement>(null);
  const userScrolledUp = useRef(false);
  const isProgrammaticScroll = useRef(false);

  const { data: listData, isLoading: entitiesLoading } = useQuery({
    queryKey: ['regulations', 'all'],
    queryFn: () => regulationService.getAll({ pageSize: 500 }),
  });

  const seedMutation = useMutation({
    mutationFn: regulationService.seedFromEcfr,
    onSuccess: () => {
      setSeeded(true);
      qc.invalidateQueries({ queryKey: ['regulations', 'all'] });
    },
  });

  const entities: GovernmentEntity[] = listData?.items ?? [];

  useEffect(() => {
    if (!entitiesLoading && entities.length === 0 && !seeded && !seedMutation.isPending) {
      seedMutation.mutate();
    }
  }, [entitiesLoading, entities.length, seeded, seedMutation]);

  // Auto-scroll to bottom when logs change or filter changes, unless user has scrolled up
  useEffect(() => {
    if (!userScrolledUp.current) {
      const el = logContainerRef.current;
      if (el) {
        isProgrammaticScroll.current = true;
        el.scrollTop = el.scrollHeight;
      }
    }
  }, [logs, logFilter]);

  const handleLogScroll = () => {
    if (isProgrammaticScroll.current) {
      isProgrammaticScroll.current = false;
      return;
    }
    const el = logContainerRef.current;
    if (!el) return;
    const atBottom = el.scrollHeight - el.scrollTop - el.clientHeight < 40;
    userScrolledUp.current = !atBottom;
  };

  const isLoading = entitiesLoading || seedMutation.isPending;
  const selected = entities.find(e => e.id === selectedId);

  const handleSync = async () => {
    if (!selectedId || isStreaming) return;

    setLogs([]);
    setSummary(null);
    setStreamError(null);
    setIsStreaming(true);
    userScrolledUp.current = false;

    try {
      const { data: startData } = await api.post(`regulations/${selectedId}/sync-start`);
      const { jobId } = startData;

      const poll = setInterval(async () => {
        try {
          const { data } = await api.get(`regulations/${selectedId}/sync-status/${jobId}`);
          setLogs(data.logs ?? []);

          if (data.status === 'done') {
            clearInterval(poll);
            setSummary(data.summary ?? null);
            setIsStreaming(false);
            qc.invalidateQueries({ queryKey: ['regulations', 'all'] });
          } else if (data.status === 'error') {
            clearInterval(poll);
            setStreamError(data.error ?? 'Unknown error');
            setIsStreaming(false);
            qc.invalidateQueries({ queryKey: ['regulations', 'all'] });
          }
        } catch (err) {
          clearInterval(poll);
          setStreamError(err instanceof Error ? err.message : 'Polling error');
          setIsStreaming(false);
        }
      }, 2000);

    } catch (err) {
      setStreamError(err instanceof Error ? err.message : 'Unknown error');
      setIsStreaming(false);
    }
  };

  return (
    <div className="max-w-2xl">
      <div className="mb-6">
        <div className="flex items-center gap-2 mb-1">
          <DatabaseZap size={20} className="text-blue-800" />
          <h1 className="text-xl font-semibold text-gray-900">Sync Regulations</h1>
        </div>
        <p className="text-sm text-gray-500">
          Select a government entity and trigger a regulation sync from eCFR.
        </p>
      </div>

      <div className="card p-6 space-y-6">
        <div>
          <label htmlFor="government-entity" className="block text-sm font-medium text-gray-700 mb-1">
            Government Entity
          </label>
          {isLoading ? (
            <div className="flex items-center gap-2 text-sm text-gray-500">
              <RefreshCw size={14} className="animate-spin" />
              {seedMutation.isPending ? 'Loading titles from eCFR…' : 'Loading…'}
            </div>
          ) : (
            <select
              id="government-entity"
              value={selectedId}
              onChange={e => setSelectedId(e.target.value)}
              className="w-full border border-gray-300 rounded-md px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              <option value="">— Select a government entity —</option>
              {entities.map(entity => (
                <option key={entity.id} value={entity.id}>
                  Title {entity.titleNumber} — {entity.titleName}
                </option>
              ))}
            </select>
          )}
        </div>

        {selected && (
          <div className="rounded-md border border-gray-200 overflow-hidden text-sm">
            {[
              { label: 'Title',        value: String(selected.titleNumber) },
              { label: 'Name',         value: selected.titleName },
              { label: 'Source',       value: selected.source ?? '—' },
            ].map(({ label, value }) => (
              <div key={label} className="flex border-b border-gray-200 last:border-b-0">
                <span className="w-36 shrink-0 bg-gray-50 px-4 py-2 text-gray-500 font-medium border-r border-gray-200">{label}</span>
                <span className="px-4 py-2 text-gray-900">{value}</span>
              </div>
            ))}
            <div className="flex border-b border-gray-200">
              <span className="w-36 shrink-0 bg-gray-50 px-4 py-2 text-gray-500 font-medium border-r border-gray-200">Status</span>
              <span className={`px-4 py-2 font-medium ${selected.isImported ? 'text-green-600' : 'text-gray-400'}`}>
                {selected.isImported ? 'Imported' : 'Not imported'}
              </span>
            </div>
            <div className="flex border-b border-gray-200">
              <span className="w-36 shrink-0 bg-gray-50 px-4 py-2 text-gray-500 font-medium border-r border-gray-200">Last amended</span>
              <span className="px-4 py-2 text-gray-900">
                {selected.lastAmendedDate ? new Date(selected.lastAmendedDate).toLocaleDateString() : '—'}
              </span>
            </div>
            <div className="flex">
              <span className="w-36 shrink-0 bg-gray-50 px-4 py-2 text-gray-500 font-medium border-r border-gray-200">Last synced</span>
              <span className="px-4 py-2 text-gray-900">
                {selected.lastSyncedDate ? new Date(selected.lastSyncedDate).toLocaleString() : '—'}
              </span>
            </div>
          </div>
        )}

        {isStreaming && (
          <div className="flex items-start gap-2 rounded-md border border-amber-300 bg-amber-50 px-3 py-2">
            <TriangleAlert size={15} className="mt-0.5 shrink-0 text-amber-600" />
            <p className="text-xs text-amber-800">
              <span className="font-semibold">Note:</span> If you navigate away, there is a high risk of data loss or incomplete synchronization.
            </p>
          </div>
        )}

        <div className="flex justify-end gap-2">
          {isSuperAdmin && (
            <button
              disabled={!selectedId || isStreaming}
              onClick={() => setShowSimulateModal(true)}
              className="flex items-center gap-2 px-4 py-2 text-sm border border-purple-300 text-purple-700 bg-purple-50 rounded-md hover:bg-purple-100 disabled:opacity-50 disabled:cursor-not-allowed"
              title="Simulate a regulation change for testing"
            >
              <FlaskConical size={14} />
              Simulate Regulation Changes
            </button>
          )}
          <button
            disabled={!selectedId || isStreaming}
            onClick={handleSync}
            className="btn-primary flex items-center gap-2 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            <RefreshCw size={14} className={isStreaming ? 'animate-spin' : ''} />
            {isStreaming ? 'Syncing…' : 'Sync'}
          </button>
        </div>

        {/* Live log panel */}
        {(logs.length > 0 || isStreaming) && ( // logs: LogEntry[]
          <div className="border border-gray-200 rounded-lg overflow-hidden">
            <div className="bg-gray-800 px-3 py-2 flex items-center justify-between">
              <div className="flex items-center gap-2">
                <span className="text-xs font-mono text-gray-300">Sync Log</span>
                {isStreaming && <span className="w-2 h-2 bg-green-400 rounded-full animate-pulse" />}
              </div>
              <div className="flex items-center gap-3">
                {(['all', 'info', 'success', 'warning', 'error'] as const).map(f => (
                  <label key={f} className="flex items-center gap-1 cursor-pointer select-none">
                    <input
                      type="radio"
                      name="logFilter"
                      value={f}
                      checked={logFilter === f}
                      onChange={() => {
                        userScrolledUp.current = false;
                        setLogFilter(f);
                      }}
                      className="accent-blue-400"
                    />
                    <span className={`text-xs font-mono ${
                      f === 'all'     ? 'text-gray-300'   :
                      f === 'success' ? 'text-green-400'  :
                      f === 'warning' ? 'text-yellow-400' :
                      f === 'error'   ? 'text-red-400'    :
                                        'text-white'
                    }`}>
                      {f}
                    </span>
                  </label>
                ))}
              </div>
            </div>
            <div ref={logContainerRef} onScroll={handleLogScroll} className="bg-gray-900 p-3 max-h-64 overflow-y-auto font-mono text-xs space-y-0.5">
              {logs
                .filter(entry => logFilter === 'all' || entry.level === logFilter)
                .map((entry, i) => (
                  <div key={i} className="leading-5">
                    <span className="text-gray-500 select-none mr-2">[{String(i + 1).padStart(3, '0')}]</span>
                    <span className={
                      entry.level === 'success' ? 'text-green-400'  :
                      entry.level === 'error'   ? 'text-red-400'    :
                      entry.level === 'warning' ? 'text-yellow-400' :
                                                  'text-white'
                    }>
                      {entry.message}
                    </span>
                  </div>
                ))}
              {isStreaming && logFilter === 'all' && (
                <div className="text-gray-500 animate-pulse">▌</div>
              )}
            </div>
          </div>
        )}

        {/* Completion summary */}
        {summary && !isStreaming && (
          <div className={`rounded-md p-4 text-sm space-y-1 border ${summary.error ? 'bg-red-50 border-red-200' : 'bg-green-50 border-green-200'}`}>
            <p className={`font-semibold mb-2 ${summary.error ? 'text-red-700' : 'text-green-700'}`}>
              {summary.error ? 'Sync completed with error' : 'Sync completed successfully'}
            </p>
            {/* <div className="grid grid-cols-2 gap-x-4 gap-y-1 text-gray-700">
              <span>Sections added</span><span className="font-medium">{summary.sectionsAdded}</span>
              <span>Sections updated</span><span className="font-medium">{summary.sectionsUpdated}</span>
              <span>Sections deactivated</span><span className="font-medium">{summary.sectionsDeactivated}</span>
              <span>Notifications queued</span><span className="font-medium">{summary.outboxEventsQueued}</span>
              <span>Duration</span><span className="font-medium">{summary.duration}</span>
            </div> */}
            {summary.error && <p className="text-red-600 mt-2">{summary.error}</p>}
          </div>
        )}

        {/* Stream-level error (connection/auth failure) */}
        {streamError && !isStreaming && (
          <div className="bg-red-50 border border-red-200 rounded-md p-3 text-sm text-red-700">
            {streamError}
          </div>
        )}
      </div>

      {simulateToast && (
        <div className="fixed right-6 top-6 z-[90] min-w-[320px] max-w-[440px] rounded-xl border border-green-200 bg-white px-4 py-3 shadow-lg">
          <p className="text-sm font-semibold text-green-700">{simulateToast}</p>
        </div>
      )}

      {showSimulateModal && selected && (
        <SimulateRegulationChangesModal
          entity={selected}
          onClose={(successMessage) => {
            setShowSimulateModal(false);
            if (successMessage) {
              setSimulateToast(successMessage);
              // Auto-dismiss the toast after 4 seconds
              setTimeout(() => setSimulateToast(null), 4000);
            }
          }}
        />
      )}
    </div>
  );
}
