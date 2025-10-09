import { useCallback, useState } from 'react';

interface ApiState<T> {
  data: T | null;
  loading: boolean;
  error: string | null;
}

interface UseApiReturn<T> {
  data: T | null;
  loading: boolean;
  error: string | null;
  execute: (...args: any[]) => Promise<void>;
  reset: () => void;
}

/**
 * Custom hook for API calls with loading, error, and success states
 */
export function useApi<T>(
  apiFunction: (...args: any[]) => Promise<T>
): UseApiReturn<T> {
  const [state, setState] = useState<ApiState<T>>({
    data: null,
    loading: false,
    error: null,
  });

  const execute = useCallback(async (...args: any[]) => {
    setState(prev => ({ ...prev, loading: true, error: null }));

    try {
      const result = await apiFunction(...args);
      setState({ data: result, loading: false, error: null });
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Error desconocido';
      setState({ data: null, loading: false, error: errorMessage });
      console.error('API Error:', err);
    }
  }, [apiFunction]);

  const reset = useCallback(() => {
    setState({ data: null, loading: false, error: null });
  }, []);

  return {
    data: state.data,
    loading: state.loading,
    error: state.error,
    execute,
    reset,
  };
}

/**
 * Custom hook for date utilities
 */
export function useDateUtils() {
  const getMondayOfWeek = useCallback((date: Date): Date => {
    const monday = new Date(date);
    const dayOfWeek = monday.getDay();
    const diff = dayOfWeek === 0 ? -6 : 1 - dayOfWeek;
    monday.setDate(monday.getDate() + diff);
    monday.setHours(0, 0, 0, 0);
    return monday;
  }, []);

  const formatDate = useCallback((date: Date, options: Intl.DateTimeFormatOptions): string => {
    return date.toLocaleDateString('es-ES', options);
  }, []);

  const formatTime = useCallback((dateString: string): string => {
    return new Date(dateString).toLocaleTimeString('es-ES', {
      hour: '2-digit',
      minute: '2-digit'
    });
  }, []);

  const isToday = useCallback((date: Date): boolean => {
    const today = new Date();
    return date.toDateString() === today.toDateString();
  }, []);

  const addDays = useCallback((date: Date, days: number): Date => {
    const result = new Date(date);
    result.setDate(result.getDate() + days);
    return result;
  }, []);

  const getWeekDays = useCallback((startDate: Date): Date[] => {
    const days = [];
    for (let i = 0; i < 7; i++) {
      days.push(addDays(startDate, i));
    }
    return days;
  }, [addDays]);

  return {
    getMondayOfWeek,
    formatDate,
    formatTime,
    isToday,
    addDays,
    getWeekDays,
  };
}