import { api } from './api';
import type { ApiResponse, ConversationSummary, ConversationDetail, SendMessageResponse } from '../types';

export const agentService = {
  async createConversation(): Promise<string> {
    const { data } = await api.post<ApiResponse<string>>('/agent/conversations');
    return data.data!;
  },

  async getConversations(): Promise<ConversationSummary[]> {
    const { data } = await api.get<ApiResponse<ConversationSummary[]>>('/agent/conversations');
    return data.data!;
  },

  async getConversation(id: string): Promise<ConversationDetail> {
    const { data } = await api.get<ApiResponse<ConversationDetail>>(`/agent/conversations/${id}`);
    return data.data!;
  },

  async sendMessage(conversationId: string, message: string): Promise<SendMessageResponse> {
    const { data } = await api.post<ApiResponse<SendMessageResponse>>(
      `/agent/conversations/${conversationId}/messages`,
      { message },
      { timeout: 130000 }
    );
    return data.data!;
  },

  async deleteConversation(id: string): Promise<void> {
    await api.delete(`/agent/conversations/${id}`);
  },
};
