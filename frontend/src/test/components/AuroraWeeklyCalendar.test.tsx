import { render, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

// Use vi.hoisted to ensure mock functions are available
const mockGetWeeklyEvents = vi.hoisted(() => vi.fn())
const mockGetEventCategories = vi.hoisted(() => vi.fn())

// Mock the apiService
vi.mock('../../services/apiService', () => ({
  apiService: {
    getWeeklyEvents: mockGetWeeklyEvents,
    getEventCategories: mockGetEventCategories
  }
}))

// Import after mocking
import AuroraWeeklyCalendar from '../../components/AuroraWeeklyCalendar'

describe('AuroraWeeklyCalendar', () => {
  beforeEach(() => {
    vi.clearAllMocks()

    // Mock current date to Sept 30, 2025 so the calendar shows September
    vi.setSystemTime(new Date('2025-09-30T12:00:00.000Z'))

    // Default mock implementations
    mockGetWeeklyEvents.mockResolvedValue({
      weekStart: '2025-09-29T00:00:00.000Z',
      weekEnd: '2025-10-05T23:59:59.999Z',
      events: [
        {
          id: '1',
          title: 'Test Event',
          description: 'Test Description',
          startDate: '2025-09-30T10:00:00.000Z',
          endDate: '2025-09-30T11:00:00.000Z',
          isAllDay: false,
          isRecurring: false,
          eventCategory: {
            id: '1',
            name: 'Personal',
            color: '#4f46e5',
            isSystemDefault: true,
            sortOrder: 1
          }
        }
      ],
      categories: [],
      userId: 'test-user'
    })

    mockGetEventCategories.mockResolvedValue([
      {
        id: '1',
        name: 'Personal',
        color: '#4f46e5',
        isSystemDefault: true,
        sortOrder: 1
      }
    ])
  })

  it('renders calendar with loading state initially', () => {
    render(<AuroraWeeklyCalendar />)

    expect(screen.getByText('Cargando calendario...')).toBeInTheDocument()
  })

  it('renders calendar after loading', async () => {
    render(<AuroraWeeklyCalendar />)

    await waitFor(() => {
      expect(screen.getByText('septiembre 2025')).toBeInTheDocument()
    })
  })

  it('displays week header with day names', async () => {
    render(<AuroraWeeklyCalendar />)

    await waitFor(() => {
      expect(screen.getByText('lun')).toBeInTheDocument()
      expect(screen.getByText('mar')).toBeInTheDocument()
      expect(screen.getByText('mié')).toBeInTheDocument()
      expect(screen.getByText('jue')).toBeInTheDocument()
      expect(screen.getByText('vie')).toBeInTheDocument()
      expect(screen.getByText('sáb')).toBeInTheDocument()
      expect(screen.getByText('dom')).toBeInTheDocument()
    })
  })

  it('displays today button', async () => {
    render(<AuroraWeeklyCalendar />)

    await waitFor(() => {
      expect(screen.getByText('Hoy')).toBeInTheDocument()
    })
  })

  it('displays events summary', async () => {
    render(<AuroraWeeklyCalendar />)

    await waitFor(() => {
      expect(screen.getByText(/eventos esta semana/)).toBeInTheDocument()
    })
  })

  it('displays week badge', async () => {
    render(<AuroraWeeklyCalendar />)

    await waitFor(() => {
      expect(screen.getByText(/Semana/)).toBeInTheDocument()
    })
  })

  it('displays add event placeholders', async () => {
    mockGetWeeklyEvents.mockResolvedValueOnce({
      weekStart: '2025-09-29T00:00:00.000Z',
      weekEnd: '2025-10-05T23:59:59.999Z',
      events: [], // Empty events
      categories: [],
      userId: 'test-user'
    })

    render(<AuroraWeeklyCalendar />)

    await waitFor(() => {
      const addEventElements = screen.getAllByText('Agregar evento')
      expect(addEventElements).toHaveLength(7) // One for each day
    })
  })

  afterEach(() => {
    vi.useRealTimers()
  })
})