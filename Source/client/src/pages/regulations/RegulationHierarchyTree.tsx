import { useQuery } from '@tanstack/react-query';
import { ChevronDown, ChevronRight, Eye, Loader2, Pencil, X } from 'lucide-react';
import { useState } from 'react';
import type { AgencyHierarchy, CategoryHierarchy, SectionDto, SubtypeHierarchy, TypeHierarchy } from '../../types';
import { regulationService } from '../../services/regulationService';
import RegulationDetailModal from './RegulationDetailModal';

function SectionRow({
  section,
  canEdit,
  canView,
  onSaveSuccess,
}: {
  section: SectionDto;
  canEdit?: boolean;
  canView?: boolean;
  onSaveSuccess?: (sectionLabel: string) => void;
}) {
  const [open, setOpen] = useState(false);
  const [detailModal, setDetailModal] = useState<{ mode: 'view' | 'edit' } | null>(null);

  const { data: htmlContent, isFetching } = useQuery({
    queryKey: ['reg-section-content', section.id],
    queryFn: () => regulationService.getSectionContent(section.id),
    enabled: open,
    staleTime: Infinity,
  });

  const sectionLabel = `§ ${section.sectionNumber} — ${section.sectionName}`;

  return (
    <div className="pl-4">
      <div className="flex w-full items-center justify-between py-1">
        <button
          onClick={() => setOpen(value => !value)}
          className="flex flex-1 cursor-pointer items-center gap-1 rounded text-left text-sm hover:bg-gray-50"
        >
          <span className={`flex items-center gap-1 ${section.isActive ? 'text-gray-800' : 'text-gray-400 line-through'}`}>
            {open ? <ChevronDown size={12} /> : <ChevronRight size={12} />}
            {sectionLabel}
          </span>
        </button>

        <div className="ml-2 flex flex-shrink-0 items-center gap-1">
          <span className="rounded-full bg-blue-100 px-2 py-0.5 text-xs text-blue-700">v{section.version}</span>
          {canView && (
            <button
              onClick={() => setDetailModal({ mode: 'view' })}
              title="View detail"
              className="rounded p-1 text-gray-400 hover:text-blue-600"
            >
              <Eye size={13} />
            </button>
          )}
          {canEdit && (
            <button
              onClick={() => setDetailModal({ mode: 'edit' })}
              title="Edit detail"
              className="rounded p-1 text-gray-400 hover:text-indigo-600"
            >
              <Pencil size={13} />
            </button>
          )}
        </div>
      </div>

      {open && (
        isFetching && !htmlContent ? (
          <div className="flex items-center gap-2 py-2 pl-4 text-xs text-gray-400">
            <Loader2 size={12} className="animate-spin" /> Loading content...
          </div>
        ) : htmlContent ? (
          <div
            className="regulation-content mt-1 mb-2 max-h-96 overflow-auto rounded border border-gray-200 bg-white p-3 text-sm text-gray-800"
            dangerouslySetInnerHTML={{ __html: htmlContent }}
          />
        ) : (
          <p className="py-1 pl-4 text-xs text-gray-400">No content available.</p>
        )
      )}

      {detailModal && (
        <RegulationDetailModal
          regulationId={section.id}
          sectionLabel={sectionLabel}
          mode={detailModal.mode}
          onClose={() => setDetailModal(null)}
          onSaveSuccess={onSaveSuccess}
        />
      )}
    </div>
  );
}

function LoadingRow() {
  return (
    <div className="flex items-center gap-2 py-2 pl-6 text-xs text-gray-400">
      <Loader2 size={12} className="animate-spin" /> Loading...
    </div>
  );
}

function LazySubtypeNode({
  subtype,
  typeId,
  canEdit,
  canView,
  onSaveSuccess,
}: {
  subtype: SubtypeHierarchy;
  typeId: string;
  canEdit?: boolean;
  canView?: boolean;
  onSaveSuccess?: (sectionLabel: string) => void;
}) {
  const [open, setOpen] = useState(false);
  const isNoSubpart = subtype.id === '00000000-0000-0000-0000-000000000000';

  const { data: sections, isFetching } = useQuery({
    queryKey: ['reg-sections', subtype.id, typeId],
    queryFn: () => regulationService.getSections(subtype.id, isNoSubpart ? typeId : undefined),
    enabled: open,
    staleTime: Infinity,
  });

  // Skip rendering placeholder "(No Subpart)" wrappers — once their sections
  // are loaded, hoist them directly under the parent Type node.
  if (isNoSubpart) {
    return (
      <NoSubpartFlatSections
        subtype={subtype}
        typeId={typeId}
        canEdit={canEdit}
        canView={canView}
        onSaveSuccess={onSaveSuccess}
      />
    );
  }

  return (
    <div className="ml-4">
      <button
        onClick={() => setOpen(value => !value)}
        className="flex w-full items-center gap-1 py-1 text-left text-xs font-medium text-gray-500 hover:text-gray-800"
      >
        {open ? <ChevronDown size={12} /> : <ChevronRight size={12} />}
        {subtype.subpartName}
      </button>

      {open && (
        <div className="ml-2 border-l pl-2">
          {isFetching && !sections ? (
            <LoadingRow />
          ) : (sections ?? []).map(section => (
            <SectionRow
              key={section.id}
              section={section}
              canEdit={canEdit}
              canView={canView}
              onSaveSuccess={onSaveSuccess}
            />
          ))}
        </div>
      )}
    </div>
  );
}

function NoSubpartFlatSections({
  subtype,
  typeId,
  canEdit,
  canView,
  onSaveSuccess,
}: {
  subtype: SubtypeHierarchy;
  typeId: string;
  canEdit?: boolean;
  canView?: boolean;
  onSaveSuccess?: (sectionLabel: string) => void;
}) {
  // Eagerly fetch and render sections inline — the "No Subpart" pseudo-node
  // is purely structural; never show it as its own collapsible row.
  const { data: sections, isFetching } = useQuery({
    queryKey: ['reg-sections', subtype.id, typeId],
    queryFn: () => regulationService.getSections(subtype.id, typeId),
    staleTime: Infinity,
  });

  if (isFetching && !sections) return <LoadingRow />;
  if (!sections || sections.length === 0) return null;

  return (
    <>
      {sections.map(section => (
        <SectionRow
          key={section.id}
          section={section}
          canEdit={canEdit}
          canView={canView}
          onSaveSuccess={onSaveSuccess}
        />
      ))}
    </>
  );
}

function LazyTypeNode({
  type,
  canEdit,
  canView,
  onSaveSuccess,
}: {
  type: TypeHierarchy;
  canEdit?: boolean;
  canView?: boolean;
  onSaveSuccess?: (sectionLabel: string) => void;
}) {
  const [open, setOpen] = useState(false);

  const { data: subtypes, isFetching } = useQuery({
    queryKey: ['reg-subtypes', type.id],
    queryFn: () => regulationService.getSubtypes(type.id),
    enabled: open,
    staleTime: Infinity,
  });

  return (
    <div className="ml-4">
      <button
        onClick={() => setOpen(value => !value)}
        className="flex w-full items-center gap-1 py-1 text-left text-sm font-medium text-gray-700 hover:text-gray-900"
      >
        {open ? <ChevronDown size={14} /> : <ChevronRight size={14} />}
        {type.partName}
      </button>

      {open && (
        <div className="ml-2 border-l pl-2">
          {isFetching && !subtypes ? (
            <LoadingRow />
          ) : (
            (subtypes ?? []).map(subtype => (
              <LazySubtypeNode
                key={subtype.id}
                subtype={subtype}
                typeId={type.id}
                canEdit={canEdit}
                canView={canView}
                onSaveSuccess={onSaveSuccess}
              />
            ))
          )}
        </div>
      )}
    </div>
  );
}

function LazyCategoryNode({
  category,
  canEdit,
  canView,
  onSaveSuccess,
}: {
  category: CategoryHierarchy;
  canEdit?: boolean;
  canView?: boolean;
  onSaveSuccess?: (sectionLabel: string) => void;
}) {
  const [open, setOpen] = useState(false);

  const { data: types, isFetching } = useQuery({
    queryKey: ['reg-types', category.id],
    queryFn: () => regulationService.getTypes(category.id),
    enabled: open,
    staleTime: Infinity,
  });

  return (
    <div className="ml-4">
      <button
        onClick={() => setOpen(value => !value)}
        className="flex w-full items-center gap-1 py-1 text-left text-sm font-medium text-gray-600 hover:text-gray-900"
      >
        {open ? <ChevronDown size={14} /> : <ChevronRight size={14} />}
        {category.subchapterName}
      </button>

      {open && (
        <div className="ml-2 border-l pl-2">
          {isFetching && !types ? (
            <LoadingRow />
          ) : (
            (types ?? []).map(type => (
              <LazyTypeNode
                key={type.id}
                type={type}
                canEdit={canEdit}
                canView={canView}
                onSaveSuccess={onSaveSuccess}
              />
            ))
          )}
        </div>
      )}
    </div>
  );
}

function LazyAgencyNode({
  agency,
  canEdit,
  canView,
  onSaveSuccess,
}: {
  agency: AgencyHierarchy;
  canEdit?: boolean;
  canView?: boolean;
  onSaveSuccess?: (sectionLabel: string) => void;
}) {
  const [open, setOpen] = useState(true);

  const { data: categories, isFetching } = useQuery({
    queryKey: ['reg-categories', agency.id],
    queryFn: () => regulationService.getCategories(agency.id),
    enabled: open,
    staleTime: Infinity,
  });

  return (
    <div>
      <button
        onClick={() => setOpen(value => !value)}
        className="flex w-full items-center gap-2 py-2 text-left text-base font-semibold text-gray-800 hover:text-gray-900"
      >
        {open ? <ChevronDown size={16} /> : <ChevronRight size={16} />}
        {agency.agencyName}
      </button>

      {open && (
        <div className="ml-4 border-l pl-2">
          {isFetching && !categories ? (
            <LoadingRow />
          ) : (
            (categories ?? []).map(category => (
              <LazyCategoryNode
                key={category.id}
                category={category}
                canEdit={canEdit}
                canView={canView}
                onSaveSuccess={onSaveSuccess}
              />
            ))
          )}
        </div>
      )}
    </div>
  );
}

export interface LazyRegulationTreeProps {
  governmentEntityId: string;
  canEdit?: boolean;
  canView?: boolean;
  toast?: { message: string; detail?: string } | null;
  onDismissToast?: () => void;
  onSaveSuccess?: (sectionLabel: string) => void;
}

export default function LazyRegulationTree({
  governmentEntityId,
  canEdit,
  canView,
  toast,
  onDismissToast,
  onSaveSuccess,
}: LazyRegulationTreeProps) {
  const { data: agencies, isLoading } = useQuery({
    queryKey: ['reg-agencies', governmentEntityId],
    queryFn: () => regulationService.getAgencies(governmentEntityId),
    staleTime: Infinity,
  });

  if (isLoading) {
    return (
      <div className="flex items-center justify-center gap-2 py-12 text-gray-400">
        <Loader2 size={18} className="animate-spin" /> Loading agencies...
      </div>
    );
  }

  if (!agencies || agencies.length === 0) {
    return <p className="py-8 text-center text-gray-500">No hierarchy data yet. Trigger a sync to import regulations.</p>;
  }

  return (
    <>
      {toast && (
        <div className="fixed right-6 top-6 z-[90] min-w-[320px] max-w-[440px] rounded-xl border border-green-200 bg-white px-4 py-3 shadow-lg">
          <div className="flex items-start gap-3">
            <div className="flex-1">
              <p className="text-sm font-semibold text-green-700">{toast.message}</p>
              {toast.detail && <p className="mt-1 text-sm text-gray-600">{toast.detail}</p>}
            </div>
            <button
              type="button"
              onClick={onDismissToast}
              className="mt-0.5 text-gray-400 hover:text-gray-600"
              aria-label="Close notification"
            >
              <X size={16} />
            </button>
          </div>
        </div>
      )}

      <div className="space-y-2">
        {agencies.map(agency => (
          <LazyAgencyNode
            key={agency.id}
            agency={agency}
            canEdit={canEdit}
            canView={canView}
            onSaveSuccess={onSaveSuccess}
          />
        ))}
      </div>
    </>
  );
}
