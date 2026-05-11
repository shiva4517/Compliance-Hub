import { api } from './api';
import type {
  ApiResponse,
  CreateGrievanceRequest,
  GrievanceContact,
  GrievanceDetail,
  GrievanceListItem,
} from '../types';

export const grievanceService = {
  async getAll() {
    const { data } = await api.get<ApiResponse<GrievanceListItem[]>>('/grievances');
    return data.data ?? [];
  },

  async getContacts() {
    const { data } = await api.get<ApiResponse<GrievanceContact[]>>('/grievances/contacts');
    return data.data ?? [];
  },

  async getById(id: string) {
    const { data } = await api.get<ApiResponse<GrievanceDetail>>(`/grievances/${id}`);
    return data.data!;
  },

  async create(request: CreateGrievanceRequest) {
    const payload = {
      ...request,
      subscriptionId: request.subscriptionId || null,
    };
    const { data } = await api.post<ApiResponse<string>>('/grievances', payload);
    return data.data!;
  },

  async reply(id: string, message: string) {
    const { data } = await api.post<ApiResponse<string>>(`/grievances/${id}/replies`, { message });
    return data.data!;
  },
};
