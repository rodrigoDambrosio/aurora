import { useEffect, useMemo, useState } from 'react';
import './App.css';
import ApiTest from './components/ApiTest';
import AuthScreen from './components/Auth/AuthScreen';
import MainDashboard from './components/MainDashboard';
import { useTheme } from './context/ThemeContext';
import { apiService } from './services/apiService';

/**
 * Main Application Component
 * 
 * Aurora Personal Planner - Mobile-First Weekly Calendar
 */
function App() {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [isCheckingAuth, setIsCheckingAuth] = useState(true);
  const { setTheme } = useTheme();

  // Toggle between dashboard view and API test (for development)
  const showApiTest = import.meta.env.DEV && new URLSearchParams(window.location.search).has('test');
  const shouldShowAuthScreen = useMemo(() => !isAuthenticated && !showApiTest, [isAuthenticated, showApiTest]);

  // Check for existing session on mount
  useEffect(() => {
    const checkExistingSession = () => {
      const token = window.localStorage.getItem('auroraAccessToken');
      const expiryStr = window.localStorage.getItem('auroraAccessTokenExpiry');

      if (token && expiryStr) {
        const expiry = new Date(expiryStr);
        const now = new Date();

        if (expiry > now) {
          // Token exists and is still valid
          console.log('Restored session from localStorage');
          setIsAuthenticated(true);
        } else {
          // Token expired, clear it
          console.log('Token expired, clearing session');
          window.localStorage.removeItem('auroraAccessToken');
          window.localStorage.removeItem('auroraAccessTokenExpiry');
          window.localStorage.removeItem('auroraUser');
        }
      }
      setIsCheckingAuth(false);
    };

    checkExistingSession();
  }, []);

  // Load user preferences (including theme) after authentication
  useEffect(() => {
    const loadUserPreferences = async () => {
      try {
        const preferences = await apiService.getUserPreferences();

        // Apply theme from backend if it exists
        if (preferences.theme && (preferences.theme === 'light' || preferences.theme === 'dark')) {
          setTheme(preferences.theme);
          console.log('Applied theme from user preferences:', preferences.theme);
        }
      } catch (error) {
        console.error('Error loading user preferences:', error);
        // Keep current theme (from localStorage or system) if there's an error
      }
    };

    if (isAuthenticated) {
      loadUserPreferences();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isAuthenticated]); // setTheme is stable, no need to include it

  return (
    <div className="App">
      {isCheckingAuth ? (
        // Show loading state while checking for existing session
        <div className="loading-container">
          <div className="loading-spinner"></div>
        </div>
      ) : showApiTest ? (
        <div className="api-test-container">
          <header className="test-header">
            <h1>🧪 Aurora API Test</h1>
            <p>
              <a href="/">← Volver al Dashboard</a> |
              <a href="http://localhost:5291/swagger" target="_blank" rel="noopener noreferrer">
                Ver API Docs
              </a>
            </p>
          </header>
          <main className="test-main">
            <ApiTest />
          </main>
        </div>
      ) : shouldShowAuthScreen ? (
        <AuthScreen onAuthSuccess={() => setIsAuthenticated(true)} />
      ) : (
        <div className="aurora-app">
          <MainDashboard />
          {import.meta.env.DEV && (
            <div className="dev-overlay">
              <a href="?test" className="dev-link">🧪 API Test</a>
            </div>
          )}
        </div>
      )}
    </div>
  )
}

export default App
