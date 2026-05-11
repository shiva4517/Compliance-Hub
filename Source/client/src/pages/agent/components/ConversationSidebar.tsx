import { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { PlusCircle, MessageSquare, Trash2, Loader2 } from 'lucide-react';
import { agentService } from '../../../services/agentService';
import type { ConversationSummary } from '../../../types';

interface Props {
  conversations: ConversationSummary[];
  activeId: string | null;
  onSelect: (id: string) => void;
  onNewConversation: (id: string) => void;
}

export default function ConversationSidebar({ conversations, activeId, onSelect, onNewConversation }: Props) {
  const qc = useQueryClient();
  const [deletingId, setDeletingId] = useState<string | null>(null);

  const createMutation = useMutation({
    mutationFn: agentService.createConversation,
    onSuccess: (id) => {
      qc.invalidateQueries({ queryKey: ['agent-conversations'] });
      onNewConversation(id);
    },
  });

  const deleteMutation = useMutation({
    mutationFn: agentService.deleteConversation,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['agent-conversations'] });
    },
  });

  const handleDelete = async (e: React.MouseEvent, id: string) => {
    e.stopPropagation();
    setDeletingId(id);
    await deleteMutation.mutateAsync(id);
    setDeletingId(null);
    if (activeId === id) onNewConversation('');
  };

  return (
    <div className="w-64 border-r border-gray-200 bg-gray-50 flex flex-col h-full">
      <div className="p-3 border-b border-gray-200">
        <button
          onClick={() => createMutation.mutate()}
          disabled={createMutation.isPending}
          className="w-full flex items-center gap-2 px-3 py-2 bg-blue-600 hover:bg-blue-700 text-white text-sm rounded-lg transition-colors disabled:opacity-50"
        >
          {createMutation.isPending
            ? <Loader2 size={14} className="animate-spin" />
            : <PlusCircle size={14} />}
          New Conversation
        </button>
      </div>

      <div className="flex-1 overflow-y-auto py-2">
        {conversations.length === 0 && (
          <p className="text-xs text-gray-400 text-center mt-8 px-4">No conversations yet. Start one!</p>
        )}
        {conversations.map((conv) => (
          <button
            key={conv.id}
            onClick={() => onSelect(conv.id)}
            className={`w-full text-left px-3 py-2.5 group flex items-start gap-2 transition-colors ${
              activeId === conv.id
                ? 'bg-blue-50 border-r-2 border-blue-600'
                : 'hover:bg-gray-100'
            }`}
          >
            <MessageSquare size={14} className="mt-0.5 shrink-0 text-gray-400" />
            <div className="flex-1 min-w-0">
              <p className="text-sm text-gray-800 truncate font-medium">{conv.title}</p>
              <p className="text-xs text-gray-400 truncate">
                {new Date(conv.updatedAt).toLocaleDateString()}
              </p>
            </div>
            <button
              onClick={(e) => handleDelete(e, conv.id)}
              disabled={deletingId === conv.id}
              className="opacity-0 group-hover:opacity-100 p-1 rounded hover:bg-red-100 hover:text-red-600 text-gray-400 transition-all"
            >
              {deletingId === conv.id
                ? <Loader2 size={12} className="animate-spin" />
                : <Trash2 size={12} />}
            </button>
          </button>
        ))}
      </div>
    </div>
  );
}
