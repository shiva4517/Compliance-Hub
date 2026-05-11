import { ListChecks, X, ChevronRight, Users, AlertTriangle, CheckCircle } from 'lucide-react';

interface Props {
  lastOperation?: string;
  contextDataJson?: string;
  onClose: () => void;
}

interface CustomerEntry {
  customerId?: string;
  customerName?: string;
  customerCode?: string;
  isDuplicate?: boolean;
}

interface CreatedEntry {
  customerName?: string;
  subscriptionId?: string;
}

interface SkippedEntry {
  customerName?: string;
  reason?: string;
}

function parseContext(json?: string): Record<string, unknown> | null {
  if (!json) return null;
  try { return JSON.parse(json); } catch { return null; }
}

const OPERATION_LABELS: Record<string, string> = {
  preview_subscription: 'Subscription Preview',
  create_subscription:  'Subscription Created',
  delete_subscription:  'Subscription Deleted',
  list_subscriptions:   'Customer Subscriptions',
  get_subscription:     'Subscription Details',
};

export default function SubscriptionPreviewPanel({ lastOperation, contextDataJson, onClose }: Props) {
  const ctx = parseContext(contextDataJson);
  const title = lastOperation ? (OPERATION_LABELS[lastOperation] ?? lastOperation) : 'Preview';

  const data: Record<string, unknown> = (() => {
    if (!ctx) return {};
    if ('result' in ctx && typeof ctx.result === 'object' && ctx.result)
      return ctx.result as Record<string, unknown>;
    if ('data' in ctx && typeof ctx.data === 'object' && ctx.data)
      return ctx.data as Record<string, unknown>;
    if ('args' in ctx && typeof ctx.args === 'object' && ctx.args)
      return ctx.args as Record<string, unknown>;
    return ctx as Record<string, unknown>;
  })();

  const customers        = data.customers as CustomerEntry[] | undefined;
  // const newCustomers     = data.newCustomers as CustomerEntry[] | undefined;
  const duplicateCustomers = data.duplicateCustomers as CustomerEntry[] | undefined;
  const hierarchyPath    = data.hierarchyPath as string | undefined;
  const level            = data.subscribingLevel as string | undefined;
  const nodeName         = data.subscribedNodeName as string | undefined;
  const summary          = data.summary as string | undefined;
  const subscriptions    = data.subscriptions as unknown[] | undefined;
  const created          = data.created as CreatedEntry[] | undefined;
  const skipped          = data.skipped as SkippedEntry[] | undefined;

  return (
    <div className="w-80 border-l border-gray-200 bg-white flex flex-col h-full">
      {/* Header */}
      <div className="flex items-center justify-between px-4 py-3 border-b border-gray-200 shrink-0">
        <div className="flex items-center gap-2">
          <ListChecks size={16} className="text-blue-600" />
          <span className="text-sm font-semibold text-gray-800">{title}</span>
        </div>
        <button onClick={onClose} className="p-1 rounded hover:bg-gray-100 text-gray-400 hover:text-gray-600 transition-colors">
          <X size={14} />
        </button>
      </div>

      <div className="flex-1 overflow-y-auto px-4 py-3 space-y-4">
        {!ctx ? (
          <p className="text-xs text-gray-400 text-center mt-8">No details to show yet.</p>
        ) : (
          <>
            {/* Summary banner */}
            {summary && (
              <div className="bg-blue-50 border border-blue-200 rounded-lg px-3 py-2">
                <p className="text-xs text-blue-700">{summary}</p>
              </div>
            )}

            {/* Customers — multi-customer preview */}
            {customers && customers.length > 0 && (
              <Section label={<span className="flex items-center gap-1"><Users size={11} />Customers ({customers.length})</span>}>
                <div className="space-y-1.5">
                  {customers.map((c, i) => (
                    <div key={i} className={`flex items-center justify-between rounded px-2 py-1.5 text-xs ${
                      c.isDuplicate ? 'bg-amber-50 border border-amber-200' : 'bg-gray-50'
                    }`}>
                      <div>
                        <p className="font-medium text-gray-800">{c.customerName ?? 'Unknown'}</p>
                        {c.customerCode && <p className="text-gray-400">{c.customerCode}</p>}
                      </div>
                      {c.isDuplicate ? (
                        <span className="flex items-center gap-0.5 text-amber-600 shrink-0 ml-2">
                          <AlertTriangle size={11} />
                          <span>Duplicate</span>
                        </span>
                      ) : (
                        <span className="flex items-center gap-0.5 text-green-600 shrink-0 ml-2">
                          <CheckCircle size={11} />
                          <span>New</span>
                        </span>
                      )}
                    </div>
                  ))}
                </div>
                {duplicateCustomers && duplicateCustomers.length > 0 && (
                  <p className="text-xs text-amber-600 mt-1.5">
                    ⚠ {duplicateCustomers.length} customer(s) already have this subscription
                  </p>
                )}
              </Section>
            )}

            {/* Hierarchy path */}
            {hierarchyPath && (
              <Section label="Regulation Hierarchy">
                <div className="flex flex-col gap-1">
                  {hierarchyPath.split(' → ').map((seg, i) => (
                    <div key={i} className="flex items-start gap-1.5">
                      <ChevronRight size={13} className="text-blue-400 mt-0.5 shrink-0" />
                      <span className="text-sm text-gray-700">{seg}</span>
                    </div>
                  ))}
                </div>
              </Section>
            )}

            {/* Subscribing level */}
            {level && (
              <Section label="Subscribing Level">
                <span className="inline-block px-2 py-0.5 bg-blue-50 text-blue-700 text-xs font-semibold rounded">
                  {level}
                </span>
                {nodeName && <p className="text-xs text-gray-500 mt-1">{nodeName}</p>}
              </Section>
            )}

            {/* Created results */}
            {created && created.length > 0 && (
              <Section label={`Created (${created.length})`}>
                <div className="space-y-1">
                  {created.map((c, i) => (
                    <div key={i} className="flex items-center gap-1.5 bg-green-50 rounded px-2 py-1.5">
                      <CheckCircle size={12} className="text-green-600 shrink-0" />
                      <span className="text-xs text-gray-800 font-medium">{c.customerName}</span>
                    </div>
                  ))}
                </div>
              </Section>
            )}

            {/* Skipped */}
            {skipped && skipped.length > 0 && (
              <Section label={`Skipped (${skipped.length})`}>
                <div className="space-y-1">
                  {skipped.map((s, i) => (
                    <div key={i} className="flex items-center gap-1.5 bg-amber-50 rounded px-2 py-1.5">
                      <AlertTriangle size={12} className="text-amber-500 shrink-0" />
                      <div>
                        <p className="text-xs text-gray-800 font-medium">{s.customerName}</p>
                        <p className="text-xs text-gray-400">{s.reason}</p>
                      </div>
                    </div>
                  ))}
                </div>
              </Section>
            )}

            {/* List subscriptions view */}
            {subscriptions && Array.isArray(subscriptions) && subscriptions.length > 0 && (
              <Section label={`Subscriptions (${subscriptions.length})`}>
                <div className="space-y-2">
                  {(subscriptions as Record<string, unknown>[]).map((s, i) => (
                    <div key={i} className="bg-gray-50 rounded p-2 text-xs text-gray-700 space-y-0.5">
                      <p className="font-medium">{String(s.subscribedNodeName ?? '')}</p>
                      <p className="text-gray-400">{String(s.subscribingLevel ?? '')}</p>
                    </div>
                  ))}
                </div>
              </Section>
            )}
          </>
        )}
      </div>
    </div>
  );
}

function Section({ label, children }: { label: React.ReactNode; children: React.ReactNode }) {
  return (
    <div>
      <p className="text-xs text-gray-400 font-medium uppercase tracking-wide mb-1.5">{label}</p>
      {children}
    </div>
  );
}
