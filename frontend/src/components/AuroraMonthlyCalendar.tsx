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
  const [selectedDay, setSelectedDay] = useState<number | null>(null);
  const [popoverPosition, setPopoverPosition] = useState<{ top: number; left: number; placement?: 'top' | 'bottom' } | null>(null);

  // Usar ref para evitar que onCategoriesLoaded cause re-renders infinitos
  const onCategoriesLoadedRef = useRef(onCategoriesLoaded);
  useEffect(() => {
    onCategoriesLoadedRef.current = onCategoriesLoaded;
  }, [onCategoriesLoaded]);

  // Cerrar popover al hacer click fuera
  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      const target = e.target as HTMLElement;
      if (!target.closest('.day-events-popover') && !target.closest('.calendar-day')) {
        setSelectedDay(null);
        setPopoverPosition(null);
      }
    };

    if (selectedDay !== null) {
      document.addEventListener('mousedown', handleClickOutside);
      return () => document.removeEventListener('mousedown', handleClickOutside);
    }
  }, [selectedDay]);

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

  // Asignar eventos a los días (incluyendo eventos multi-día)
  useEffect(() => {
    setCalendarDays(prevDays =>
      prevDays.map(day => ({
        ...day,
        events: events.filter(event => {
          const eventStart = new Date(event.startDate);
          const eventEnd = new Date(event.endDate);

          // Normalizar las fechas a medianoche para comparación de días
          const dayStart = new Date(day.date.getFullYear(), day.date.getMonth(), day.date.getDate());

          const eventStartDay = new Date(eventStart.getFullYear(), eventStart.getMonth(), eventStart.getDate());
          const eventEndDay = new Date(eventEnd.getFullYear(), eventEnd.getMonth(), eventEnd.getDate());

          // El evento se muestra si:
          // 1. Empieza en este día (eventStartDay === dayStart)
          // 2. Termina en este día (eventEndDay === dayStart)
          // 3. Abarca este día (eventStartDay < dayStart && eventEndDay > dayStart)
          return (
            (eventStartDay.getTime() <= dayStart.getTime() && eventEndDay.getTime() >= dayStart.getTime())
          );
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
              } ${selectedDay === index ? 'selected' : ''}`}
            onClick={(e) => {
              if (!day.isCurrentMonth) return;

              // Si el día tiene eventos, mostrar popover
              if (day.events.length > 0) {
                e.stopPropagation();
                if (selectedDay === index) {
                  // Si ya está seleccionado, cerrarlo
                  setSelectedDay(null);
                  setPopoverPosition(null);
                } else {
                  // Abrir popover con posicionamiento inteligente
                  setSelectedDay(index);
                  const rect = e.currentTarget.getBoundingClientRect();
                  const viewportHeight = window.innerHeight;
                  const viewportWidth = window.innerWidth;

                  // Calcular si hay más espacio arriba o abajo
                  const spaceBelow = viewportHeight - rect.bottom;
                  const spaceAbove = rect.top;

                  // Estimar altura del popover (aproximado)
                  const estimatedPopoverHeight = Math.min(day.events.length * 80 + 100, Math.min(500, viewportHeight - 100));

                  let top = rect.bottom + 8; // Por defecto, debajo del día
                  let placement: 'top' | 'bottom' = 'bottom';

                  // Si no hay suficiente espacio abajo y hay más espacio arriba, mostrarlo arriba
                  if (spaceBelow < estimatedPopoverHeight && spaceAbove > spaceBelow) {
                    top = rect.top - 8;
                    placement = 'top';
                  }

                  // Ajustar si se sale por arriba
                  if (placement === 'top' && top - estimatedPopoverHeight < 16) {
                    top = Math.max(16 + estimatedPopoverHeight, rect.top - 8);
                  }

                  // Ajustar si se sale por abajo
                  if (placement === 'bottom' && top + estimatedPopoverHeight > viewportHeight - 16) {
                    top = Math.min(viewportHeight - 16, rect.bottom + 8);
                  }

                  // Ajustar posición horizontal para mantenerlo en pantalla
                  const popoverWidth = Math.min(400, viewportWidth - 32);
                  let left = rect.left + rect.width / 2;

                  // Asegurar que el popover no se salga por la derecha
                  if (left + popoverWidth / 2 > viewportWidth - 16) {
                    left = viewportWidth - popoverWidth / 2 - 16;
                  }
                  // Asegurar que el popover no se salga por la izquierda
                  if (left - popoverWidth / 2 < 16) {
                    left = popoverWidth / 2 + 16;
                  }

                  setPopoverPosition({
                    top,
                    left,
                    placement
                  });
                }
              } else {
                // Si no hay eventos, abrir formulario de creación
                onAddEvent(day.date);
              }
            }}
          >
            <div className={`day-number ${day.isToday ? 'today-number' : ''}`}>
              {day.dayNumber}
            </div>
            <div className="day-events">
              {day.events.slice(0, 2).map(event => {
                const eventStart = new Date(event.startDate);
                const eventEnd = new Date(event.endDate);
                const eventStartDay = new Date(eventStart.getFullYear(), eventStart.getMonth(), eventStart.getDate());
                const eventEndDay = new Date(eventEnd.getFullYear(), eventEnd.getMonth(), eventEnd.getDate());
                const isMultiDay = eventStartDay.getTime() !== eventEndDay.getTime();
                const currentDay = new Date(day.date.getFullYear(), day.date.getMonth(), day.date.getDate());
                const isFirstDay = eventStartDay.getTime() === currentDay.getTime();
                const isLastDay = eventEndDay.getTime() === currentDay.getTime();

                return (
                  <div
                    key={event.id}
                    className={`month-event-chip ${isMultiDay ? 'multi-day' : ''} ${isMultiDay && !isFirstDay ? 'continues-from-before' : ''} ${isMultiDay && !isLastDay ? 'continues-after' : ''}`}
                    style={{ backgroundColor: (event.eventCategory?.color ?? '#3b82f6') + '33' }}
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
                      style={{ color: event.eventCategory?.color ?? '#3b82f6' }}
                    >
                      {event.title}
                    </span>
                  </div>
                );
              })}
              {day.events.length > 2 && (
                <div className="more-events-indicator">
                  +{day.events.length - 2} más
                </div>
              )}
            </div>
          </div>
        ))}
      </div>

      {/* Event Popover */}
      {selectedDay !== null && popoverPosition && (
        <div
          className={`day-events-popover ${popoverPosition.placement || 'bottom'}`}
          style={{
            top: `${popoverPosition.top}px`,
            left: `${popoverPosition.left}px`
          }}
        >
          <div className="popover-content">
            <div className="popover-header">
              <div className="popover-date">
                {calendarDays[selectedDay].date.toLocaleDateString('es-ES', {
                  weekday: 'long',
                  day: 'numeric',
                  month: 'long'
                })}
              </div>
              <button
                className="popover-close-btn"
                onClick={(e) => {
                  e.stopPropagation();
                  setSelectedDay(null);
                  setPopoverPosition(null);
                }}
                aria-label="Cerrar"
              >
                ×
              </button>
            </div>
            <div className="popover-events">
              {calendarDays[selectedDay].events.map(event => (
                <div
                  key={event.id}
                  className="popover-event-item"
                  onClick={(e) => {
                    e.stopPropagation();
                    onEventClick(event);
                  }}
                >
                  <div
                    className="popover-event-color"
                    style={{ backgroundColor: event.eventCategory?.color ?? '#3b82f6' }}
                  />
                  <div className="popover-event-details">
                    <div className="popover-event-title">{event.title}</div>
                    <div className="popover-event-time">
                      {(() => {
                        const start = new Date(event.startDate);
                        const end = new Date(event.endDate);
                        const startDay = new Date(start.getFullYear(), start.getMonth(), start.getDate());
                        const endDay = new Date(end.getFullYear(), end.getMonth(), end.getDate());
                        const isMultiDay = startDay.getTime() !== endDay.getTime();

                        if (isMultiDay) {
                          return (
                            <>
                              {start.toLocaleDateString('es-ES', { day: 'numeric', month: 'short' })} {start.toLocaleTimeString('es-ES', { hour: '2-digit', minute: '2-digit' })}
                              {' - '}
                              {end.toLocaleDateString('es-ES', { day: 'numeric', month: 'short' })} {end.toLocaleTimeString('es-ES', { hour: '2-digit', minute: '2-digit' })}
                            </>
                          );
                        }

                        return (
                          <>
                            {start.toLocaleTimeString('es-ES', { hour: '2-digit', minute: '2-digit' })}
                            {' - '}
                            {end.toLocaleTimeString('es-ES', { hour: '2-digit', minute: '2-digit' })}
                          </>
                        );
                      })()}
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>
      )}

      {loading && <div className="loading-overlay">Cargando eventos...</div>}
      {error && <div className="error-message">{error}</div>}
    </div>
  );
};

export default AuroraMonthlyCalendar;
