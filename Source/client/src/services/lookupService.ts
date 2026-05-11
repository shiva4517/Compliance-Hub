import { api } from './api';
import type { ApiResponse, LookupItem } from '../types';

export const lookupService = {
  getFrequencyTypes: async (): Promise<LookupItem[]> => {
    const response = await api.get<ApiResponse<LookupItem[]>>('/lookups/frequency-types');
    return response.data.data ?? [];
  },

  getDueDateTypes: async (): Promise<LookupItem[]> => {
    const response = await api.get<ApiResponse<LookupItem[]>>('/lookups/due-date-types');
    return response.data.data ?? [];
  },
};
