import { ChevronLeft, ChevronRight, Filter } from 'lucide-react';
import React, { useEffect, useMemo, useState } from 'react';
import { formatMonthTitle } from '../lib/utils';
import type { EventCategoryDto, EventDto, WeeklyEventsResponseDto } from '../services/apiService';
import { apiService } from '../services/apiService';
import './AuroraWeeklyCalendar.css';
import './CalendarHeader.css';
import { CategoryFilter } from './CategoryFilter';

interface AuroraWeeklyCalendarProps {
  onEventClick?: (event: EventDto) => void;
  onAddEvent?: (date: Date) => void;
  selectedCategoryId?: string | null;
  onCategoriesLoaded?: (categories: EventCategoryDto[]) => void;
  showFilters?: boolean;
  onToggleFilters?: () => void;
  categories?: EventCategoryDto[];
  onCategoryChange?: (categoryId: string | null) => void;
  refreshToken?: number;
  firstDayOfWeek?: number; // 0 = Sunday, 1 = Monday, ..., 6 = Saturday
}

const AuroraWeeklyCalendar: React.FC<AuroraWeeklyCalendarProps> = ({
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
  const [weeklyData, setWeeklyData] = useState<WeeklyEventsResponseDto | null>(null);
  const [loading, setLoading] = useState<boolean>(false);
  const [error, setError] = useState<string>('');

  // Drag and drop state
  const [draggedEvent, setDraggedEvent] = useState<EventDto | null>(null);
  const [dragOverDate, setDragOverDate] = useState<Date | null>(null);

  // Get start of the week based on user's preference
  const weekStart = useMemo(() => {
    const getWeekStart = (date: Date): Date => {
      const d = new Date(date);
      const day = d.getDay(); // 0 = Sunday, 1 = Monday, etc.

      // Calculate the difference to get to firstDayOfWeek
      let diff = day - firstDayOfWeek;
      if (diff < 0) {
        diff += 7; // Wrap around to previous week
      }

      const result = new Date(d);
      result.setDate(d.getDate() - diff);
      return result;
    };

    return getWeekStart(currentDate);
  }, [currentDate, firstDayOfWeek]);

  // Generate week dates
  const weekDates = useMemo(() => {
    return Array.from({ length: 7 }, (_, i) => {
      const date = new Date(weekStart);
      date.setDate(weekStart.getDate() + i);
      return date;
    });
  }, [weekStart]);

  // Load weekly events from API
  const loadWeeklyEvents = async (weekStartDate: Date) => {
    setLoading(true);
    setError('');

    try {
      const weekStartISO = weekStartDate.toISOString().split('T')[0]; // Format: YYYY-MM-DD
      const response = await apiService.getWeeklyEvents(
        weekStartISO,
        undefined,
        selectedCategoryId || undefined
      );
      setWeeklyData(response);

      // Notificar categorías cargadas al padre
      if (onCategoriesLoaded && response.categories) {
        onCategoriesLoaded(response.categories);
      }

      console.log('Weekly events loaded successfully:', response);
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Error desconocido';
      setError(`Error cargando eventos: ${errorMessage}`);
      console.error('Error loading weekly events:', err);
    } finally {
      setLoading(false);
    }
  };

  // Load events when component mounts, current date changes, or category filter changes
  useEffect(() => {
    loadWeeklyEvents(weekStart);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [weekStart, selectedCategoryId, refreshToken]);

  // Navigation functions
  const goToPreviousWeek = () => {
    const newDate = new Date(currentDate);
    newDate.setDate(currentDate.getDate() - 7);
    setCurrentDate(newDate);
  };

  const goToNextWeek = () => {
    const newDate = new Date(currentDate);
    newDate.setDate(currentDate.getDate() + 7);
    setCurrentDate(newDate);
  };

  const goToToday = () => {
    setCurrentDate(new Date());
  };

  // Format functions
  const formatDayName = (date: Date): string => {
    const days = ['dom', 'lun', 'mar', 'mié', 'jue', 'vie', 'sáb'];
    return days[date.getDay()];
  };

  const formatTime = (dateTime: string): string => {
    const date = new Date(dateTime);
    return date.toLocaleTimeString('es-ES', {
      hour: '2-digit',
      minute: '2-digit',
      hour12: false
    });
  };

  const getEventTimeRange = (event: EventDto): string => {
    const start = new Date(event.startDate);
    const end = new Date(event.endDate);
    const startDay = new Date(start.getFullYear(), start.getMonth(), start.getDate());
    const endDay = new Date(end.getFullYear(), end.getMonth(), end.getDate());
    const isMultiDay = startDay.getTime() !== endDay.getTime();

    if (isMultiDay) {
      return `${start.toLocaleDateString('es-ES', { day: 'numeric', month: 'short' })} ${formatTime(event.startDate)} - ${end.toLocaleDateString('es-ES', { day: 'numeric', month: 'short' })} ${formatTime(event.endDate)}`;
    }

    const startTime = formatTime(event.startDate);
    const endTime = formatTime(event.endDate);
    return `${startTime} - ${endTime}`;
  };

  const getEventsByDate = (date: Date): EventDto[] => {
    if (!weeklyData?.events) return [];

    // Normalizar la fecha del día a medianoche para comparación
    const dayStart = new Date(date.getFullYear(), date.getMonth(), date.getDate());

    return weeklyData.events.filter((event: EventDto) => {
      const eventStart = new Date(event.startDate);
      const eventEnd = new Date(event.endDate);

      const eventStartDay = new Date(eventStart.getFullYear(), eventStart.getMonth(), eventStart.getDate());
      const eventEndDay = new Date(eventEnd.getFullYear(), eventEnd.getMonth(), eventEnd.getDate());

      // El evento se muestra si abarca este día
      return (eventStartDay.getTime() <= dayStart.getTime() && eventEndDay.getTime() >= dayStart.getTime());
    });
  };

  const isToday = (date: Date): boolean => {
    const today = new Date();
    return date.toDateString() === today.toDateString();
  };

  // Drag and drop handlers
  const handleDragStart = (event: EventDto, e: React.DragEvent) => {
    setDraggedEvent(event);
    e.dataTransfer.effectAllowed = 'move';
    // Add visual feedback
    e.currentTarget.classList.add('dragging');
  };

  const handleDragEnd = (e: React.DragEvent) => {
    setDraggedEvent(null);
    setDragOverDate(null);
    e.currentTarget.classList.remove('dragging');
  };

  const handleDragOver = (date: Date, e: React.DragEvent) => {
    e.preventDefault();
    e.dataTransfer.dropEffect = 'move';
    setDragOverDate(date);
  };

  const handleDragLeave = () => {
    setDragOverDate(null);
  };

  const handleDrop = async (targetDate: Date, e: React.DragEvent) => {
    e.preventDefault();
    setDragOverDate(null);

    if (!draggedEvent) return;

    try {
      // Parse dates as UTC to preserve the original time
      const originalStart = new Date(draggedEvent.startDate);
      const originalEnd = new Date(draggedEvent.endDate);

      // Get UTC date components to compare dates without time
      const originalDateOnly = `${originalStart.getUTCFullYear()}-${String(originalStart.getUTCMonth() + 1).padStart(2, '0')}-${String(originalStart.getUTCDate()).padStart(2, '0')}`;

      // Get target date in UTC
      const targetYear = targetDate.getFullYear();
      const targetMonth = String(targetDate.getMonth() + 1).padStart(2, '0');
      const targetDay = String(targetDate.getDate()).padStart(2, '0');
      const targetDateOnly = `${targetYear}-${targetMonth}-${targetDay}`;

      // Check if the date actually changed
      if (originalDateOnly === targetDateOnly) {
        console.log('Event dropped on same day, no update needed');
        setDraggedEvent(null);
        return;
      }

      // Calculate day difference using UTC dates
      const originalDateObj = new Date(originalDateOnly + 'T00:00:00Z');
      const targetDateObj = new Date(targetDateOnly + 'T00:00:00Z');
      const dayDiff = Math.floor((targetDateObj.getTime() - originalDateObj.getTime()) / (1000 * 60 * 60 * 24));

      // Create new dates in UTC by preserving the time and only changing the date
      const newStartDate = new Date(originalStart.getTime() + (dayDiff * 24 * 60 * 60 * 1000));
      const newEndDate = new Date(originalEnd.getTime() + (dayDiff * 24 * 60 * 60 * 1000));

      // Update event via API using ISO strings (which preserve UTC)
      const updatedEvent = {
        title: draggedEvent.title,
        description: draggedEvent.description,
        startDate: newStartDate.toISOString(),
        endDate: newEndDate.toISOString(),
        location: draggedEvent.location,
        eventCategoryId: draggedEvent.eventCategory?.id || '',
        isAllDay: draggedEvent.isAllDay,
        priority: draggedEvent.priority
      };

      console.log('Moving event from', originalDateOnly, 'to', targetDateOnly);
      console.log('Original time:', originalStart.toISOString(), '-> New time:', newStartDate.toISOString());
      await apiService.updateEvent(draggedEvent.id, updatedEvent);
      console.log('Event moved successfully');

      // Reload events
      await loadWeeklyEvents(weekStart);
    } catch (err) {
      console.error('Error moving event:', err);
      setError('Error al mover el evento');
    } finally {
      setDraggedEvent(null);
    }
  };

  const totalEvents = weeklyData?.events.length || 0;

  // Show loading state
  if (loading && !weeklyData) {
    return (
      <div className="aurora-weekly-calendar">
        <div className="loading-container">
          <div className="loading-spinner"></div>
          <p>Cargando calendario...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="aurora-weekly-calendar">
      {/* Error display */}
      {error && (
        <div className="error-message">
          <span className="error-icon">⚠️</span>
          <p>{error}</p>
          <button
            className="retry-button"
            onClick={() => loadWeeklyEvents(weekStart)}
          >
            Reintentar
          </button>
        </div>
      )}

      {/* Header */}
      <div className="calendar-top-header">
        <div className="calendar-nav">
          <div className="calendar-nav-buttons">
            <button
              className="calendar-nav-button"
              onClick={goToPreviousWeek}
              aria-label="Semana anterior"
            >
              <ChevronLeft size={16} />
            </button>
            <button
              className="calendar-nav-button"
              onClick={goToNextWeek}
              aria-label="Semana siguiente"
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
          <button
            className="calendar-add-btn"
            onClick={() => onAddEvent?.(new Date())}
            aria-label="Crear nuevo evento"
          >
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

      {/* Week Days Header */}
      <div className="week-header">
        {weekDates.map((date, index) => (
          <div
            key={index}
            className={`day-header ${isToday(date) ? 'today' : ''}`}
          >
            <div className="day-name">{formatDayName(date)}</div>
            <div className="day-number">{date.getDate()}</div>
          </div>
        ))}
      </div>

      {/* Calendar Grid */}
      <div className="calendar-grid">
        {weekDates.map((date, dayIndex) => {
          const dayEvents = getEventsByDate(date);

          return (
            <div
              key={dayIndex}
              className={`day-column ${dragOverDate?.toDateString() === date.toDateString() ? 'drag-over' : ''}`}
              onDragOver={(e) => handleDragOver(date, e)}
              onDragLeave={handleDragLeave}
              onDrop={(e) => handleDrop(date, e)}
            >
              {dayEvents.length === 0 ? (
                <div className="add-event-placeholder" onClick={() => onAddEvent?.(date)}>
                  <svg width="20" height="20" viewBox="0 0 20 20" fill="none">
                    <path d="M10 4v12M4 10h12" stroke="currentColor" strokeWidth="2" />
                  </svg>
                  <span>Agregar evento</span>
                </div>
              ) : (
                <div className="events-container">
                  {dayEvents.map((event) => {
                    const categoryColor = event.eventCategory?.color || '#1447e6';
                    const priorityLevel = Math.min(Math.max(event.priority ?? 2, 1), 4);

                    // Detectar si es evento multi-día
                    const start = new Date(event.startDate);
                    const end = new Date(event.endDate);
                    const startDay = new Date(start.getFullYear(), start.getMonth(), start.getDate());
                    const endDay = new Date(end.getFullYear(), end.getMonth(), end.getDate());
                    const isMultiDay = startDay.getTime() !== endDay.getTime();

                    return (
                      <div
                        key={event.id}
                        className={`event-card ${isMultiDay ? 'multi-day-event' : ''}`}
                        draggable
                        onDragStart={(e) => handleDragStart(event, e)}
                        onDragEnd={handleDragEnd}
                        style={{
                          backgroundColor: categoryColor + '33',
                          borderLeftColor: categoryColor,
                          color: categoryColor
                        }}
                        onClick={() => onEventClick?.(event)}
                        onKeyDown={(keyboardEvent) => {
                          if (keyboardEvent.key === 'Enter') {
                            keyboardEvent.preventDefault();
                            onEventClick?.(event);
                          }
                        }}
                        onKeyUp={(keyboardEvent) => {
                          if (keyboardEvent.key === ' ') {
                            keyboardEvent.preventDefault();
                            onEventClick?.(event);
                          }
                        }}
                        role="button"
                        tabIndex={0}
                        aria-label={`Ver detalle del evento ${event.title}`}
                      >
                        <div className="event-title">{event.title}</div>
                        <div className="event-time">{getEventTimeRange(event)}</div>
                        <div className="event-priority" aria-label={`Prioridad ${priorityLevel} de 4`}>
                          {Array.from({ length: 4 }).map((_, i) => (
                            <svg
                              key={i}
                              width="12"
                              height="12"
                              viewBox="0 0 12 12"
                              fill="none"
                              className={i < priorityLevel ? 'filled' : ''}
                              aria-hidden="true"
                            >
                              <path d="M6 1L7.5 4.5L11 5L8.5 7.5L9 11L6 9.5L3 11L3.5 7.5L1 5L4.5 4.5L6 1Z" fill="currentColor" />
                            </svg>
                          ))}
                        </div>
                      </div>
                    );
                  })}
                  {/* Add event button at the bottom */}
                  <div className="add-event-placeholder compact" onClick={() => onAddEvent?.(date)}>
                    <svg width="20" height="20" viewBox="0 0 20 20" fill="none">
                      <path d="M10 4v12M4 10h12" stroke="currentColor" strokeWidth="2" />
                    </svg>
                    <span>Agregar evento</span>
                  </div>
                </div>
              )}
            </div>
          );
        })}
      </div>

      {/* Footer */}
      <div className="calendar-footer">
        <div className="events-summary">
          {totalEvents} eventos esta semana
        </div>
      </div>
    </div>
  );
};

export default AuroraWeeklyCalendar;