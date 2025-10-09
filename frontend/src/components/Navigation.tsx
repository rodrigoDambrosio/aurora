import { Calendar, Heart, MessageCircle, Settings, Sparkles } from 'lucide-react';
import React from 'react';
import './Navigation.css';

type View = 'calendar-week' | 'calendar-month' | 'wellness' | 'assistant' | 'settings';

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
  const handleViewClick = (view: string) => {
    if (onViewChange) {
      onViewChange(view);
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
        <button className="dark-mode-toggle">
          <svg width="16" height="16" viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg">
            <path d="M8 12a4 4 0 100-8 4 4 0 000 8z" fill="currentColor" />
          </svg>
          <span>Modo Oscuro</span>
        </button>
      </div>
    </div>
  );
};

export default Navigation;