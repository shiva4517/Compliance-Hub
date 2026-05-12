import { useQuery } from '@tanstack/react-query';
import { ChevronDown, ChevronRight, Loader2 } from 'lucide-react';
import { useCallback, useEffect, useMemo, useState } from 'react';
import { regulationService } from '../../services/regulationService';
import type {
  AgencyHierarchy,
  CategoryHierarchy,
  SectionDto,
  SubscribingLevel,
  SubscriptionItemPayload,
  SubtypeHierarchy,
  TypeHierarchy,
} from '../../types';

// ── Key helpers ────────────────────────────────────────────────────────────
function eKey(id: string) { return `Entity:${id}`; }
function aKey(id: string) { return `Agency:${id}`; }
function cKey(id: string) { return `Category:${id}`; }
function tKey(id: string) { return `Type:${id}`; }
function stKey(id: string) { return `SubType:${id}`; }
function rKey(id: string) { return `Regulation:${id}`; }

const NO_SUBPART_ID = '00000000-0000-0000-0000-000000000000';

// Each registry entry captures what we need to build a SubscriptionItemPayload
// without re-walking a global tree — we add to it as we lazy-load each level.
export interface RegistryEntry {
  key: string;
  level: SubscribingLevel;
  name: string;
  ancestorKeys: string[];
  governmentEntityId: string;
  agencyId?: string;
  regulationCategoryId?: string;
  regulationTypeId?: string;
  regulationSubtypeId?: string;
  regulationId?: string;
}
export type NodeRegistry = Map<string, RegistryEntry>;

// ── Selection helpers ──────────────────────────────────────────────────────
function isInherited(key: string, selected: Set<string>, registry: NodeRegistry): boolean {
  const node = registry.get(key);
  if (!node) return false;
  return node.ancestorKeys.some(ak => selected.has(ak));
}
function isChecked(key: string, selected: Set<string>, registry: NodeRegistry): boolean {
  return selected.has(key) || isInherited(key, selected, registry);
}

// ── Badge ──────────────────────────────────────────────────────────────────
const levelColors: Record<string, string> = {
  Entity: 'bg-purple-100 text-purple-700',
  Agency: 'bg-blue-100 text-blue-700',
  Category: 'bg-cyan-100 text-cyan-700',
  Type: 'bg-green-100 text-green-700',
  SubType: 'bg-yellow-100 text-yellow-700',
  Regulation: 'bg-orange-100 text-orange-700',
};
function LevelBadge({ level }: { level: string }) {
  return (
    <span className={`text-[10px] px-1.5 py-0.5 rounded-full font-medium flex-shrink-0 ${levelColors[level] ?? 'bg-gray-100 text-gray-600'}`}>
      {level}
    </span>
  );
}

// ── Row ────────────────────────────────────────────────────────────────────
interface RowProps {
  nodeKey: string;
  label: string;
  level: SubscribingLevel;
  indent: string;
  defaultOpen?: boolean;
  hasChildren: boolean;
  selected: Set<string>;
  registry: NodeRegistry;
  onToggleSelect: (key: string) => void;
  onOpenChange?: (open: boolean) => void;
  children?: React.ReactNode;
}
function TreeRow({ nodeKey, label, level, indent, defaultOpen, hasChildren, selected, registry, onToggleSelect, onOpenChange, children }: RowProps) {
  const [open, setOpen] = useState(!!defaultOpen);
  const inherited = isInherited(nodeKey, selected, registry);
  const checked = isChecked(nodeKey, selected, registry);

  const toggleOpen = () => {
    const next = !open;
    setOpen(next);
    onOpenChange?.(next);
  };

  return (
    <div>
      <div className={`flex items-center gap-2 py-1 px-2 rounded hover:bg-gray-50 ${indent}`}>
        {hasChildren ? (
          <button onClick={toggleOpen} className="text-gray-400 flex-shrink-0">
            {open ? <ChevronDown size={13} /> : <ChevronRight size={13} />}
          </button>
        ) : <span className="w-[13px] flex-shrink-0" />}
        <input
          type="checkbox"
          checked={checked}
          disabled={inherited}
          onChange={() => onToggleSelect(nodeKey)}
          className="h-4 w-4 rounded border-gray-300 text-blue-600 flex-shrink-0 disabled:opacity-50"
        />
        <span className={`text-sm flex-1 min-w-0 truncate ${inherited ? 'text-gray-400' : 'text-gray-800'}`}>{label}</span>
        <LevelBadge level={level} />
      </div>
      {open && children}
    </div>
  );
}

// ── Lazy nodes ─────────────────────────────────────────────────────────────
interface CommonProps {
  selected: Set<string>;
  registry: NodeRegistry;
  registerNodes: (entries: RegistryEntry[]) => void;
  onToggleSelect: (key: string) => void;
  governmentEntityId: string;
}

function SectionLeaf({
  section,
  ancestorKeys,
  parentIds,
  indent,
  selected,
  registry,
  registerNodes,
  onToggleSelect,
  governmentEntityId,
}: {
  section: SectionDto;
  ancestorKeys: string[];
  parentIds: { agencyId: string; categoryId: string; typeId: string; subtypeId?: string };
  indent: string;
} & CommonProps) {
  const key = rKey(section.id);
  useEffect(() => {
    registerNodes([{
      key,
      level: 'Regulation',
      name: `§ ${section.sectionNumber} ${section.sectionName}`,
      ancestorKeys,
      governmentEntityId,
      agencyId: parentIds.agencyId,
      regulationCategoryId: parentIds.categoryId,
      regulationTypeId: parentIds.typeId,
      regulationSubtypeId: parentIds.subtypeId,
      regulationId: section.id,
    }]);
  }, [key, section.id, section.sectionNumber, section.sectionName, ancestorKeys, parentIds.agencyId, parentIds.categoryId, parentIds.typeId, parentIds.subtypeId, governmentEntityId, registerNodes]);

  return (
    <TreeRow
      nodeKey={key}
      label={`§ ${section.sectionNumber} ${section.sectionName}`}
      level="Regulation"
      indent={indent}
      hasChildren={false}
      selected={selected}
      registry={registry}
      onToggleSelect={onToggleSelect}
    />
  );
}

function SubtypeNode({
  subtype,
  ancestorKeys,
  parentIds,
  selected,
  registry,
  registerNodes,
  onToggleSelect,
  governmentEntityId,
}: {
  subtype: SubtypeHierarchy;
  ancestorKeys: string[];
  parentIds: { agencyId: string; categoryId: string; typeId: string };
} & CommonProps) {
  const isNoSubpart = subtype.id === NO_SUBPART_ID;
  const key = stKey(subtype.id);
  const [open, setOpen] = useState(false);

  // For real subtypes, register the subtype itself in the selection registry.
  // The NO_SUBPART sentinel is not a real selectable node — its sections hoist
  // up under the parent Type.
  useEffect(() => {
    if (isNoSubpart) return;
    registerNodes([{
      key,
      level: 'SubType',
      name: subtype.subpartName,
      ancestorKeys,
      governmentEntityId,
      agencyId: parentIds.agencyId,
      regulationCategoryId: parentIds.categoryId,
      regulationTypeId: parentIds.typeId,
      regulationSubtypeId: subtype.id,
    }]);
  }, [key, subtype.id, subtype.subpartName, ancestorKeys, parentIds.agencyId, parentIds.categoryId, parentIds.typeId, isNoSubpart, governmentEntityId, registerNodes]);

  const shouldFetch = isNoSubpart || open;
  const { data: sections, isFetching } = useQuery({
    queryKey: ['reg-sections-picker', subtype.id, parentIds.typeId],
    queryFn: () => regulationService.getSections(subtype.id, isNoSubpart ? parentIds.typeId : undefined),
    enabled: shouldFetch,
    staleTime: Infinity,
  });

  const sectionAncestors = isNoSubpart ? ancestorKeys : [...ancestorKeys, key];
  const sectionParentIds = {
    agencyId: parentIds.agencyId,
    categoryId: parentIds.categoryId,
    typeId: parentIds.typeId,
    subtypeId: isNoSubpart ? undefined : subtype.id,
  };

  if (isNoSubpart) {
    if (isFetching && !sections) {
      return (
        <div className="flex items-center gap-2 py-1 pl-12 text-xs text-gray-400">
          <Loader2 size={12} className="animate-spin" /> Loading...
        </div>
      );
    }
    return (
      <>
        {(sections ?? []).map(section => (
          <SectionLeaf
            key={section.id}
            section={section}
            ancestorKeys={sectionAncestors}
            parentIds={sectionParentIds}
            indent="pl-16"
            selected={selected}
            registry={registry}
            registerNodes={registerNodes}
            onToggleSelect={onToggleSelect}
            governmentEntityId={governmentEntityId}
          />
        ))}
      </>
    );
  }

  return (
    <TreeRow
      nodeKey={key}
      label={subtype.subpartName}
      level="SubType"
      indent="pl-16"
      hasChildren
      selected={selected}
      registry={registry}
      onToggleSelect={onToggleSelect}
      onOpenChange={setOpen}
    >
      {isFetching && !sections ? (
        <div className="flex items-center gap-2 py-1 pl-20 text-xs text-gray-400">
          <Loader2 size={12} className="animate-spin" /> Loading...
        </div>
      ) : (
        (sections ?? []).map(section => (
          <SectionLeaf
            key={section.id}
            section={section}
            ancestorKeys={sectionAncestors}
            parentIds={sectionParentIds}
            indent="pl-20"
            selected={selected}
            registry={registry}
            registerNodes={registerNodes}
            onToggleSelect={onToggleSelect}
            governmentEntityId={governmentEntityId}
          />
        ))
      )}
    </TreeRow>
  );
}

function TypeNode({
  type,
  ancestorKeys,
  parentIds,
  selected,
  registry,
  registerNodes,
  onToggleSelect,
  governmentEntityId,
}: {
  type: TypeHierarchy;
  ancestorKeys: string[];
  parentIds: { agencyId: string; categoryId: string };
} & CommonProps) {
  const key = tKey(type.id);
  const [open, setOpen] = useState(false);

  useEffect(() => {
    registerNodes([{
      key,
      level: 'Type',
      name: type.partName,
      ancestorKeys,
      governmentEntityId,
      agencyId: parentIds.agencyId,
      regulationCategoryId: parentIds.categoryId,
      regulationTypeId: type.id,
    }]);
  }, [key, type.id, type.partName, ancestorKeys, parentIds.agencyId, parentIds.categoryId, governmentEntityId, registerNodes]);

  const { data: subtypes, isFetching } = useQuery({
    queryKey: ['reg-subtypes-picker', type.id],
    queryFn: () => regulationService.getSubtypes(type.id),
    enabled: open,
    staleTime: Infinity,
  });

  const childAncestors = [...ancestorKeys, key];

  return (
    <TreeRow
      nodeKey={key}
      label={type.partName}
      level="Type"
      indent="pl-12"
      hasChildren
      selected={selected}
      registry={registry}
      onToggleSelect={onToggleSelect}
      onOpenChange={setOpen}
    >
      {isFetching && !subtypes ? (
        <div className="flex items-center gap-2 py-1 pl-16 text-xs text-gray-400">
          <Loader2 size={12} className="animate-spin" /> Loading...
        </div>
      ) : (
        (subtypes ?? []).map(subtype => (
          <SubtypeNode
            key={subtype.id}
            subtype={subtype}
            ancestorKeys={childAncestors}
            parentIds={{ ...parentIds, typeId: type.id }}
            selected={selected}
            registry={registry}
            registerNodes={registerNodes}
            onToggleSelect={onToggleSelect}
            governmentEntityId={governmentEntityId}
          />
        ))
      )}
    </TreeRow>
  );
}

function CategoryNode({
  category,
  ancestorKeys,
  parentIds,
  selected,
  registry,
  registerNodes,
  onToggleSelect,
  governmentEntityId,
}: {
  category: CategoryHierarchy;
  ancestorKeys: string[];
  parentIds: { agencyId: string };
} & CommonProps) {
  const key = cKey(category.id);
  const [open, setOpen] = useState(false);

  useEffect(() => {
    registerNodes([{
      key,
      level: 'Category',
      name: category.subchapterName,
      ancestorKeys,
      governmentEntityId,
      agencyId: parentIds.agencyId,
      regulationCategoryId: category.id,
    }]);
  }, [key, category.id, category.subchapterName, ancestorKeys, parentIds.agencyId, governmentEntityId, registerNodes]);

  const { data: types, isFetching } = useQuery({
    queryKey: ['reg-types-picker', category.id],
    queryFn: () => regulationService.getTypes(category.id),
    enabled: open,
    staleTime: Infinity,
  });

  const childAncestors = [...ancestorKeys, key];

  return (
    <TreeRow
      nodeKey={key}
      label={category.subchapterName}
      level="Category"
      indent="pl-8"
      hasChildren
      selected={selected}
      registry={registry}
      onToggleSelect={onToggleSelect}
      onOpenChange={setOpen}
    >
      {isFetching && !types ? (
        <div className="flex items-center gap-2 py-1 pl-12 text-xs text-gray-400">
          <Loader2 size={12} className="animate-spin" /> Loading...
        </div>
      ) : (
        (types ?? []).map(type => (
          <TypeNode
            key={type.id}
            type={type}
            ancestorKeys={childAncestors}
            parentIds={{ ...parentIds, categoryId: category.id }}
            selected={selected}
            registry={registry}
            registerNodes={registerNodes}
            onToggleSelect={onToggleSelect}
            governmentEntityId={governmentEntityId}
          />
        ))
      )}
    </TreeRow>
  );
}

function AgencyNode({
  agency,
  ancestorKeys,
  selected,
  registry,
  registerNodes,
  onToggleSelect,
  governmentEntityId,
}: {
  agency: AgencyHierarchy;
  ancestorKeys: string[];
} & CommonProps) {
  const key = aKey(agency.id);
  const [open, setOpen] = useState(false);

  useEffect(() => {
    registerNodes([{
      key,
      level: 'Agency',
      name: agency.agencyName,
      ancestorKeys,
      governmentEntityId,
      agencyId: agency.id,
    }]);
  }, [key, agency.id, agency.agencyName, ancestorKeys, governmentEntityId, registerNodes]);

  const { data: categories, isFetching } = useQuery({
    queryKey: ['reg-categories-picker', agency.id],
    queryFn: () => regulationService.getCategories(agency.id),
    enabled: open,
    staleTime: Infinity,
  });

  const childAncestors = [...ancestorKeys, key];

  return (
    <TreeRow
      nodeKey={key}
      label={agency.agencyName}
      level="Agency"
      indent="pl-4"
      hasChildren
      selected={selected}
      registry={registry}
      onToggleSelect={onToggleSelect}
      onOpenChange={setOpen}
    >
      {isFetching && !categories ? (
        <div className="flex items-center gap-2 py-1 pl-8 text-xs text-gray-400">
          <Loader2 size={12} className="animate-spin" /> Loading...
        </div>
      ) : (
        (categories ?? []).map(category => (
          <CategoryNode
            key={category.id}
            category={category}
            ancestorKeys={childAncestors}
            parentIds={{ agencyId: agency.id }}
            selected={selected}
            registry={registry}
            registerNodes={registerNodes}
            onToggleSelect={onToggleSelect}
            governmentEntityId={governmentEntityId}
          />
        ))
      )}
    </TreeRow>
  );
}

// ── Main component ─────────────────────────────────────────────────────────
interface Props {
  governmentEntityId: string;
  governmentEntityName: string;
  titleName: string;
  selected: Set<string>;
  onSelectionChange: (next: Set<string>) => void;
  onRegistryChange?: (registry: NodeRegistry) => void;
}

export default function SubscriptionHierarchyPicker({
  governmentEntityId,
  governmentEntityName,
  titleName,
  selected,
  onSelectionChange,
  onRegistryChange,
}: Props) {
  const [registry, setRegistry] = useState<NodeRegistry>(() => new Map());

  // Reset registry when title changes
  useEffect(() => {
    setRegistry(new Map());
  }, [governmentEntityId]);

  const registerNodes = useCallback((entries: RegistryEntry[]) => {
    setRegistry(prev => {
      let changed = false;
      const next = new Map(prev);
      for (const entry of entries) {
        const existing = next.get(entry.key);
        if (!existing || existing.name !== entry.name) {
          next.set(entry.key, entry);
          changed = true;
        }
      }
      return changed ? next : prev;
    });
  }, []);

  useEffect(() => { onRegistryChange?.(registry); }, [registry, onRegistryChange]);

  // Register the Entity node up-front so it can be selected (Title-level subscription).
  const entityKey = eKey(governmentEntityId);
  useEffect(() => {
    registerNodes([{
      key: entityKey,
      level: 'Entity',
      name: `Title — ${titleName}`,
      ancestorKeys: [],
      governmentEntityId,
    }]);
  }, [entityKey, governmentEntityId, titleName, registerNodes]);

  const { data: agencies, isLoading } = useQuery({
    queryKey: ['reg-agencies-picker', governmentEntityId],
    queryFn: () => regulationService.getAgencies(governmentEntityId),
    enabled: !!governmentEntityId,
    staleTime: Infinity,
  });

  const toggleSelect = useCallback((key: string) => {
    const next = new Set(selected);
    if (next.has(key)) next.delete(key); else next.add(key);
    onSelectionChange(next);
  }, [selected, onSelectionChange]);

  if (isLoading) {
    return <div className="flex justify-center py-8"><div className="animate-spin rounded-full h-6 w-6 border-b-2 border-blue-600" /></div>;
  }
  if (!agencies || agencies.length === 0) {
    return <p className="text-sm text-gray-400 text-center py-4">No hierarchy data available.</p>;
  }

  return (
    <div className="border border-gray-200 rounded-lg overflow-hidden">
      <div className="bg-gray-50 px-3 py-2 border-b border-gray-200 flex items-center justify-between">
        <span className="text-xs font-semibold text-gray-600 uppercase tracking-wide">Regulation Hierarchy</span>
        <span className="text-xs font-semibold text-gray-600 uppercase tracking-wide">Subscribing Level</span>
      </div>
      <div className="max-h-[500px] overflow-y-auto p-1">
        <TreeRow
          nodeKey={entityKey}
          label={governmentEntityName}
          level="Entity"
          indent=""
          defaultOpen
          hasChildren
          selected={selected}
          registry={registry}
          onToggleSelect={toggleSelect}
        >
          {agencies.map(agency => (
            <AgencyNode
              key={agency.id}
              agency={agency}
              ancestorKeys={[entityKey]}
              selected={selected}
              registry={registry}
              registerNodes={registerNodes}
              onToggleSelect={toggleSelect}
              governmentEntityId={governmentEntityId}
            />
          ))}
        </TreeRow>
      </div>
    </div>
  );
}

// ── Build submission payload from selection + registry ─────────────────────
export function getSubscriptionItemsFromRegistry(
  selected: Set<string>,
  registry: NodeRegistry,
): SubscriptionItemPayload[] {
  const items: SubscriptionItemPayload[] = [];
  for (const key of selected) {
    const entry = registry.get(key);
    if (!entry) continue;
    // Drop selections whose ancestor is also selected (the ancestor covers them).
    if (entry.ancestorKeys.some(ak => selected.has(ak))) continue;

    items.push({
      governmentEntityId: entry.governmentEntityId,
      agencyId: entry.agencyId,
      regulationCategoryId: entry.regulationCategoryId,
      regulationTypeId: entry.regulationTypeId,
      regulationSubtypeId: entry.regulationSubtypeId,
      regulationId: entry.regulationId,
      subscribingLevel: entry.level,
      subscribedNodeName: entry.level === 'Entity' ? entry.name : entry.name,
    });
  }
  return items;
}

// Kept for type re-export convenience
export type { NodeRegistry as SubscriptionRegistry };

// Tiny stable hook to keep useMemo signature for callers that pass identity-stable args.
export function useStableRegistry(registry: NodeRegistry) {
  return useMemo(() => registry, [registry]);
}
