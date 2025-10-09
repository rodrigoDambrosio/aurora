import { act, renderHook } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { useApi, useDateUtils } from '../../hooks/useApi'

// Mock API function for testing
const mockApiFunction = vi.fn()

describe('useApi', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('should initialize with correct default state', () => {
    const { result } = renderHook(() => useApi(mockApiFunction))

    expect(result.current.loading).toBe(false)
    expect(result.current.error).toBe(null)
    expect(result.current.data).toBe(null)
  })

  it('should set loading state during API calls', async () => {
    mockApiFunction.mockImplementation(() => new Promise(resolve => setTimeout(() => resolve('test'), 100)))

    const { result } = renderHook(() => useApi(mockApiFunction))

    act(() => {
      result.current.execute()
    })

    expect(result.current.loading).toBe(true)
    expect(result.current.error).toBe(null)
  })

  it('should handle successful API calls', async () => {
    const testData = { test: 'data' }
    mockApiFunction.mockResolvedValue(testData)

    const { result } = renderHook(() => useApi(mockApiFunction))

    await act(async () => {
      await result.current.execute()
    })

    expect(result.current.loading).toBe(false)
    expect(result.current.error).toBe(null)
    expect(result.current.data).toEqual(testData)
  })

  it('should handle API errors', async () => {
    const errorMessage = 'Test error'
    mockApiFunction.mockRejectedValue(new Error(errorMessage))

    const { result } = renderHook(() => useApi(mockApiFunction))

    await act(async () => {
      await result.current.execute()
    })

    expect(result.current.loading).toBe(false)
    expect(result.current.error).toBe(errorMessage)
    expect(result.current.data).toBe(null)
  })

  it('should reset state correctly', () => {
    const { result } = renderHook(() => useApi(mockApiFunction))

    act(() => {
      result.current.reset()
    })

    expect(result.current.loading).toBe(false)
    expect(result.current.error).toBe(null)
    expect(result.current.data).toBe(null)
  })
})

describe('useDateUtils', () => {
  it('should format dates correctly', () => {
    const { result } = renderHook(() => useDateUtils())
    const testDate = new Date('2025-09-29')

    const formatted = result.current.formatDate(testDate, {
      weekday: 'short',
      day: 'numeric'
    })

    expect(formatted).toMatch(/lun|mar|mié|jue|vie|sáb|dom/)
  })

  it('should format time correctly', () => {
    const { result } = renderHook(() => useDateUtils())
    const testDateString = '2025-09-29T10:30:00.000Z'

    const formatted = result.current.formatTime(testDateString)

    expect(formatted).toMatch(/\d{1,2}:\d{2}/)
  })

  it('should identify today correctly', () => {
    const { result } = renderHook(() => useDateUtils())
    const today = new Date()
    const yesterday = new Date(today)
    yesterday.setDate(yesterday.getDate() - 1)

    expect(result.current.isToday(today)).toBe(true)
    expect(result.current.isToday(yesterday)).toBe(false)
  })

  it('should add days correctly', () => {
    const { result } = renderHook(() => useDateUtils())
    const startDate = new Date('2025-09-29') // Sunday Sept 29

    const futureDate = result.current.addDays(startDate, 3) // Add 3 days to get Wednesday Oct 1

    expect(futureDate.getDate()).toBe(1) // September 29 + 3 days = October 1
    expect(futureDate.getMonth()).toBe(9) // October (0-indexed)
  })

  it('should get Monday of week correctly', () => {
    const { result } = renderHook(() => useDateUtils())
    const wednesday = new Date('2025-10-01') // Wednesday

    const monday = result.current.getMondayOfWeek(wednesday)

    expect(monday.getDay()).toBe(1) // Monday
    expect(monday.getDate()).toBe(29) // September 29, 2025
  })

  it('should generate week days array', () => {
    const { result } = renderHook(() => useDateUtils())
    // Get the monday of the week that contains Sept 29 (Sunday)
    const sunday = new Date('2025-09-29') // This is a Sunday
    const monday = result.current.getMondayOfWeek(sunday) // Should be Sept 23

    const weekDays = result.current.getWeekDays(monday)

    expect(weekDays).toHaveLength(7)
    expect(weekDays[0].getDay()).toBe(1) // Monday
    expect(weekDays[6].getDay()).toBe(0) // Sunday
  })
})