import { useState, useEffect, useRef } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import axios from 'axios';
import { AlertTriangle, X } from 'lucide-react';
import { agentService } from '../../services/agentService';
import type { AgentMessage } from '../../types';
import ConversationSidebar from './components/ConversationSidebar';
import MessageThread from './components/MessageThread';
import MessageInput from './components/MessageInput';
import SubscriptionPreviewPanel from './components/SubscriptionPreviewPanel';

type BannerType = 'error' | 'warning';
interface Banner { type: BannerType; message: string; action?: { label: string; onClick: () => void } }

export default function AdminAgentPage() {
  const qc = useQueryClient();
  const navigate = useNavigate();

  const [activeId, setActiveId]               = useState<string | null>(null);
  const [messages, setMessages]               = useState<AgentMessage[]>([]);
  const [isTyping, setIsTyping]               = useState(false);
  const [showPreview, setShowPreview]         = useState(false);
  const [lastOperation, setLastOperation]     = useState<string | undefined>();
  const [contextDataJson, setContextDataJson] = useState<string | undefined>();
  const [banner, setBanner]                   = useState<Banner | null>(null);

  const sendingRef = useRef(false);

  const { data: conversations = [] } = useQuery({
    queryKey: ['agent-conversations'],
    queryFn: agentService.getConversations,
  });

  const { data: conversationData, isFetching: loadingConversation } = useQuery({
    queryKey: ['agent-conversation', activeId],
    queryFn: () => agentService.getConversation(activeId!),
    enabled: !!activeId,
  });

  useEffect(() => {
    if (!conversationData || sendingRef.current) return;
    setMessages(conversationData.messages);
    setLastOperation(conversationData.lastOperation);
    setContextDataJson(conversationData.contextDataJson);
    setShowPreview(!!conversationData.lastOperation && !!conversationData.contextDataJson);
  }, [conversationData]);

  const sendMutation = useMutation({
    mutationFn: ({ convId, message }: { convId: string; message: string }) =>
      agentService.sendMessage(convId, message),
    onSuccess: (res) => {
      sendingRef.current = false;
      setMessages((prev) => {
        const stabilized = prev.map((m) =>
          m.id.startsWith('optimistic-') ? { ...m, id: m.id.replace('optimistic-', 'sent-') } : m
        );
        return [...stabilized, res.assistantMessage];
      });
      setLastOperation(res.lastOperation);
      setContextDataJson(res.contextDataJson);
      if (res.lastOperation && res.contextDataJson) setShowPreview(true);
      setIsTyping(false);
      setBanner(null);
      qc.invalidateQueries({ queryKey: ['agent-conversations'] });
      // Invalidate subscription-related caches when agent writes data
      const subOps = ['create_subscription', 'delete_subscription'];
      if (res.lastOperation && subOps.includes(res.lastOperation)) {
        qc.invalidateQueries({ queryKey: ['subscriptions'] });
        qc.invalidateQueries({ queryKey: ['customer-subscriptions'] });
      }
    },
    onError: (error) => {
      sendingRef.current = false;
      setIsTyping(false);
      setMessages((prev) => prev.filter((m) => !m.id.startsWith('optimistic-')));

      if (axios.isAxiosError(error) && error.response?.status === 412) {
        setBanner({
          type: 'warning',
          message: 'No AI provider configured. Set one up before using the Agent.',
          action: { label: 'Go to Settings', onClick: () => navigate('/ai-provider-settings') },
        });
      } else if (axios.isAxiosError(error) && error.response?.status === 502) {
        setBanner({
          type: 'error',
          message: error.response.data?.message ?? 'The AI provider returned an error. Check your provider settings.',
        });
      } else {
        const msg = axios.isAxiosError(error)
          ? (error.response?.data?.message ?? 'Failed to send message. Please try again.')
          : 'An unexpected error occurred.';
        setBanner({ type: 'error', message: msg });
      }
    },
  });

  const handleSelect = (id: string) => {
    sendingRef.current = false;
    setActiveId(id);
    setMessages([]);
    setShowPreview(false);
    setBanner(null);
  };

  const handleNewConversation = (id: string) => {
    sendingRef.current = false;
    setActiveId(id || null);
    setMessages([]);
    setShowPreview(false);
    setBanner(null);
  };

  const handleSend = (text: string) => {
    if (!activeId) return;
    const optimistic: AgentMessage = {
      id: `optimistic-${Date.now()}`,
      role: 'user',
      content: text,
      createdAt: new Date().toISOString(),
    };
    sendingRef.current = true;
    setMessages((prev) => [...prev, optimistic]);
    setIsTyping(true);
    setBanner(null);
    sendMutation.mutate({ convId: activeId, message: text });
  };

  return (
    <div className="flex h-full overflow-hidden">
      {/* Left: conversation list */}
      <ConversationSidebar
        conversations={conversations}
        activeId={activeId}
        onSelect={handleSelect}
        onNewConversation={handleNewConversation}
      />

      {/* Center: chat */}
      <div className="flex-1 flex flex-col min-w-0 bg-gray-50 overflow-hidden">

        {banner && (
          <div className={`flex items-center gap-3 px-4 py-3 text-sm border-b shrink-0 ${
            banner.type === 'warning'
              ? 'bg-amber-50 border-amber-200 text-amber-800'
              : 'bg-red-50 border-red-200 text-red-800'
          }`}>
            <AlertTriangle size={15} className="shrink-0" />
            <span className="flex-1">{banner.message}</span>
            {banner.action && (
              <button
                onClick={banner.action.onClick}
                className="text-xs font-semibold underline hover:no-underline shrink-0"
              >
                {banner.action.label}
              </button>
            )}
            <button onClick={() => setBanner(null)} className="p-0.5 rounded hover:bg-black/10">
              <X size={13} />
            </button>
          </div>
        )}

        {!activeId ? (
          <div className="flex-1 flex flex-col items-center justify-center text-gray-400 gap-3">
            <p className="text-base font-medium text-gray-500">Select or start a conversation</p>
            <p className="text-sm">Use the sidebar to open an existing conversation or create a new one.</p>
          </div>
        ) : loadingConversation && messages.length === 0 ? (
          <div className="flex-1 flex items-center justify-center">
            <div className="w-6 h-6 border-2 border-blue-600 border-t-transparent rounded-full animate-spin" />
          </div>
        ) : (
          <>
            <MessageThread messages={messages} isTyping={isTyping} />
            <MessageInput onSend={handleSend} disabled={isTyping || sendMutation.isPending} />
          </>
        )}
      </div>

      {/* Right: subscription preview panel */}
      {showPreview && activeId && (
        <SubscriptionPreviewPanel
          lastOperation={lastOperation}
          contextDataJson={contextDataJson}
          onClose={() => setShowPreview(false)}
        />
      )}
    </div>
  );
}
