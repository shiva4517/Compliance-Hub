import {api} from './api';
import type { ApiResponse, PaginatedList, NotificationLog, NotificationLogDetail, RetryNotificationResult } from '../types';

export const notificationHistoryService = {
  async getAll(companyId: string, search?: string, pageNumber = 1, pageSize = 20) {
    const params = new URLSearchParams({ companyId, pageNumber: String(pageNumber), pageSize: String(pageSize) });
    if (search) params.set('search', search);
    const res = await api.get<ApiResponse<PaginatedList<NotificationLog>>>(`/notifications?${params}`);
    return res.data.data!;
  },

  async getById(id: string, companyId: string) {
    const res = await api.get<ApiResponse<NotificationLogDetail>>(`/notifications/${id}?companyId=${companyId}`);
    return res.data.data!;
  },

  async retry(id: string, companyId: string) {
    const res = await api.post<ApiResponse<RetryNotificationResult>>(`/notifications/${id}/retry?companyId=${companyId}`);
    return res.data;
  },

  async getUnseenCount(companyId: string): Promise<number> {
    const res = await api.get<ApiResponse<number>>(`/notifications/unseen-count?companyId=${companyId}`);
    return res.data.data ?? 0;
  },

  async markSeen(companyId: string): Promise<void> {
    await api.post(`/notifications/mark-seen?companyId=${companyId}`);
  },
};
