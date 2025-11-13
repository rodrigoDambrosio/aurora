import { CalendarDays, Eye, EyeOff, Lock, Mail, Moon, Sparkles, Sun, User } from 'lucide-react';
import type { FormEvent } from 'react';
import { useMemo, useState } from 'react';
import { useTheme } from '../../context/useTheme';
import type { AuthResponseDto } from '../../services/apiService';
import { ApiError, apiService } from '../../services/apiService';
import './AuthScreen.css';

interface AuthScreenProps {
  onAuthSuccess?: () => void;
  /**
   * Custom simulation handler used mainly for testing scenarios.
   * When provided, it overrides the default timeout-based simulation.
   */
  simulateAuth?: () => Promise<AuthResponseDto | void>;
}

type AuthMode = 'login' | 'register';

const INITIAL_FORM = {
  name: '',
  email: '',
  password: '',
  confirmPassword: ''
};

const MODE_COPY: Record<AuthMode, { headline: string; button: string; subtle: string }> = {
  login: {
    headline: 'Bienvenido de vuelta',
    button: 'Iniciar sesión',
    subtle: '¿Olvidaste tu contraseña?'
  },
  register: {
    headline: 'Crear tu cuenta',
    button: 'Registrarme',
    subtle: '¿Ya tienes cuenta?'
  }
};

function AuthScreen({ onAuthSuccess, simulateAuth }: AuthScreenProps) {
  const [mode, setMode] = useState<AuthMode>('login');
  const [formValues, setFormValues] = useState(INITIAL_FORM);
  const [showPassword, setShowPassword] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const { theme, toggleTheme } = useTheme();

  const isRegister = useMemo(() => mode === 'register', [mode]);

  const handleModeChange = (nextMode: AuthMode) => {
    if (nextMode === mode) {
      return;
    }

    setMode(nextMode);
    setFormValues(INITIAL_FORM);
    setErrorMessage(null);
    setShowPassword(false);
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setErrorMessage(null);

    if (!formValues.email.trim() || !formValues.password.trim()) {
      setErrorMessage('Por favor, completa todos los campos requeridos.');
      return;
    }

    if (isRegister && !formValues.name.trim()) {
      setErrorMessage('Tu nombre es obligatorio para crear la cuenta.');
      return;
    }

    if (isRegister && formValues.password !== formValues.confirmPassword) {
      setErrorMessage('Las contraseñas no coinciden.');
      return;
    }

    try {
      setIsSubmitting(true);

      const performAuth = simulateAuth ?? (async (): Promise<AuthResponseDto> => {
        if (isRegister) {
          return apiService.registerUser({
            name: formValues.name.trim(),
            email: formValues.email.trim(),
            password: formValues.password
          });
        }

        return apiService.loginUser({
          email: formValues.email.trim(),
          password: formValues.password
        });
      });

      const authResult = await performAuth();

      if (authResult && typeof window !== 'undefined') {
        window.localStorage.setItem('auroraAccessToken', authResult.accessToken);
        window.localStorage.setItem('auroraAccessTokenExpiry', authResult.expiresAtUtc);
        window.localStorage.setItem('auroraUser', JSON.stringify(authResult.user));
      }

      onAuthSuccess?.();
      setFormValues(INITIAL_FORM);
    } catch (error) {
      console.error('Authentication error', error);
      if (error instanceof ApiError) {
        setErrorMessage(error.message || 'Ocurrió un error inesperado.');
      } else if (error instanceof Error) {
        setErrorMessage(error.message);
      } else {
        setErrorMessage('No logramos completar la solicitud. Inténtalo nuevamente.');
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  const headlineCopy = MODE_COPY[mode];

  return (
    <div className="auth-screen">
      <div className="auth-theme-toggle">
        <button
          type="button"
          className="auth-theme-button"
          onClick={toggleTheme}
          aria-label={theme === 'dark' ? 'Cambiar a modo claro' : 'Cambiar a modo oscuro'}
        >
          {theme === 'dark' ? <Sun size={16} aria-hidden="true" /> : <Moon size={16} aria-hidden="true" />}
          <span>{theme === 'dark' ? 'Modo claro' : 'Modo oscuro'}</span>
        </button>
      </div>
      <div className="auth-gradient" aria-hidden="true" />

      <div className="auth-card" role="main">
        <header className="auth-header">
          <div className="auth-logo" aria-hidden="true">
            <CalendarDays size={28} />
          </div>
          <div className="auth-brand">
            <h1>Aurora</h1>
            <p>Tu planificador inteligente</p>
          </div>
        </header>

        <nav className="auth-tabs" aria-label="Selector de modo">
          <button
            type="button"
            className={mode === 'login' ? 'auth-tab is-active' : 'auth-tab'}
            onClick={() => handleModeChange('login')}
          >
            Iniciar Sesión
          </button>
          <button
            type="button"
            className={mode === 'register' ? 'auth-tab is-active' : 'auth-tab'}
            onClick={() => handleModeChange('register')}
          >
            Registro
          </button>
        </nav>

        <div className="auth-copy">
          <h2>{headlineCopy.headline}</h2>
        </div>

        <form className="auth-form" onSubmit={handleSubmit} noValidate>
          {isRegister && (
            <label className="auth-field">
              <span>Nombre</span>
              <div className="auth-input-wrapper">
                <User size={18} aria-hidden="true" />
                <input
                  type="text"
                  name="name"
                  autoComplete="name"
                  placeholder="Tu nombre"
                  value={formValues.name}
                  onChange={(event) => setFormValues((prev) => ({
                    ...prev,
                    name: event.target.value
                  }))}
                />
              </div>
            </label>
          )}

          <label className="auth-field">
            <span>Email</span>
            <div className="auth-input-wrapper">
              <Mail size={18} aria-hidden="true" />
              <input
                type="email"
                name="email"
                autoComplete="email"
                placeholder="tu@email.com"
                value={formValues.email}
                onChange={(event) => setFormValues((prev) => ({
                  ...prev,
                  email: event.target.value
                }))}
              />
            </div>
          </label>

          <label className="auth-field">
            <span>Contraseña</span>
            <div className="auth-input-wrapper">
              <Lock size={18} aria-hidden="true" />
              <input
                type={showPassword ? 'text' : 'password'}
                name="password"
                autoComplete={isRegister ? 'new-password' : 'current-password'}
                placeholder="••••••••"
                value={formValues.password}
                onChange={(event) => setFormValues((prev) => ({
                  ...prev,
                  password: event.target.value
                }))}
              />
              <button
                type="button"
                className="auth-visibility"
                aria-label={showPassword ? 'Ocultar contraseña' : 'Mostrar contraseña'}
                onClick={() => setShowPassword((prev) => !prev)}
              >
                {showPassword ? <EyeOff size={18} /> : <Eye size={18} />}
              </button>
            </div>
          </label>

          {isRegister && (
            <label className="auth-field">
              <span>Confirmar contraseña</span>
              <div className="auth-input-wrapper">
                <Lock size={18} aria-hidden="true" />
                <input
                  type={showPassword ? 'text' : 'password'}
                  name="confirmPassword"
                  autoComplete="new-password"
                  placeholder="••••••••"
                  value={formValues.confirmPassword}
                  onChange={(event) => setFormValues((prev) => ({
                    ...prev,
                    confirmPassword: event.target.value
                  }))}
                />
              </div>
            </label>
          )}

          {errorMessage && <p className="auth-error" role="alert">{errorMessage}</p>}

          <button className="auth-submit" type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Procesando…' : headlineCopy.button}
          </button>
        </form>

        <div className="auth-footer">
          <button
            type="button"
            className="auth-link"
            onClick={() => handleModeChange(isRegister ? 'login' : 'register')}
          >
            {headlineCopy.subtle}
          </button>
          <div className="auth-extra">
            <p>¿Qué hace especial a Aurora?</p>
            <div className="auth-extra-features">
              <span><Sparkles size={16} aria-hidden="true" /> IA Conversacional</span>
              <span><CalendarDays size={16} aria-hidden="true" /> Análisis Inteligentes</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

export default AuthScreen;
