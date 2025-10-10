import { ChevronLeft, ChevronRight } from 'lucide-react';
import React, { useCallback, useEffect, useRef, useState } from 'react';
import { apiService, type EventCategoryDto, type EventDto } from '../services/apiService';
import './AuroraMonthlyCalendar.css';

interface AuroraMonthlyCalendarProps {
  onEventClick: (event: EventDto) => void;
  onAddEvent: (date: Date) => void;
  selectedCategoryId?: string | null;
  onCategoriesLoaded?: (categories: EventCategoryDto[]) => void;
}

interface CalendarDay {
  date: Date;
  dayNumber: number;
  isCurrentMonth: boolean;
  isToday: boolean;
  events: EventDto[];
}

const AuroraMonthlyCalendar: React.FC<AuroraMonthlyCalendarProps> = ({
  onEventClick,
  onAddEvent,
  selectedCategoryId,
  onCategoriesLoaded
}) => {
  const [currentDate, setCurrentDate] = useState(new Date());
  const [calendarDays, setCalendarDays] = useState<CalendarDay[]>([]);
  const [events, setEvents] = useState<EventDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Usar ref para evitar que onCategoriesLoaded cause re-renders infinitos
  const onCategoriesLoadedRef = useRef(onCategoriesLoaded);
  useEffect(() => {
    onCategoriesLoadedRef.current = onCategoriesLoaded;
  }, [onCategoriesLoaded]);

  const loadMonthlyEvents = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const year = currentDate.getFullYear();
      const month = currentDate.getMonth() + 1;
      const response = await apiService.getMonthlyEvents(year, month, undefined, selectedCategoryId || undefined);
      setEvents(response.events);

      // Notificar categorías al padre usando ref
      if (onCategoriesLoadedRef.current && response.categories) {
        onCategoriesLoadedRef.current(response.categories);
      }
    } catch (err) {
      console.error('Error loading monthly events:', err);
      setError('Error al cargar eventos');
    } finally {
      setLoading(false);
    }
  }, [currentDate, selectedCategoryId]);

  // Cargar eventos del mes
  useEffect(() => {
    loadMonthlyEvents();
  }, [loadMonthlyEvents]);

  // Generar días del calendario
  useEffect(() => {
    const year = currentDate.getFullYear();
    const month = currentDate.getMonth();

    // Primer día del mes
    const firstDay = new Date(year, month, 1);
    // Último día del mes
    const lastDay = new Date(year, month + 1, 0);

    // Día de la semana del primer día (0 = Domingo, ajustar a Lunes = 0)
    let firstDayOfWeek = firstDay.getDay() - 1;
    if (firstDayOfWeek < 0) firstDayOfWeek = 6;

    // Crear array de días
    const days: CalendarDay[] = [];

    // Días del mes anterior
    const prevMonthLastDay = new Date(year, month, 0).getDate();
    for (let i = firstDayOfWeek - 1; i >= 0; i--) {
      const date = new Date(year, month - 1, prevMonthLastDay - i);
      days.push({
        date,
        dayNumber: prevMonthLastDay - i,
        isCurrentMonth: false,
        isToday: false,
        events: []
      });
    }

    // Días del mes actual
    const today = new Date();
    for (let day = 1; day <= lastDay.getDate(); day++) {
      const date = new Date(year, month, day);
      const isToday = date.toDateString() === today.toDateString();
      days.push({
        date,
        dayNumber: day,
        isCurrentMonth: true,
        isToday,
        events: []
      });
    }

    // Días del mes siguiente para completar la última semana
    const remainingDays = 42 - days.length; // 6 semanas x 7 días
    for (let day = 1; day <= remainingDays; day++) {
      const date = new Date(year, month + 1, day);
      days.push({
        date,
        dayNumber: day,
        isCurrentMonth: false,
        isToday: false,
        events: []
      });
    }

    setCalendarDays(days);
  }, [currentDate]);

  // Asignar eventos a los días
  useEffect(() => {
    setCalendarDays(prevDays =>
      prevDays.map(day => ({
        ...day,
        events: events.filter(event => {
          const eventDate = new Date(event.startDate);
          return eventDate.toDateString() === day.date.toDateString();
        })
      }))
    );
  }, [events]);

  const goToPreviousMonth = () => {
    setCurrentDate(new Date(currentDate.getFullYear(), currentDate.getMonth() - 1, 1));
  };

  const goToNextMonth = () => {
    setCurrentDate(new Date(currentDate.getFullYear(), currentDate.getMonth() + 1, 1));
  };

  const goToToday = () => {
    setCurrentDate(new Date());
  };

  const formatMonth = (): string => {
    return currentDate.toLocaleDateString('es-ES', { month: 'long', year: 'numeric' });
  };

  const weekDays = ['Lun', 'Mar', 'Mié', 'Jue', 'Vie', 'Sáb', 'Dom'];

  const getEventCountText = (): string => {
    const count = events.length;
    return `${count} evento${count !== 1 ? 's' : ''} este mes`;
  };

  const getCurrentMonthBadge = (): string => {
    return currentDate.toLocaleDateString('es-ES', { month: 'long' });
  };

  return (
    <div className="monthly-calendar">
      {/* Header */}
      <div className="monthly-calendar-header">
        <div className="monthly-calendar-nav">
          <div className="month-nav-buttons">
            <button
              className="month-nav-button"
              onClick={goToPreviousMonth}
              aria-label="Mes anterior"
            >
              <ChevronLeft size={16} />
            </button>
            <button
              className="month-nav-button"
              onClick={goToNextMonth}
              aria-label="Mes siguiente"
            >
              <ChevronRight size={16} />
            </button>
          </div>
          <h2 className="month-title">{formatMonth()}</h2>
        </div>
        <div className="monthly-calendar-actions">
          <button className="action-button today-button" onClick={goToToday}>
            Hoy
          </button>
          <button className="add-event-btn" onClick={() => onAddEvent?.(new Date())}>
            <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
              <path d="M8 3v10M3 8h10" stroke="currentColor" strokeWidth="2" />
            </svg>
            Evento
          </button>
        </div>
      </div>

      {/* Days of week header */}
      <div className="monthly-calendar-weekdays">
        {weekDays.map(day => (
          <div key={day} className="weekday-header">
            {day}
          </div>
        ))}
      </div>

      {/* Calendar grid */}
      <div className="monthly-calendar-grid">
        {calendarDays.map((day, index) => (
          <div
            key={index}
            className={`calendar-day ${!day.isCurrentMonth ? 'other-month' : ''} ${day.isToday ? 'today' : ''
              }`}
            onClick={() => day.isCurrentMonth && onAddEvent(day.date)}
          >
            <div className={`day-number ${day.isToday ? 'today-number' : ''}`}>
              {day.dayNumber}
            </div>
            <div className="day-events">
              {day.events.slice(0, 3).map(event => (
                <div
                  key={event.id}
                  className="month-event-chip"
                  style={{ backgroundColor: event.eventCategory.color + '33' }}
                  onClick={(e) => {
                    e.stopPropagation();
                    onEventClick(event);
                  }}
                >
                  <span
                    className="event-chip-text"
                    style={{ color: event.eventCategory.color }}
                  >
                    {event.title}
                  </span>
                </div>
              ))}
              {day.events.length > 3 && (
                <div className="more-events-indicator">
                  +{day.events.length - 3} más
                </div>
              )}
            </div>
          </div>
        ))}
      </div>

      {/* Footer */}
      <div className="monthly-calendar-footer">
        <div className="footer-stats">{getEventCountText()}</div>
        <div className="footer-badge">{getCurrentMonthBadge()}</div>
      </div>

      {loading && <div className="loading-overlay">Cargando eventos...</div>}
      {error && <div className="error-message">{error}</div>}
    </div>
  );
};

export default AuroraMonthlyCalendar;
