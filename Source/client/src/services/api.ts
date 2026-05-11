import axios, { type AxiosInstance } from 'axios';

const BASE_URL = import.meta.env.VITE_API_BASE_URL
  ? `${import.meta.env.VITE_API_BASE_URL}/api`
  : '/api';

const redirectToLogin = (): void => {
  localStorage.clear();
  if (window.location.pathname !== '/login') {
    window.location.href = '/login';
  }
};

const createAxiosInstance = (): AxiosInstance => {
  const instance = axios.create({ baseURL: BASE_URL, timeout: 30000 });

  instance.interceptors.request.use((config) => {
    const token = localStorage.getItem('accessToken');
    if (token) config.headers.Authorization = `Bearer ${token}`;
    const authUser = localStorage.getItem('authUser');
    if (authUser) {
      try {
        const parsed = JSON.parse(authUser);
        if (parsed?.userId) config.headers['X-User-Id'] = parsed.userId;
      } catch { /* ignore */ }
    }
    return config;
  });

  instance.interceptors.response.use(
    (res) => res,
    async (error) => {
      const original = error.config;

      if (error.response?.status === 401 && !original._retry) {
        original._retry = true;
        try {
          const refreshToken = localStorage.getItem('refreshToken');
          const accessToken = localStorage.getItem('accessToken');
          if (!refreshToken) throw new Error('No refresh token');

          const { data } = await axios.post(`${BASE_URL}/auth/refresh`, { accessToken, refreshToken });
          localStorage.setItem('accessToken', data.data.accessToken);
          localStorage.setItem('refreshToken', data.data.refreshToken);
          original.headers.Authorization = `Bearer ${data.data.accessToken}`;
          return instance(original);
        } catch {
          redirectToLogin();
        }
      }
      return Promise.reject(error);
    }
  );

  return instance;
};

export const api = createAxiosInstance();

export const handleApiError = (error: unknown): string => {
  if (axios.isAxiosError(error)) {
    if (!error.response) {
      return error.code === 'ECONNABORTED'
        ? 'The request timed out. Please try again.'
        : 'API is unavailable. Please try again once the server is running.';
    }
    return error.response?.data?.message ?? error.message;
  }
  return 'An unexpected error occurred.';
};
