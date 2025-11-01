import { useCallback, useEffect, useRef, useState } from 'react';
import { apiService } from '../services/apiService';
import { notificationService } from '../services/notificationService';

interface UseNotificationsReturn {
  permission: NotificationPermission;
  isPolling: boolean;
  requestPermission: () => Promise<void>;
  startPolling: () => void;
  stopPolling: () => void;
}

const POLLING_INTERVAL = 60000; // 60 segundos

/**
 * Hook para gestionar notificaciones del navegador con polling de recordatorios pendientes
 */
export function useNotifications(): UseNotificationsReturn {
  const [permission, setPermission] = useState<NotificationPermission>(
    notificationService.getPermissionStatus()
  );
  const [isPolling, setIsPolling] = useState(false);
  const pollingIntervalRef = useRef<number | null>(null);
  const processedRemindersRef = useRef<Set<string>>(new Set());

  /**
   * Solicita permisos de notificaciones al usuario
   */
  const requestPermission = useCallback(async () => {
    const newPermission = await notificationService.requestPermission();
    setPermission(newPermission);

    if (newPermission === 'granted') {
      startPolling();
    }
  }, []);

  /**
   * Verifica y procesa recordatorios pendientes
   */
  const checkPendingReminders = useCallback(async () => {
    if (permission !== 'granted') {
      return;
    }

    try {
      const pendingReminders = await apiService.getPendingReminders();

      for (const reminder of pendingReminders) {
        // Evitar procesar el mismo recordatorio múltiples veces
        if (processedRemindersRef.current.has(reminder.id)) {
          continue;
        }

        // Mostrar la notificación
        notificationService.showNotification(reminder);

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
  }, [permission]);

  /**
   * Inicia el polling de recordatorios pendientes
   */
  const startPolling = useCallback(() => {
    if (permission !== 'granted' || isPolling) {
      return;
    }

    setIsPolling(true);

    // Verificar inmediatamente
    checkPendingReminders();

    // Configurar intervalo de polling
    pollingIntervalRef.current = window.setInterval(() => {
      checkPendingReminders();
    }, POLLING_INTERVAL);

    console.log('Polling de recordatorios iniciado');
  }, [permission, isPolling, checkPendingReminders]);

  /**
   * Detiene el polling de recordatorios pendientes
   */
  const stopPolling = useCallback(() => {
    if (pollingIntervalRef.current !== null) {
      window.clearInterval(pollingIntervalRef.current);
      pollingIntervalRef.current = null;
      setIsPolling(false);
      console.log('Polling de recordatorios detenido');
    }
  }, []);

  // Iniciar polling automáticamente si hay permisos concedidos
  useEffect(() => {
    if (permission === 'granted' && !isPolling) {
      startPolling();
    }

    return () => {
      stopPolling();
    };
  }, [permission]);

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
