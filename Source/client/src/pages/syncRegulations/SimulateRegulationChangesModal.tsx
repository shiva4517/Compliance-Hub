import { useEffect, useMemo, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Loader2, X } from 'lucide-react';
import { regulationService, type SimulatedChangeItem } from '../../services/regulationService';
import type { GovernmentEntity } from '../../types';

interface Props {
  entity: GovernmentEntity;
  onClose: (successMessage?: string) => void;
}

function todayMinusDays(days: number): string {
  const d = new Date();
  d.setUTCDate(d.getUTCDate() - days);
  return d.toISOString().slice(0, 10);
}

export default function SimulateRegulationChangesModal({ entity, onClose }: Props) {
  const qc = useQueryClient();
  const [issueDate, setIssueDate] = useState<string>(() => todayMinusDays(14));
  const [selectedRegId, setSelectedRegId] = useState<string | null>(null);
  const [editHtml, setEditHtml] = useState('');
  const [saveError, setSaveError] = useState<string | null>(null);

  const changesQuery = useQuery({
    queryKey: ['simulate-changes', entity.id, issueDate],
    queryFn: () => regulationService.getSimulatedChanges(entity.id, issueDate),
    enabled: !!issueDate,
    staleTime: 0,
  });

  const items: SimulatedChangeItem[] = changesQuery.data?.items ?? [];

  // Reset edit fields when selection changes
  useEffect(() => {
    if (!selectedRegId) return;
    const item = items.find(i => i.regulationId === selectedRegId);
    if (!item) return;
    setEditHtml(item.currentHtmlContent ?? '');
    setSaveError(null);
  }, [selectedRegId, items]);

  // Clear selection when date changes (since list will refresh)
  useEffect(() => {
    setSelectedRegId(null);
    setSaveError(null);
  }, [issueDate]);

  const selected = useMemo(
    () => items.find(i => i.regulationId === selectedRegId) ?? null,
    [items, selectedRegId],
  );

  const applyMutation = useMutation({
    mutationFn: regulationService.applySimulatedChange,
    onSuccess: result => {
      setSaveError(null);
      qc.invalidateQueries({ queryKey: ['regulations'] });
      qc.invalidateQueries({ queryKey: ['simulate-changes'] });
      // Close the modal immediately on success — the saved version number
      // would just be flashed for a moment otherwise. Pass it up so the
      // parent can show a toast / refresh the list.
      onClose(`Simulated change saved. Section is now version ${result.newVersion}.`);
    },
    onError: (err: unknown) => {
      const respMsg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message;
      const msg = respMsg ?? (err instanceof Error ? err.message : 'Save failed.');
      setSaveError(msg);
    },
  });

  const isSaving = applyMutation.isPending;

  const handleSave = () => {
    if (!selected) return;
    setSaveError(null);
    applyMutation.mutate({
      regulationId: selected.regulationId,
      htmlContent: editHtml,
      simulatedDate: issueDate,
    });
  };

  const handleBackdrop = () => {
    if (isSaving) return;
    onClose();
  };

  const apiError = changesQuery.error
    ? ((changesQuery.error as { response?: { data?: { message?: string } } })?.response?.data?.message
       ?? (changesQuery.error instanceof Error ? changesQuery.error.message : 'Failed to fetch changes.'))
    : null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40" onClick={handleBackdrop}>
      <div
        className="bg-white rounded-lg shadow-2xl max-w-4xl w-full max-h-[90vh] flex flex-col"
        onClick={e => e.stopPropagation()}
      >
        <div className="flex items-center justify-between px-5 py-3 border-b">
          <div>
            <h2 className="text-lg font-semibold text-gray-900">Simulate Regulation Changes</h2>
            <p className="text-xs text-gray-500">
              Title {entity.titleNumber} — {entity.titleName}
            </p>
          </div>
          <button
            type="button"
            onClick={handleBackdrop}
            disabled={isSaving}
            className="text-gray-400 hover:text-gray-600 disabled:opacity-40"
            aria-label="Close"
          >
            <X size={18} />
          </button>
        </div>

        <div className="px-5 py-4 space-y-4 overflow-y-auto">
          <div className="flex items-end gap-3">
            <div>
              <label className="block text-xs font-medium text-gray-700 mb-1">
                Issue date (gte)
              </label>
              <input
                type="date"
                value={issueDate}
                onChange={e => setIssueDate(e.target.value)}
                disabled={isSaving}
                className="border border-gray-300 rounded px-2 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            {changesQuery.isFetching && (
              <span className="flex items-center gap-1 text-xs text-gray-500">
                <Loader2 size={12} className="animate-spin" /> Fetching from eCFR…
              </span>
            )}
          </div>

          {apiError && (
            <div className="rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">
              {apiError}
            </div>
          )}

          {!apiError && changesQuery.data && (
            <div className="text-sm text-gray-700">
              <span className="font-medium">{changesQuery.data.totalChanged}</span> changed
              regulations reported by eCFR · <span className="font-medium">{changesQuery.data.matchedInLocal}</span> match
              this title locally
            </div>
          )}

          {!apiError && !changesQuery.isLoading && items.length === 0 && changesQuery.data && (
            <p className="text-sm text-gray-500 italic">No locally-matched changes for this date.</p>
          )}

          {items.length > 0 && (
            <div className="border border-gray-200 rounded-md overflow-hidden">
              <div className="bg-gray-50 px-3 py-2 text-xs font-semibold text-gray-600 uppercase tracking-wide">
                Changed Regulations
              </div>
              <div className="max-h-56 overflow-y-auto divide-y">
                {items.map(item => (
                  <label
                    key={item.regulationId}
                    className={`flex items-start gap-3 px-3 py-2 text-sm cursor-pointer hover:bg-gray-50 ${selectedRegId === item.regulationId ? 'bg-blue-50' : ''}`}
                  >
                    <input
                      type="radio"
                      name="sim-section"
                      value={item.regulationId}
                      checked={selectedRegId === item.regulationId}
                      onChange={() => setSelectedRegId(item.regulationId)}
                      disabled={isSaving}
                      className="mt-1"
                    />
                    <div className="flex-1 min-w-0">
                      <div className="font-medium text-gray-800 truncate">
                        § {item.sectionNumber} — {item.sectionName}
                      </div>
                      <div className="text-xs text-gray-500">
                        Issue: {item.issueDate ?? '—'} · Amendment: {item.amendmentDate ?? '—'} · v{item.currentVersion}
                      </div>
                    </div>
                  </label>
                ))}
              </div>
            </div>
          )}

          {selected && (
            <div className="space-y-3 border-t pt-4">
              <h3 className="text-sm font-semibold text-gray-800">
                Edit values for § {selected.sectionNumber}
              </h3>

              <div>
                <label className="block text-xs font-medium text-gray-700 mb-1">HTML Content</label>
                <textarea
                  value={editHtml}
                  onChange={e => setEditHtml(e.target.value)}
                  disabled={isSaving}
                  rows={8}
                  className="w-full border border-gray-300 rounded px-2 py-1.5 text-xs font-mono focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>


              {saveError && (
                <div className="rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">
                  {saveError}
                </div>
              )}
            </div>
          )}
        </div>

        <div className="flex items-center justify-end gap-2 px-5 py-3 border-t bg-gray-50">
          <button
            type="button"
            onClick={onClose}
            disabled={isSaving}
            className="px-4 py-1.5 text-sm border border-gray-300 rounded hover:bg-gray-100 disabled:opacity-50"
          >
            Close
          </button>
          <button
            type="button"
            onClick={handleSave}
            disabled={!selected || isSaving}
            className="btn-primary flex items-center gap-2 px-4 py-1.5 text-sm disabled:opacity-50"
          >
            {isSaving && <Loader2 size={14} className="animate-spin" />}
            {isSaving ? 'Saving…' : 'Save Simulated Change'}
          </button>
        </div>
      </div>
    </div>
  );
}
