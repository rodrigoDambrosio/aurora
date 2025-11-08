/**
 * API Service Configuration
 * 
 * Centralizes all API communication for the Aurora application.
 * Uses environment variables or falls back to proxy configuration in development.
 */

// API Configuration
const API_BASE_URL = import.meta.env.VITE_API_URL || '/api';

const getAccessToken = (): string | null => {
  if (typeof window === 'undefined') {
    return null;
  }

  try {
    return window.localStorage.getItem('auroraAccessToken');
  } catch (storageError) {
    console.warn('Accessing localStorage for auth token failed', storageError);
    return null;
  }
};

// ===== TYPE DEFINITIONS =====

export interface HealthResponse {
  status: string;
  message: string;
  timestamp: string;
  version: string;
  environment: string;
}

export interface EventCategoryDto {
  id: string;
  name: string;
  description?: string;
  color: string;
  icon?: string;
  isSystemDefault: boolean;
  sortOrder: number;
  userId?: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface CreateEventCategoryDto {
  name: string;
  description?: string;
  color: string;
  icon?: string;
}

export interface UpdateEventCategoryDto {
  name: string;
  description?: string;
  color: string;
  icon?: string;
}

export interface DeleteEventCategoryDto {
  reassignToCategoryId?: string;
}

export type EventPriority = 1 | 2 | 3 | 4;

export interface EventDto {
  id: string;
  title: string;
  description?: string;
  startDate: string;
  endDate: string;
  isAllDay: boolean;
  location?: string;
  color?: string;
  notes?: string;
  isRecurring: boolean;
  priority: EventPriority;
  eventCategoryId: string;
  eventCategory?: EventCategoryDto;
  moodRating?: number | null;
  moodNotes?: string | null;
  createdAt?: string;
  updatedAt?: string;
  userId?: string;
}

export interface WeeklyEventsRequestDto {
  weekStart: string;
  userId?: string;
}

export interface WeeklyEventsResponseDto {
  weekStart: string;
  weekEnd: string;
  events: EventDto[];
  categories: EventCategoryDto[];
  userId: string;
}

export interface CreateEventDto {
  title: string;
  description?: string;
  startDate: string;
  endDate: string;
  isAllDay: boolean;
  location?: string;
  color?: string;
  notes?: string;
  eventCategoryId: string;
  priority: EventPriority;
  timezoneOffsetMinutes?: number;
}

export interface UpdateEventMoodDto {
  moodRating?: number | null;
  moodNotes?: string | null;
}

export interface ParseNaturalLanguageRequestDto {
  text: string;
  timezoneOffsetMinutes: number;
}

export interface AIValidationResult {
  isApproved: boolean;
  recommendationMessage: string;
  severity: 'Info' | 'Warning' | 'Critical' | 0 | 1 | 2 | number;
  suggestions: string[];
  usedAi?: boolean;
}

export interface ParseNaturalLanguageResponseDto {
  success: boolean;
  event: CreateEventDto;
  validation?: AIValidationResult;
  errorMessage?: string;
}

export interface GeneratePlanRequestDto {
  goal: string;
  timezoneOffsetMinutes: number;
  startDate?: string;
  durationWeeks?: number;
  sessionsPerWeek?: number;
  sessionDurationMinutes?: number;
  preferredTimeOfDay?: string;
  categoryId?: string;
}

export interface GeneratePlanResponseDto {
  planTitle: string;
  planDescription: string;
  durationWeeks: number;
  totalSessions: number;
  events: CreateEventDto[];
  additionalTips?: string;
  hasPotentialConflicts: boolean;
  conflictWarnings: string[];
}

export interface RegisterUserRequestDto {
  name: string;
  email: string;
  password: string;
}

export interface LoginRequestDto {
  email: string;
  password: string;
}

export interface UserSummaryDto {
  id: string;
  name: string;
  email: string;
  isEmailVerified: boolean;
}

export interface AuthResponseDto {
  accessToken: string;
  expiresAtUtc: string;
  user: UserSummaryDto;
}

export interface UserProfileDto {
  id: string;
  name: string;
  email: string;
  timezone?: string;
}

export interface UpdateUserProfileDto {
  name?: string;
  email?: string;
  timezone?: string;
}

export interface UserPreferencesDto {
  id: string;
  userId: string;
  theme: 'light' | 'dark';
  language: string;
  defaultReminderMinutes: number;
  firstDayOfWeek: number;
  timeFormat: '12h' | '24h';
  dateFormat: string;
  workStartTime?: string;
  workEndTime?: string;
  workDaysOfWeek?: number[];
  exerciseDaysOfWeek?: number[];
  nlpKeywords?: string[];
  notificationsEnabled: boolean;
}

export interface UpdateUserPreferencesDto {
  theme?: 'light' | 'dark';
  language?: string;
  defaultReminderMinutes?: number;
  firstDayOfWeek?: number;
  timeFormat?: '12h' | '24h';
  dateFormat?: string;
  workStartTime?: string;
  workEndTime?: string;
  workDaysOfWeek?: number[];
  exerciseDaysOfWeek?: number[];
  nlpKeywords?: string[];
  notificationsEnabled?: boolean;
}

export interface DailyMoodEntryDto {
  id: string;
  entryDate: string;
  moodRating: number;
  notes?: string | null;
  userId?: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface MonthlyMoodResponseDto {
  year: number;
  month: number;
  entries: DailyMoodEntryDto[];
  userId?: string;
}

export interface UpsertDailyMoodRequestDto {
  entryDate: string;
  moodRating: number;
  notes?: string | null;
}

// ===== RECOMMENDATIONS DTOs =====

export interface RecommendationRequestDto {
  referenceDate?: string;
  limit?: number;
  currentMood?: number;
  externalContext?: string;
}

export interface RecommendationDto {
  id: string;
  title: string;
  subtitle?: string | null;
  reason: string;
  recommendationType: string;
  suggestedStart: string;
  suggestedDurationMinutes: number;
  confidence: number;
  categoryId?: string | null;
  categoryName?: string | null;
  moodImpact?: string | null;
  summary?: string | null;
}

export interface RecommendationFeedbackDto {
  recommendationId: string;
  accepted: boolean;
  notes?: string;
  moodAfter?: number;
  submittedAtUtc?: string;
}

export interface RecommendationFeedbackSummaryDto {
  totalFeedback: number;
  acceptedCount: number;
  rejectedCount: number;
  acceptanceRate: number;
  averageMoodAfter?: number | null;
  periodStartUtc: string;
  periodEndUtc: string;
}

// ===== PRODUCTIVITY ANALYSIS DTOs =====

export interface ProductivityAnalysisDto {
  hourlyProductivity: HourlyProductivityDto[];
  dailyProductivity: DailyProductivityDto[];
  goldenHours: GoldenHourDto[];
  lowEnergyHours: LowEnergyHourDto[];
  categoryProductivity: CategoryProductivityDto[];
  recommendations: ProductivityRecommendationDto[];
  analysisPeriodStart: string;
  analysisPeriodEnd: string;
  totalEventsAnalyzed: number;
  totalMoodRecordsAnalyzed: number;
}

export interface HourlyProductivityDto {
  hour: number;
  averageMood: number;
  eventsCompleted: number;
  totalEvents: number;
  completionRate: number;
  productivityScore: number;
}

export interface DailyProductivityDto {
  dayOfWeek: number;
  dayName: string;
  averageMood: number;
  productivityScore: number;
  totalEvents: number;
}

export interface GoldenHourDto {
  startHour: number;
  endHour: number;
  averageProductivityScore: number;
  description: string;
  applicableDays?: number[] | null;
}

export interface LowEnergyHourDto {
  startHour: number;
  endHour: number;
  averageProductivityScore: number;
  description: string;
}

export interface CategoryProductivityDto {
  categoryId: string;
  categoryName: string;
  categoryColor: string;
  optimalHours: number[];
  averageProductivityScore: number;
  bestDayOfWeek: number;
}

export interface ProductivityRecommendationDto {
  title: string;
  description: string;
  priority: number;
  type: string;
  affectedCategories: string[];
  suggestedHours: number[];
}

// ===== WELLNESS ANALYTICS DTOs =====

export interface MoodDaySnapshotDto {
  date: string;
  moodRating: number;
  notes?: string | null;
}

export interface MoodTrendPointDto {
  date: string;
  averageMood: number | null;
  entries: number;
}

export interface MoodDistributionSliceDto {
  moodRating: number;
  count: number;
  percentage: number;
}

export interface MoodStreaksDto {
  currentPositive: number;
  longestPositive: number;
  currentNegative: number;
  longestNegative: number;
}

export interface CategoryMoodImpactDto {
  categoryId: string;
  categoryName: string;
  categoryColor?: string | null;
  averageMood: number;
  eventCount: number;
  positiveCount: number;
  negativeCount: number;
}

export interface WellnessSummaryDto {
  year: number;
  month: number;
  averageMood: number;
  bestDay?: MoodDaySnapshotDto | null;
  worstDay?: MoodDaySnapshotDto | null;
  moodTrend: MoodTrendPointDto[];
  moodDistribution: MoodDistributionSliceDto[];
  streaks: MoodStreaksDto;
  categoryImpacts: CategoryMoodImpactDto[];
  totalTrackedDays: number;
  positiveDays: number;
  neutralDays: number;
  negativeDays: number;
  trackingCoverage: number;
  hasEventMoodData: boolean;
}

// Legacy interfaces for backward compatibility
export interface TestDataItem {
  id: number;
  name: string;
  date: string;
}

export interface TestResponse {
  message: string;
  data: TestDataItem[];
  requestInfo: {
    method: string;
    path: string;
    timestamp: string;
  };
}

// ===== API ERROR HANDLING =====

export class ApiError extends Error {
  status?: number;
  endpoint?: string;
  details?: unknown;

  constructor(
    message: string,
    status?: number,
    endpoint?: string,
    details?: unknown
  ) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.endpoint = endpoint;
    this.details = details;
  }
}

// ===== API SERVICE =====

export const apiService = {
  /**
   * Generic fetch wrapper with error handling
   */
  async fetchApi<T>(endpoint: string, options?: RequestInit): Promise<T> {
    const url = `${API_BASE_URL}${endpoint}`;

    try {
      console.log(`API Request: ${options?.method || 'GET'} ${url}`);

      const requestHeaders = new Headers(options?.headers || {});

      if (!requestHeaders.has('Content-Type')) {
        requestHeaders.set('Content-Type', 'application/json');
      }

      const accessToken = getAccessToken();
      if (accessToken && !requestHeaders.has('Authorization')) {
        requestHeaders.set('Authorization', `Bearer ${accessToken}`);
      }

      const response = await fetch(url, {
        ...options,
        headers: requestHeaders,
      });

      if (!response.ok) {
        const contentType = response.headers.get('content-type') ?? '';
        let errorPayload: unknown = null;
        let errorMessage = `HTTP ${response.status}: ${response.statusText}`;

        try {
          if (contentType.includes('application/json')) {
            errorPayload = await response.json();
            if (errorPayload && typeof errorPayload === 'object') {
              const problem = errorPayload as Record<string, unknown>;
              const detail = typeof problem.detail === 'string' ? problem.detail : undefined;
              const title = typeof problem.title === 'string' ? problem.title : undefined;
              errorMessage = detail ?? title ?? errorMessage;
            }
          } else {
            errorMessage = await response.text();
          }
        } catch (parseError) {
          console.warn('API error payload parsing failed', parseError);
        }

        throw new ApiError(errorMessage, response.status, endpoint, errorPayload);
      }

      // Handle empty responses (like DELETE 204)
      if (response.status === 204 || response.headers.get('content-length') === '0') {
        console.log(`API Success: ${endpoint} (no content)`);
        return undefined as T;
      }

      const data = await response.json();
      console.log(`API Success: ${endpoint}`, data);
      return data;
    } catch (error) {
      console.error(`API Error: ${endpoint}`, error);
      throw error instanceof ApiError ? error : new ApiError(`Network error: ${error}`);
    }
  },

  // ===== HEALTH & TESTING ENDPOINTS =====

  /**
   * Check API health status
   */
  async checkHealth(): Promise<HealthResponse> {
    return this.fetchApi<HealthResponse>('/health');
  },

  /**
   * Get test data for connectivity verification
   */
  async getTestData(): Promise<TestResponse> {
    return this.fetchApi<TestResponse>('/health/test');
  },

  // ===== AUTHENTICATION ENDPOINTS =====

  /**
   * Register a new user account
   */
  async registerUser(payload: RegisterUserRequestDto): Promise<AuthResponseDto> {
    return this.fetchApi<AuthResponseDto>('/auth/register', {
      method: 'POST',
      body: JSON.stringify(payload)
    });
  },

  /**
   * Authenticate an existing user
   */
  async loginUser(payload: LoginRequestDto): Promise<AuthResponseDto> {
    return this.fetchApi<AuthResponseDto>('/auth/login', {
      method: 'POST',
      body: JSON.stringify(payload)
    });
  },

  /**
   * Revoke the current access token
   */
  async logoutUser(): Promise<void> {
    await this.fetchApi<void>('/auth/logout', {
      method: 'POST'
    });
  },

  // ===== EVENT CATEGORIES ENDPOINTS =====

  /**
   * Get all available event categories for a user
   */
  async getEventCategories(userId?: string): Promise<EventCategoryDto[]> {
    const queryParam = userId ? `?userId=${userId}` : '';
    return this.fetchApi<EventCategoryDto[]>(`/eventcategories${queryParam}`);
  },

  /**
   * Get system (default) event categories
   */
  async getSystemCategories(): Promise<EventCategoryDto[]> {
    return this.fetchApi<EventCategoryDto[]>('/eventcategories/system');
  },

  /**
   * Get custom event categories for a user
   */
  async getCustomCategories(userId?: string): Promise<EventCategoryDto[]> {
    const queryParam = userId ? `?userId=${userId}` : '';
    return this.fetchApi<EventCategoryDto[]>(`/eventcategories/custom${queryParam}`);
  },

  /**
   * Get a specific event category by ID
   */
  async getEventCategory(id: string): Promise<EventCategoryDto> {
    return this.fetchApi<EventCategoryDto>(`/eventcategories/${id}`);
  },

  /**
   * Create a new custom event category
   */
  async createEventCategory(categoryData: CreateEventCategoryDto, userId?: string): Promise<EventCategoryDto> {
    const queryParam = userId ? `?userId=${userId}` : '';
    return this.fetchApi<EventCategoryDto>(`/eventcategories${queryParam}`, {
      method: 'POST',
      body: JSON.stringify(categoryData)
    });
  },

  /**
   * Update an existing custom event category
   */
  async updateEventCategory(id: string, categoryData: UpdateEventCategoryDto, userId?: string): Promise<EventCategoryDto> {
    const queryParam = userId ? `?userId=${userId}` : '';
    return this.fetchApi<EventCategoryDto>(`/eventcategories/${id}${queryParam}`, {
      method: 'PUT',
      body: JSON.stringify(categoryData)
    });
  },

  /**
   * Delete a custom event category, optionally reassigning events to another category
   */
  async deleteEventCategory(id: string, deleteData?: DeleteEventCategoryDto, userId?: string): Promise<void> {
    const params = new URLSearchParams();
    if (userId) params.append('userId', userId);
    if (deleteData?.reassignToCategoryId) params.append('reassignToCategoryId', deleteData.reassignToCategoryId);
    
    const queryString = params.toString();
    const url = `/eventcategories/${id}${queryString ? `?${queryString}` : ''}`;
    
    return this.fetchApi<void>(url, {
      method: 'DELETE'
    });
  },

  // ===== EVENTS ENDPOINTS =====

  /**
   * Get weekly events for a specific week
   */
  async getWeeklyEvents(weekStart: string, userId?: string, categoryId?: string): Promise<WeeklyEventsResponseDto> {
    const request: WeeklyEventsRequestDto = {
      weekStart,
      userId
    };

    const queryParam = categoryId ? `?categoryId=${categoryId}` : '';

    return this.fetchApi<WeeklyEventsResponseDto>(`/events/weekly${queryParam}`, {
      method: 'POST',
      body: JSON.stringify(request)
    });
  },

  /**
   * Get events for a specific month
   */
  async getMonthlyEvents(year: number, month: number, userId?: string, categoryId?: string): Promise<WeeklyEventsResponseDto> {
    const params = new URLSearchParams();
    params.append('year', year.toString());
    params.append('month', month.toString());
    if (userId) params.append('userId', userId);
    if (categoryId) params.append('categoryId', categoryId);

    return this.fetchApi<WeeklyEventsResponseDto>(`/events/monthly?${params.toString()}`);
  },

  /**
   * Get all events for a user
   */
  async getEvents(userId?: string): Promise<EventDto[]> {
    const queryParam = userId ? `?userId=${userId}` : '';
    return this.fetchApi<EventDto[]>(`/events${queryParam}`);
  },

  /**
   * Get a specific event by ID
   */
  async getEvent(id: string): Promise<EventDto> {
    return this.fetchApi<EventDto>(`/events/${id}`);
  },

  /**
   * Create a new event
   */
  async createEvent(eventData: CreateEventDto): Promise<EventDto> {
    return this.fetchApi<EventDto>('/events', {
      method: 'POST',
      body: JSON.stringify(eventData)
    });
  },

  /**
   * Request an AI validation for an event without creating it
   */
  async validateEvent(eventData: CreateEventDto): Promise<AIValidationResult> {
    return this.fetchApi<AIValidationResult>('/events/validate', {
      method: 'POST',
      body: JSON.stringify(eventData)
    });
  },

  /**
   * Update an existing event
   */
  async updateEvent(id: string, eventData: CreateEventDto): Promise<EventDto> {
    return this.fetchApi<EventDto>(`/events/${id}`, {
      method: 'PUT',
      body: JSON.stringify(eventData)
    });
  },

  /**
   * Delete an event
   */
  async deleteEvent(id: string): Promise<void> {
    return this.fetchApi<void>(`/events/${id}`, {
      method: 'DELETE'
    });
  },

  /**
   * Update mood tracking for an event
   */
  async updateEventMood(eventId: string, payload: UpdateEventMoodDto): Promise<EventDto> {
    return this.fetchApi<EventDto>(`/events/${eventId}/mood`, {
      method: 'PATCH',
      body: JSON.stringify(payload)
    });
  },

  /**
   * Parse natural language text to event using AI
   */
  async parseNaturalLanguage(text: string): Promise<ParseNaturalLanguageResponseDto> {
    // Obtener el offset de zona horaria del navegador en minutos
    const timezoneOffsetMinutes = -new Date().getTimezoneOffset();

    const request: ParseNaturalLanguageRequestDto = {
      text,
      timezoneOffsetMinutes
    };

    return this.fetchApi<ParseNaturalLanguageResponseDto>('/events/from-text', {
      method: 'POST',
      body: JSON.stringify(request)
    });
  },

  /**
   * Generate a multi-day plan from a high-level goal using AI
   */
  async generatePlan(goal: string, options?: {
    startDate?: string;
    durationWeeks?: number;
    sessionsPerWeek?: number;
    sessionDurationMinutes?: number;
    preferredTimeOfDay?: string;
    categoryId?: string;
  }): Promise<GeneratePlanResponseDto> {
    // Obtener el offset de zona horaria del navegador en minutos
    const timezoneOffsetMinutes = -new Date().getTimezoneOffset();

    const request: GeneratePlanRequestDto = {
      goal,
      timezoneOffsetMinutes,
      ...options
    };

    return this.fetchApi<GeneratePlanResponseDto>('/events/generate-plan', {
      method: 'POST',
      body: JSON.stringify(request)
    });
  },

  // ===== MOOD TRACKING ENDPOINTS =====

  /**
   * Get daily mood entries for a specific month
   */
  async getMonthlyMoodEntries(year: number, month: number): Promise<MonthlyMoodResponseDto> {
    const params = new URLSearchParams();
    params.append('year', year.toString());
    params.append('month', month.toString());
    return this.fetchApi<MonthlyMoodResponseDto>(`/moods/monthly?${params.toString()}`);
  },

  /**
   * Create or update a daily mood entry
   */
  async upsertDailyMood(payload: UpsertDailyMoodRequestDto): Promise<DailyMoodEntryDto> {
    return this.fetchApi<DailyMoodEntryDto>('/moods', {
      method: 'POST',
      body: JSON.stringify(payload)
    });
  },

  /**
   * Delete a daily mood entry for a specific date
   */
  async deleteDailyMood(entryDateUtcIso: string): Promise<void> {
    const params = new URLSearchParams();
    params.append('date', entryDateUtcIso);
    return this.fetchApi<void>(`/moods?${params.toString()}`, {
      method: 'DELETE'
    });
  },

  // ===== USER PROFILE ENDPOINTS =====

  /**
   * Get current user profile
   */
  async getUserProfile(): Promise<UserProfileDto> {
    return this.fetchApi<UserProfileDto>('/user/profile');
  },

  /**
   * Update current user profile
   */
  async updateUserProfile(payload: UpdateUserProfileDto): Promise<UserProfileDto> {
    return this.fetchApi<UserProfileDto>('/user/profile', {
      method: 'PUT',
      body: JSON.stringify(payload)
    });
  },

  // ===== USER PREFERENCES ENDPOINTS =====

  /**
   * Get current user preferences
   */
  async getUserPreferences(): Promise<UserPreferencesDto> {
    return this.fetchApi<UserPreferencesDto>('/user/preferences');
  },

  /**
   * Update current user preferences
   */
  async updateUserPreferences(payload: UpdateUserPreferencesDto): Promise<UserPreferencesDto> {
    return this.fetchApi<UserPreferencesDto>('/user/preferences', {
      method: 'PUT',
      body: JSON.stringify(payload)
    });
  },

  // ===== WELLNESS ANALYTICS ENDPOINTS =====

  async getWellnessSummary(year: number, month: number): Promise<WellnessSummaryDto> {
    const params = new URLSearchParams();
    params.append('year', year.toString());
    params.append('month', month.toString());
    return this.fetchApi<WellnessSummaryDto>(`/wellness/summary?${params.toString()}`);
  },

  // ===== RECOMMENDATIONS ENDPOINTS =====

  async getRecommendations(params?: RecommendationRequestDto): Promise<RecommendationDto[]> {
    const searchParams = new URLSearchParams();

    if (params?.referenceDate) {
      searchParams.append('ReferenceDate', params.referenceDate);
    }

    if (typeof params?.limit === 'number') {
      searchParams.append('Limit', params.limit.toString());
    }

    if (typeof params?.currentMood === 'number') {
      searchParams.append('CurrentMood', params.currentMood.toString());
    }

    if (params?.externalContext) {
      searchParams.append('ExternalContext', params.externalContext);
    }

    const queryString = searchParams.toString();
    const endpoint = queryString.length > 0 ? `/recommendations?${queryString}` : '/recommendations';

    return this.fetchApi<RecommendationDto[]>(endpoint);
  },

  async submitRecommendationFeedback(payload: RecommendationFeedbackDto): Promise<void> {
    const body = {
      ...payload,
      submittedAtUtc: payload.submittedAtUtc ?? new Date().toISOString()
    };

    await this.fetchApi<void>('/recommendations/feedback', {
      method: 'POST',
      body: JSON.stringify(body)
    });
  },

  async getRecommendationFeedbackSummary(days = 30): Promise<RecommendationFeedbackSummaryDto> {
    const clamped = Math.min(Math.max(days, 1), 180);
    return this.fetchApi<RecommendationFeedbackSummaryDto>(`/recommendations/feedback/summary?days=${clamped}`);
  },

  // Productivity Analysis
  async getProductivityAnalysis(periodDays = 30): Promise<ProductivityAnalysisDto> {
    const clamped = Math.min(Math.max(periodDays, 1), 365);
    return this.fetchApi<ProductivityAnalysisDto>(`/user/productivity-analysis?periodDays=${clamped}`);
  }
};