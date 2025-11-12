import { useCallback, useEffect, useState } from 'react';
import { apiService } from '../services/apiService';
import type { CreateReminderDto, ReminderDto } from '../types/reminder.types';

interface UseRemindersReturn {
  reminders: ReminderDto[];
  isLoading: boolean;
  error: string | null;
  addReminder: (data: CreateReminderDto) => Promise<void>;
  removeReminder: (id: string) => Promise<void>;
  refresh: () => Promise<void>;
}

/**
 * Hook para gestionar recordatorios de un evento
 */
export function useReminders(eventId: string | null): UseRemindersReturn {
  const [reminders, setReminders] = useState<ReminderDto[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  /**
   * Carga los recordatorios del evento
   */
  const loadReminders = useCallback(async () => {
    if (!eventId) {
      setReminders([]);
      return;
    }

    setIsLoading(true);
    setError(null);

    try {
      const data = await apiService.getRemindersByEventId(eventId);
      setReminders(data);
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Error al cargar recordatorios';
      setError(errorMessage);
      console.error('Error loading reminders:', err);
    } finally {
      setIsLoading(false);
    }
  }, [eventId]);

  /**
   * Agrega un nuevo recordatorio
   */
  const addReminder = useCallback(async (data: CreateReminderDto) => {
    setIsLoading(true);
    setError(null);

    try {
      const newReminder = await apiService.createReminder(data);
      setReminders((prev) => [...prev, newReminder]);
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Error al crear recordatorio';
      setError(errorMessage);
      console.error('Error creating reminder:', err);
      throw err; // Re-throw para que el componente pueda manejarlo
    } finally {
      setIsLoading(false);
    }
  }, []);

  /**
   * Elimina un recordatorio
   */
  const removeReminder = useCallback(async (id: string) => {
    setIsLoading(true);
    setError(null);

    try {
      await apiService.deleteReminder(id);
      setReminders((prev) => prev.filter((r) => r.id !== id));
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Error al eliminar recordatorio';
      setError(errorMessage);
      console.error('Error deleting reminder:', err);
      throw err;
    } finally {
      setIsLoading(false);
    }
  }, []);

  /**
   * Recarga los recordatorios
   */
  const refresh = useCallback(async () => {
    await loadReminders();
  }, [loadReminders]);

  // Cargar recordatorios cuando el eventId cambie y establecer polling cada 60 segundos
  useEffect(() => {
    loadReminders();

    // Configurar polling cada 60 segundos
    const intervalId = setInterval(() => {
      loadReminders();
    }, 60000); // 60 segundos

    // Limpiar el interval al desmontar o cuando cambie el eventId
    return () => {
      clearInterval(intervalId);
    };
  }, [loadReminders]);

  return {
    reminders,
    isLoading,
    error,
    addReminder,
    removeReminder,
    refresh,
  };
}
