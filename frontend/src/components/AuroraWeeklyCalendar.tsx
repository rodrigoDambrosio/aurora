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
  // Hour grid configuration
  const START_HOUR = 0;
  const END_HOUR = 24;
  const HOUR_HEIGHT = 80; // pixels per hour

  const [currentDate, setCurrentDate] = useState(new Date());
  const [weeklyData, setWeeklyData] = useState<WeeklyEventsResponseDto | null>(null);
  const [loading, setLoading] = useState<boolean>(false);
  const [error, setError] = useState<string>('');

  // Drag and drop state
  const [draggedEvent, setDraggedEvent] = useState<EventDto | null>(null);
  const [dragOverDate, setDragOverDate] = useState<Date | null>(null);
  const [dragOffsetY, setDragOffsetY] = useState<number>(0);
  const [dragPreviewTop, setDragPreviewTop] = useState<number | null>(null);

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

  // Formato compacto para eventos angostos
  const getCompactTimeRange = (event: EventDto): string => {
    const start = new Date(event.startDate);
    const end = new Date(event.endDate);
    const startDay = new Date(start.getFullYear(), start.getMonth(), start.getDate());
    const endDay = new Date(end.getFullYear(), end.getMonth(), end.getDate());
    const isMultiDay = startDay.getTime() !== endDay.getTime();

    if (isMultiDay) {
      return `${start.getDate()}/${start.getMonth() + 1} ${formatTime(event.startDate)}`;
    }

    const startTime = formatTime(event.startDate);
    const endTime = formatTime(event.endDate);
    return `${startTime}-${endTime}`; // Sin espacios para ahorrar espacio
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

  // Separar eventos de todo el día de eventos con hora específica
  const getAllDayEvents = (date: Date): EventDto[] => {
    const dayEvents = getEventsByDate(date);
    return dayEvents.filter(event => event.isAllDay);
  };

  const getTimedEvents = (date: Date): EventDto[] => {
    const dayEvents = getEventsByDate(date);
    return dayEvents.filter(event => !event.isAllDay);
  };

  // Detectar superposiciones y calcular columnas para eventos
  interface EventWithLayout extends EventDto {
    column: number;
    totalColumns: number;
  }

  const calculateEventLayout = (events: EventDto[]): EventWithLayout[] => {
    if (events.length === 0) return [];
    if (events.length === 1) {
      return [{
        ...events[0],
        column: 0,
        totalColumns: 1
      }];
    }

    // Ordenar eventos por hora de inicio, luego por duración (más largos primero)
    const sortedEvents = [...events].sort((a, b) => {
      const startA = new Date(a.startDate).getTime();
      const startB = new Date(b.startDate).getTime();
      if (startA !== startB) return startA - startB;

      // Si empiezan igual, ordenar por duración (más largos primero)
      const durationA = new Date(a.endDate).getTime() - startA;
      const durationB = new Date(b.endDate).getTime() - startB;
      return durationB - durationA;
    });

    // Detectar grupos de eventos superpuestos
    const groups: EventDto[][] = [];
    let currentGroup: EventDto[] = [sortedEvents[0]];
    let groupEndTime = new Date(sortedEvents[0].endDate).getTime();

    for (let i = 1; i < sortedEvents.length; i++) {
      const event = sortedEvents[i];
      const eventStart = new Date(event.startDate).getTime();
      const eventEnd = new Date(event.endDate).getTime();

      // Si el evento empieza antes de que termine el grupo, es parte del grupo
      if (eventStart < groupEndTime) {
        currentGroup.push(event);
        groupEndTime = Math.max(groupEndTime, eventEnd);
      } else {
        // Nuevo grupo
        groups.push(currentGroup);
        currentGroup = [event];
        groupEndTime = eventEnd;
      }
    }
    groups.push(currentGroup);

    // Asignar columnas dentro de cada grupo
    const eventsWithLayout: EventWithLayout[] = [];

    groups.forEach(group => {
      if (group.length === 1) {
        eventsWithLayout.push({
          ...group[0],
          column: 0,
          totalColumns: 1
        });
      } else {
        // Sin límite de columnas - crear tantas como sean necesarias
        const columns: { end: number }[] = [];

        group.forEach(event => {
          const eventStart = new Date(event.startDate).getTime();
          const eventEnd = new Date(event.endDate).getTime();

          // Encontrar la primera columna disponible
          let columnIndex = -1;
          for (let i = 0; i < columns.length; i++) {
            if (columns[i].end <= eventStart) {
              columnIndex = i;
              break;
            }
          }

          // Si no hay columna disponible, crear una nueva
          if (columnIndex === -1) {
            columnIndex = columns.length;
            columns.push({ end: eventEnd });
          } else {
            columns[columnIndex].end = eventEnd;
          }

          eventsWithLayout.push({
            ...event,
            column: columnIndex,
            totalColumns: columns.length
          });
        });

        // Actualizar totalColumns para todos los eventos del grupo
        const actualColumns = columns.length;
        eventsWithLayout
          .filter(e => group.some(ge => ge.id === e.id))
          .forEach(e => {
            e.totalColumns = actualColumns;
          });
      }
    });

    return eventsWithLayout;
  };

  // Calculate event position in hour grid
  const getEventPosition = (event: EventDto, currentDate: Date): { top: number; height: number } => {
    const start = new Date(event.startDate);
    const end = new Date(event.endDate);

    // Get the current day boundaries
    const dayStart = new Date(currentDate.getFullYear(), currentDate.getMonth(), currentDate.getDate());
    const dayEnd = new Date(dayStart);
    dayEnd.setHours(23, 59, 59, 999);

    // Determine effective start and end times for this day
    let effectiveStart = start;
    let effectiveEnd = end;

    // If event starts before this day, use 00:00 of this day
    if (start < dayStart) {
      effectiveStart = dayStart;
    }

    // If event ends after this day, use 23:59 of this day
    if (end > dayEnd) {
      effectiveEnd = dayEnd;
    }

    // Get hours and minutes as decimal
    const startHour = effectiveStart.getHours() + effectiveStart.getMinutes() / 60;
    const endHour = effectiveEnd.getHours() + effectiveEnd.getMinutes() / 60;

    // Calculate position
    const top = (startHour - START_HOUR) * HOUR_HEIGHT;
    const height = (endHour - startHour) * HOUR_HEIGHT;

    return { top, height: Math.max(height, 30) }; // Minimum height of 30px
  };

  // Helper function to create event background that hides grid lines
  const getEventBackgroundColor = (color: string) => {
    // Convert hex to RGB
    const hex = color.replace('#', '');
    const r = parseInt(hex.substring(0, 2), 16);
    const g = parseInt(hex.substring(2, 4), 16);
    const b = parseInt(hex.substring(4, 6), 16);

    // Use linear-gradient with two layers:
    // 1. Semi-transparent color layer (20% opacity)
    // 2. Solid surface color layer underneath
    return `linear-gradient(rgba(${r}, ${g}, ${b}, 0.20), rgba(${r}, ${g}, ${b}, 0.20)), var(--color-surface)`;
  };

  // Generate hours array for grid
  const hours = useMemo(() => {
    return Array.from({ length: END_HOUR - START_HOUR }, (_, i) => i + START_HOUR);
  }, []);

  const isToday = (date: Date): boolean => {
    const today = new Date();
    return date.toDateString() === today.toDateString();
  };

  // Drag and drop handlers
  const handleDragStart = (event: EventDto, e: React.DragEvent) => {
    setDraggedEvent(event);
    e.dataTransfer.effectAllowed = 'move';

    // Calcular el offset Y dentro del elemento arrastrado
    const rect = e.currentTarget.getBoundingClientRect();
    const offsetY = e.clientY - rect.top;
    setDragOffsetY(offsetY);

    // Add visual feedback
    e.currentTarget.classList.add('dragging');
  };

  const handleDragEnd = (e: React.DragEvent) => {
    setDraggedEvent(null);
    setDragOverDate(null);
    setDragOffsetY(0);
    e.currentTarget.classList.remove('dragging');
  };

  const handleDragOver = (date: Date, e: React.DragEvent) => {
    e.preventDefault();
    e.dataTransfer.dropEffect = 'move';
    setDragOverDate(date);

    if (draggedEvent) {
      // Calcular la posición ajustada a intervalos de 15 minutos
      const rect = e.currentTarget.getBoundingClientRect();
      const mouseY = e.clientY - rect.top - dragOffsetY;

      // Calcular la hora basada en la posición
      const hour = START_HOUR + (mouseY / HOUR_HEIGHT);

      // Redondear a 15 minutos
      const totalMinutes = hour * 60;
      const roundedMinutes = Math.round(totalMinutes / 15) * 15;
      const snappedHour = Math.floor(roundedMinutes / 60);
      const snappedMinutes = roundedMinutes % 60;

      // Calcular la posición top en pixels para el snap
      const snappedTop = ((snappedHour - START_HOUR) * HOUR_HEIGHT) + (snappedMinutes / 60 * HOUR_HEIGHT);

      setDragPreviewTop(snappedTop);
    }
  };

  const handleDragLeave = () => {
    setDragOverDate(null);
    setDragPreviewTop(null);
  };

  const handleDrop = async (targetDate: Date, e: React.DragEvent) => {
    e.preventDefault();
    setDragOverDate(null);
    setDragPreviewTop(null);

    if (!draggedEvent) return;

    try {
      // Parse dates as UTC to preserve the original time
      const originalStart = new Date(draggedEvent.startDate);
      const originalEnd = new Date(draggedEvent.endDate);
      const duration = originalEnd.getTime() - originalStart.getTime();

      // Get target date in UTC
      const targetYear = targetDate.getFullYear();
      const targetMonth = String(targetDate.getMonth() + 1).padStart(2, '0');
      const targetDay = String(targetDate.getDate()).padStart(2, '0');
      const targetDateOnly = `${targetYear}-${targetMonth}-${targetDay}`;

      // Calcular la nueva hora basándose en la posición del drop
      const rect = e.currentTarget.getBoundingClientRect();
      const dropY = e.clientY - rect.top - dragOffsetY; // Restar el offset para obtener la posición del inicio de la tarjeta

      // Calculate the hour based on the top of the card position
      const droppedHour = START_HOUR + (dropY / HOUR_HEIGHT);

      // Round to nearest 15 minutes
      const roundedHour = Math.floor(droppedHour);
      const fractionalHour = droppedHour - roundedHour;
      const minutes = Math.round(fractionalHour * 60 / 15) * 15;

      // Create new start date with calculated time on target date
      const newStartDate = new Date(targetDate);
      newStartDate.setHours(roundedHour, minutes, 0, 0);

      // Calculate end date maintaining the duration
      const newEndDate = new Date(newStartDate.getTime() + duration);

      console.log('Event moved to', targetDateOnly, 'at', `${roundedHour}:${String(minutes).padStart(2, '0')}`);

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
      <div className="week-header-container">
        {/* Empty space for time column */}
        <div className="time-column-header"></div>

        {/* Days header */}
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
      </div>

      {/* All-Day Events Row */}
      <div className="all-day-events-container">
        {/* Time column label */}
        <div className="all-day-time-label">
          <span>Todo el día</span>
        </div>

        {/* All-day events grid with absolute positioning */}
        <div className="all-day-events-grid-wrapper">
          <div className="all-day-events-grid">
            {weekDates.map((_, dayIndex) => (
              <div
                key={dayIndex}
                className="all-day-column"
              />
            ))}
          </div>

          {/* Positioned all-day events */}
          <div className="all-day-events-layer">
            {(() => {
              // Get all unique all-day events for the week
              const allDayEventsSet = new Set<string>();
              const allDayEventsMap = new Map<string, { event: EventDto, startDay: number, endDay: number }>();

              weekDates.forEach((date) => {
                const dayEvents = getAllDayEvents(date);
                dayEvents.forEach(event => {
                  if (!allDayEventsSet.has(event.id)) {
                    allDayEventsSet.add(event.id);

                    const eventStart = new Date(event.startDate);
                    const eventEnd = new Date(event.endDate);
                    const eventStartDay = new Date(eventStart.getFullYear(), eventStart.getMonth(), eventStart.getDate());
                    const eventEndDay = new Date(eventEnd.getFullYear(), eventEnd.getMonth(), eventEnd.getDate());

                    // Calculate which day columns this event spans in the current week
                    let startDayIndex = -1;
                    let endDayIndex = -1;

                    weekDates.forEach((weekDate, idx) => {
                      const weekDay = new Date(weekDate.getFullYear(), weekDate.getMonth(), weekDate.getDate());

                      if (eventStartDay.getTime() <= weekDay.getTime() && eventEndDay.getTime() >= weekDay.getTime()) {
                        if (startDayIndex === -1) startDayIndex = idx;
                        endDayIndex = idx;
                      }
                    });

                    if (startDayIndex !== -1) {
                      allDayEventsMap.set(event.id, { event, startDay: startDayIndex, endDay: endDayIndex });
                    }
                  }
                });
              });

              // Render events with calculated positions
              return Array.from(allDayEventsMap.values()).map(({ event, startDay, endDay }) => {
                const categoryColor = event.eventCategory?.color || '#1447e6';
                const spanDays = endDay - startDay + 1;
                const widthPercentage = (spanDays * 100) / 7;
                const leftPercentage = (startDay * 100) / 7;

                const eventStart = new Date(event.startDate);
                const eventEnd = new Date(event.endDate);
                const eventStartDay = new Date(eventStart.getFullYear(), eventStart.getMonth(), eventStart.getDate());
                const eventEndDay = new Date(eventEnd.getFullYear(), eventEnd.getMonth(), eventEnd.getDate());
                const isMultiDay = eventStartDay.getTime() !== eventEndDay.getTime();

                return (
                  <div
                    key={event.id}
                    className={`all-day-event-card ${isMultiDay ? 'multi-day' : ''}`}
                    style={{
                      background: getEventBackgroundColor(categoryColor),
                      borderLeftColor: categoryColor,
                      color: categoryColor,
                      width: `calc(${widthPercentage}% - 8px)`,
                      left: `calc(${leftPercentage}% + 4px)`,
                    }}
                    onClick={() => onEventClick?.(event)}
                    role="button"
                    tabIndex={0}
                    aria-label={`Ver detalle del evento ${event.title}`}
                  >
                    <div className="event-title">{event.title}</div>
                    {isMultiDay && (
                      <div className="multi-day-indicator">
                        {spanDays} días
                      </div>
                    )}
                  </div>
                );
              });
            })()}
          </div>
        </div>
      </div>

      {/* Calendar Grid with Hours */}
      <div className="calendar-grid-container">
        {/* Time column */}
        <div className="time-column">
          {hours.map((hour) => (
            <div
              key={hour}
              className="time-slot"
              style={{ height: `${HOUR_HEIGHT}px` }}
            >
              <span className="time-label">
                {hour.toString().padStart(2, '0')}:00
              </span>
            </div>
          ))}
        </div>

        {/* Days grid */}
        <div className="calendar-grid-with-hours">
          {weekDates.map((date, dayIndex) => {
            const timedEvents = getTimedEvents(date);
            const eventsWithLayout = calculateEventLayout(timedEvents);

            return (
              <div
                key={dayIndex}
                className={`day-column-with-hours ${dragOverDate?.toDateString() === date.toDateString() ? 'drag-over' : ''}`}
                onDragOver={(e) => handleDragOver(date, e)}
                onDragLeave={handleDragLeave}
                onDrop={(e) => handleDrop(date, e)}
                onClick={(e) => {
                  // Solo crear evento si el click no fue en un evento existente
                  if ((e.target as HTMLElement).classList.contains('day-column-with-hours') ||
                    (e.target as HTMLElement).classList.contains('hour-line') ||
                    (e.target as HTMLElement).classList.contains('events-layer')) {
                    const rect = e.currentTarget.getBoundingClientRect();
                    const clickY = e.clientY - rect.top;

                    // Calcular la hora basada en la posición del click
                    const clickedHour = START_HOUR + (clickY / HOUR_HEIGHT);

                    // Redondear a la media hora más cercana
                    const roundedHour = Math.floor(clickedHour);
                    const minutes = clickY % HOUR_HEIGHT > HOUR_HEIGHT / 2 ? 30 : 0;

                    // Crear fecha con la hora estimada
                    const eventDate = new Date(date);
                    eventDate.setHours(roundedHour, minutes, 0, 0);

                    onAddEvent?.(eventDate);
                  }
                }}
                style={{ cursor: 'pointer' }}
              >
                {/* Hour grid lines */}
                {hours.map((hour) => (
                  <div
                    key={hour}
                    className="hour-line"
                    style={{ top: `${(hour - START_HOUR) * HOUR_HEIGHT}px` }}
                  />
                ))}

                {/* Events positioned by time */}
                <div className="events-layer">
                  {eventsWithLayout.map((event) => {
                    const categoryColor = event.eventCategory?.color || '#1447e6';
                    const priorityLevel = Math.min(Math.max(event.priority ?? 2, 1), 4);

                    // Detectar si es evento multi-día
                    const start = new Date(event.startDate);
                    const end = new Date(event.endDate);
                    const startDay = new Date(start.getFullYear(), start.getMonth(), start.getDate());
                    const endDay = new Date(end.getFullYear(), end.getMonth(), end.getDate());
                    const isMultiDay = startDay.getTime() !== endDay.getTime();

                    // Detectar si el evento continúa desde el día anterior o hacia el día siguiente
                    const currentDay = new Date(date.getFullYear(), date.getMonth(), date.getDate());
                    const continuesFromPrevious = startDay.getTime() < currentDay.getTime();
                    const continuesToNext = endDay.getTime() > currentDay.getTime();

                    // Get position in hour grid
                    const { top, height } = getEventPosition(event, date);

                    // Determinar la clase de tamaño según la altura
                    let sizeClass = '';
                    if (height < 40) {
                      sizeClass = 'very-small';
                    } else if (height < 60) {
                      sizeClass = 'small';
                    } else if (height < 80) {
                      sizeClass = 'medium';
                    }

                    // Calcular estilos para eventos superpuestos
                    const isOverlapping = event.totalColumns > 1;
                    const hasManyColumns = event.totalColumns > 3;
                    let width: string;
                    let left: string;

                    if (isOverlapping) {
                      // Eventos en paralelo: dividir el ancho equitativamente
                      const widthPercentage = 100 / event.totalColumns;
                      const leftPercentage = (event.column * 100) / event.totalColumns;

                      width = `${widthPercentage}%`;
                      left = `${leftPercentage}%`;
                    } else {
                      // Evento único: usar todo el ancho
                      width = '100%';
                      left = '0';
                    }

                    // Detectar si este evento está siendo arrastrado
                    const isBeingDragged = draggedEvent?.id === event.id;
                    const usePreviewPosition = isBeingDragged && dragPreviewTop !== null && dragOverDate?.toDateString() === date.toDateString();

                    // Usar la posición del preview si está siendo arrastrado en el mismo día
                    const displayTop = usePreviewPosition ? dragPreviewTop : top;

                    return (
                      <div
                        key={event.id}
                        className={`event-card-timed ${sizeClass} ${isMultiDay ? 'multi-day-event' : ''} ${isOverlapping ? 'overlapping' : ''} ${hasManyColumns ? 'many-columns' : ''} ${continuesFromPrevious ? 'continues-from-previous' : ''} ${continuesToNext ? 'continues-to-next' : ''} ${isBeingDragged ? 'being-dragged' : ''}`}
                        draggable
                        onDragStart={(e) => handleDragStart(event, e)}
                        onDragEnd={handleDragEnd}
                        style={{
                          background: getEventBackgroundColor(categoryColor),
                          borderLeftColor: categoryColor,
                          color: categoryColor,
                          top: `${displayTop}px`,
                          height: `${height}px`,
                          left: left,
                          width: width,
                          zIndex: isBeingDragged ? 999 : event.column + 2
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
                        {continuesFromPrevious && (
                          <div className="continuation-indicator top">
                            <svg width="12" height="8" viewBox="0 0 12 8" fill="currentColor">
                              <path d="M6 0L0 8h12L6 0z" />
                            </svg>
                          </div>
                        )}
                        <div className="event-title">{event.title}</div>
                        <div className="event-time">
                          {hasManyColumns ? getCompactTimeRange(event) : getEventTimeRange(event)}
                        </div>
                        {height > 45 && (
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
                        )}
                        {continuesToNext && (
                          <div className="continuation-indicator bottom">
                            <svg width="12" height="8" viewBox="0 0 12 8" fill="currentColor">
                              <path d="M6 8L0 0h12L6 8z" />
                            </svg>
                          </div>
                        )}
                      </div>
                    );
                  })}
                </div>
              </div>
            );
          })}
        </div>
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