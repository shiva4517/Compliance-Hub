import { api } from './api';
import type { ApiResponse, PaginatedList, User, UserRole } from '../types';

export const userService = {
  async getAll(search?: string, role?: UserRole, pageNumber = 1, pageSize = 20) {
    const { data } = await api.get<ApiResponse<PaginatedList<User>>>('/users', {
      params: { search, role, pageNumber, pageSize }
    });
    return data.data!;
  },
  async getById(id: string) {
    const { data } = await api.get<ApiResponse<User>>(`/users/${id}`);
    return data.data!;
  },
  async create(payload: {
    firstName: string; lastName: string; email: string; password?: string;
    phoneNumber?: string; title?: string; role: UserRole; securityGroupId?: string; customerId?: string; refId?: string;
  }) {
    const { data } = await api.post<ApiResponse<string>>('/users', payload);
    return data.data!;
  },
  async update(id: string, payload: Partial<User & { isActive: boolean }>) {
    await api.put(`/users/${id}`, { ...payload, id });
  },
  async changePassword(currentPassword: string, newPassword: string) {
    await api.post('/auth/change-password', { currentPassword, newPassword });
  }
};
