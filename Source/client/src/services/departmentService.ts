import { api } from './api';
import type { ApiResponse, Department, PaginatedList } from '../types';

export const departmentService = {
  async getAll(companyId: string, search?: string, pageNumber = 1, pageSize = 500) {
    const { data } = await api.get<ApiResponse<PaginatedList<Department>>>('/departments', {
      params: { companyId, search, pageNumber, pageSize }
    });
    return data.data!;
  },
  async getById(id: string) {
    const { data } = await api.get<ApiResponse<Department>>(`/departments/${id}`);
    return data.data!;
  },
  async create(payload: { companyId: string; name: string; description?: string }) {
    const { data } = await api.post<ApiResponse<string>>('/departments', payload);
    return data.data!;
  },
  async update(id: string, payload: { name: string; description?: string }) {
    await api.put(`/departments/${id}`, { ...payload, id });
  },
  async toggleStatus(id: string, isActive: boolean) {
    await api.patch(`/departments/${id}/status`, { isActive });
  }
};
