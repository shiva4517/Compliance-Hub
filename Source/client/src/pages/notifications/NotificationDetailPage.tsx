import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { ArrowLeft, RotateCcw, CheckCircle, XCircle } from 'lucide-react';
import { notificationHistoryService } from '../../services/notificationHistoryService';
import type { NotificationLog } from '../../types';
import { useAuth } from '../../contexts/AuthContext';

function StatusBadge({ status }: { status: NotificationLog['status'] }) {
  const styles: Record<string, string> = {
    Sent: 'bg-green-100 text-green-800',
    Failed: 'bg-red-100 text-red-800',
    Pending: 'bg-yellow-100 text-yellow-800',
  };
  return (
    <span className={`inline-flex items-center px-2.5 py-1 rounded-full text-xs font-semibold ${styles[status] ?? 'bg-gray-100 text-gray-700'}`}>
      {status}
    </span>
  );
}

function Field({ label, value }: { label: string; value?: string | null }) {
  return (
    <div>
      <dt className="text-xs text-gray-500 font-medium uppercase tracking-wide">{label}</dt>
      <dd className="mt-0.5 text-sm text-gray-900">{value || '—'}</dd>
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

export default function NotificationDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const qc = useQueryClient();
  const { user } = useAuth();
  const companyId = user!.userId;
  const [toast, setToast] = useState<{ type: 'success' | 'error'; message: string } | null>(null);

  const { data: notification, isLoading } = useQuery({
    queryKey: ['notification-log', id, companyId],
    queryFn: () => notificationHistoryService.getById(id!, companyId),
    enabled: !!id && !!companyId,
  });

  const retryMutation = useMutation({
    mutationFn: () => notificationHistoryService.retry(id!, companyId),
    onSuccess: (response) => {
      setToast({
        type: response.data?.success ? 'success' : 'error',
        message: response.data?.message || response.message || 'Retry completed.',
      });
      qc.invalidateQueries({ queryKey: ['notification-log', id] });
      qc.invalidateQueries({ queryKey: ['notification-logs'] });
    },
    onError: (error: any) => {
      setToast({
        type: 'error',
        message: error?.response?.data?.message || 'Retry failed. Please try again.',
      });
    },
  });

  useEffect(() => {
    if (!toast) {
      return;
    }

    const timeoutId = window.setTimeout(() => {
      setToast(null);
    }, 4000);

    return () => window.clearTimeout(timeoutId);
  }, [toast]);

  if (isLoading) {
    return <div className="text-center text-gray-400 py-20">Loading...</div>;
  }

  if (!notification) {
    return (
      <div className="text-center text-gray-400 py-20">
        <p>Notification not found.</p>
        <button onClick={() => navigate('/notifications')} className="btn-primary mt-4">Back to list</button>
      </div>
    );
  }

  const n = notification;

  return (
    <div className="space-y-4 max-w-4xl">
      {toast && (
        <div className={`rounded-lg border px-4 py-3 text-sm ${
          toast.type === 'success'
            ? 'border-green-200 bg-green-50 text-green-700'
            : 'border-red-200 bg-red-50 text-red-700'
        }`}>
          {toast.message}
        </div>
      )}

      <div className="flex items-center gap-3">
        <button
          onClick={() => navigate('/notifications')}
          className="flex items-center gap-1.5 text-sm text-gray-500 hover:text-gray-800 transition-colors"
        >
          <ArrowLeft size={16} /> Back
        </button>
        <h1 className="text-xl font-bold text-gray-900">Notification Detail</h1>
      </div>

      {/* 1. Regulation Details */}
      <Section title="Regulation Details">
        <Field label="Government Entity" value={n.governmentEntityName} />
        <Field label="Agency" value={n.agencyName} />
        <Field label="Category" value={n.regulationCategoryName} />
        <Field label="Type" value={n.regulationTypeName} />
        <Field label="Subtype" value={n.regulationSubtypeName} />
      </Section>

      {/* 2. Change */}
      <Section title="Change">
        <Field label="Previous Regulation" value={n.previousRegulationName} />
        <Field label="Present Regulation" value={n.presentRegulationName} />
      </Section>

      {/* 3. Sender / Recipient */}
      <Section title="Sender / Recipient">
        <Field label="Sender Name" value={n.senderName} />
        <Field label="Sender Email" value={n.senderEmail} />
        <Field label="Recipient Name" value={n.recipientName} />
        <Field label="Recipient Email" value={n.recipientEmail} />
      </Section>

      {/* 4. Email Content */}
      {(n.subject || n.body) && (
        <div className="card p-5">
          <h3 className="text-sm font-semibold text-gray-700 mb-4 pb-2 border-b border-gray-100">Email Content</h3>
          {n.subject && (
            <div className="mb-3">
              <p className="text-xs text-gray-500 font-medium uppercase tracking-wide">Subject</p>
              <p className="mt-0.5 text-sm text-gray-900">{n.subject}</p>
            </div>
          )}
          {n.body && (
            <div>
              <p className="text-xs text-gray-500 font-medium uppercase tracking-wide mb-2">Body</p>
              {/* Body is system-generated content from EmailService, not user input */}
              <div
                className="border border-gray-200 rounded-lg p-4 overflow-auto max-h-96 text-sm bg-white"
                dangerouslySetInnerHTML={{ __html: n.body }}
              />
            </div>
          )}
        </div>
      )}

      {/* 5. Status & Retry */}
      <div className="card p-5">
        <h3 className="text-sm font-semibold text-gray-700 mb-4 pb-2 border-b border-gray-100">Status & Delivery</h3>
        <div className="grid grid-cols-2 gap-4">
          <div>
            <p className="text-xs text-gray-500 font-medium uppercase tracking-wide">Status</p>
            <div className="mt-1"><StatusBadge status={n.status} /></div>
          </div>
          <div>
            <p className="text-xs text-gray-500 font-medium uppercase tracking-wide">Notified</p>
            <p className="mt-0.5 text-sm text-gray-900">{n.isNotified ? 'Yes' : 'No'}</p>
          </div>
          <div>
            <p className="text-xs text-gray-500 font-medium uppercase tracking-wide">Retry Count</p>
            <p className="mt-0.5 text-sm text-gray-900">{n.retryCount}</p>
          </div>
          {n.lastRetryAt && (
            <div>
              <p className="text-xs text-gray-500 font-medium uppercase tracking-wide">Last Retry</p>
              <p className="mt-0.5 text-sm text-gray-900">{new Date(n.lastRetryAt).toLocaleString()}</p>
            </div>
          )}
          <div>
            <p className="text-xs text-gray-500 font-medium uppercase tracking-wide">Sent At</p>
            <p className="mt-0.5 text-sm text-gray-900">{n.createdAt ? new Date(n.createdAt).toLocaleString() : '—'}</p>
          </div>
          {n.failureReason && (
            <div className="col-span-2">
              <p className="text-xs text-gray-500 font-medium uppercase tracking-wide">Failure Reason</p>
              <p className="mt-0.5 text-sm text-red-600">{n.failureReason}</p>
            </div>
          )}
        </div>
        {n.status !== 'Sent' && (
          <div className="mt-4 pt-4 border-t border-gray-100">
            <button
              onClick={() => retryMutation.mutate()}
              disabled={retryMutation.isPending}
              className="flex items-center gap-2 bg-orange-600 text-white px-4 py-2 rounded-lg text-sm hover:bg-orange-700 transition-colors disabled:opacity-50"
            >
              <RotateCcw size={15} />
              {retryMutation.isPending ? 'Retrying...' : 'Retry Email'}
            </button>
          </div>
        )}
      </div>

      {/* 6. Sent History */}
      <div className="card p-5">
        <h3 className="text-sm font-semibold text-gray-700 mb-4 pb-2 border-b border-gray-100">
          Sent History
          <span className="ml-2 text-xs font-normal text-gray-400">({n.sentHistory?.length ?? 0} attempt{(n.sentHistory?.length ?? 0) !== 1 ? 's' : ''})</span>
        </h3>
        {!n.sentHistory?.length ? (
          <p className="text-sm text-gray-400">No delivery attempts recorded yet.</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="bg-gray-50 border-b border-gray-200">
                  <th className="text-left px-3 py-2 text-xs font-medium text-gray-500 uppercase tracking-wide">#</th>
                  <th className="text-left px-3 py-2 text-xs font-medium text-gray-500 uppercase tracking-wide">Status</th>
                  <th className="text-left px-3 py-2 text-xs font-medium text-gray-500 uppercase tracking-wide">Result</th>
                  <th className="text-left px-3 py-2 text-xs font-medium text-gray-500 uppercase tracking-wide">Attempted At</th>
                  <th className="text-left px-3 py-2 text-xs font-medium text-gray-500 uppercase tracking-wide">Failure Reason</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {n.sentHistory.map(h => (
                  <tr key={h.id} className="hover:bg-gray-50">
                    <td className="px-3 py-2.5 text-gray-600 font-medium">{h.attemptNumber}</td>
                    <td className="px-3 py-2.5">
                      <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ${
                        h.status === 'Sent' ? 'bg-green-100 text-green-800' :
                        h.status === 'DeadLettered' ? 'bg-purple-100 text-purple-800' :
                        'bg-red-100 text-red-800'
                      }`}>
                        {h.status}
                      </span>
                    </td>
                    <td className="px-3 py-2.5">
                      {h.isSuccess
                        ? <CheckCircle size={16} className="text-green-500" />
                        : <XCircle size={16} className="text-red-400" />}
                    </td>
                    <td className="px-3 py-2.5 text-gray-500 whitespace-nowrap">
                      {new Date(h.attemptedAt).toLocaleString()}
                    </td>
                    <td className="px-3 py-2.5 text-red-600 max-w-xs truncate" title={h.failureReason ?? ''}>
                      {h.failureReason ?? '—'}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* 7. Audit */}
      <Section title="Audit">
        <Field label="Created At" value={new Date(n.createdAt).toLocaleString()} />
        <Field label="Last Retry" value={n.lastRetryAt ? new Date(n.lastRetryAt).toLocaleString() : undefined} />
        <Field label="Customer" value={n.customerName} />
      </Section>
    </div>
  );
}
