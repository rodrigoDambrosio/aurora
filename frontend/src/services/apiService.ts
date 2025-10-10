/**
 * API Service Configuration
 * 
 * Centralizes all API communication for the Aurora application.
 * Uses environment variables or falls back to proxy configuration in development.
 */

// API Configuration
const API_BASE_URL = import.meta.env.VITE_API_URL || '/api';

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
}

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
}

export interface ParseNaturalLanguageRequestDto {
  text: string;
  timezoneOffsetMinutes: number;
}

export interface AIValidationResult {
  isApproved: boolean;
  recommendationMessage: string;
  severity: 'Info' | 'Warning' | 'Critical';
  suggestions: string[];
}

export interface ParseNaturalLanguageResponseDto {
  success: boolean;
  event: CreateEventDto;
  validation?: AIValidationResult;
  errorMessage?: string;
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

class ApiError extends Error {
  status?: number;
  endpoint?: string;

  constructor(
    message: string,
    status?: number,
    endpoint?: string
  ) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.endpoint = endpoint;
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

      const response = await fetch(url, {
        headers: {
          'Content-Type': 'application/json',
          ...options?.headers,
        },
        ...options,
      });

      if (!response.ok) {
        throw new ApiError(
          `HTTP ${response.status}: ${response.statusText}`,
          response.status,
          endpoint
        );
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

  // ===== EVENTS ENDPOINTS =====

  /**
   * Get weekly events for a specific week
   */
  async getWeeklyEvents(weekStart: string, userId?: string): Promise<WeeklyEventsResponseDto> {
    const request: WeeklyEventsRequestDto = {
      weekStart,
      userId
    };

    return this.fetchApi<WeeklyEventsResponseDto>('/events/weekly', {
      method: 'POST',
      body: JSON.stringify(request)
    });
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
  }
};