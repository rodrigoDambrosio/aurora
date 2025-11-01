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
  eventCategory: EventCategoryDto;
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
    const queryParam = userId ? `?userId=${userId}` : '';
    return this.fetchApi<void>(`/eventcategories/${id}${queryParam}`, {
      method: 'DELETE',
      body: deleteData ? JSON.stringify(deleteData) : undefined
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
  }
};