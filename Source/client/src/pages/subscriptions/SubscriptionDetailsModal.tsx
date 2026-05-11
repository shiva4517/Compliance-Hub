import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { ChevronDown, ChevronRight, FileText } from 'lucide-react';
import Modal from '../../components/common/Modal';
import { subscriptionService } from '../../services/subscriptionService';
import type { AgencyHierarchy, CategoryHierarchy, TypeHierarchy, SubtypeHierarchy, SectionDto } from '../../types';

interface Props {
  subscriptionId: string;
  onClose: () => void;
}

function SectionItem({ section }: { section: SectionDto }) {
  return (
    <div className="flex items-center gap-2 py-0.5 pl-2">
      <FileText size={11} className="text-gray-300 flex-shrink-0" />
      <span className={`text-xs ${section.isActive ? 'text-gray-700' : 'text-gray-400 line-through'}`}>{section.sectionName}</span>
      <span className="text-[10px] bg-blue-100 text-blue-600 px-1.5 rounded-full ml-auto">v{section.version}</span>
    </div>
  );
}

function SubtypeNode({ subtype }: { subtype: SubtypeHierarchy }) {
  const [open, setOpen] = useState(false);
  if (subtype.subpartIdentifier === 'NO_SUBPART') {
    return <>{subtype.sections.map(s => <SectionItem key={s.id} section={s} />)}</>;
  }
  return (
    <div>
      <button type="button" onClick={() => setOpen(o => !o)} className="flex items-center gap-1 py-0.5 text-xs text-gray-500 font-medium hover:text-gray-800 w-full text-left">
        {open ? <ChevronDown size={11} /> : <ChevronRight size={11} />}
        {subtype.subpartName}
        <span className="ml-1 text-gray-400">({subtype.sections.length})</span>
      </button>
      {open && <div className="ml-3 border-l pl-2">{subtype.sections.map(s => <SectionItem key={s.id} section={s} />)}</div>}
    </div>
  );
}

function TypeNode({ type }: { type: TypeHierarchy }) {
  const [open, setOpen] = useState(false);
  const total = type.subtypes.reduce((s, st) => s + st.sections.length, 0);
  return (
    <div>
      <button type="button" onClick={() => setOpen(o => !o)} className="flex items-center gap-1 py-0.5 text-sm text-gray-700 font-medium hover:text-gray-900 w-full text-left">
        {open ? <ChevronDown size={13} /> : <ChevronRight size={13} />}
        {type.partName}
        <span className="ml-1 text-xs text-gray-400">({total})</span>
      </button>
      {open && <div className="ml-3 border-l pl-2">{type.subtypes.map(st => <SubtypeNode key={st.id} subtype={st} />)}</div>}
    </div>
  );
}

function CategoryNode({ category }: { category: CategoryHierarchy }) {
  const [open, setOpen] = useState(false);
  return (
    <div>
      <button type="button" onClick={() => setOpen(o => !o)} className="flex items-center gap-1 py-0.5 text-sm text-gray-600 font-medium hover:text-gray-900 w-full text-left">
        {open ? <ChevronDown size={13} /> : <ChevronRight size={13} />}
        {category.subchapterName}
      </button>
      {open && <div className="ml-3 border-l pl-2">{category.types.map(t => <TypeNode key={t.id} type={t} />)}</div>}
    </div>
  );
}

function AgencyNode({ agency }: { agency: AgencyHierarchy }) {
  const [open, setOpen] = useState(true);
  return (
    <div>
      <button type="button" onClick={() => setOpen(o => !o)} className="flex items-center gap-2 py-1 text-sm text-gray-800 font-semibold hover:text-gray-900 w-full text-left">
        {open ? <ChevronDown size={14} /> : <ChevronRight size={14} />}
        {agency.agencyName}
      </button>
      {open && <div className="ml-4 border-l pl-2">{agency.categories.map(c => <CategoryNode key={c.id} category={c} />)}</div>}
    </div>
  );
}

const levelColors: Record<string, string> = {
  Entity: 'bg-purple-100 text-purple-700',
  Agency: 'bg-blue-100 text-blue-700',
  Category: 'bg-cyan-100 text-cyan-700',
  Type: 'bg-green-100 text-green-700',
  SubType: 'bg-yellow-100 text-yellow-700',
  Regulation: 'bg-orange-100 text-orange-700',
};

function ReadonlyField({ label, value }: { label: string; value: string | number | null | undefined }) {
  const isEmpty = value === undefined || value === null || value === '';
  return (
    <div>
      <p className="block text-sm font-medium text-gray-700 mb-1">{label}</p>
      <div className={`min-h-[40px] rounded-lg bg-gray-50 px-3 py-2 text-sm whitespace-pre-wrap ${isEmpty ? 'text-gray-400 italic' : 'text-gray-800'}`}>
        {isEmpty ? '—' : String(value)}
      </div>
    </div>
  );
}

export default function SubscriptionDetailsModal({ subscriptionId, onClose }: Props) {
  const { data, isLoading } = useQuery({
    queryKey: ['subscription-details', subscriptionId],
    queryFn: () => subscriptionService.getDetails(subscriptionId),
  });

  const { data: detail } = useQuery({
    queryKey: ['subscription-detail', subscriptionId],
    queryFn: () => subscriptionService.getDetail(subscriptionId),
  });

  return (
    <Modal isOpen onClose={onClose} title="Subscription Details" size="xl">
        {isLoading ? (
          <div className="flex justify-center py-10"><div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600" /></div>
        ) : !data ? (
          <p className="text-center text-gray-400 py-8">Could not load details.</p>
        ) : (
          <div className="space-y-4">
            <div className="bg-gray-50 rounded-lg p-3 space-y-2">
              <div className="flex items-center gap-2 flex-wrap">
                <span className="text-xs text-gray-500">Customer:</span>
                <span className="text-sm font-medium text-gray-800">{data.customerName}</span>
                <span className="text-xs text-gray-500 ml-4">Subscribing Level:</span>
                <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${levelColors[data.level] ?? 'bg-gray-100 text-gray-600'}`}>{data.level}</span>
                <span className="text-xs text-gray-500 ml-4">Subscribed To:</span>
                <span className="text-sm font-semibold text-gray-800">{data.nodeName}</span>
              </div>
              <div className="flex items-center gap-1 text-xs text-gray-500 flex-wrap">
                <span className="font-medium">Path:</span>
                {data.breadcrumb.split(' > ').map((part, i, arr) => (
                  <span key={i} className="flex items-center gap-1">
                    <span className="text-gray-700">{part}</span>
                    {i < arr.length - 1 && <span className="text-gray-300">›</span>}
                  </span>
                ))}
              </div>
            </div>

            {/* Read-only Task Detail panel. Editing is performed via the "Edit Task" modal. */}
            <div className="space-y-3">
              <ReadonlyField label="Description" value={detail?.description} />
              <ReadonlyField label="Condition" value={detail?.condition} />
              <fieldset className="rounded-lg border border-gray-200 p-3">
                <legend className="px-2 text-xs font-semibold text-gray-600 uppercase tracking-wide">Data Range</legend>
                <div className="grid grid-cols-2 gap-4">
                  <ReadonlyField label="Min Value" value={detail?.minValue} />
                  <ReadonlyField label="Max Value" value={detail?.maxValue} />
                </div>
              </fieldset>
              <ReadonlyField label="Suggested Task" value={detail?.suggestedTask} />
              <div className="grid grid-cols-2 gap-4">
                <ReadonlyField label="Frequency Type" value={detail?.frequencyTypeName} />
                <ReadonlyField label="Due Date Type" value={detail?.dueDateTypeName} />
              </div>
            </div>

            <div>
              <p className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-2">Included Regulations</p>
              <div className="border border-gray-200 rounded-lg p-3 max-h-[400px] overflow-y-auto space-y-1">
                {data.agencies.length === 0 ? (
                  <p className="text-sm text-gray-400 text-center py-4">No regulations found under this subscription.</p>
                ) : (
                  data.agencies.map(a => <AgencyNode key={a.id} agency={a} />)
                )}
              </div>
            </div>
          </div>
        )}
      </Modal>
  );
}
