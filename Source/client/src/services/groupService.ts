import { api } from './api';
import type { ApiResponse, SecurityGroup, UserRole } from '../types';

export const groupService = {
  async getAll() {
    const { data } = await api.get<ApiResponse<SecurityGroup[]>>('/groups');
    return data.data!;
  },
  async create(payload: { groupName: string; description?: string; role: UserRole }) {
    const { data } = await api.post<ApiResponse<string>>('/groups', payload);
    return data.data!;
  },
  async update(id: string, payload: { groupName: string; description?: string; role: UserRole; isActive: boolean }) {
    await api.put(`/groups/${id}`, { ...payload, id });
  },
  async delete(id: string) {
    await api.delete(`/groups/${id}`);
  }
};
