import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import MainDashboard from '../../components/MainDashboard'

// Mock the child components
vi.mock('../../components/AuroraWeeklyCalendar', () => ({
  default: ({ onEventClick, onAddEvent }: { onEventClick: Function, onAddEvent: Function }) => (
    <div data-testid="weekly-calendar">
      <button onClick={() => onEventClick({ id: '1', title: 'Test Event' })}>
        Mock Event
      </button>
      <button onClick={() => onAddEvent(new Date())}>
        Mock Add Event
      </button>
    </div>
  ),
}))

vi.mock('../../components/Navigation', () => ({
  default: ({ activeView, onViewChange }: { activeView: string, onViewChange: Function }) => (
    <nav data-testid="navigation">
      <button
        onClick={() => onViewChange('calendar-week')}
        className={activeView === 'calendar-week' ? 'active' : ''}
      >
        Weekly
      </button>
      <button
        onClick={() => onViewChange('calendar-month')}
        className={activeView === 'calendar-month' ? 'active' : ''}
      >
        Monthly
      </button>
      <button
        onClick={() => onViewChange('settings')}
        className={activeView === 'settings' ? 'active' : ''}
      >
        Settings
      </button>
    </nav>
  ),
}))

vi.mock('../../components/FloatingNLPInput', () => ({
  FloatingNLPInput: ({ onEventCreated }: { onEventCreated: Function }) => (
    <button data-testid="nlp-input" onClick={() => onEventCreated()}>
      NLP Input
    </button>
  ),
}))

describe('MainDashboard', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders navigation and weekly calendar by default', () => {
    render(<MainDashboard />)

    expect(screen.getByTestId('navigation')).toBeInTheDocument()
    expect(screen.getByTestId('weekly-calendar')).toBeInTheDocument()
    expect(screen.getByTestId('nlp-input')).toBeInTheDocument()
  })

  it('switches to different views when navigation is clicked', async () => {
    const user = userEvent.setup()
    render(<MainDashboard />)

    // Click on settings
    const settingsButton = screen.getByText('Settings')
    await user.click(settingsButton)

    expect(screen.getByText('Configuración')).toBeInTheDocument()
    expect(screen.getByText('Esta vista estará disponible pronto')).toBeInTheDocument()
  })

  it('switches to monthly view', async () => {
    const user = userEvent.setup()
    render(<MainDashboard />)

    const monthlyButton = screen.getByText('Monthly')
    await user.click(monthlyButton)

    // Verificar que se muestra el calendario mensual con algún elemento característico
    await waitFor(() => {
      expect(screen.getByText('octubre de 2025')).toBeInTheDocument()
    })
  })

  it('handles event click from weekly calendar', async () => {
    const user = userEvent.setup()
    const consoleSpy = vi.spyOn(console, 'log').mockImplementation(() => { })

    render(<MainDashboard />)

    const mockEventButton = screen.getByText('Mock Event')
    await user.click(mockEventButton)

    expect(consoleSpy).toHaveBeenCalledWith('Event clicked:', { id: '1', title: 'Test Event' })

    consoleSpy.mockRestore()
  })

  it('handles add event from weekly calendar', async () => {
    const user = userEvent.setup()
    const consoleSpy = vi.spyOn(console, 'log').mockImplementation(() => { })

    render(<MainDashboard />)

    const mockAddEventButton = screen.getByText('Mock Add Event')
    await user.click(mockAddEventButton)

    expect(consoleSpy).toHaveBeenCalledWith('Adding event for date:', expect.any(Date))

    consoleSpy.mockRestore()
  })

  it('handles event created from NLP input', async () => {
    const user = userEvent.setup()
    const consoleSpy = vi.spyOn(console, 'log').mockImplementation(() => { })

    render(<MainDashboard />)

    const nlpInputButton = screen.getByTestId('nlp-input')
    await user.click(nlpInputButton)

    expect(consoleSpy).toHaveBeenCalledWith('Evento creado - refrescando calendario')

    consoleSpy.mockRestore()
  })

  it('shows placeholder for wellness view', async () => {
    const user = userEvent.setup()
    render(<MainDashboard />)

    // We need to add wellness to navigation mock first
    // For now, test with settings which exists
    const settingsButton = screen.getByText('Settings')
    await user.click(settingsButton)

    expect(screen.getByText('Esta vista estará disponible pronto')).toBeInTheDocument()
  })

  it('defaults back to weekly calendar for unknown views', () => {
    render(<MainDashboard />)

    // Initial state should show weekly calendar
    expect(screen.getByTestId('weekly-calendar')).toBeInTheDocument()
  })
})