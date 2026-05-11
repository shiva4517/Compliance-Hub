import { api } from './api';
import type { ApiResponse, Employee, EmployeeAssignedWork, AvailableSubscription, PaginatedList, MyProfileDto, MyStatsDto, MyAssignmentDetailDto, ChangeNotice } from '../types';

export interface CreateEmployeePayload {
  companyId: string;
  firstName: string;
  lastName: string;
  primaryEmail: string;
  secondaryEmail?: string;
  phoneNumber?: string;
  mobileNumber?: string;
  departmentId?: string;
  companyDivisionId?: string;
  companyDistrictId?: string;
  primaryAddress: string;
  primaryCity: string;
  primaryState: string;
  primaryPostalCode: string;
  secondaryAddress?: string;
  secondaryCity?: string;
  secondaryState?: string;
  secondaryPostalCode?: string;
  skipRestoreCheck?: boolean;
}

export interface UpdateEmployeePayload {
  id: string;
  firstName: string;
  lastName: string;
  secondaryEmail?: string;
  phoneNumber?: string;
  mobileNumber?: string;
  departmentId?: string;
  companyDivisionId?: string;
  companyDistrictId?: string;
  primaryAddress: string;
  primaryCity: string;
  primaryState: string;
  primaryPostalCode: string;
  secondaryAddress?: string;
  secondaryCity?: string;
  secondaryState?: string;
  secondaryPostalCode?: string;
  isActive: boolean;
}

export interface RestoreEmployeePayload {
  id: string;
  firstName: string;
  lastName: string;
  secondaryEmail?: string;
  phoneNumber?: string;
  mobileNumber?: string;
  departmentId?: string;
  companyDivisionId?: string;
  companyDistrictId?: string;
  primaryAddress: string;
  primaryCity: string;
  primaryState: string;
  primaryPostalCode: string;
  secondaryAddress?: string;
  secondaryCity?: string;
  secondaryState?: string;
  secondaryPostalCode?: string;
}

export const employeeService = {
  async getAll(companyId: string, search?: string, pageNumber = 1, pageSize = 500) {
    const { data } = await api.get<ApiResponse<PaginatedList<Employee>>>('/employees', {
      params: { companyId, search, pageNumber, pageSize }
    });
    return data.data!;
  },
  async getById(id: string) {
    const { data } = await api.get<ApiResponse<Employee>>(`/employees/${id}`);
    return data.data!;
  },
  async create(payload: CreateEmployeePayload) {
    const { data } = await api.post<ApiResponse<{ employeeId: string; employeeCode: string }>>('/employees', payload);
    return data.data!;
  },
  async update(id: string, payload: UpdateEmployeePayload) {
    await api.put(`/employees/${id}`, payload);
  },
  async restore(id: string, payload: RestoreEmployeePayload) {
    await api.post(`/employees/${id}/restore`, payload);
  },
  async delete(id: string) {
    await api.delete(`/employees/${id}`);
  },
  async getAssignedWork(employeeId: string) {
    const { data } = await api.get<ApiResponse<EmployeeAssignedWork[]>>(`/employees/${employeeId}/assigned-work`);
    return data.data!;
  },
  async getAssignedWorkById(assignmentId: string) {
    const { data } = await api.get<ApiResponse<EmployeeAssignedWork>>(`/employees/assigned-work/${assignmentId}`);
    return data.data!;
  },
  async getAvailableSubscriptions(employeeId: string, customerId: string) {
    const { data } = await api.get<ApiResponse<AvailableSubscription[]>>(
      `/employees/${employeeId}/available-subscriptions`,
      { params: { customerId } }
    );
    return data.data!;
  },
  async assignWork(employeeId: string, subscriptionId: string, customerId: string) {
    const { data } = await api.post<ApiResponse<string>>(`/employees/${employeeId}/assign-work`, {
      subscriptionId,
      customerId,
    });
    return data.data!;
  },
  async deleteAssignedWork(assignmentId: string) {
    await api.delete(`/employees/assigned-work/${assignmentId}`);
  },

  // Employee self-service (/me)
  async getMyProfile() {
    const { data } = await api.get<ApiResponse<MyProfileDto>>('/employees/me');
    return data.data!;
  },
  async getMyStats() {
    const { data } = await api.get<ApiResponse<MyStatsDto>>('/employees/me/stats');
    return data.data!;
  },
  async getMyAssignedWork() {
    const { data } = await api.get<ApiResponse<EmployeeAssignedWork[]>>('/employees/me/assigned-work');
    return data.data!;
  },
  async getMyAssignmentDetail(assignmentId: string) {
    const { data } = await api.get<ApiResponse<MyAssignmentDetailDto>>(`/employees/me/assigned-work/${assignmentId}`);
    return data.data!;
  },
  async getMyChangeNotices(params?: { search?: string; fromDate?: string; toDate?: string; pageNumber?: number; pageSize?: number }) {
    const { data } = await api.get<ApiResponse<PaginatedList<ChangeNotice>>>('/employees/me/change-notices', { params });
    return data.data!;
  },
};
