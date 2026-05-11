import { api } from './api';
import type { ApiResponse, District, PaginatedList } from '../types';

export const districtService = {
  async getAll(companyId: string, search?: string, pageNumber = 1, pageSize = 500) {
    const { data } = await api.get<ApiResponse<PaginatedList<District>>>('/districts', {
      params: { companyId, search, pageNumber, pageSize }
    });
    return data.data!;
  },
  async getById(id: string) {
    const { data } = await api.get<ApiResponse<District>>(`/districts/${id}`);
    return data.data!;
  },
  async create(payload: { companyId: string; name: string; description?: string }) {
    const { data } = await api.post<ApiResponse<string>>('/districts', payload);
    return data.data!;
  },
  async update(id: string, payload: { name: string; description?: string }) {
    await api.put(`/districts/${id}`, { ...payload, id });
  },
  async toggleStatus(id: string, isActive: boolean) {
    await api.patch(`/districts/${id}/status`, { isActive });
  }
};
