import { api } from './api';
import type { ApiResponse, CurrentCustomerProfile, CustomerSubscription, Customer, PaginatedList } from '../types';

export interface CustomerFormData {
  companyId: string;
  customerName: string;
  primaryContactFirstName: string;
  primaryContactLastName: string;
  primaryEmail: string;
  secondaryEmail?: string;
  phoneNumber?: string;
  mobileNumber?: string;
  primaryAddress?: string;
  primaryCity?: string;
  primaryState?: string;
  primaryPostalCode?: string;
  secondaryAddress?: string;
  secondaryCity?: string;
  secondaryState?: string;
  secondaryPostalCode?: string;
  skipRestoreCheck?: boolean;
}

export const customerService = {
  async getAll(companyId: string, search?: string, pageNumber = 1, pageSize = 20) {
    const { data } = await api.get<ApiResponse<PaginatedList<Customer>>>('/customers', {
      params: { companyId, search, pageNumber, pageSize }
    });
    return data.data!;
  },
  async getById(id: string, companyId: string) {
    const { data } = await api.get<ApiResponse<Customer>>(`/customers/${id}`, {
      params: { companyId }
    });
    return data.data!;
  },
  async getCurrentProfile() {
    const { data } = await api.get<ApiResponse<CurrentCustomerProfile>>('/customers/me');
    return data.data!;
  },
  async create(payload: CustomerFormData) {
    const { data } = await api.post<ApiResponse<string>>('/customers', payload);
    return data.data!;
  },
  async update(id: string, payload: CustomerFormData & { id: string }) {
    await api.put(`/customers/${id}`, payload);
  },
  async toggleStatus(id: string, companyId: string, isActive: boolean) {
    await api.patch(`/customers/${id}/status`, { isActive }, { params: { companyId } });
  },
  async restore(id: string, payload: CustomerFormData) {
    await api.post(`/customers/${id}/restore`, payload);
  },
  async delete(id: string, companyId: string) {
    await api.delete(`/customers/${id}`, { params: { companyId } });
  },
  async getSubscriptions(customerId: string, companyId: string) {
    const { data } = await api.get<ApiResponse<CustomerSubscription[]>>(
      `/customers/${customerId}/subscriptions`,
      { params: { companyId } }
    );
    return data.data!;
  },
};
