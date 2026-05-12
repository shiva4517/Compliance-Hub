import { api } from './api';
import type {
  AgencyHierarchy,
  AiProviderConnection,
  AiProviderModelOption,
  ApiResponse,
  CategoryHierarchy,
  GetAiProviderModelsRequest,
  GovernmentEntity,
  PaginatedList,
  RegulationDetail,
  RegulationDetailSuggestionRequest,
  RegulationDetailSuggestionResponse,
  RegulationHierarchy,
  SectionDto,
  SubtypeHierarchy,
  SyncResult,
  TypeHierarchy,
  UpsertAiProviderConnectionRequest,
} from '../types';

export const regulationService = {
  getAll: async (params?: { search?: string; isSyncEnabled?: boolean; isImported?: boolean; pageNumber?: number; pageSize?: number }) => {
    const response = await api.get<ApiResponse<PaginatedList<GovernmentEntity>>>('/regulations', { params });
    return response.data.data!;
  },

  getById: async (id: string) => {
    const response = await api.get<ApiResponse<GovernmentEntity>>(`/regulations/${id}`);
    return response.data.data!;
  },

  getHierarchy: async (id: string) => {
    const response = await api.get<ApiResponse<RegulationHierarchy>>(`/regulations/${id}/hierarchy`);
    return response.data.data!;
  },

  create: async (data: { titleNumber: number; titleName: string; source?: string; isSyncEnabled: boolean }) => {
    const response = await api.post<ApiResponse<string>>('/regulations', data);
    return response.data.data!;
  },

  update: async (id: string, data: { titleName: string; source?: string; isSyncEnabled: boolean }) => {
    await api.put(`/regulations/${id}`, { id, ...data });
  },

  toggleSync: async (id: string) => {
    const response = await api.put<ApiResponse<boolean>>(`/regulations/${id}/toggle-sync`);
    return response.data.data!;
  },

  triggerSync: async (id: string) => {
    const response = await api.post<ApiResponse<SyncResult>>(`/regulations/${id}/sync`);
    return response.data.data!;
  },

  seedFromEcfr: async () => {
    const response = await api.post<ApiResponse<number>>('/regulations/seed-from-ecfr');
    return response.data.data!;
  },

  getDetail: async (regulationId: string) => {
    const response = await api.get<ApiResponse<RegulationDetail | null>>(`/regulations/${regulationId}/detail`);
    return response.data.data ?? null;
  },

  upsertDetail: async (regulationId: string, data: Omit<RegulationDetail, 'id' | 'regulationId' | 'frequencyTypeName' | 'dueDateTypeName'>) => {
    const response = await api.put<ApiResponse<string>>(`/regulations/${regulationId}/detail`, data);
    return response.data.data!;
  },

  getAiConnection: async () => {
    const response = await api.get<ApiResponse<AiProviderConnection | null>>('/regulations/ai-connection');
    return response.data.data ?? null;
  },

  upsertAiConnection: async (data: UpsertAiProviderConnectionRequest) => {
    const response = await api.put<ApiResponse<AiProviderConnection>>('/regulations/ai-connection', data);
    return response.data.data!;
  },

  getAiModels: async (data: GetAiProviderModelsRequest) => {
    const response = await api.post<ApiResponse<AiProviderModelOption[]>>('/regulations/ai-models', data);
    return response.data.data ?? [];
  },

  suggestDetail: async (regulationId: string, data: RegulationDetailSuggestionRequest) => {
    const response = await api.post<ApiResponse<RegulationDetailSuggestionResponse>>(`/regulations/${regulationId}/detail/suggest`, data);
    return response.data.data!;
  },

  // Lazy hierarchy endpoints
  getAgencies: async (entityId: string): Promise<AgencyHierarchy[]> => {
    const response = await api.get<ApiResponse<AgencyHierarchy[]>>(`/regulations/${entityId}/agencies`);
    return response.data.data!;
  },

  getCategories: async (agencyId: string): Promise<CategoryHierarchy[]> => {
    const response = await api.get<ApiResponse<CategoryHierarchy[]>>(`/regulations/agencies/${agencyId}/categories`);
    return response.data.data!;
  },

  getTypes: async (categoryId: string): Promise<TypeHierarchy[]> => {
    const response = await api.get<ApiResponse<TypeHierarchy[]>>(`/regulations/categories/${categoryId}/types`);
    return response.data.data!;
  },

  getSubtypes: async (typeId: string): Promise<SubtypeHierarchy[]> => {
    const response = await api.get<ApiResponse<SubtypeHierarchy[]>>(`/regulations/types/${typeId}/subtypes`);
    return response.data.data!;
  },

  getSections: async (subtypeId: string, typeId?: string): Promise<SectionDto[]> => {
    const params = typeId ? { typeId } : undefined;
    const response = await api.get<ApiResponse<SectionDto[]>>(`/regulations/subtypes/${subtypeId}/sections`, { params });
    return response.data.data!;
  },

  getSectionContent: async (sectionId: string): Promise<string | null> => {
    const response = await api.get<ApiResponse<string | null>>(`/regulations/sections/${sectionId}/content`);
    return response.data.data ?? null;
  },

  getSimulatedChanges: async (governmentEntityId: string, issueDate: string): Promise<SimulatedChangesResult> => {
    const response = await api.get<ApiResponse<SimulatedChangesResult>>(
      `/regulations/${governmentEntityId}/simulate/changes`, { params: { issueDate } });
    return response.data.data!;
  },

  applySimulatedChange: async (payload: ApplySimulatedChangeRequest): Promise<ApplySimulatedChangeResult> => {
    const response = await api.post<ApiResponse<ApplySimulatedChangeResult>>(
      `/regulations/simulate/apply`, payload);
    return response.data.data!;
  },
};

export interface SimulatedChangeItem {
  regulationId: string;
  sectionNumber: string;
  sectionName: string;
  amendmentDate: string | null;
  issueDate: string | null;
  currentVersion: number;
  currentContentHash: string;
  currentLastAmendedDate: string | null;
  currentHtmlContent: string;
}

export interface SimulatedChangesResult {
  titleNumber: number;
  totalChanged: number;
  matchedInLocal: number;
  items: SimulatedChangeItem[];
}

export interface ApplySimulatedChangeRequest {
  regulationId: string;
  htmlContent: string;
  simulatedDate: string;
}

export interface ApplySimulatedChangeResult {
  regulationId: string;
  newVersion: number;
}
