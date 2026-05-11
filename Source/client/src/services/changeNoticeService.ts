import { api } from './api';
import type { ApiResponse, ChangeNotice, NotificationItem, PaginatedList } from '../types';

export const changeNoticeService = {
  getAll: async (params?: {
    role?: string;
    referenceId?: string;
    fromDate?: string;
    toDate?: string;
    search?: string;
    pageNumber?: number;
    pageSize?: number;
  }) => {
    const response = await api.get<ApiResponse<PaginatedList<ChangeNotice>>>('/change-notices', { params });
    return response.data.data!;
  },

  getNotifications: async (params: { customerId?: string; companyId?: string; isRead?: boolean; pageNumber?: number; pageSize?: number }) => {
    const response = await api.get<ApiResponse<PaginatedList<NotificationItem>>>('/change-notices/notifications', {
      params: { ...params, pageNumber: params.pageNumber ?? 1, pageSize: params.pageSize ?? 20 },
    });
    return response.data.data!;
  },

  getUnreadCount: async (params: { customerId?: string; companyId?: string }) => {
    const response = await api.get<ApiResponse<PaginatedList<NotificationItem>>>('/change-notices/notifications', {
      params: { ...params, isRead: false, pageSize: 1 },
    });
    return response.data.data?.totalCount ?? 0;
  },

  markRead: async (id: string) => {
    await api.put(`/change-notices/notifications/${id}/read`);
  },
};
