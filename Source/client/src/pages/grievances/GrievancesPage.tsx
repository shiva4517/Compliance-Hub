import { useEffect, useMemo, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { AlertCircle, MessageSquare, Plus, Send } from 'lucide-react';
import { grievanceService } from '../../services/grievanceService';
import type {
  CreateGrievanceRequest,
  GrievanceContact,
  GrievanceListItem,
  GrievancePriority,
} from '../../types';
import { handleApiError } from '../../services/api';
import { useAuth } from '../../contexts/AuthContext';

function roleBadge(role: string) {
  const styles: Record<string, string> = {
    SuperAdmin: 'bg-purple-100 text-purple-700',
    Admin: 'bg-blue-100 text-blue-700',
    Customer: 'bg-emerald-100 text-emerald-700',
    Employee: 'bg-amber-100 text-amber-700',
  };

  return (
    <span className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${styles[role] ?? 'bg-gray-100 text-gray-700'}`}>
      {role}
    </span>
  );
}

function priorityBadge(priority: GrievancePriority) {
  const styles: Record<GrievancePriority, string> = {
    Low: 'bg-slate-100 text-slate-700',
    Medium: 'bg-orange-100 text-orange-700',
    High: 'bg-red-100 text-red-700',
  };

  return (
    <span className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${styles[priority]}`}>
      {priority}
    </span>
  );
}

function matchesRoleScopeMessage(role: string) {
  switch (role) {
    case 'SuperAdmin':
      return 'You can raise and reply only to company Admin grievances.';
    case 'Admin':
      return 'You can view all grievances in your company and communicate with Super Admin, Customers, and Employees.';
    case 'Customer':
      return 'You can communicate only with your Admin and employees assigned through active subscriptions.';
    case 'Employee':
      return 'You can communicate only with your Admin and customers assigned to you through active subscriptions.';
    default:
      return '';
  }
}

export default function GrievancesPage() {
  const qc = useQueryClient();
  const { user } = useAuth();

  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [recipientId, setRecipientId] = useState('');
  const [subscriptionId, setSubscriptionId] = useState('');
  const [subject, setSubject] = useState('');
  const [description, setDescription] = useState('');
  const [priority, setPriority] = useState<GrievancePriority>('Medium');
  const [replyMessage, setReplyMessage] = useState('');
  const [formError, setFormError] = useState<string | null>(null);
  const [replyError, setReplyError] = useState<string | null>(null);

  const grievancesQuery = useQuery({
    queryKey: ['grievances'],
    queryFn: () => grievanceService.getAll(),
  });

  const contactsQuery = useQuery({
    queryKey: ['grievance-contacts'],
    queryFn: () => grievanceService.getContacts(),
  });

  useEffect(() => {
    if (!selectedId && grievancesQuery.data && grievancesQuery.data.length > 0) {
      setSelectedId(grievancesQuery.data[0].id);
    }
  }, [grievancesQuery.data, selectedId]);

  const selectedContact = useMemo(
    () => contactsQuery.data?.find((contact) => contact.securityUserId === recipientId) ?? null,
    [contactsQuery.data, recipientId]
  );

  useEffect(() => {
    if (!selectedContact?.availableSubscriptions.length) {
      setSubscriptionId('');
      return;
    }

    if (!selectedContact.availableSubscriptions.some((item) => item.id === subscriptionId)) {
      setSubscriptionId(selectedContact.availableSubscriptions[0].id);
    }
  }, [selectedContact, subscriptionId]);

  const grievanceDetailQuery = useQuery({
    queryKey: ['grievances', selectedId],
    queryFn: () => grievanceService.getById(selectedId!),
    enabled: !!selectedId,
  });

  const createMutation = useMutation({
    mutationFn: (request: CreateGrievanceRequest) => grievanceService.create(request),
    onSuccess: async (id) => {
      setSubject('');
      setDescription('');
      setRecipientId('');
      setSubscriptionId('');
      setPriority('Medium');
      setFormError(null);
      await qc.invalidateQueries({ queryKey: ['grievances'] });
      setSelectedId(id);
    },
    onError: (error) => setFormError(handleApiError(error)),
  });

  const replyMutation = useMutation({
    mutationFn: ({ grievanceId, message }: { grievanceId: string; message: string }) =>
      grievanceService.reply(grievanceId, message),
    onSuccess: async () => {
      setReplyMessage('');
      setReplyError(null);
      await Promise.all([
        qc.invalidateQueries({ queryKey: ['grievances'] }),
        qc.invalidateQueries({ queryKey: ['grievances', selectedId] }),
      ]);
    },
    onError: (error) => setReplyError(handleApiError(error)),
  });

  const grievances = grievancesQuery.data ?? [];
  const selectedGrievance = grievanceDetailQuery.data;
  const contacts = contactsQuery.data ?? [];

  const submitCreate = () => {
    if (!recipientId || !subject.trim() || !description.trim()) {
      setFormError('Recipient, subject, and description are required.');
      return;
    }

    createMutation.mutate({
      recipientSecurityUserId: recipientId,
      subject: subject.trim(),
      description: description.trim(),
      subscriptionId: subscriptionId || undefined,
      priority,
    });
  };

  const submitReply = () => {
    if (!selectedId || !replyMessage.trim()) {
      setReplyError('Reply message is required.');
      return;
    }

    replyMutation.mutate({ grievanceId: selectedId, message: replyMessage.trim() });
  };

  return (
    <div className="space-y-5">
      <div className="flex items-start justify-between gap-4 flex-wrap">
        <div>
          <div className="flex items-center gap-3 mb-1">
            <MessageSquare size={22} className="text-blue-800" />
            <h1 className="text-2xl font-bold text-gray-900">Grievances</h1>
          </div>
          <p className="text-sm text-gray-500">{matchesRoleScopeMessage(user?.role ?? '')}</p>
        </div>
      </div>

      <div className="grid grid-cols-1 xl:grid-cols-[340px_minmax(0,1fr)] gap-5">
        <div className="space-y-5">
          <section className="card p-4 space-y-4">
            <div className="flex items-center gap-2">
              <Plus size={18} className="text-blue-700" />
              <h2 className="text-lg font-semibold text-gray-900">New Grievance</h2>
            </div>

            {contactsQuery.isLoading ? (
              <p className="text-sm text-gray-500">Loading available contacts...</p>
            ) : contacts.length === 0 ? (
              <div className="rounded-xl border border-amber-200 bg-amber-50 p-3 text-sm text-amber-800">
                No allowed grievance recipients are available for your current role scope.
              </div>
            ) : (
              <>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Recipient</label>
                  <select
                    value={recipientId}
                    onChange={(e) => setRecipientId(e.target.value)}
                    className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                  >
                    <option value="">Select recipient</option>
                    {contacts.map((contact: GrievanceContact) => (
                      <option key={contact.securityUserId} value={contact.securityUserId}>
                        {contact.fullName} ({contact.role})
                      </option>
                    ))}
                  </select>
                </div>

                {selectedContact && selectedContact.availableSubscriptions.length > 0 && (
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Subscription Scope</label>
                    <select
                      value={subscriptionId}
                      onChange={(e) => setSubscriptionId(e.target.value)}
                      className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                    >
                      {selectedContact.availableSubscriptions.map((item) => (
                        <option key={item.id} value={item.id}>
                          {item.name}
                        </option>
                      ))}
                    </select>
                  </div>
                )}

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Priority</label>
                  <select
                    value={priority}
                    onChange={(e) => setPriority(e.target.value as GrievancePriority)}
                    className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                  >
                    <option value="Low">Low</option>
                    <option value="Medium">Medium</option>
                    <option value="High">High</option>
                  </select>
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Subject</label>
                  <input
                    value={subject}
                    onChange={(e) => setSubject(e.target.value)}
                    className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                    placeholder="Enter grievance subject"
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Description</label>
                  <textarea
                    value={description}
                    onChange={(e) => setDescription(e.target.value)}
                    rows={5}
                    className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                    placeholder="Describe the grievance or question"
                  />
                </div>

                {formError && (
                  <div className="flex items-start gap-2 rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">
                    <AlertCircle size={16} className="mt-0.5" />
                    <span>{formError}</span>
                  </div>
                )}

                <button
                  onClick={submitCreate}
                  disabled={createMutation.isPending}
                  className="btn-primary w-full flex items-center justify-center gap-2 py-2 disabled:opacity-50"
                >
                  <Send size={16} />
                  {createMutation.isPending ? 'Creating...' : 'Create Grievance'}
                </button>
              </>
            )}
          </section>

          <section className="card overflow-hidden">
            <div className="border-b border-gray-200 px-4 py-3">
              <h2 className="text-lg font-semibold text-gray-900">Conversations</h2>
              <p className="text-xs text-gray-500 mt-0.5">Only grievances inside your allowed scope are shown.</p>
            </div>

            <div className="max-h-[620px] overflow-y-auto">
              {grievancesQuery.isLoading ? (
                <div className="p-5 text-sm text-gray-500">Loading grievances...</div>
              ) : grievances.length === 0 ? (
                <div className="p-5 text-sm text-gray-500">No grievances available in your role scope.</div>
              ) : (
                grievances.map((item: GrievanceListItem) => (
                  <button
                    key={item.id}
                    onClick={() => setSelectedId(item.id)}
                    className={`w-full border-b border-gray-100 p-4 text-left transition-colors hover:bg-gray-50 ${selectedId === item.id ? 'bg-blue-50' : ''}`}
                  >
                    <div className="flex items-start justify-between gap-3">
                      <div className="min-w-0">
                        <div className="font-semibold text-gray-900 truncate">{item.subject}</div>
                        <div className="mt-1 text-xs text-gray-500">
                          {item.createdByName} to {item.recipientName}
                        </div>
                      </div>
                      {priorityBadge(item.priority)}
                    </div>

                    <div className="mt-2 flex items-center gap-2 flex-wrap">
                      {roleBadge(item.createdByRole)}
                      {roleBadge(item.recipientRole)}
                      <span className="inline-flex items-center rounded-full bg-gray-100 px-2 py-0.5 text-xs text-gray-700">
                        {item.status}
                      </span>
                    </div>

                    {item.subscriptionName && (
                      <div className="mt-2 text-xs text-gray-500">Subscription: {item.subscriptionName}</div>
                    )}

                    <div className="mt-2 text-xs text-gray-400">
                      {new Date(item.lastMessageAt).toLocaleString()} • {item.replyCount} repl{item.replyCount === 1 ? 'y' : 'ies'}
                    </div>
                  </button>
                ))
              )}
            </div>
          </section>
        </div>

        <section className="card min-h-[720px]">
          {!selectedId ? (
            <div className="flex h-full items-center justify-center text-sm text-gray-500">
              Select a grievance to view the conversation.
            </div>
          ) : grievanceDetailQuery.isLoading ? (
            <div className="p-6 text-sm text-gray-500">Loading grievance details...</div>
          ) : !selectedGrievance ? (
            <div className="p-6 text-sm text-gray-500">Unable to load grievance details.</div>
          ) : (
            <div className="flex h-full flex-col">
              <div className="border-b border-gray-200 p-5">
                <div className="flex items-start justify-between gap-3 flex-wrap">
                  <div>
                    <h2 className="text-xl font-semibold text-gray-900">{selectedGrievance.subject}</h2>
                    <p className="mt-1 text-sm text-gray-500">
                      {selectedGrievance.createdByName} to {selectedGrievance.recipientName}
                    </p>
                  </div>
                  <div className="flex items-center gap-2 flex-wrap">
                    {priorityBadge(selectedGrievance.priority)}
                    <span className="inline-flex items-center rounded-full bg-gray-100 px-2 py-0.5 text-xs text-gray-700">
                      {selectedGrievance.status}
                    </span>
                  </div>
                </div>

                <div className="mt-3 flex items-center gap-2 flex-wrap">
                  {roleBadge(selectedGrievance.createdByRole)}
                  {roleBadge(selectedGrievance.recipientRole)}
                  {selectedGrievance.subscriptionName && (
                    <span className="inline-flex items-center rounded-full bg-slate-100 px-2 py-0.5 text-xs text-slate-700">
                      {selectedGrievance.subscriptionName}
                    </span>
                  )}
                </div>

                <div className="mt-4 rounded-xl border border-gray-200 bg-gray-50 p-4">
                  <div className="text-xs font-medium uppercase tracking-wide text-gray-500">Initial Message</div>
                  <p className="mt-2 whitespace-pre-wrap text-sm text-gray-700">{selectedGrievance.description}</p>
                  <div className="mt-3 text-xs text-gray-400">
                    {new Date(selectedGrievance.createdAt).toLocaleString()}
                  </div>
                </div>
              </div>

              <div className="flex-1 space-y-4 overflow-y-auto p-5 bg-slate-50/60">
                {selectedGrievance.replies.length === 0 ? (
                  <div className="text-sm text-gray-500">No replies yet.</div>
                ) : (
                  selectedGrievance.replies.map((reply) => {
                    const isMine = reply.senderSecurityUserId === selectedGrievance.createdBySecurityUserId
                      ? user?.fullName === selectedGrievance.createdByName
                      : reply.senderName === user?.fullName;

                    return (
                      <div
                        key={reply.id}
                        className={`max-w-3xl rounded-2xl border p-4 shadow-sm ${isMine ? 'ml-auto bg-blue-50 border-blue-100' : 'bg-white border-gray-200'}`}
                      >
                        <div className="flex items-center gap-2 flex-wrap">
                          <span className="text-sm font-semibold text-gray-900">{reply.senderName}</span>
                          {roleBadge(reply.senderRole)}
                        </div>
                        <p className="mt-2 whitespace-pre-wrap text-sm text-gray-700">{reply.message}</p>
                        <div className="mt-3 text-xs text-gray-400">{new Date(reply.createdAt).toLocaleString()}</div>
                      </div>
                    );
                  })
                )}
              </div>

              <div className="border-t border-gray-200 p-5 space-y-3">
                {selectedGrievance.canReply ? (
                  <>
                    <textarea
                      value={replyMessage}
                      onChange={(e) => setReplyMessage(e.target.value)}
                      rows={4}
                      className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                      placeholder="Write your reply"
                    />
                    {replyError && (
                      <div className="flex items-start gap-2 rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">
                        <AlertCircle size={16} className="mt-0.5" />
                        <span>{replyError}</span>
                      </div>
                    )}
                    <div className="flex justify-end">
                      <button
                        onClick={submitReply}
                        disabled={replyMutation.isPending}
                        className="btn-primary flex items-center gap-2 py-2 px-4 disabled:opacity-50"
                      >
                        <Send size={16} />
                        {replyMutation.isPending ? 'Sending...' : 'Send Reply'}
                      </button>
                    </div>
                  </>
                ) : (
                  <div className="rounded-xl border border-amber-200 bg-amber-50 p-3 text-sm text-amber-800">
                    You can view this grievance, but replying is not allowed for your current role on this thread.
                  </div>
                )}
              </div>
            </div>
          )}
        </section>
      </div>
    </div>
  );
}
