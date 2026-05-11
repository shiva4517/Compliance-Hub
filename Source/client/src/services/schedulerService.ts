import { api } from './api';
import type { ApiResponse, PaginatedList, RegulationSyncSchedulerHistory } from '../types';

export const schedulerService = {
  async getHistory(params: {
    sortBy?: string;
    sortDir?: string;
    pageNumber?: number;
    pageSize?: number;
  } = {}) {
    const q = new URLSearchParams();
    if (params.sortBy) q.set('sortBy', params.sortBy);
    if (params.sortDir) q.set('sortDir', params.sortDir);
    q.set('pageNumber', String(params.pageNumber ?? 1));
    q.set('pageSize', String(params.pageSize ?? 0));

    const res = await api.get<ApiResponse<PaginatedList<RegulationSyncSchedulerHistory>>>(`/regulations/sync-scheduler-history?${q}`);
    return res.data.data!;
  },
};
