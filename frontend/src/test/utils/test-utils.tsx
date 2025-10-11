import { render } from '@testing-library/react'
import type { ReactElement } from 'react'
import { vi } from 'vitest'

// Custom render function that includes any providers we might need
export const renderWithProviders = (ui: ReactElement, options = {}) => {
  // For now, we don't have any context providers to wrap
  // But this is where we would add them (Theme, Auth, etc.)
  return render(ui, {
    // Add any custom render options here
    ...options,
  })
}

// Utility functions for testing
export const createMockEvent = (overrides = {}) => ({
  id: '1',
  title: 'Test Event',
  description: 'Test Description',
  startDate: '2025-09-30T10:00:00.000Z',
  endDate: '2025-09-30T11:00:00.000Z',
  isAllDay: false,
  isRecurring: false,
  priority: 2,
  eventCategory: {
    id: '1',
    name: 'Test Category',
    description: 'Test Category Description',
    color: '#3b82f6',
    isSystemDefault: true,
    sortOrder: 1,
  },
  ...overrides,
})

export const createMockCategory = (overrides = {}) => ({
  id: '1',
  name: 'Test Category',
  description: 'Test Category Description',
  color: '#3b82f6',
  isSystemDefault: true,
  sortOrder: 1,
  ...overrides,
})

export const createMockWeeklyResponse = (overrides = {}) => ({
  weekStart: '2025-09-29T00:00:00.000Z',
  weekEnd: '2025-10-05T23:59:59.999Z',
  events: [createMockEvent()],
  categories: [createMockCategory()],
  userId: 'test-user',
  ...overrides,
})

// Date utilities for testing
export const getMonday = (date = new Date()) => {
  const monday = new Date(date)
  const day = monday.getDay()
  const diff = monday.getDate() - day + (day === 0 ? -6 : 1) // Adjust when day is Sunday
  monday.setDate(diff)
  monday.setHours(0, 0, 0, 0)
  return monday
}

export const addDays = (date: Date, days: number) => {
  const result = new Date(date)
  result.setDate(result.getDate() + days)
  return result
}

export const formatDateForAPI = (date: Date) => {
  return date.toISOString()
}

// Wait utilities
export const waitForNextTick = () => new Promise(resolve => setTimeout(resolve, 0))

// Mock localStorage
export const mockLocalStorage = (() => {
  let store: Record<string, string> = {}

  return {
    getItem: (key: string) => store[key] || null,
    setItem: (key: string, value: string) => {
      store[key] = value.toString()
    },
    removeItem: (key: string) => {
      delete store[key]
    },
    clear: () => {
      store = {}
    },
    length: () => Object.keys(store).length,
    key: (index: number) => Object.keys(store)[index] || null,
  }
})()

// Console utilities for testing
export const mockConsole = () => {
  const originalConsole = { ...console }

  const mockLog = vi.fn()
  const mockError = vi.fn()
  const mockWarn = vi.fn()

  console.log = mockLog
  console.error = mockError
  console.warn = mockWarn

  return {
    mockLog,
    mockError,
    mockWarn,
    restore: () => {
      Object.assign(console, originalConsole)
    },
  }
}

// Re-export everything from testing library for convenience
// eslint-disable-next-line react-refresh/only-export-components
export * from '@testing-library/react'
export { userEvent } from '@testing-library/user-event'

