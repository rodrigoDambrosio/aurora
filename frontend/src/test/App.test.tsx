import { render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'

// Use vi.hoisted to ensure mock functions are available
const mockGetUserPreferences = vi.hoisted(() => vi.fn())

// Mock apiService
vi.mock('../services/apiService', () => ({
  apiService: {
    getUserPreferences: mockGetUserPreferences
  }
}))

// Import after mocking
import App from '../App'
import { ThemeProvider } from '../context/ThemeContext'

// Mock AuthScreen
vi.mock('../components/Auth/AuthScreen', () => ({
  default: ({ onAuthSuccess }: { onAuthSuccess: () => void }) => (
    <div data-testid="auth-screen">
      <button onClick={onAuthSuccess}>Login</button>
    </div>
  )
}))

// Mock MainDashboard
vi.mock('../components/MainDashboard', () => ({
  default: () => <div data-testid="main-dashboard">Dashboard</div>
}))

// Mock ApiTest
vi.mock('../components/ApiTest', () => ({
  default: () => <div data-testid="api-test">API Test</div>
}))

describe('App - Theme Synchronization (PLAN-130)', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    // Clear localStorage before each test
    localStorage.clear()
  })

  it('debería cargar el tema desde las preferencias del usuario después de autenticarse', async () => {
    // Mock user preferences with dark theme
    mockGetUserPreferences.mockResolvedValue({
      theme: 'dark',
      language: 'es',
      firstDayOfWeek: 1,
      defaultReminderMinutes: 15
    })

    render(
      <ThemeProvider>
        <App />
      </ThemeProvider>
    )

    // Initially should show auth screen
    expect(screen.getByTestId('auth-screen')).toBeInTheDocument()

    // Click login button to authenticate
    const loginButton = screen.getByText('Login')
    loginButton.click()

    // Wait for authentication and preferences to load
    await waitFor(() => {
      expect(mockGetUserPreferences).toHaveBeenCalledTimes(1)
    })

    // Check that dark theme was applied to document
    await waitFor(() => {
      expect(document.documentElement.getAttribute('data-theme')).toBe('dark')
    })

    // Check that theme was saved to localStorage
    expect(localStorage.getItem('aurora-theme-preference')).toBe('dark')
  })

  it('debería cargar el tema light desde las preferencias del usuario', async () => {
    // Mock user preferences with light theme
    mockGetUserPreferences.mockResolvedValue({
      theme: 'light',
      language: 'es',
      firstDayOfWeek: 1,
      defaultReminderMinutes: 15
    })

    render(
      <ThemeProvider>
        <App />
      </ThemeProvider>
    )

    // Click login button to authenticate
    const loginButton = screen.getByText('Login')
    loginButton.click()

    // Wait for authentication and preferences to load
    await waitFor(() => {
      expect(mockGetUserPreferences).toHaveBeenCalledTimes(1)
    })

    // Check that light theme was applied to document
    await waitFor(() => {
      expect(document.documentElement.getAttribute('data-theme')).toBe('light')
    })

    // Check that theme was saved to localStorage
    expect(localStorage.getItem('aurora-theme-preference')).toBe('light')
  })

  it('debería mantener el tema actual si las preferencias no incluyen tema', async () => {
    // Set initial theme in localStorage
    localStorage.setItem('aurora-theme-preference', 'dark')

    // Mock user preferences without theme
    mockGetUserPreferences.mockResolvedValue({
      language: 'es',
      firstDayOfWeek: 1,
      defaultReminderMinutes: 15
    })

    render(
      <ThemeProvider>
        <App />
      </ThemeProvider>
    )

    // Click login button to authenticate
    const loginButton = screen.getByText('Login')
    loginButton.click()

    // Wait for authentication and preferences to load
    await waitFor(() => {
      expect(mockGetUserPreferences).toHaveBeenCalledTimes(1)
    })

    // Theme should remain dark (from localStorage)
    expect(localStorage.getItem('aurora-theme-preference')).toBe('dark')
  })

  it('debería manejar errores al cargar preferencias sin afectar el tema actual', async () => {
    // Set initial theme in localStorage
    localStorage.setItem('aurora-theme-preference', 'light')

    // Mock error when loading preferences
    mockGetUserPreferences.mockRejectedValue(new Error('Network error'))

    const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => { })

    render(
      <ThemeProvider>
        <App />
      </ThemeProvider>
    )

    // Click login button to authenticate
    const loginButton = screen.getByText('Login')
    loginButton.click()

    // Wait for error to be logged
    await waitFor(() => {
      expect(consoleSpy).toHaveBeenCalledWith(
        'Error loading user preferences:',
        expect.any(Error)
      )
    })

    // Theme should remain light (from localStorage)
    expect(localStorage.getItem('aurora-theme-preference')).toBe('light')

    consoleSpy.mockRestore()
  })

  it('no debería cargar preferencias antes de autenticarse', async () => {
    render(
      <ThemeProvider>
        <App />
      </ThemeProvider>
    )

    // Auth screen should be visible
    expect(screen.getByTestId('auth-screen')).toBeInTheDocument()

    // Preferences should not be loaded yet
    expect(mockGetUserPreferences).not.toHaveBeenCalled()
  })

  it('debería sincronizar tema entre backend y localStorage', async () => {
    // Mock user preferences with dark theme
    mockGetUserPreferences.mockResolvedValue({
      theme: 'dark',
      language: 'es',
      firstDayOfWeek: 1
    })

    render(
      <ThemeProvider>
        <App />
      </ThemeProvider>
    )

    // Click login button
    const loginButton = screen.getByText('Login')
    loginButton.click()

    // Wait for theme to be synchronized
    await waitFor(() => {
      expect(document.documentElement.getAttribute('data-theme')).toBe('dark')
      expect(localStorage.getItem('aurora-theme-preference')).toBe('dark')
    })

    // Verify both document and localStorage are in sync
    const documentTheme = document.documentElement.getAttribute('data-theme')
    const localStorageTheme = localStorage.getItem('aurora-theme-preference')
    expect(documentTheme).toBe(localStorageTheme)
  })
})
