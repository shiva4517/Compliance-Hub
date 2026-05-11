import { Building2, X } from 'lucide-react';

interface Props {
  lastOperation?: string;
  contextDataJson?: string;
  onClose: () => void;
}

function parseContext(json?: string): Record<string, unknown> | null {
  if (!json) return null;
  try { return JSON.parse(json); } catch { return null; }
}

function FieldRow({ label, value }: { label: string; value: unknown }) {
  if (value === null || value === undefined || value === '') return null;
  return (
    <div className="flex flex-col gap-0.5 py-2 border-b border-gray-100 last:border-0">
      <span className="text-xs text-gray-400 font-medium uppercase tracking-wide">{label}</span>
      <span className="text-sm text-gray-800">{String(value)}</span>
    </div>
  );
}

const OPERATION_LABELS: Record<string, string> = {
  create_company: 'Creating Company',
  update_company: 'Updating Company',
  list_companies: 'Company List',
  get_company:    'Company Details',
  preview_company_data: 'Preview',
};

export default function PreviewPanel({ lastOperation, contextDataJson, onClose }: Props) {
  const ctx = parseContext(contextDataJson);

  const title = lastOperation ? (OPERATION_LABELS[lastOperation] ?? lastOperation) : 'Preview';

  const displayData: Record<string, unknown> = (() => {
    if (!ctx) return {};
    // For create/update, backend stores { args, result }
    if (typeof ctx === 'object' && 'result' in ctx && typeof ctx.result === 'object' && ctx.result) {
      return ctx.result as Record<string, unknown>;
    }
    if (typeof ctx === 'object' && 'args' in ctx && typeof ctx.args === 'object' && ctx.args) {
      return ctx.args as Record<string, unknown>;
    }
    return ctx as Record<string, unknown>;
  })();

  return (
    <div className="w-72 border-l border-gray-200 bg-white flex flex-col h-full">
      <div className="flex items-center justify-between px-4 py-3 border-b border-gray-200">
        <div className="flex items-center gap-2">
          <Building2 size={16} className="text-blue-600" />
          <span className="text-sm font-semibold text-gray-800">{title}</span>
        </div>
        <button onClick={onClose} className="p-1 rounded hover:bg-gray-100 text-gray-400 hover:text-gray-600 transition-colors">
          <X size={14} />
        </button>
      </div>

      <div className="flex-1 overflow-y-auto px-4 py-2">
        {Object.keys(displayData).length === 0 ? (
          <p className="text-xs text-gray-400 text-center mt-8">No details to show yet.</p>
        ) : (
          Object.entries(displayData).map(([k, v]) => (
            <FieldRow key={k} label={k.replace(/_/g, ' ')} value={v} />
          ))
        )}
      </div>
    </div>
  );
}
