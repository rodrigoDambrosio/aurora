import type { ReactNode } from 'react';
import { createContext, useContext, useState } from 'react';
import { InAppNotification } from '../components/InAppNotification';
import type { ReminderDto } from '../types/reminder.types';

interface NotificationContextType {
  showNotification: (reminder: ReminderDto) => void;
  dismissNotification: (id: string) => void;
}

const NotificationContext = createContext<NotificationContextType | undefined>(undefined);

export function useNotifications() {
  const context = useContext(NotificationContext);
  if (!context) {
    throw new Error('useNotifications debe usarse dentro de NotificationProvider');
  }
  return context;
}

interface NotificationProviderProps {
  children: ReactNode;
  onViewEvent?: (eventId: string) => void | Promise<void>;
}

export function NotificationProvider({ children, onViewEvent }: NotificationProviderProps) {
  const [notifications, setNotifications] = useState<ReminderDto[]>([]);

  const showNotification = (reminder: ReminderDto) => {
    // Evitar duplicados
    setNotifications(prev => {
      const exists = prev.some(n => n.id === reminder.id);
      if (exists) return prev;
      return [...prev, reminder];
    });
  };

  const dismissNotification = (id: string) => {
    setNotifications(prev => prev.filter(n => n.id !== id));
  };

  return (
    <NotificationContext.Provider value={{ showNotification, dismissNotification }}>
      {children}

      {/* Renderizar notificaciones activas */}
      {notifications.map((notification, index) => (
        <InAppNotification
          key={notification.id}
          reminder={notification}
          onDismiss={() => dismissNotification(notification.id)}
          onViewEvent={onViewEvent}
          stackIndex={index}
        />
      ))}
    </NotificationContext.Provider>
  );
}