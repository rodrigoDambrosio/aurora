import { useCallback, useEffect, useRef, useState } from 'react';
import { apiService } from '../services/apiService';
import { notificationService } from '../services/notificationService';
import type { ReminderDto } from '../types/reminder.types';

interface UseNotificationsReturn {
  permission: NotificationPermission;
  isPolling: boolean;
  requestPermission: () => Promise<void>;
  startPolling: () => void;
  stopPolling: () => void;
}

const POLLING_INTERVAL = 60000; // 60 segundos

/**
 * Hook para gestionar notificaciones in-app con polling de recordatorios pendientes
 */
export function useNotifications(showInAppNotification?: (reminder: ReminderDto) => void): UseNotificationsReturn {
  const [permission, setPermission] = useState<NotificationPermission>('default');
  const [isPolling, setIsPolling] = useState(false);
  const pollingIntervalRef = useRef<number | null>(null);
  const processedRemindersRef = useRef<Set<string>>(new Set());

  // Configurar el callback en el servicio de notificaciones
  useEffect(() => {
    if (showInAppNotification) {
      notificationService.setInAppNotificationCallback(showInAppNotification);
    }
  }, [showInAppNotification]);

  /**
   * Verifica y procesa recordatorios pendientes
   */
  const checkPendingReminders = useCallback(async () => {
    try {
      const pendingReminders = await apiService.getPendingReminders();

      for (const reminder of pendingReminders) {
        // Evitar procesar el mismo recordatorio múltiples veces
        if (processedRemindersRef.current.has(reminder.id)) {
          continue;
        }

        // Mostrar la notificación in-app usando el servicio
        notificationService.showInAppNotification(reminder);

        // Marcar como enviado en el backend
        try {
          await apiService.markReminderAsSent(reminder.id);
          processedRemindersRef.current.add(reminder.id);
        } catch (error) {
          console.error(`Error al marcar recordatorio ${reminder.id} como enviado:`, error);
        }
      }
    } catch (error) {
      console.error('Error al verificar recordatorios pendientes:', error);
    }
  }, [showInAppNotification]);

  /**
   * Inicia el polling de recordatorios pendientes
   */
  const startPolling = useCallback(() => {
    if (isPolling) {
      return;
    }

    setIsPolling(true);

    // Verificar inmediatamente
    checkPendingReminders();

    // Configurar intervalo de polling
    pollingIntervalRef.current = window.setInterval(() => {
      checkPendingReminders();
    }, POLLING_INTERVAL);
  }, [isPolling, checkPendingReminders]);

  /**
   * Detiene el polling de recordatorios pendientes
   */
  const stopPolling = useCallback(() => {
    if (pollingIntervalRef.current !== null) {
      window.clearInterval(pollingIntervalRef.current);
      pollingIntervalRef.current = null;
      setIsPolling(false);
    }
  }, []);

  /**
   * Solicita permisos de notificaciones (mantiene compatibilidad, pero ya no es necesario para in-app)
   */
  const requestPermission = useCallback(async () => {
    // Para notificaciones in-app no necesitamos permisos del navegador
    // Pero mantenemos la función para compatibilidad
    setPermission('granted');

    if (!isPolling) {
      startPolling();
    }
  }, [isPolling, startPolling]);

  // Iniciar polling automáticamente (sin depender de permisos del navegador)
  useEffect(() => {
    if (!isPolling) {
      startPolling();
    }

    return () => {
      stopPolling();
    };
  }, [startPolling, stopPolling, isPolling]);

  // Limpiar al desmontar
  useEffect(() => {
    return () => {
      stopPolling();
    };
  }, [stopPolling]);

  return {
    permission,
    isPolling,
    requestPermission,
    startPolling,
    stopPolling,
  };
}
