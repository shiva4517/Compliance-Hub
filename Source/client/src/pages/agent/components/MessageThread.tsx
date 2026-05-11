import { useEffect, useRef } from 'react';
import { Bot, User, Loader2 } from 'lucide-react';
import type { AgentMessage } from '../../../types';

interface Props {
  messages: AgentMessage[];
  isTyping: boolean;
}

export default function MessageThread({ messages, isTyping }: Props) {
  const bottomRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages, isTyping]);

  if (messages.length === 0 && !isTyping) {
    return (
      <div className="flex-1 flex flex-col items-center justify-center text-gray-400 gap-3">
        <Bot size={48} className="text-gray-300" />
        <p className="text-lg font-medium text-gray-500">How can I help you today?</p>
        <p className="text-sm text-center max-w-sm">
          I can create, update, search, and manage company records. Just ask!
        </p>
      </div>
    );
  }

  return (
    <div className="flex-1 overflow-y-auto px-4 py-4 space-y-4">
      {messages.map((msg) => (
        <div key={msg.id} className={`flex gap-3 ${msg.role === 'user' ? 'justify-end' : 'justify-start'}`}>
          {msg.role === 'assistant' && (
            <div className="w-7 h-7 rounded-full bg-blue-100 flex items-center justify-center shrink-0 mt-0.5">
              <Bot size={14} className="text-blue-600" />
            </div>
          )}

          <div
            className={`max-w-[70%] rounded-2xl px-4 py-2.5 text-sm whitespace-pre-wrap break-words ${
              msg.role === 'user'
                ? 'bg-blue-600 text-white rounded-br-sm'
                : 'bg-white border border-gray-200 text-gray-800 rounded-bl-sm shadow-sm'
            }`}
          >
            {msg.content}
          </div>

          {msg.role === 'user' && (
            <div className="w-7 h-7 rounded-full bg-gray-200 flex items-center justify-center shrink-0 mt-0.5">
              <User size={14} className="text-gray-600" />
            </div>
          )}
        </div>
      ))}

      {isTyping && (
        <div className="flex gap-3 justify-start">
          <div className="w-7 h-7 rounded-full bg-blue-100 flex items-center justify-center shrink-0 mt-0.5">
            <Bot size={14} className="text-blue-600" />
          </div>
          <div className="bg-white border border-gray-200 rounded-2xl rounded-bl-sm px-4 py-3 shadow-sm">
            <Loader2 size={14} className="animate-spin text-gray-400" />
          </div>
        </div>
      )}

      <div ref={bottomRef} />
    </div>
  );
}
