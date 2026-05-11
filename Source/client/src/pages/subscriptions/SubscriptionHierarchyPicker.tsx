import { useQuery } from '@tanstack/react-query';
import { ChevronDown, ChevronRight } from 'lucide-react';
import { useState } from 'react';
import { regulationService } from '../../services/regulationService';
import type { RegulationHierarchy, AgencyHierarchy, CategoryHierarchy, TypeHierarchy, SubscribingLevel, SubscriptionItemPayload } from '../../types';

// ── Key helpers ────────────────────────────────────────────────────────────
function eKey(id: string) { return `Entity:${id}`; }
function aKey(id: string) { return `Agency:${id}`; }
function cKey(id: string) { return `Category:${id}`; }
function tKey(id: string) { return `Type:${id}`; }
function stKey(id: string) { return `SubType:${id}`; }
function rKey(id: string) { return `Regulation:${id}`; }

// Build flat tree structure for ancestor lookup
interface TreeNode { key: string; ancestorKeys: string[] }
function buildTreeNodes(h: RegulationHierarchy): TreeNode[] {
  const nodes: TreeNode[] = [];
  const entityKey = eKey(h.governmentEntityId);
  nodes.push({ key: entityKey, ancestorKeys: [] });
  for (const agency of h.agencies) {
    const ak = aKey(agency.id);
    nodes.push({ key: ak, ancestorKeys: [entityKey] });
    for (const cat of agency.categories) {
      const ck = cKey(cat.id);
      nodes.push({ key: ck, ancestorKeys: [entityKey, ak] });
      for (const type of cat.types) {
        const tk = tKey(type.id);
        nodes.push({ key: tk, ancestorKeys: [entityKey, ak, ck] });
        for (const sub of type.subtypes) {
          const sk = stKey(sub.id);
          nodes.push({ key: sk, ancestorKeys: [entityKey, ak, ck, tk] });
          for (const sec of sub.sections) {
            nodes.push({ key: rKey(sec.id), ancestorKeys: [entityKey, ak, ck, tk, sk] });
          }
        }
        // Direct regulations under the type (no subtype)
        for (const sec of type.sections ?? []) {
          nodes.push({ key: rKey(sec.id), ancestorKeys: [entityKey, ak, ck, tk] });
        }
      }
    }
  }
  return nodes;
}

function isInherited(key: string, selected: Set<string>, treeNodes: TreeNode[]): boolean {
  const node = treeNodes.find(n => n.key === key);
  return node ? node.ancestorKeys.some(ak => selected.has(ak)) : false;
}

function isChecked(key: string, selected: Set<string>, treeNodes: TreeNode[]): boolean {
  return selected.has(key) || isInherited(key, selected, treeNodes);
}

function getAllDescendantKeys(key: string, h: RegulationHierarchy): string[] {
  const entityKey = eKey(h.governmentEntityId);
  if (key === entityKey) {
    return [entityKey, ...h.agencies.flatMap(a => [
      aKey(a.id),
      ...a.categories.flatMap(c => [
        cKey(c.id),
        ...c.types.flatMap(t => [
          tKey(t.id),
          ...t.subtypes.flatMap(s => [stKey(s.id), ...s.sections.map(sec => rKey(sec.id))]),
          ...(t.sections ?? []).map(sec => rKey(sec.id)),
        ]),
      ]),
    ])];
  }
  // For other levels find in hierarchy
  for (const agency of h.agencies) {
    if (aKey(agency.id) === key) return [aKey(agency.id), ...descendantsOfAgency(agency)];
    for (const cat of agency.categories) {
      if (cKey(cat.id) === key) return [cKey(cat.id), ...descendantsOfCategory(cat)];
      for (const type of cat.types) {
        if (tKey(type.id) === key) return [tKey(type.id), ...descendantsOfType(type)];
        for (const sub of type.subtypes) {
          if (stKey(sub.id) === key) return [stKey(sub.id), ...sub.sections.map(s => rKey(s.id))];
          for (const sec of sub.sections) {
            if (rKey(sec.id) === key) return [rKey(sec.id)];
          }
        }
      }
    }
  }
  return [key];
}
function descendantsOfAgency(a: AgencyHierarchy): string[] {
  return a.categories.flatMap(c => [cKey(c.id), ...descendantsOfCategory(c)]);
}
function descendantsOfCategory(c: CategoryHierarchy): string[] {
  return c.types.flatMap(t => [tKey(t.id), ...descendantsOfType(t)]);
}
function descendantsOfType(t: TypeHierarchy): string[] {
  return [
    ...t.subtypes.flatMap(s => [stKey(s.id), ...s.sections.map(sec => rKey(sec.id))]),
    ...(t.sections ?? []).map(sec => rKey(sec.id)),
  ];
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
  return <span className={`text-[10px] px-1.5 py-0.5 rounded-full font-medium flex-shrink-0 ${levelColors[level] ?? 'bg-gray-100 text-gray-600'}`}>{level}</span>;
}

// ── Row ────────────────────────────────────────────────────────────────────
function TreeRow({ nodeKey, label, level, indent, selected, treeNodes, onToggle, children }: {
  nodeKey: string; label: string; level: SubscribingLevel; indent: string;
  selected: Set<string>; treeNodes: TreeNode[];
  onToggle: (key: string) => void;
  children?: React.ReactNode;
}) {
  const [open, setOpen] = useState(level === 'Entity' || level === 'Agency');
  const inherited = isInherited(nodeKey, selected, treeNodes);
  const checked = isChecked(nodeKey, selected, treeNodes);
  const hasChildren = !!children;

  return (
    <div>
      <div className={`flex items-center gap-2 py-1 px-2 rounded hover:bg-gray-50 ${indent}`}>
        {hasChildren ? (
          <button onClick={() => setOpen(o => !o)} className="text-gray-400 flex-shrink-0">
            {open ? <ChevronDown size={13} /> : <ChevronRight size={13} />}
          </button>
        ) : <span className="w-[13px] flex-shrink-0" />}
        <input
          type="checkbox"
          checked={checked}
          disabled={inherited}
          onChange={() => onToggle(nodeKey)}
          className="h-4 w-4 rounded border-gray-300 text-blue-600 flex-shrink-0 disabled:opacity-50"
        />
        <span className={`text-sm flex-1 min-w-0 truncate ${inherited ? 'text-gray-400' : 'text-gray-800'}`}>{label}</span>
        <LevelBadge level={level} />
      </div>
      {open && children}
    </div>
  );
}

// ── Main component ─────────────────────────────────────────────────────────
interface Props {
  governmentEntityId: string;
  governmentEntityName: string;
  selected: Set<string>;
  onSelectionChange: (next: Set<string>) => void;
}

export default function SubscriptionHierarchyPicker({ governmentEntityId, governmentEntityName, selected, onSelectionChange }: Props) {
  const { data: hierarchy, isLoading } = useQuery({
    queryKey: ['regulation-hierarchy', governmentEntityId],
    queryFn: () => regulationService.getHierarchy(governmentEntityId),
    enabled: !!governmentEntityId,
  });

  if (isLoading) return <div className="flex justify-center py-8"><div className="animate-spin rounded-full h-6 w-6 border-b-2 border-blue-600" /></div>;
  if (!hierarchy) return <p className="text-sm text-gray-400 text-center py-4">No hierarchy data available.</p>;

  const treeNodes = buildTreeNodes(hierarchy);

  const toggle = (key: string) => {
    const next = new Set(selected);
    if (next.has(key)) {
      // Uncheck key + all descendants
      const desc = getAllDescendantKeys(key, hierarchy);
      desc.forEach(k => next.delete(k));
    } else {
      // Check key + all descendants
      const desc = getAllDescendantKeys(key, hierarchy);
      desc.forEach(k => next.add(k));
    }
    onSelectionChange(next);
  };

  const entityKey = eKey(hierarchy.governmentEntityId);

  return (
    <div className="border border-gray-200 rounded-lg overflow-hidden">
      <div className="bg-gray-50 px-3 py-2 border-b border-gray-200 flex items-center justify-between">
        <span className="text-xs font-semibold text-gray-600 uppercase tracking-wide">Regulation Hierarchy</span>
        <span className="text-xs font-semibold text-gray-600 uppercase tracking-wide">Subscribing Level</span>
      </div>
      <div className="max-h-[500px] overflow-y-auto p-1">
        <TreeRow nodeKey={entityKey} label={governmentEntityName} level="Entity" indent="" selected={selected} treeNodes={treeNodes} onToggle={toggle}>
          {hierarchy.agencies.map(agency => (
            <TreeRow key={agency.id} nodeKey={aKey(agency.id)} label={`${agency.agencyName}`} level="Agency" indent="pl-4" selected={selected} treeNodes={treeNodes} onToggle={toggle}>
              {agency.categories.map(cat => (
                <TreeRow key={cat.id} nodeKey={cKey(cat.id)} label={`${cat.subchapterName}`} level="Category" indent="pl-8" selected={selected} treeNodes={treeNodes} onToggle={toggle}>
                  {cat.types.map(type => (
                    <TreeRow key={type.id} nodeKey={tKey(type.id)} label={`${type.partName}`} level="Type" indent="pl-12" selected={selected} treeNodes={treeNodes} onToggle={toggle}>
                      {type.subtypes.map(sub => (
                        <TreeRow key={sub.id} nodeKey={stKey(sub.id)} label={`${sub.subpartName}`} level="SubType" indent="pl-16" selected={selected} treeNodes={treeNodes} onToggle={toggle}>
                          {sub.sections.map(sec => (
                            <TreeRow key={sec.id} nodeKey={rKey(sec.id)} label={`${sec.sectionName}`} level="Regulation" indent="pl-20" selected={selected} treeNodes={treeNodes} onToggle={toggle} />
                          ))}
                        </TreeRow>
                      ))}
                      {(type.sections ?? []).map(sec => (
                        <TreeRow key={sec.id} nodeKey={rKey(sec.id)} label={`${sec.sectionName}`} level="Regulation" indent="pl-16" selected={selected} treeNodes={treeNodes} onToggle={toggle} />
                      ))}
                    </TreeRow>
                  ))}
                </TreeRow>
              ))}
            </TreeRow>
          ))}
        </TreeRow>
      </div>
    </div>
  );
}

// ── Export helper to build items from selected keys ────────────────────────
export function getSubscriptionItems(
  selected: Set<string>,
  hierarchy: RegulationHierarchy,
  treeNodes: TreeNode[]
): SubscriptionItemPayload[] {
  const items: SubscriptionItemPayload[] = [];
  for (const key of selected) {
    // Only include "root" selections (no ancestor also in selected set)
    const node = treeNodes.find(n => n.key === key);
    if (!node) continue;
    if (node.ancestorKeys.some(ak => selected.has(ak))) continue;

    const [levelStr, id] = key.split(':');
    const level = levelStr as SubscribingLevel;
    let nodeName = '';
    let agencyId: string | undefined;
    let regulationCategoryId: string | undefined;
    let regulationTypeId: string | undefined;
    let regulationSubtypeId: string | undefined;
    let regulationId: string | undefined;

    if (level === 'Entity') {
      nodeName = `Title — ${hierarchy.titleName}`;
    } else if (level === 'Agency') {
      const agency = hierarchy.agencies.find(a => a.id === id);
      nodeName = agency ? agency.agencyName : id;
      agencyId = id;
    } else if (level === 'Category') {
      for (const agency of hierarchy.agencies) {
        const cat = agency.categories.find(c => c.id === id);
        if (cat) { nodeName = cat.subchapterName; agencyId = agency.id; regulationCategoryId = id; break; }
      }
    } else if (level === 'Type') {
      for (const agency of hierarchy.agencies) {
        for (const cat of agency.categories) {
          const type = cat.types.find(t => t.id === id);
          if (type) { nodeName = type.partName; agencyId = agency.id; regulationCategoryId = cat.id; regulationTypeId = id; break; }
        }
      }
    } else if (level === 'SubType') {
      for (const agency of hierarchy.agencies) {
        for (const cat of agency.categories) {
          for (const type of cat.types) {
            const sub = type.subtypes.find(s => s.id === id);
            if (sub) { nodeName = sub.subpartName; agencyId = agency.id; regulationCategoryId = cat.id; regulationTypeId = type.id; regulationSubtypeId = id; break; }
          }
        }
      }
    } else if (level === 'Regulation') {
      outer:
      for (const agency of hierarchy.agencies) {
        for (const cat of agency.categories) {
          for (const type of cat.types) {
            // Direct regulations (no subtype)
            const direct = (type.sections ?? []).find(s => s.id === id);
            if (direct) {
              nodeName = `§ ${direct.sectionNumber} ${direct.sectionName}`;
              agencyId = agency.id; regulationCategoryId = cat.id; regulationTypeId = type.id;
              regulationSubtypeId = undefined; regulationId = id;
              break outer;
            }
            for (const sub of type.subtypes) {
              const sec = sub.sections.find(s => s.id === id);
              if (sec) { nodeName = `§ ${sec.sectionNumber} ${sec.sectionName}`; agencyId = agency.id; regulationCategoryId = cat.id; regulationTypeId = type.id; regulationSubtypeId = sub.id === '00000000-0000-0000-0000-000000000000' ? undefined : sub.id; regulationId = id; break outer; }
            }
          }
        }
      }
    }

    items.push({
      governmentEntityId: hierarchy.governmentEntityId,
      agencyId,
      regulationCategoryId,
      regulationTypeId,
      regulationSubtypeId,
      regulationId,
      subscribingLevel: level,
      subscribedNodeName: nodeName,
    });
  }

  return items;
}

export { buildTreeNodes };
