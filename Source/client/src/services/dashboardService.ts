import { api } from './api';
import type { ApiResponse, AdminDashboardStats, SuperAdminDashboardStats } from '../types';

export const dashboardService = {
  async getStats(companyId: string) {
    const { data } = await api.get<ApiResponse<AdminDashboardStats>>('/dashboard/stats', {
      params: { companyId },
    });
    return data.data!;
  },
  async getSuperAdminStats() {
    const { data } = await api.get<ApiResponse<SuperAdminDashboardStats>>('/dashboard/super-admin-stats');
    return data.data!;
  },
};
