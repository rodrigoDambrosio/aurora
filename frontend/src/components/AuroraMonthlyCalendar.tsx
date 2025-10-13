import { ChevronLeft, ChevronRight, Filter } from 'lucide-react';
import React, { useCallback, useEffect, useRef, useState } from 'react';
import { formatMonthTitle } from '../lib/utils';
import { apiService, type EventCategoryDto, type EventDto } from '../services/apiService';
import './AuroraMonthlyCalendar.css';
import './CalendarHeader.css';
import { CategoryFilter } from './CategoryFilter';

interface AuroraMonthlyCalendarProps {
  onEventClick: (event: EventDto) => void;
  onAddEvent: (date: Date) => void;
  selectedCategoryId?: string | null;
  onCategoriesLoaded?: (categories: EventCategoryDto[]) => void;
  showFilters?: boolean;
  onToggleFilters?: () => void;
  categories?: EventCategoryDto[];
  onCategoryChange?: (categoryId: string | null) => void;
  refreshToken?: number;
  firstDayOfWeek?: number; // 0 = Sunday, 1 = Monday, ..., 6 = Saturday
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
  onCategoriesLoaded,
  showFilters = true,
  onToggleFilters,
  categories = [],
  onCategoryChange,
  refreshToken,
  firstDayOfWeek = 1 // Default to Monday
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
  }, [loadMonthlyEvents, refreshToken]);

  // Generar días del calendario
  useEffect(() => {
    const year = currentDate.getFullYear();
    const month = currentDate.getMonth();

    // Primer día del mes
    const firstDay = new Date(year, month, 1);
    // Último día del mes
    const lastDay = new Date(year, month + 1, 0);

    // Día de la semana del primer día (0 = Domingo, 1 = Lunes, etc.)
    const firstDayOfMonth = firstDay.getDay();

    // Calcular cuántos días del mes anterior mostrar
    let daysFromPrevMonth = firstDayOfMonth - firstDayOfWeek;
    if (daysFromPrevMonth < 0) {
      daysFromPrevMonth += 7;
    }

    // Crear array de días
    const days: CalendarDay[] = [];

    // Días del mes anterior
    const prevMonthLastDay = new Date(year, month, 0).getDate();
    for (let i = daysFromPrevMonth - 1; i >= 0; i--) {
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
  }, [currentDate, firstDayOfWeek]);

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

  // Generate weekDays array based on firstDayOfWeek preference
  const weekDays = React.useMemo(() => {
    const allDays = ['Dom', 'Lun', 'Mar', 'Mié', 'Jue', 'Vie', 'Sáb'];
    // Rotate array to start from firstDayOfWeek
    return [...allDays.slice(firstDayOfWeek), ...allDays.slice(0, firstDayOfWeek)];
  }, [firstDayOfWeek]);

  const getEventCountText = (): string => {
    const count = events.length;
    return `${count} evento${count !== 1 ? 's' : ''} este mes`;
  };

  return (
    <div className="monthly-calendar">
      {/* Header */}
      <div className="calendar-top-header">
        <div className="calendar-nav">
          <div className="calendar-nav-buttons">
            <button
              className="calendar-nav-button"
              onClick={goToPreviousMonth}
              aria-label="Mes anterior"
            >
              <ChevronLeft size={16} />
            </button>
            <button
              className="calendar-nav-button"
              onClick={goToNextMonth}
              aria-label="Mes siguiente"
            >
              <ChevronRight size={16} />
            </button>
          </div>
          <h2 className="calendar-title">{formatMonthTitle(currentDate)}</h2>
        </div>
        <div className="calendar-actions">
          <button className="calendar-action-button calendar-today-button" onClick={goToToday}>
            Hoy
          </button>
          <button
            className={`calendar-settings-btn ${showFilters ? 'is-active' : ''}`}
            onClick={onToggleFilters}
            title={showFilters ? 'Ocultar filtros' : 'Mostrar filtros'}
            aria-label="Filtrar por categoría"
          >
            <Filter className="w-4 h-4" />
          </button>
          <button className="calendar-add-btn" onClick={() => onAddEvent?.(new Date())} aria-label="Crear nuevo evento">
            <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
              <path d="M8 3v10M3 8h10" stroke="currentColor" strokeWidth="2" />
            </svg>
            Evento
          </button>
        </div>
      </div>

      {/* Category Filter */}
      {showFilters && categories.length > 0 && (
        <div className="calendar-filter">
          <CategoryFilter
            categories={categories}
            selectedCategoryId={selectedCategoryId || null}
            onCategoryChange={onCategoryChange || (() => { })}
          />
        </div>
      )}

      {/* Separator line */}
      <div className="calendar-divider" />

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
                  onKeyDown={(keyboardEvent) => {
                    if (keyboardEvent.key === 'Enter') {
                      keyboardEvent.preventDefault();
                      onEventClick(event);
                    }
                  }}
                  onKeyUp={(keyboardEvent) => {
                    if (keyboardEvent.key === ' ') {
                      keyboardEvent.preventDefault();
                      onEventClick(event);
                    }
                  }}
                  role="button"
                  tabIndex={0}
                  aria-label={`Ver detalle del evento ${event.title}`}
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
      </div>

      {loading && <div className="loading-overlay">Cargando eventos...</div>}
      {error && <div className="error-message">{error}</div>}
    </div>
  );
};

export default AuroraMonthlyCalendar;
