import { Calendar, Heart, LogOut, MessageCircle, Moon, Settings, Smile, Sparkles, Sun } from 'lucide-react';
import React, { useState } from 'react';
import { useTheme } from '../context/ThemeContext';
import { apiService } from '../services/apiService';
import './Navigation.css';

type View = 'calendar-week' | 'calendar-month' | 'mood-month' | 'wellness' | 'assistant' | 'settings';

interface NavigationProps {
  activeView?: string;
  onViewChange?: (view: string) => void;
}

const navigationItems = [
  {
    id: 'calendar-week' as View,
    label: 'Vista Semanal',
    icon: Calendar,
    description: 'Calendario semanal'
  },
  {
    id: 'calendar-month' as View,
    label: 'Vista Mensual',
    icon: Calendar,
    description: 'Calendario mensual'
  },
  {
    id: 'mood-month' as View,
    label: 'Humor mensual',
    icon: Smile,
    description: 'Seguimiento diario'
  },
  {
    id: 'wellness' as View,
    label: 'Bienestar',
    icon: Heart,
    description: 'Dashboard de bienestar'
  },
  {
    id: 'assistant' as View,
    label: 'Asistente IA',
    icon: MessageCircle,
    description: 'Chat conversacional'
  },
  {
    id: 'settings' as View,
    label: 'Configuración',
    icon: Settings,
    description: 'Perfil y preferencias'
  }
];

const Navigation: React.FC<NavigationProps> = ({
  activeView = 'calendar-week',
  onViewChange
}) => {
  const { theme, setTheme } = useTheme();
  const [isTogglingTheme, setIsTogglingTheme] = useState(false);
  const [isLoggingOut, setIsLoggingOut] = useState(false);

  const handleViewClick = (view: string) => {
    if (onViewChange) {
      onViewChange(view);
    }
  };

  const handleThemeToggle = async () => {
    if (isTogglingTheme) return; // Prevent multiple clicks

    try {
      setIsTogglingTheme(true);

      // Calculate new theme
      const newTheme = theme === 'dark' ? 'light' : 'dark';

      // Apply theme immediately (synchronous, no flicker)
      setTheme(newTheme);

      // Save to backend asynchronously (in background, single call)
      try {
        await apiService.updateUserPreferences({ theme: newTheme });
        console.log('Theme preference saved to backend:', newTheme);
      } catch (error) {
        console.error('Failed to save theme preference to backend:', error);
        // Theme is already changed locally, so we don't revert
      }
    } finally {
      setIsTogglingTheme(false);
    }
  };

  const handleLogout = async () => {
    if (!window.confirm('¿Cerrar sesión?')) {
      return;
    }

    try {
      setIsLoggingOut(true);

      // Call backend to revoke session
      await apiService.logoutUser();

      // Clear local storage
      localStorage.removeItem('auroraAccessToken');
      localStorage.removeItem('auroraAccessTokenExpiry');

      // Reload to trigger login screen
      window.location.reload();
    } catch (error) {
      console.error('Error during logout:', error);
      // Even if backend call fails, clear local storage and reload
      localStorage.removeItem('auroraAccessToken');
      localStorage.removeItem('auroraAccessTokenExpiry');
      window.location.reload();
    } finally {
      setIsLoggingOut(false);
    }
  };

  return (
    <div className="navigation">
      <div className="navigation-header">
        <div className="navigation-logo">
          <div className="w-10 h-10 bg-primary-gradient rounded-lg flex items-center justify-center relative">
            <Calendar className="w-6 h-6 text-primary-foreground" />
            <div className="absolute -top-1 -right-1 w-5 h-5 bg-gradient-to-br from-yellow-400 to-orange-500 sparkle-badge rounded-full flex items-center justify-center">
              <Sparkles className="w-2.5 h-2.5 text-white" />
            </div>
          </div>
          <div className="logo-text">
            <h2>Aurora</h2>
            <p>Planificador IA</p>
          </div>
        </div>
      </div>

      <nav className="navigation-menu">
        {navigationItems.map((item) => {
          const IconComponent = item.icon;
          return (
            <button
              key={item.id}
              className={`nav-button ${activeView === item.id ? 'active' : ''}`}
              onClick={() => handleViewClick(item.id)}
            >
              <IconComponent size={16} />
              <div className="nav-text">
                <span className="nav-title">{item.label}</span>
                <span className="nav-subtitle">{item.description}</span>
              </div>
            </button>
          );
        })}
      </nav>

      <div className="navigation-footer">
        <button
          className="dark-mode-toggle"
          onClick={handleThemeToggle}
          disabled={isTogglingTheme}
        >
          {theme === 'dark' ? (
            <Sun size={16} aria-hidden="true" />
          ) : (
            <Moon size={16} aria-hidden="true" />
          )}
          <span>{theme === 'dark' ? 'Modo Claro' : 'Modo Oscuro'}</span>
        </button>

        <button
          className="logout-button"
          onClick={handleLogout}
          disabled={isLoggingOut}
        >
          <LogOut size={16} aria-hidden="true" />
          <span>Cerrar Sesión</span>
        </button>
      </div>
    </div>
  );
};

export default Navigation;