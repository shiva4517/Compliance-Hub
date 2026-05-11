import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { ArrowLeft, ChevronDown, ChevronUp } from 'lucide-react';
import { regulationChangeLogService } from '../../services/regulationChangeLogService';

function formatDate(iso?: string | null) {
  if (!iso) return '—';
  return new Date(iso).toLocaleString('en-US', {
    day: '2-digit', month: 'short', year: 'numeric',
    hour: '2-digit', minute: '2-digit', timeZone: 'UTC', timeZoneName: 'short',
  });
}

function formatDateOnly(d?: string | null) {
  if (!d) return '—';
  return new Date(d + 'T00:00:00Z').toLocaleDateString('en-US', {
    day: '2-digit', month: 'short', year: 'numeric', timeZone: 'UTC',
  });
}

function Field({ label, value, mono }: { label: string; value?: string | number | null; mono?: boolean }) {
  return (
    <div>
      <dt className="text-xs text-gray-500 font-medium uppercase tracking-wide">{label}</dt>
      <dd className={`mt-0.5 text-sm text-gray-900 ${mono ? 'font-mono text-xs break-all' : ''}`}>{value ?? '—'}</dd>
    </div>
  );
}

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="card p-5">
      <h3 className="text-sm font-semibold text-gray-700 mb-4 pb-2 border-b border-gray-100">{title}</h3>
      <dl className="grid grid-cols-2 gap-4">{children}</dl>
    </div>
  );
}

function HtmlPanel({ label, html }: { label: string; html?: string | null }) {
  const [expanded, setExpanded] = useState(false);
  const isLarge = (html?.length ?? 0) > 50_000;

  if (!html) {
    return (
      <div className="flex-1 border border-gray-200 rounded-lg overflow-hidden">
        <div className="bg-gray-50 px-4 py-2 border-b border-gray-200 text-xs font-semibold text-gray-600">{label}</div>
        <div className="p-4 text-gray-400 text-sm">No content available.</div>
      </div>
    );
  }

  return (
    <div className="flex-1 border border-gray-200 rounded-lg overflow-hidden">
      <div className="bg-gray-50 px-4 py-2 border-b border-gray-200 flex items-center justify-between">
        <span className="text-xs font-semibold text-gray-600">{label}</span>
        {isLarge && (
          <button
            onClick={() => setExpanded(e => !e)}
            className="flex items-center gap-1 text-xs text-blue-600 hover:text-blue-800"
          >
            {expanded ? <><ChevronUp size={13} /> Collapse</> : <><ChevronDown size={13} /> Expand</>}
          </button>
        )}
      </div>
      <div
        className={`overflow-auto transition-all ${isLarge && !expanded ? 'max-h-64' : 'max-h-[600px]'} p-4 text-sm bg-white`}
        // HTML content is system-generated from eCFR government regulations, not user input
        dangerouslySetInnerHTML={{ __html: html }}
      />
      {isLarge && !expanded && (
        <div className="border-t border-gray-200 px-4 py-2 bg-gray-50 text-center">
          <button onClick={() => setExpanded(true)} className="text-xs text-blue-600 hover:text-blue-800">
            Content truncated — click Expand to view full content
          </button>
        </div>
      )}
    </div>
  );
}

export default function ChangeNoticeDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const { data: log, isLoading } = useQuery({
    queryKey: ['regulation-change-log', id],
    queryFn: () => regulationChangeLogService.getById(id!),
    enabled: !!id,
  });

  if (isLoading) {
    return <div className="text-center text-gray-400 py-20">Loading...</div>;
  }

  if (!log) {
    return (
      <div className="text-center text-gray-400 py-20">
        <p>Change notice not found.</p>
        <button onClick={() => navigate('/change-notices')} className="btn-primary mt-4">Back to list</button>
      </div>
    );
  }

  return (
    <div className="space-y-4 max-w-5xl">
      <div className="flex items-center gap-3">
        <button
          onClick={() => navigate('/change-notices')}
          className="flex items-center gap-1.5 text-sm text-gray-500 hover:text-gray-800 transition-colors"
        >
          <ArrowLeft size={16} /> Back
        </button>
        <h1 className="text-xl font-bold text-gray-900">Change Notice Detail</h1>
      </div>

      {/* 1. Hierarchy Details */}
      <div className="card p-5">
        <h3 className="text-sm font-semibold text-gray-700 mb-4 pb-2 border-b border-gray-100">Hierarchy Details</h3>
        <dl className="grid grid-cols-2 gap-4">
          <Field label="Government Entity" value={log.governmentEntityName} />
          <Field label="Gov. Entity ID" value={log.governmentEntityId} mono />
          <Field label="Agency" value={log.agencyName} />
          <Field label="Agency ID" value={log.agencyId} mono />
          <Field label="Category" value={log.regulationCategoryName} />
          <Field label="Category ID" value={log.regulationCategoryId} mono />
          <Field label="Type" value={log.regulationTypeName} />
          <Field label="Type ID" value={log.regulationTypeId} mono />
          {(log.regulationSubtypeName || log.regulationSubtypeId) && (
            <>
              <Field label="Subtype" value={log.regulationSubtypeName} />
              <Field label="Subtype ID" value={log.regulationSubtypeId} mono />
            </>
          )}
        </dl>
      </div>

      {/* 2. Section Details */}
      <Section title="Section Details">
        <Field label="Section Number" value={log.sectionNumber} />
        <div />
        <div className="col-span-2">
          <dt className="text-xs text-gray-500 font-medium uppercase tracking-wide">Section Title</dt>
          <dd className="mt-0.5 text-sm text-gray-900">{log.sectionTitle || '—'}</dd>
        </div>
      </Section>

      {/* 3. Version Info */}
      <Section title="Version Info">
        <Field label="Archived Version" value={log.archivedVersion} />
        <Field label="Current Version" value={log.newVersion} />
        <Field label="Previous Amended Date" value={formatDateOnly(log.previousAmendedDate)} />
        <Field label="New Amended Date" value={formatDateOnly(log.newAmendedDate)} />
        <Field label="Changed At" value={formatDate(log.changedAt)} />
      </Section>

      {/* 4. Content Hashes */}
      {(log.previousContentHash || log.newContentHash) && (
        <div className="card p-5">
          <h3 className="text-sm font-semibold text-gray-700 mb-4 pb-2 border-b border-gray-100">Content Hashes (SHA-256)</h3>
          <dl className="space-y-3">
            <div>
              <dt className="text-xs text-gray-500 font-medium uppercase tracking-wide mb-1">Previous Hash</dt>
              <dd className="font-mono text-xs text-gray-700 bg-gray-50 rounded px-3 py-2 break-all">{log.previousContentHash ?? '—'}</dd>
            </div>
            <div>
              <dt className="text-xs text-gray-500 font-medium uppercase tracking-wide mb-1">New Hash</dt>
              <dd className="font-mono text-xs text-gray-700 bg-gray-50 rounded px-3 py-2 break-all">{log.newContentHash ?? '—'}</dd>
            </div>
          </dl>
        </div>
      )}

      {/* 5. Content Comparison — side-by-side */}
      <div className="card p-5">
        <h3 className="text-sm font-semibold text-gray-700 mb-4 pb-2 border-b border-gray-100">
          Content Comparison
          <span className="ml-2 text-xs font-normal text-gray-400">v{log.archivedVersion} → v{log.newVersion}</span>
        </h3>
        <div className="flex gap-4">
          <HtmlPanel label={`Previous (v${log.archivedVersion})`} html={log.previousHtmlContent} />
          <HtmlPanel label={`Current (v${log.newVersion})`} html={log.newHtmlContent} />
        </div>
      </div>
    </div>
  );
}
