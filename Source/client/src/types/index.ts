export interface ApiResponse<T> {
  success: boolean;
  message?: string;
  data?: T;
  errors?: Record<string, string[]>;
}

export interface RetryNotificationResult {
  success: boolean;
  message: string;
}

export interface PaginatedList<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  email: string;
  fullName: string;
  role: string;
  userId: string;
  isForcePasswordChange: boolean;
}

export interface Company {
  id: string;
  companyCode: string;
  companyName: string;
  primaryEmail: string;
  secondaryEmail?: string;
  phoneNumber?: string;
  primaryAddress: string;
  primaryCity: string;
  primaryState: string;
  primaryPostalCode: string;
  secondaryAddress?: string;
  secondaryCity?: string;
  secondaryState?: string;
  secondaryPostalCode?: string;
  websiteUrl?: string;
  isActive: boolean;
  createdAt: string;
}

// ── Agent ────────────────────────────────────────────────────────────────────
export interface AgentMessage {
  id: string;
  role: 'user' | 'assistant';
  content: string;
  toolName?: string;
  createdAt: string;
}

export interface ConversationSummary {
  id: string;
  title: string;
  status: string;
  lastOperation?: string;
  updatedAt: string;
  createdAt: string;
}

export interface ConversationDetail {
  id: string;
  title: string;
  status: string;
  lastOperation?: string;
  contextDataJson?: string;
  messages: AgentMessage[];
  createdAt: string;
  updatedAt: string;
}

export interface SendMessageResponse {
  conversationId: string;
  assistantMessage: AgentMessage;
  status: string;
  lastOperation?: string;
  contextDataJson?: string;
}

export interface CompanyDivision {
  id: string;
  companyId: string;
  companyName: string;
  departmentId?: string;
  departmentName?: string;
  name: string;
  description?: string;
  isActive: boolean;
  createdAt: string;
}

export interface Department {
  id: string;
  companyId: string;
  name: string;
  description?: string;
  isActive: boolean;
  divisionCount: number;
  createdAt: string;
}

export interface District {
  id: string;
  companyId: string;
  name: string;
  description?: string;
  isActive: boolean;
  createdAt: string;
}

export interface Customer {
  id: string;
  companyId: string;
  companyName: string;
  customerCode: string;
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
  isActive: boolean;
  createdAt: string;
}

export interface CurrentCustomerProfile {
  customer: Customer;
  company: Company;
}

export interface User {
  id: string;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
  phoneNumber?: string;
  title?: string;
  role: UserRole;
  roleName: string;
  isActive: boolean;
  isForcePasswordChange: boolean;
  lastLoginAt?: string;
  securityGroupId?: string;
  groupName?: string;
  userId?: string;
  refId?: string;
  createdAt: string;
}

export interface Employee {
  id: string;
  employeeCode: string;
  companyId: string;
  companyName: string;
  firstName: string;
  lastName: string;
  fullName: string;
  primaryEmail: string;
  secondaryEmail?: string;
  phoneNumber?: string;
  mobileNumber?: string;
  departmentId?: string;
  departmentName?: string;
  companyDivisionId?: string;
  divisionName?: string;
  companyDistrictId?: string;
  districtName?: string;
  primaryAddress: string;
  primaryCity: string;
  primaryState: string;
  primaryPostalCode: string;
  secondaryAddress?: string;
  secondaryCity?: string;
  secondaryState?: string;
  secondaryPostalCode?: string;
  isActive: boolean;
  createdAt: string;
}

export interface EmployeeAssignedWork {
  id: string;
  employeeId: string;
  subscriptionId: string;
  customerId: string;
  customerName: string;
  subscribingLevel: SubscribingLevel;
  subscribedNodeName: string;
  governmentEntityId: string;
  governmentEntityName: string;
  agencyId?: string;
  agencyName?: string;
  regulationCategoryId?: string;
  regulationCategoryName?: string;
  regulationTypeId?: string;
  regulationTypeName?: string;
  regulationSubtypeId?: string;
  regulationSubtypeName?: string;
  isActive: boolean;
  assignedAt: string;
  assignedBy: string;
  createdAt: string;
  createdBy?: string;
}

export interface AvailableSubscription {
  id: string;
  subscribingLevel: SubscribingLevel;
  subscribedNodeName: string;
  governmentEntityId: string;
  governmentEntityName: string;
}

export interface SecurityGroup {
  id: string;
  groupName: string;
  description?: string;
  role: UserRole;
  roleName: string;
  isActive: boolean;
  userCount: number;
  createdAt: string;
}

export interface EntityCounts {
  total: number;
  active: number;
  inactive: number;
}

export interface GrievanceCounts {
  received: number;
  replied: number;
  pending: number;
}

export interface AdminDashboardStats {
  customers: EntityCounts;
  employees: EntityCounts;
  subscriptions: EntityCounts;
  divisions: EntityCounts;
  districts: EntityCounts;
  departments: EntityCounts;
  grievances: GrievanceCounts;
}

export interface UserCounts {
  total: number;
  superAdminCount: number;
  adminCount: number;
}

export interface SuperAdminDashboardStats {
  companies: EntityCounts;
  users: UserCounts;
  grievances: GrievanceCounts;
}

export interface CompanySummary {
  company: Company;
  employeeCount: number;
  customerCount: number;
}

export type UserRole = 'SuperAdmin' | 'Admin' | 'Customer' | 'Employee';
export const UserRoles: UserRole[] = ['SuperAdmin', 'Admin', 'Customer', 'Employee'];

// Regulations
export interface GovernmentEntity {
  id: string;
  titleNumber: number;
  titleName: string;
  source?: string;
  isSyncEnabled: boolean;
  isImported: boolean;
  lastAmendedDate?: string;
  lastSyncedDate?: string;
  createdAt: string;
}

export interface SectionDto {
  id: string;
  sectionNumber: string;
  sectionName: string;
  version: number;
  isActive: boolean;
  lastAmendedDate?: string;
  htmlContent?: string;
}

export interface SubtypeHierarchy {
  id: string;
  subpartIdentifier: string;
  subpartName: string;
  sections: SectionDto[];
}

export interface TypeHierarchy {
  id: string;
  partNumber: number;
  partName: string;
  subtypes: SubtypeHierarchy[];
  sections: SectionDto[];
}

export interface CategoryHierarchy {
  id: string;
  subchapterIdentifier: string;
  subchapterName: string;
  types: TypeHierarchy[];
}

export interface AgencyHierarchy {
  id: string;
  chapterNumber: string;
  agencyName: string;
  categories: CategoryHierarchy[];
}

export interface RegulationHierarchy {
  governmentEntityId: string;
  titleNumber: number;
  titleName: string;
  agencies: AgencyHierarchy[];
}

export interface LookupItem {
  id: string;
  name: string;
}

export interface RegulationDetail {
  id?: string;
  regulationId: string;
  description?: string;
  condition?: string;
  suggestedTask?: string;
  frequencyTypeId?: string;
  frequencyTypeName?: string;
  dueDateTypeId?: string;
  dueDateTypeName?: string;
}

export type AiProvider = 'azure-openai' | 'gemini' | 'claude' | 'openai' | 'openrouter';

export interface RegulationDetailSuggestionRequest {
  description?: string;
  condition?: string;
  suggestedTask?: string;
}

export interface AiProviderConnection {
  provider: AiProvider;
  providerDisplayName: string;
  endpoint?: string;
  deploymentName?: string;
  model?: string;
  apiVersion?: string;
  isConfigured: boolean;
}

export interface UpsertAiProviderConnectionRequest {
  provider: AiProvider;
  apiKey: string;
  endpoint?: string;
  deploymentName?: string;
  model?: string;
  apiVersion?: string;
}

export interface GetAiProviderModelsRequest {
  provider: AiProvider;
  apiKey: string;
  endpoint?: string;
}

export interface AiProviderModelOption {
  value: string;
  label: string;
}

export interface RegulationDetailSuggestionResponse {
  description: string;
  condition: string;
  suggestedTask: string;
  frequencyType?: string;
  dueDateType?: string;
  provider: AiProvider;
}

export interface SyncResult {
  governmentEntityId: string;
  sectionsAdded: number;
  sectionsUpdated: number;
  sectionsDeactivated: number;
  outboxEventsQueued: number;
  duration: string;
  error?: string;
}

export interface RegulationSyncSchedulerHistory {
  id: string;
  governmentEntityId: string;
  governmentEntityName: string;
  titleNumber: number;
  operationType: string;
  status: string;
  triggerSource: string;
  importedRecordsCount: number;
  changedRecordsCount: number;
  deactivatedRecordsCount: number;
  outboxEventsQueuedCount: number;
  details?: string;
  startedAt: string;
  completedAt: string;
}

// Subscriptions
export type SubscribingLevel = 'Entity' | 'Agency' | 'Category' | 'Type' | 'SubType' | 'Regulation';

export interface CustomerSubscription {
  id: string;
  subscribingLevel: SubscribingLevel;
  subscribedNodeName: string;
  governmentEntityId: string;
  governmentEntityName: string;
  agencyId?: string;
  agencyName?: string;
  regulationCategoryId?: string;
  regulationCategoryName?: string;
  regulationTypeId?: string;
  regulationTypeName?: string;
  regulationSubtypeId?: string;
  regulationSubtypeName?: string;
  isActive: boolean;
  createdAt: string;
}

export interface Subscription {
  id: string;
  customerId: string;
  customerName: string;
  governmentEntityId: string;
  agencyId?: string;
  regulationCategoryId?: string;
  regulationTypeId?: string;
  regulationSubtypeId?: string;
  regulationId?: string;
  subscribingLevel: SubscribingLevel;
  subscribedNodeName: string;
  isActive: boolean;
  createdAt: string;
}

export interface SubscriptionItemPayload {
  governmentEntityId: string;
  agencyId?: string;
  regulationCategoryId?: string;
  regulationTypeId?: string;
  regulationSubtypeId?: string;
  regulationId?: string;
  subscribingLevel: SubscribingLevel;
  subscribedNodeName: string;
}

export interface CreateSubscriptionsResult {
  saved: number;
  duplicates: string[];
}

export interface SubscriptionDetailsDto {
  subscriptionId: string;
  customerId: string;
  customerName: string;
  level: SubscribingLevel;
  nodeName: string;
  breadcrumb: string;
  agencies: AgencyHierarchy[];
}

// Change Notices & Notifications
export interface ChangeNotice {
  id: string;
  regulationId: string;
  governmentEntityId: string;
  governmentEntityName: string;
  agencyName: string;
  regulationCategoryName: string;
  regulationTypeName: string;
  regulationSubtypeName?: string;
  sectionNumber: string;
  sectionTitle: string;
  archivedVersion: number;
  newVersion: number;
  changedAt: string;
  createdAt: string;
}

export interface NotificationItem {
  id: string;
  subscriptionId: string;
  changeNoticeId: string;
  customerId: string;
  customerName?: string;
  recipientEmail?: string;
  subject?: string;
  governmentEntityName?: string;
  presentRegulationName?: string;
  status: string;
  sentAt?: string;
  isRead: boolean;
  deliveryStatus: string;
  createdAt: string;
}

// Regulation Change Logs
export interface RegulationChangeLog {
  id: string;
  governmentEntityId: string;
  governmentEntityName: string;
  agencyId: string;
  agencyName: string;
  regulationCategoryId: string;
  regulationCategoryName: string;
  regulationTypeId: string;
  regulationTypeName: string;
  regulationSubtypeId?: string;
  regulationSubtypeName?: string;
  sectionNumber: string;
  sectionTitle: string;
  archivedVersion: number;
  newVersion: number;
  changedAt: string;
}

export interface RegulationChangeLogDetail extends RegulationChangeLog {
  regulationId: string;
  previousContentHash?: string;
  newContentHash?: string;
  previousHtmlContent?: string;
  newHtmlContent?: string;
  previousAmendedDate?: string;
  newAmendedDate?: string;
  createdAt: string;
}

export interface NotificationLog {
  id: string;
  customerId: string;
  customerName?: string;
  companyId?: string;
  governmentEntityId?: string;
  governmentEntityName?: string;
  agencyId?: string;
  agencyName?: string;
  regulationCategoryId?: string;
  regulationCategoryName?: string;
  regulationTypeId?: string;
  regulationTypeName?: string;
  regulationSubtypeId?: string;
  regulationSubtypeName?: string;
  previousRegulationId?: string;
  previousRegulationName?: string;
  presentRegulationId?: string;
  presentRegulationName?: string;
  subject?: string;
  body?: string;
  senderEmail?: string;
  senderName?: string;
  recipientEmail?: string;
  recipientName?: string;
  isNotified: boolean;
  status: 'Pending' | 'Sent' | 'Failed';
  retryCount: number;
  lastRetryAt?: string;
  sentAt?: string;
  failureReason?: string;
  isRead: boolean;
  deliveryStatus: string;
  createdAt: string;
}

export interface NotificationSentHistoryItem {
  id: string;
  attemptNumber: number;
  isSuccess: boolean;
  status: string;
  failureReason?: string;
  attemptedAt: string;
}

export interface NotificationLogDetail extends NotificationLog {
  subscriptionId: string;
  changeNoticeId: string;
  sentHistory: NotificationSentHistoryItem[];
}

export interface AuthUser {
  userId: string;
  email: string;
  fullName: string;
  role: UserRole;
  accessToken: string;
  refreshToken: string;
  isForcePasswordChange: boolean;
}

export type GrievancePriority = 'Low' | 'Medium' | 'High';
export type GrievanceStatus = 'Open' | 'InProgress' | 'Escalated' | 'Closed';

export interface GrievanceRecipientSubscription {
  id: string;
  name: string;
}

export interface GrievanceContact {
  securityUserId: string;
  userId?: string;
  fullName: string;
  role: UserRole;
  companyId?: string;
  companyName?: string;
  availableSubscriptions: GrievanceRecipientSubscription[];
}

export interface GrievanceListItem {
  id: string;
  subject: string;
  description: string;
  companyId: string;
  companyName?: string;
  subscriptionId?: string;
  subscriptionName?: string;
  status: GrievanceStatus;
  priority: GrievancePriority;
  isActive: boolean;
  createdBySecurityUserId: string;
  createdByName: string;
  createdByRole: UserRole;
  recipientSecurityUserId: string;
  recipientName: string;
  recipientRole: UserRole;
  replyCount: number;
  createdAt: string;
  updatedAt?: string;
  lastMessageAt: string;
  canReply: boolean;
}

export interface GrievanceReply {
  id: string;
  senderSecurityUserId: string;
  senderName: string;
  senderRole: UserRole;
  message: string;
  createdAt: string;
}

export interface GrievanceDetail extends Omit<GrievanceListItem, 'replyCount'> {
  replies: GrievanceReply[];
}

export interface CreateGrievanceRequest {
  subject: string;
  description: string;
  recipientSecurityUserId: string;
  subscriptionId?: string;
  priority: GrievancePriority;
}

export interface MyProfileDto {
  id: string;
  employeeCode: string;
  firstName: string;
  lastName: string;
  fullName: string;
  primaryEmail: string;
  secondaryEmail?: string;
  phoneNumber?: string;
  mobileNumber?: string;
  departmentName?: string;
  divisionName?: string;
  districtName?: string;
  primaryAddress: string;
  primaryCity: string;
  primaryState: string;
  primaryPostalCode: string;
  secondaryAddress?: string;
  secondaryCity?: string;
  secondaryState?: string;
  secondaryPostalCode?: string;
  isActive: boolean;
  createdAt: string;
  companyId: string;
  companyName: string;
  companyEmail: string;
  companyPhone?: string;
  companyAddress: string;
  companyCity: string;
  companyState: string;
  companyPostalCode: string;
  companyWebsite?: string;
}

export interface MyStatsDto {
  totalAssignments: number;
  activeAssignments: number;
  totalChangeNotices: number;
}

export interface MyAssignmentDetailDto {
  id: string;
  employeeId: string;
  subscriptionId: string;
  customerId: string;
  customerName: string;
  customerCode: string;
  primaryContactFirstName: string;
  primaryContactLastName: string;
  customerEmail?: string;
  customerPhone?: string;
  customerMobileNumber?: string;
  primaryAddress?: string;
  primaryCity?: string;
  primaryState?: string;
  primaryPostalCode?: string;
  subscribingLevel: SubscribingLevel;
  subscribedNodeName: string;
  governmentEntityId: string;
  governmentEntityName: string;
  agencyId?: string;
  agencyName?: string;
  regulationCategoryId?: string;
  regulationCategoryName?: string;
  regulationTypeId?: string;
  regulationTypeName?: string;
  regulationSubtypeId?: string;
  regulationSubtypeName?: string;
  regulationId?: string;
  regulationDescription?: string;
  regulationCondition?: string;
  suggestedTask?: string;
  frequencyTypeName?: string;
  dueDateTypeName?: string;
  isActive: boolean;
  assignedAt: string;
  assignedBy: string;
  createdAt: string;
  createdBy?: string;
}
