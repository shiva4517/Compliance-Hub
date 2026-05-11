import { api } from './api';
import type { ApiResponse, LoginRequest, LoginResponse } from '../types';

export const authService = {
  async login(request: LoginRequest) {
    const { data } = await api.post<ApiResponse<LoginResponse>>('/auth/login', request);
    return data.data!;
  },
  async refresh(accessToken: string, refreshToken: string) {
    const { data } = await api.post<ApiResponse<{ accessToken: string; refreshToken: string }>>(
      '/auth/refresh', { accessToken, refreshToken }
    );
    return data.data!;
  }
};
