import { api } from './api';
import type { ApiResponse, Company, CompanySummary, PaginatedList } from '../types';

export type CompanyCreatePayload = Omit<Company, 'id' | 'companyCode' | 'createdAt' | 'isActive'> & { skipRestoreCheck?: boolean };

export const companyService = {
  async getAll(search?: string, pageNumber = 1, pageSize = 20) {
    const { data } = await api.get<ApiResponse<PaginatedList<Company>>>('/companies', {
      params: { search, pageNumber, pageSize }
    });
    return data.data!;
  },
  async getById(id: string) {
    const { data } = await api.get<ApiResponse<Company>>(`/companies/${id}`);
    return data.data!;
  },
  async create(payload: CompanyCreatePayload) {
    const { data } = await api.post<ApiResponse<{ companyId: string; companyCode: string }>>('/companies', payload);
    return data.data!;
  },
  async update(id: string, payload: Omit<Company, 'companyCode' | 'createdAt'>) {
    await api.put(`/companies/${id}`, { ...payload, id });
  },
  async restore(id: string, payload: Omit<Company, 'id' | 'companyCode' | 'createdAt' | 'isActive'>) {
    await api.post(`/companies/${id}/restore`, { ...payload, id });
  },
  async delete(id: string) {
    await api.delete(`/companies/${id}`);
  },
  async getSummary(id: string) {
    const { data } = await api.get<ApiResponse<CompanySummary>>(`/companies/${id}/summary`);
    return data.data!;
  },
};
