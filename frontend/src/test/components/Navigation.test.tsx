import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'

// Use vi.hoisted to ensure mock functions are available
const mockUpdateUserPreferences = vi.hoisted(() => vi.fn())

// Mock apiService
vi.mock('../../services/apiService', () => ({
  apiService: {
    updateUserPreferences: mockUpdateUserPreferences
  }
}))

// Import after mocking
import Navigation from '../../components/Navigation'
import { ThemeProvider } from '../../context/ThemeContext'

describe('Navigation - Theme Toggle with Backend Sync (PLAN-130)', () => {
  const mockOnViewChange = vi.fn()

  beforeEach(() => {
    vi.clearAllMocks()
    localStorage.clear()

    // Default mock for updateUserPreferences (returns the input as result)
    mockUpdateUserPreferences.mockImplementation((prefs) => Promise.resolve(prefs))
  })

  it('debería renderizar el botón de tema', () => {
    render(
      <ThemeProvider>
        <Navigation activeView="calendar-week" onViewChange={mockOnViewChange} />
      </ThemeProvider>
    )

    const themeButton = screen.getByText('Modo Oscuro')
    expect(themeButton).toBeInTheDocument()
  })

  it('debería cambiar el tema y guardarlo en el backend al hacer clic', async () => {
    render(
      <ThemeProvider>
        <Navigation activeView="calendar-week" onViewChange={mockOnViewChange} />
      </ThemeProvider>
    )

    // Click theme toggle button
    const themeButton = screen.getByText('Modo Oscuro')
    fireEvent.click(themeButton)

    // Wait for theme to change in UI
    await waitFor(() => {
      expect(screen.getByText('Modo Claro')).toBeInTheDocument()
    })

    // Verify theme was saved to backend (optimized: single call)
    await waitFor(() => {
      expect(mockUpdateUserPreferences).toHaveBeenCalledTimes(1)
      expect(mockUpdateUserPreferences).toHaveBeenCalledWith({ theme: 'dark' })
    })

    // Verify localStorage was updated
    expect(localStorage.getItem('aurora-theme-preference')).toBe('dark')
  })

  it('debería alternar entre light y dark correctamente', async () => {
    render(
      <ThemeProvider>
        <Navigation activeView="calendar-week" onViewChange={mockOnViewChange} />
      </ThemeProvider>
    )

    // Initial: light mode
    expect(screen.getByText('Modo Oscuro')).toBeInTheDocument()

    // Click to dark
    fireEvent.click(screen.getByText('Modo Oscuro'))
    await waitFor(() => {
      expect(screen.getByText('Modo Claro')).toBeInTheDocument()
    })

    // Click to light again
    fireEvent.click(screen.getByText('Modo Claro'))
    await waitFor(() => {
      expect(screen.getByText('Modo Oscuro')).toBeInTheDocument()
    })

    // Should have called update twice (optimized: single call each time)
    expect(mockUpdateUserPreferences).toHaveBeenCalledTimes(2)
    expect(mockUpdateUserPreferences).toHaveBeenNthCalledWith(1, { theme: 'dark' })
    expect(mockUpdateUserPreferences).toHaveBeenNthCalledWith(2, { theme: 'light' })
  })

  it('debería manejar errores al guardar en backend sin revertir el tema', async () => {
    const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => { })
    mockUpdateUserPreferences.mockRejectedValue(new Error('Network error'))

    render(
      <ThemeProvider>
        <Navigation activeView="calendar-week" onViewChange={mockOnViewChange} />
      </ThemeProvider>
    )

    // Click theme toggle
    fireEvent.click(screen.getByText('Modo Oscuro'))

    // Theme should still change in UI
    await waitFor(() => {
      expect(screen.getByText('Modo Claro')).toBeInTheDocument()
    })

    // Error should be logged
    await waitFor(() => {
      expect(consoleSpy).toHaveBeenCalledWith(
        'Failed to save theme preference to backend:',
        expect.any(Error)
      )
    })

    // localStorage should still be updated
    expect(localStorage.getItem('aurora-theme-preference')).toBe('dark')

    consoleSpy.mockRestore()
  })

  it('debería prevenir múltiples clics mientras se guarda', async () => {
    // Make updateUserPreferences slow
    mockUpdateUserPreferences.mockImplementation(
      () => new Promise(resolve => setTimeout(() => resolve({ theme: 'dark' }), 100))
    )

    render(
      <ThemeProvider>
        <Navigation activeView="calendar-week" onViewChange={mockOnViewChange} />
      </ThemeProvider>
    )

    const themeButton = screen.getByText('Modo Oscuro')

    // Click multiple times quickly
    fireEvent.click(themeButton)
    fireEvent.click(themeButton)
    fireEvent.click(themeButton)

    // Should only call once
    await waitFor(() => {
      expect(mockUpdateUserPreferences).toHaveBeenCalledTimes(1)
    })

    // Button should be disabled during save
    expect(themeButton.closest('button')).toBeDisabled()

    // Wait for save to complete
    await waitFor(() => {
      expect(themeButton.closest('button')).not.toBeDisabled()
    }, { timeout: 200 })
  })

  it('debería sincronizar tema con localStorage inmediatamente', async () => {
    render(
      <ThemeProvider>
        <Navigation activeView="calendar-week" onViewChange={mockOnViewChange} />
      </ThemeProvider>
    )

    // Initial theme in localStorage
    expect(localStorage.getItem('aurora-theme-preference')).toBe('light')

    // Click theme toggle
    fireEvent.click(screen.getByText('Modo Oscuro'))

    // localStorage should update immediately (synchronously via ThemeContext)
    await waitFor(() => {
      expect(localStorage.getItem('aurora-theme-preference')).toBe('dark')
    })

    // Backend save is async but should complete
    await waitFor(() => {
      expect(mockUpdateUserPreferences).toHaveBeenCalledWith(
        expect.objectContaining({ theme: 'dark' })
      )
    })
  })
})
