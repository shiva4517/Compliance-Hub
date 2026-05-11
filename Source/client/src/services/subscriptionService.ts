import { api } from './api';
import type { ApiResponse, CreateSubscriptionsResult, PaginatedList, Subscription, SubscriptionDetailsDto, SubscriptionItemPayload } from '../types';

export interface SubscriptionDetailDto {
  id?: string;
  subscriptionId: string;
  subscribingLevel?: string;
  description?: string;
  condition?: string;
  suggestedTask?: string;
  frequencyTypeId?: string;
  frequencyTypeName?: string;
  dueDateTypeId?: string;
  dueDateTypeName?: string;
  minValue?: number;
  maxValue?: number;
}

export interface UpsertSubscriptionDetailRequest {
  description?: string;
  condition?: string;
  suggestedTask?: string;
  frequencyTypeId?: string;
  dueDateTypeId?: string;
  minValue?: number;
  maxValue?: number;
}

export interface SuggestSubscriptionDetailResponse {
  description: string;
  condition: string;
  suggestedTask: string;
  frequencyType: string;
  dueDateType: string;
  provider: string;
  minValue?: number | null;
  maxValue?: number | null;
}

export const subscriptionService = {
  getAll: async (params: { customerId?: string; companyId?: string }, pageNumber = 1, pageSize = 50) => {
    const response = await api.get<ApiResponse<PaginatedList<Subscription>>>('/subscriptions', {
      params: { ...params, pageNumber, pageSize },
    });
    return response.data.data!;
  },

  createBulk: async (customerId: string, items: SubscriptionItemPayload[]) => {
    const response = await api.post<ApiResponse<CreateSubscriptionsResult>>('/subscriptions', { customerId, items });
    return response.data.data!;
  },
  create: async ({ customerId, governmentEntityId }: { customerId: string; governmentEntityId: string }) => {
    const response = await api.post<ApiResponse<CreateSubscriptionsResult>>('/subscriptions', {
      customerId,
      items: [{ governmentEntityId, subscribingLevel: 'Entity', subscribedNodeName: '' }],
    });
    return response.data.data!;
  },

  getDetails: async (id: string) => {
    const response = await api.get<ApiResponse<SubscriptionDetailsDto>>(`/subscriptions/${id}/details`);
    return response.data.data!;
  },

  delete: async (id: string) => {
    await api.delete(`/subscriptions/${id}`);
  },

  getDetail: async (id: string): Promise<SubscriptionDetailDto | null> => {
    const response = await api.get<ApiResponse<SubscriptionDetailDto | null>>(`/subscriptions/${id}/detail`);
    return response.data.data ?? null;
  },

  upsertDetail: async (id: string, data: UpsertSubscriptionDetailRequest): Promise<string> => {
    const response = await api.put<ApiResponse<string>>(`/subscriptions/${id}/detail`, data);
    return response.data.data!;
  },

  suggestDetail: async (id: string, data: { description?: string; condition?: string; suggestedTask?: string; minValue?: number; maxValue?: number }) => {
    const response = await api.post<ApiResponse<SuggestSubscriptionDetailResponse>>(`/subscriptions/${id}/detail/suggest`, data);
    return response.data.data!;
  },
};
