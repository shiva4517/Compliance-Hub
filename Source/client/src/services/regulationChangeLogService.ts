import {api} from './api';
import type { ApiResponse, PaginatedList, RegulationChangeLog, RegulationChangeLogDetail } from '../types';

export const regulationChangeLogService = {
  async getAll(params: {
    search?: string;
    fromDate?: string;
    toDate?: string;
    sortBy?: string;
    sortDir?: string;
    pageNumber?: number;
    pageSize?: number;
  } = {}) {
    const q = new URLSearchParams();
    if (params.search) q.set('search', params.search);
    if (params.fromDate) q.set('fromDate', params.fromDate);
    if (params.toDate) q.set('toDate', params.toDate);
    if (params.sortBy) q.set('sortBy', params.sortBy);
    if (params.sortDir) q.set('sortDir', params.sortDir);
    q.set('pageNumber', String(params.pageNumber ?? 1));
    q.set('pageSize', String(params.pageSize ?? 20));
    const res = await api.get<ApiResponse<PaginatedList<RegulationChangeLog>>>(`/regulation-change-logs?${q}`);
    return res.data.data!;
  },

  async getById(id: string) {
    const res = await api.get<ApiResponse<RegulationChangeLogDetail>>(`/regulation-change-logs/${id}`);
    return res.data.data!;
  },
};
