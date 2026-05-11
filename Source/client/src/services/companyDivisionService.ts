import { api } from './api';
import type { ApiResponse, CompanyDivision, PaginatedList } from '../types';

export const companyDivisionService = {
  async getAll(companyId?: string, search?: string, pageNumber = 1, pageSize = 20) {
    const { data } = await api.get<ApiResponse<PaginatedList<CompanyDivision>>>('/company-divisions', {
      params: { companyId, search, pageNumber, pageSize }
    });
    return data.data!;
  },
  async getById(id: string) {
    const { data } = await api.get<ApiResponse<CompanyDivision>>(`/company-divisions/${id}`);
    return data.data!;
  },
  async create(payload: { companyId: string; departmentId?: string; name: string; description?: string }) {
    const { data } = await api.post<ApiResponse<string>>('/company-divisions', payload);
    return data.data!;
  },
  async update(id: string, payload: { departmentId?: string; name: string; description?: string }) {
    await api.put(`/company-divisions/${id}`, { ...payload, id });
  },
  async toggleStatus(id: string, isActive: boolean) {
    await api.patch(`/company-divisions/${id}/status`, { isActive });
  },
  async delete(id: string) {
    await api.delete(`/company-divisions/${id}`);
  }
};
