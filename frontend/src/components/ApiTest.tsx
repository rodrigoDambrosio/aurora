import React, { useState, useEffect } from 'react';
import { apiService } from '../services/apiService';
import type { HealthResponse, TestResponse } from '../services/apiService';

const ApiTest: React.FC = () => {
  const [healthStatus, setHealthStatus] = useState<HealthResponse | null>(null);
  const [testData, setTestData] = useState<TestResponse | null>(null);
  const [loading, setLoading] = useState<boolean>(false);
  const [error, setError] = useState<string>('');

  const testHealthEndpoint = async () => {
    setLoading(true);
    setError('');
    try {
      const health = await apiService.checkHealth();
      setHealthStatus(health);
    } catch (err) {
      setError(`Error probando endpoint de salud: ${err instanceof Error ? err.message : 'Error desconocido'}`);
    } finally {
      setLoading(false);
    }
  };

  const testDataEndpoint = async () => {
    setLoading(true);
    setError('');
    try {
      const data = await apiService.getTestData();
      setTestData(data);
    } catch (err) {
      setError(`Error obteniendo datos de prueba: ${err instanceof Error ? err.message : 'Error desconocido'}`);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    testHealthEndpoint();
  }, []);

  const containerStyle: React.CSSProperties = {
    maxWidth: '1200px',
    margin: '0 auto',
    padding: '20px',
    fontFamily: '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif',
    backgroundColor: '#f8fafc',
    minHeight: '100vh',
    lineHeight: '1.6'
  };

  const headerStyle: React.CSSProperties = {
    textAlign: 'center',
    color: '#1e293b',
    fontSize: '2.5rem',
    fontWeight: 'bold',
    marginBottom: '2rem',
    textShadow: '0 2px 4px rgba(0,0,0,0.1)'
  };

  const cardStyle: React.CSSProperties = {
    backgroundColor: '#ffffff',
    borderRadius: '12px',
    padding: '24px',
    marginBottom: '24px',
    boxShadow: '0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06)',
    border: '1px solid #e2e8f0'
  };

  const cardTitleStyle: React.CSSProperties = {
    fontSize: '1.5rem',
    fontWeight: '600',
    color: '#334155',
    marginBottom: '16px',
    display: 'flex',
    alignItems: 'center',
    gap: '8px'
  };

  const buttonStyle: React.CSSProperties = {
    padding: '12px 24px',
    borderRadius: '8px',
    border: 'none',
    fontWeight: '500',
    fontSize: '1rem',
    cursor: 'pointer',
    transition: 'all 0.2s ease',
    marginRight: '12px',
    marginBottom: '16px',
    boxShadow: '0 2px 4px rgba(0,0,0,0.1)'
  };

  const primaryButtonStyle: React.CSSProperties = {
    ...buttonStyle,
    backgroundColor: loading ? '#94a3b8' : '#10b981',
    color: 'white',
    cursor: loading ? 'not-allowed' : 'pointer'
  };

  const secondaryButtonStyle: React.CSSProperties = {
    ...buttonStyle,
    backgroundColor: loading ? '#94a3b8' : '#3b82f6',
    color: 'white',
    cursor: loading ? 'not-allowed' : 'pointer'
  };

  const successCardStyle: React.CSSProperties = {
    backgroundColor: '#f0fdf4',
    border: '2px solid #22c55e',
    borderRadius: '12px',
    padding: '20px',
    marginTop: '16px'
  };

  const errorCardStyle: React.CSSProperties = {
    backgroundColor: '#fef2f2',
    border: '2px solid #ef4444',
    borderRadius: '12px',
    padding: '20px',
    color: '#dc2626',
    marginBottom: '24px',
    fontSize: '1.1rem'
  };

  const dataCardStyle: React.CSSProperties = {
    backgroundColor: '#eff6ff',
    border: '2px solid #3b82f6',
    borderRadius: '12px',
    padding: '20px',
    marginTop: '16px'
  };

  const infoCardStyle: React.CSSProperties = {
    backgroundColor: '#f1f5f9',
    border: '2px solid #64748b',
    borderRadius: '12px',
    padding: '20px',
    marginTop: '24px'
  };

  const listStyle: React.CSSProperties = {
    listStyle: 'none',
    padding: '0'
  };

  const listItemStyle: React.CSSProperties = {
    backgroundColor: '#ffffff',
    padding: '12px 16px',
    margin: '8px 0',
    borderRadius: '8px',
    border: '1px solid #e2e8f0',
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center'
  };

  const statusDotStyle = (isOnline: boolean): React.CSSProperties => ({
    width: '12px',
    height: '12px',
    borderRadius: '50%',
    backgroundColor: isOnline ? '#22c55e' : '#ef4444',
    display: 'inline-block',
    marginRight: '8px',
    animation: isOnline ? 'pulse 2s infinite' : 'none'
  });

  return (
    <div style={containerStyle}>
      <style>
        {`
          @keyframes pulse {
            0%, 100% { opacity: 1; }
            50% { opacity: 0.5; }
          }
          
          .hover-button:hover {
            transform: translateY(-2px);
            box-shadow: 0 4px 8px rgba(0,0,0,0.15);
          }
        `}
      </style>

      <h1 style={headerStyle}>🧪 Aurora - Prueba de Conectividad</h1>
      
      {error && (
        <div style={errorCardStyle}>
          <div style={{ fontSize: '1.2rem', fontWeight: '600', marginBottom: '8px' }}>
            ❌ Error de Conexión
          </div>
          {error}
        </div>
      )}

      <div style={cardStyle}>
        <h2 style={cardTitleStyle}>
          <span style={statusDotStyle(healthStatus !== null)}></span>
          🔧 Estado del Backend
        </h2>
        <button 
          className="hover-button"
          onClick={testHealthEndpoint} 
          disabled={loading}
          style={primaryButtonStyle}
        >
          {loading ? '🔄 Verificando...' : '✅ Probar Estado del Servidor'}
        </button>

        {healthStatus && (
          <div style={successCardStyle}>
            <h3 style={{ color: '#16a34a', fontSize: '1.3rem', marginBottom: '16px' }}>
              ✅ ¡Backend Conectado!
            </h3>
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(250px, 1fr))', gap: '12px' }}>
              <div><strong>Estado:</strong> <span style={{ color: '#16a34a' }}>{healthStatus.status}</span></div>
              <div><strong>Versión:</strong> {healthStatus.version}</div>
              <div><strong>Mensaje:</strong> {healthStatus.message}</div>
              <div><strong>Timestamp:</strong> {new Date(healthStatus.timestamp).toLocaleString('es-ES')}</div>
            </div>
          </div>
        )}
      </div>

      <div style={cardStyle}>
        <h2 style={cardTitleStyle}>
          📊 Prueba de Datos
        </h2>
        <button 
          className="hover-button"
          onClick={testDataEndpoint} 
          disabled={loading}
          style={secondaryButtonStyle}
        >
          {loading ? '🔄 Cargando...' : '📥 Obtener Datos de Prueba'}
        </button>

        {testData && (
          <div style={dataCardStyle}>
            <h3 style={{ color: '#2563eb', fontSize: '1.3rem', marginBottom: '16px' }}>
              📊 Datos Recibidos Exitosamente
            </h3>
            <p style={{ fontSize: '1.1rem', marginBottom: '20px' }}>
              <strong>Mensaje del servidor:</strong> {testData.message}
            </p>
            <h4 style={{ color: '#1e40af', marginBottom: '12px' }}>📅 Eventos de Prueba:</h4>
            <ul style={listStyle}>
              {testData.data.map((item) => (
                <li key={item.id} style={listItemStyle}>
                  <div>
                    <strong style={{ color: '#1e40af' }}>{item.name}</strong>
                    <div style={{ fontSize: '0.9rem', color: '#64748b' }}>ID: {item.id}</div>
                  </div>
                  <div style={{ textAlign: 'right' }}>
                    <div style={{ color: '#059669', fontWeight: '500' }}>
                      📅 {new Date(item.date).toLocaleDateString('es-ES')}
                    </div>
                    <div style={{ fontSize: '0.8rem', color: '#64748b' }}>
                      {new Date(item.date).toLocaleTimeString('es-ES')}
                    </div>
                  </div>
                </li>
              ))}
            </ul>
          </div>
        )}
      </div>

      <div style={infoCardStyle}>
        <h3 style={{ color: '#475569', fontSize: '1.3rem', marginBottom: '16px' }}>
          ℹ️ Información del Sistema
        </h3>
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(250px, 1fr))', gap: '16px' }}>
          <div style={{ padding: '12px', backgroundColor: '#ffffff', borderRadius: '8px' }}>
            <strong>🌐 Frontend:</strong><br />
            React + TypeScript (Vite)<br />
            <span style={{ color: '#059669' }}>http://localhost:5173</span>
          </div>
          <div style={{ padding: '12px', backgroundColor: '#ffffff', borderRadius: '8px' }}>
            <strong>⚙️ Backend:</strong><br />
            ASP.NET Core (.NET 9)<br />
            <span style={{ color: '#059669' }}>http://localhost:5000/api</span>
          </div>
          <div style={{ padding: '12px', backgroundColor: '#ffffff', borderRadius: '8px' }}>
            <strong>🔗 Arquitectura:</strong><br />
            Clean Architecture<br />
            <span style={{ color: '#7c3aed' }}>Domain + Application + Infrastructure + API</span>
          </div>
          <div style={{ padding: '12px', backgroundColor: '#ffffff', borderRadius: '8px' }}>
            <strong>🛡️ Funcionalidades:</strong><br />
            CORS habilitado<br />
            <span style={{ color: '#dc2626' }}>FluentValidation + SQLite</span>
          </div>
        </div>
      </div>
    </div>
  );
};

export default ApiTest;