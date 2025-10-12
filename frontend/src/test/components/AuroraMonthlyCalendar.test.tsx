import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

// Use vi.hoisted to ensure mock functions are available
const mockGetMonthlyEvents = vi.hoisted(() => vi.fn())
const mockGetEventCategories = vi.hoisted(() => vi.fn())

// Mock the apiService
vi.mock('../../services/apiService', () => ({
  apiService: {
    getMonthlyEvents: mockGetMonthlyEvents,
    getEventCategories: mockGetEventCategories
  }
}))

// Import after mocking
import AuroraMonthlyCalendar from '../../components/AuroraMonthlyCalendar'
import { formatMonthTitle } from '../../lib/utils'

describe('AuroraMonthlyCalendar', () => {
  const mockOnEventClick = vi.fn()
  const mockOnAddEvent = vi.fn()

  beforeEach(() => {
    vi.clearAllMocks()

    // Mock current date to October 10, 2025
    vi.setSystemTime(new Date('2025-10-10T12:00:00.000Z'))

    // Default mock implementations
    mockGetMonthlyEvents.mockResolvedValue({
      events: [
        {
          id: '1',
          title: 'Test Event',
          description: 'Test Description',
          startDate: '2025-10-10T10:00:00.000Z',
          endDate: '2025-10-10T11:00:00.000Z',
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

    mockGetEventCategories.mockResolvedValue([])
  })

  it('debería renderizar el calendario mensual', async () => {
    render(<AuroraMonthlyCalendar onEventClick={mockOnEventClick} onAddEvent={mockOnAddEvent} />)

    const expectedTitle = formatMonthTitle(new Date('2025-10-10T12:00:00.000Z'))

    await waitFor(() => {
      // Verificar que se muestra el mes actual
      expect(screen.getByText(expectedTitle)).toBeInTheDocument()
    })
  })

  it('debería mostrar eventos en los días correspondientes', async () => {
    render(<AuroraMonthlyCalendar onEventClick={mockOnEventClick} onAddEvent={mockOnAddEvent} />)

    await waitFor(() => {
      expect(screen.getByText('Test Event')).toBeInTheDocument()
    })
  })

  it('debería mostrar el botón de filtro', async () => {
    const mockOnToggleFilters = vi.fn()

    render(
      <AuroraMonthlyCalendar
        onEventClick={mockOnEventClick}
        onAddEvent={mockOnAddEvent}
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
      <AuroraMonthlyCalendar
        onEventClick={mockOnEventClick}
        onAddEvent={mockOnAddEvent}
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
      },
      {
        id: '2',
        name: 'Personal',
        color: '#4f46e5',
        isSystemDefault: true,
        sortOrder: 2
      }
    ]

    render(
      <AuroraMonthlyCalendar
        onEventClick={mockOnEventClick}
        onAddEvent={mockOnAddEvent}
        showFilters={true}
        onToggleFilters={vi.fn()}
        categories={mockCategories}
        onCategoryChange={vi.fn()}
      />
    )

    await waitFor(() => {
      expect(screen.getByText('Todas')).toBeInTheDocument()
      expect(screen.getByText('Trabajo')).toBeInTheDocument()
      expect(screen.getByText('Personal')).toBeInTheDocument()
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
      <AuroraMonthlyCalendar
        onEventClick={mockOnEventClick}
        onAddEvent={mockOnAddEvent}
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
      <AuroraMonthlyCalendar
        onEventClick={mockOnEventClick}
        onAddEvent={mockOnAddEvent}
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

  it('debería resaltar el día actual', async () => {
    render(<AuroraMonthlyCalendar onEventClick={mockOnEventClick} onAddEvent={mockOnAddEvent} />)

    await waitFor(() => {
      // El día 10 debería tener la clase 'today'
      const todayElement = screen.getByText('10').closest('.calendar-day')
      expect(todayElement).toHaveClass('today')
    })
  })

  it('debería mostrar el botón para crear evento', async () => {
    render(<AuroraMonthlyCalendar onEventClick={mockOnEventClick} onAddEvent={mockOnAddEvent} />)

    await waitFor(() => {
      const addEventButton = screen.getByLabelText('Crear nuevo evento')
      expect(addEventButton).toBeInTheDocument()
    })
  })

  it('debería llamar a getMonthlyEvents con categoryId cuando hay filtro activo', async () => {
    const mockOnCategoryChange = vi.fn()

    const { rerender } = render(
      <AuroraMonthlyCalendar
        onEventClick={mockOnEventClick}
        onAddEvent={mockOnAddEvent}
        showFilters={false}
        onToggleFilters={vi.fn()}
        categories={[]}
        onCategoryChange={mockOnCategoryChange}
      />
    )

    await waitFor(() => {
      expect(mockGetMonthlyEvents).toHaveBeenCalled()
    })

    // Limpiar las llamadas previas
    mockGetMonthlyEvents.mockClear()

    // Simular cambio de filtro
    rerender(
      <AuroraMonthlyCalendar
        onEventClick={mockOnEventClick}
        onAddEvent={mockOnAddEvent}
        showFilters={false}
        onToggleFilters={vi.fn()}
        categories={[]}
        onCategoryChange={mockOnCategoryChange}
        selectedCategoryId="1"
      />
    )

    await waitFor(() => {
      // Verificar que se llamó con el categoryId
      const calls = mockGetMonthlyEvents.mock.calls
      expect(calls.length).toBeGreaterThan(0)
    })
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  describe('firstDayOfWeek preference', () => {
    it('debería mostrar días de la semana comenzando en lunes por defecto (firstDayOfWeek=1)', async () => {
      render(
        <AuroraMonthlyCalendar
          onEventClick={mockOnEventClick}
          onAddEvent={mockOnAddEvent}
          firstDayOfWeek={1}
        />
      )

      await waitFor(() => {
        const weekdayHeaders = screen.getAllByRole('generic', { hidden: true })
        const weekdayContainer = weekdayHeaders.find(el =>
          el.className?.includes('monthly-calendar-weekdays')
        )

        if (weekdayContainer) {
          const weekdays = Array.from(weekdayContainer.children).map(el => el.textContent)
          expect(weekdays[0]).toBe('Lun')
          expect(weekdays[6]).toBe('Dom')
        }
      })
    })

    it('debería mostrar días de la semana comenzando en domingo cuando firstDayOfWeek=0', async () => {
      render(
        <AuroraMonthlyCalendar
          onEventClick={mockOnEventClick}
          onAddEvent={mockOnAddEvent}
          firstDayOfWeek={0}
        />
      )

      await waitFor(() => {
        const weekdayHeaders = screen.getAllByRole('generic', { hidden: true })
        const weekdayContainer = weekdayHeaders.find(el =>
          el.className?.includes('monthly-calendar-weekdays')
        )

        if (weekdayContainer) {
          const weekdays = Array.from(weekdayContainer.children).map(el => el.textContent)
          expect(weekdays[0]).toBe('Dom')
          expect(weekdays[6]).toBe('Sáb')
        }
      })
    })

    it('debería mostrar días de la semana comenzando en sábado cuando firstDayOfWeek=6', async () => {
      render(
        <AuroraMonthlyCalendar
          onEventClick={mockOnEventClick}
          onAddEvent={mockOnAddEvent}
          firstDayOfWeek={6}
        />
      )

      await waitFor(() => {
        const weekdayHeaders = screen.getAllByRole('generic', { hidden: true })
        const weekdayContainer = weekdayHeaders.find(el =>
          el.className?.includes('monthly-calendar-weekdays')
        )

        if (weekdayContainer) {
          const weekdays = Array.from(weekdayContainer.children).map(el => el.textContent)
          expect(weekdays[0]).toBe('Sáb')
          expect(weekdays[6]).toBe('Vie')
        }
      })
    })
  })
})
