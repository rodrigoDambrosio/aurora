import './App.css';
import ApiTest from './components/ApiTest';
import MainDashboard from './components/MainDashboard';

/**
 * Main Application Component
 * 
 * Aurora Personal Planner - Mobile-First Weekly Calendar
 */
function App() {
  // Toggle between dashboard view and API test (for development)
  const showApiTest = import.meta.env.DEV && new URLSearchParams(window.location.search).has('test');

  return (
    <div className="App">
      {showApiTest ? (
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
