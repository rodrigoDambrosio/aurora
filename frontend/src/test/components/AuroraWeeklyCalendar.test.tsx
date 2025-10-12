import { fireEvent, render, screen, waitFor } from '@testing-library/react'
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
import { formatMonthTitle } from '../../lib/utils'

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

    const expectedTitle = formatMonthTitle(new Date('2025-09-30T12:00:00.000Z'))

    await waitFor(() => {
      expect(screen.getByText(expectedTitle)).toBeInTheDocument()
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

  it('debería mostrar el botón de filtro', async () => {
    const mockOnToggleFilters = vi.fn()

    render(
      <AuroraWeeklyCalendar
        showFilters={false}
        onToggleFilters={mockOnToggleFilters}
        categories={[]}
        onCategoryChange={vi.fn()}
      />
    )

    await waitFor(() => {
      const filterButton = screen.getByLabelText('Filtrar por categoría')
      expect(filterButton).toBeInTheDocument()
    })
  })

  it('debería llamar onToggleFilters cuando se hace clic en el botón de filtro', async () => {
    const mockOnToggleFilters = vi.fn()

    render(
      <AuroraWeeklyCalendar
        showFilters={false}
        onToggleFilters={mockOnToggleFilters}
        categories={[]}
        onCategoryChange={vi.fn()}
      />
    )

    await waitFor(async () => {
      const filterButton = screen.getByLabelText('Filtrar por categoría')
      fireEvent.click(filterButton)
      expect(mockOnToggleFilters).toHaveBeenCalledTimes(1)
    })
  })

  it('debería mostrar CategoryFilter cuando showFilters es true', async () => {
    const mockCategories = [
      {
        id: '1',
        name: 'Trabajo',
        color: '#2b7fff',
        isSystemDefault: true,
        sortOrder: 1
      }
    ]

    render(
      <AuroraWeeklyCalendar
        showFilters={true}
        onToggleFilters={vi.fn()}
        categories={mockCategories}
        onCategoryChange={vi.fn()}
      />
    )

    await waitFor(() => {
      expect(screen.getByText('Todas')).toBeInTheDocument()
      expect(screen.getByText('Trabajo')).toBeInTheDocument()
    })
  })

  it('debería ocultar CategoryFilter cuando showFilters es false', async () => {
    const mockCategories = [
      {
        id: '1',
        name: 'Trabajo',
        color: '#2b7fff',
        isSystemDefault: true,
        sortOrder: 1
      }
    ]

    render(
      <AuroraWeeklyCalendar
        showFilters={false}
        onToggleFilters={vi.fn()}
        categories={mockCategories}
        onCategoryChange={vi.fn()}
      />
    )

    await waitFor(() => {
      expect(screen.queryByText('Todas')).not.toBeInTheDocument()
      expect(screen.queryByText('Trabajo')).not.toBeInTheDocument()
    })
  })

  it('debería marcar el botón de filtro como activo cuando showFilters es true', async () => {
    render(
      <AuroraWeeklyCalendar
        showFilters={true}
        onToggleFilters={vi.fn()}
        categories={[]}
        onCategoryChange={vi.fn()}
      />
    )

    await waitFor(() => {
      const filterButton = screen.getByLabelText('Filtrar por categoría')
      expect(filterButton).toHaveClass('is-active')
    })
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  describe('firstDayOfWeek preference', () => {
    it('debería iniciar la semana en lunes por defecto (firstDayOfWeek=1)', async () => {
      render(<AuroraWeeklyCalendar firstDayOfWeek={1} />)

      await waitFor(() => {
        // Verificar que la semana comience con lunes
        expect(mockGetWeeklyEvents).toHaveBeenCalledWith(
          expect.stringMatching(/2025-09-29/), // Lunes de la semana
          undefined,
          undefined
        )
      })
    })

    it('debería iniciar la semana en domingo cuando firstDayOfWeek=0', async () => {
      render(<AuroraWeeklyCalendar firstDayOfWeek={0} />)

      await waitFor(() => {
        // Verificar que la semana comience con domingo
        expect(mockGetWeeklyEvents).toHaveBeenCalledWith(
          expect.stringMatching(/2025-09-28/), // Domingo de la semana
          undefined,
          undefined
        )
      })
    })

    it('debería iniciar la semana en sábado cuando firstDayOfWeek=6', async () => {
      render(<AuroraWeeklyCalendar firstDayOfWeek={6} />)

      await waitFor(() => {
        // Verificar que la semana comience con sábado
        expect(mockGetWeeklyEvents).toHaveBeenCalledWith(
          expect.stringMatching(/2025-09-27/), // Sábado de la semana
          undefined,
          undefined
        )
      })
    })
  })
})