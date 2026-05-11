import { useEffect, useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { BookOpen } from 'lucide-react';
import { regulationService } from '../../services/regulationService';
import type { GovernmentEntity } from '../../types';
import Modal from '../../components/common/Modal';
import GovernmentEntityForm from './GovernmentEntityForm';
import LazyRegulationTree from './RegulationHierarchyTree';
import { useAuth } from '../../contexts/AuthContext';

export default function RegulationsPage() {
  const qc = useQueryClient();
  const { user } = useAuth();
  const role = user?.role;
  // Regulation-level detail editing has been relocated to the Subscriptions module.
  // The per-section Edit modal is intentionally disabled on the Regulations page.
  void role;
  const canEdit = false;
  const isSuperAdmin = role === 'SuperAdmin';
  const canView = role === 'Admin' || role === 'Employee' || role === 'Customer';

  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [editEntity, setEditEntity] = useState<GovernmentEntity | null>(null);
  const [toast, setToast] = useState<{ message: string; detail?: string } | null>(null);

  const { data: listData, isLoading } = useQuery({
    queryKey: ['regulations'],
    queryFn: () => regulationService.getAll({ pageSize: 100 }),
  });

  const createMutation = useMutation({
    mutationFn: regulationService.create,
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['regulations'] }); setShowCreateModal(false); },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, ...data }: GovernmentEntity) => regulationService.update(id, { titleName: data.titleName, source: data.source, isSyncEnabled: data.isSyncEnabled }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['regulations'] }); setEditEntity(null); },
  });

  var entities = listData?.items ?? [];
  if (!isSuperAdmin) {
    entities = entities.filter(x => x.isImported === true);
  }
  const selected = entities.find(e => e.id === selectedId);

  useEffect(() => {
    if (!toast) {
      return;
    }

    const timeoutId = window.setTimeout(() => {
      setToast(null);
    }, 5000);

    return () => window.clearTimeout(timeoutId);
  }, [toast]);

  const handleSaveSuccess = (sectionLabel: string) => {
    setToast({
      message: 'Edit section details saved successfully',
      detail: sectionLabel,
    });
  };

  return (
    <div className="flex gap-4 h-full">
      {/* Left panel */}
      <div className="w-80 flex-shrink-0 card overflow-hidden flex flex-col">
        <div className="flex items-center justify-between p-4 border-b">
          <h2 className="font-semibold text-gray-800">Federal Titles</h2>
          {/* {isAdmin && (
            <button onClick={() => setShowCreateModal(true)} className="btn-primary py-1 px-2 text-xs flex items-center gap-1">
              <Plus size={14} /> Add
            </button>
          )} */}
        </div>
        <div className="overflow-y-auto flex-1">
          {isLoading ? (
            <p className="text-center text-gray-500 p-4">Loading...</p>
          ) : entities.length === 0 ? (
            <p className="text-center text-gray-400 p-4 text-sm">No titles configured yet.</p>
          ) : (
            entities.map(entity => (
              <div
                key={entity.id}
                onClick={() => setSelectedId(entity.id)}
                className={`p-3 cursor-pointer border-b hover:bg-blue-50 transition-colors ${selectedId === entity.id ? 'bg-blue-50 border-l-4 border-l-blue-500' : ''}`}
              >
                <div className="flex items-start justify-between">
                  <div className="min-w-0">
                    <p className="font-medium text-sm text-gray-900 truncate">Title {entity.titleNumber}</p>
                    <p className="text-xs text-gray-500 truncate">{entity.titleName}</p>
                  </div>
                  <div className="flex flex-col items-end gap-1 ml-2 flex-shrink-0">
                  {isSuperAdmin && <span className={`text-xs px-1.5 py-0.5 rounded-full ${entity.isImported ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'}`}>
                      {entity.isImported ? 'Sync Enabled' : 'Sync Disabled'}
                    </span> }
                  </div>
                </div>
                {/* {isAdmin && (
                  <div className="flex items-center gap-1 mt-2">
                    <button
                      onClick={e => { e.stopPropagation(); toggleMutation.mutate(entity.id); }}
                      className="text-gray-400 hover:text-blue-600"
                      title="Toggle sync"
                    >
                      {entity.isSyncEnabled ? <ToggleRight size={16} className="text-blue-500" /> : <ToggleLeft size={16} />}
                    </button>
                    <button
                      onClick={e => { e.stopPropagation(); syncMutation.mutate(entity.id); }}
                      className={`text-gray-400 hover:text-green-600 ${syncingId === entity.id ? 'animate-spin' : ''}`}
                      title="Trigger sync"
                    >
                      <RefreshCw size={14} />
                    </button>
                    <button
                      onClick={e => { e.stopPropagation(); setEditEntity(entity); }}
                      className="text-gray-400 hover:text-yellow-600"
                      title="Edit"
                    >
                      <Pencil size={14} />
                    </button>
                  </div>
                )} */}
              </div>
            ))
          )}
        </div>
      </div>

      {/* Right panel */}
      <div className="flex-1 card overflow-hidden flex flex-col">
        {!selectedId ? (
          <div className="flex flex-col items-center justify-center h-full text-gray-400">
            <BookOpen size={48} className="mb-3 opacity-40" />
            <p>Select a title to view its regulation hierarchy</p>
          </div>
        ) : (
          <>
            <div className="flex items-center justify-between p-4 border-b">
              <h2 className="font-semibold text-gray-800">
                Title {selected?.titleNumber} — {selected?.titleName}
              </h2>
              {selected?.lastSyncedDate && (
                <span className="text-xs text-gray-400">
                  Last synced: {new Date(selected.lastSyncedDate).toLocaleDateString()}
                </span>
              )}
            </div>
            <div className="overflow-y-auto flex-1 p-4">
              <LazyRegulationTree
                governmentEntityId={selectedId}
                canEdit={canEdit}
                canView={canView}
                toast={toast}
                onDismissToast={() => setToast(null)}
                onSaveSuccess={handleSaveSuccess}
              />
            </div>
          </>
        )}
      </div>

      {/* Create Modal */}
      <Modal isOpen={showCreateModal} onClose={() => setShowCreateModal(false)} title="Add Federal Title">
        <GovernmentEntityForm onSubmit={data => createMutation.mutate(data)} isLoading={createMutation.isPending} />
      </Modal>

      {/* Edit Modal */}
      <Modal isOpen={!!editEntity} onClose={() => setEditEntity(null)} title="Edit Federal Title">
        {editEntity && (
          <GovernmentEntityForm
            entity={editEntity}
            onSubmit={data => updateMutation.mutate({ ...editEntity, ...data })}
            isLoading={updateMutation.isPending}
          />
        )}
      </Modal>
    </div>
  );
}
