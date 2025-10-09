import { Filter } from 'lucide-react';
import React, { useEffect, useState } from 'react';
import type { EventDto, WeeklyEventsResponseDto } from '../services/apiService';
import { apiService } from '../services/apiService';
import './WeeklyCalendar.css';

interface WeeklyCalendarProps {
  userId?: string;
}

const WeeklyCalendar: React.FC<WeeklyCalendarProps> = ({ userId }) => {
  // Simple Button component for this file
  interface ButtonProps {
    variant?: 'outline' | 'default';
    size?: 'sm' | 'default';
    onClick?: () => void;
    className?: string;
    children: React.ReactNode;
  }

  const Button: React.FC<ButtonProps> = ({
    variant = 'default',
    size = 'default',
    onClick,
    className = '',
    children
  }) => {
    const baseClasses = 'inline-flex items-center justify-center rounded-md font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:opacity-50 disabled:pointer-events-none';
    const variantClasses = variant === 'outline'
      ? 'border border-input hover:bg-accent hover:text-accent-foreground'
      : 'bg-primary text-primary-foreground hover:bg-primary/90';
    const sizeClasses = size === 'sm' ? 'h-9 px-3' : 'h-10 py-2 px-4';

    return (
      <button
        className={`${baseClasses} ${variantClasses} ${sizeClasses} ${className}`}
        onClick={onClick}
      >
        {children}
      </button>
    );
  };
  // State management
  const [weeklyData, setWeeklyData] = useState<WeeklyEventsResponseDto | null>(null);
  const [currentWeekStart, setCurrentWeekStart] = useState<Date>(new Date());
  const [loading, setLoading] = useState<boolean>(false);
  const [error, setError] = useState<string>('');
  const [showFilters, setShowFilters] = useState<boolean>(false);

  // Get Monday of current week
  const getMondayOfWeek = (date: Date): Date => {
    const monday = new Date(date);
    const dayOfWeek = monday.getDay();
    const diff = dayOfWeek === 0 ? -6 : 1 - dayOfWeek; // Sunday = 0, Monday = 1
    monday.setDate(monday.getDate() + diff);
    monday.setHours(0, 0, 0, 0);
    return monday;
  };

  // Initialize with current week's Monday
  useEffect(() => {
    setCurrentWeekStart(getMondayOfWeek(new Date()));
  }, []);

  // Generate week days array
  const weekDays = useMemo(() => {
    const days = [];
    for (let i = 0; i < 7; i++) {
      const date = new Date(currentWeekStart);
      date.setDate(currentWeekStart.getDate() + i);
      days.push(date);
    }
    return days;
  }, [currentWeekStart]);

  // Load weekly events
  const loadWeeklyEvents = async (weekStart: Date) => {
    setLoading(true);
    setError('');

    try {
      console.log('Loading weekly events for:', weekStart.toISOString());
      const weekStartISO = weekStart.toISOString().split('T')[0]; // Format: YYYY-MM-DD
      const response = await apiService.getWeeklyEvents(weekStartISO, userId);
      setWeeklyData(response);
      console.log('Weekly events loaded successfully:', response);
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Error desconocido';
      setError(`Error cargando eventos: ${errorMessage}`);
      console.error('Error loading weekly events:', err);
    } finally {
      setLoading(false);
    }
  };

  // Load events when week changes
  useEffect(() => {
    if (currentWeekStart) {
      loadWeeklyEvents(currentWeekStart);
    }
  }, [currentWeekStart, userId]);

  // Navigation functions
  const goToPreviousWeek = () => {
    const newDate = new Date(currentWeekStart);
    newDate.setDate(newDate.getDate() - 7);
    setCurrentWeekStart(newDate);
  };

  const goToNextWeek = () => {
    const newDate = new Date(currentWeekStart);
    newDate.setDate(newDate.getDate() + 7);
    setCurrentWeekStart(newDate);
  };

  const goToCurrentWeek = () => {
    setCurrentWeekStart(getMondayOfWeek(new Date()));
  };

  // Get events for specific day
  const getEventsForDay = (date: Date): EventDto[] => {
    if (!weeklyData?.events) return [];

    const dayStart = new Date(date);
    dayStart.setHours(0, 0, 0, 0);

    const dayEnd = new Date(date);
    dayEnd.setHours(23, 59, 59, 999);

    return weeklyData.events.filter(event => {
      const eventStart = new Date(event.startDate);
      return eventStart >= dayStart && eventStart <= dayEnd;
    });
  };

  // Format time for display
  const formatTime = (dateString: string): string => {
    return new Date(dateString).toLocaleTimeString('es-ES', {
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  // Check if date is today
  const isToday = (date: Date): boolean => {
    const today = new Date();
    return date.toDateString() === today.toDateString();
  };

  // Get current week display
  const getWeekDisplay = (): string => {
    const weekEnd = new Date(currentWeekStart);
    weekEnd.setDate(currentWeekStart.getDate() + 6);

    const startStr = currentWeekStart.toLocaleDateString('es-ES', {
      day: 'numeric',
      month: 'short'
    });
    const endStr = weekEnd.toLocaleDateString('es-ES', {
      day: 'numeric',
      month: 'short',
      year: 'numeric'
    });

    return `${startStr} - ${endStr}`;
  };

  if (loading && !weeklyData) {
    return (
      <div className="weekly-calendar">
        <div className="loading-container">
          <div className="loading-spinner"></div>
          <p>Cargando calendario...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="weekly-calendar">
      {/* Header with navigation */}
      <div className="calendar-header">
        <div className="calendar-nav">
          <button
            className="nav-button"
            onClick={goToPreviousWeek}
            disabled={loading}
            aria-label="Semana anterior"
          >
            ←
          </button>

          <div className="week-display">
            <h2>{getWeekDisplay()}</h2>
            <button
              className="today-button"
              onClick={goToCurrentWeek}
              disabled={loading}
            >
              Hoy
            </button>
          </div>

          <button
            className="nav-button"
            onClick={goToNextWeek}
            disabled={loading}
            aria-label="Semana siguiente"
          >
            →
          </button>
        </div>

        {loading && (
          <div className="loading-indicator">
            <div className="loading-spinner small"></div>
            <span>Actualizando...</span>
          </div>
        )}
      </div>

      {/* Filter button */}
      <div className="calendar-filters">
        <Button
          variant="outline"
          size="sm"
          onClick={() => setShowFilters(!showFilters)}
          className={showFilters ? 'bg-primary text-primary-foreground' : ''}
        >
          <Filter className="w-4 h-4" />
        </Button>
      </div>

      {/* Error display */}
      {error && (
        <div className="error-message">
          <span className="error-icon">⚠️</span>
          <p>{error}</p>
          <button
            className="retry-button"
            onClick={() => loadWeeklyEvents(currentWeekStart)}
          >
            Reintentar
          </button>
        </div>
      )}

      {/* Calendar grid */}
      <div className="calendar-grid">
        {weekDays.map((date, index) => {
          const dayEvents = getEventsForDay(date);
          const dayName = ['Lun', 'Mar', 'Mié', 'Jue', 'Vie', 'Sáb', 'Dom'][index];

          return (
            <div
              key={date.toISOString()}
              className={`calendar-day ${isToday(date) ? 'today' : ''}`}
            >
              {/* Day header */}
              <div className="day-header">
                <span className="day-name">{dayName}</span>
                <span className="day-number">{date.getDate()}</span>
              </div>

              {/* Events list */}
              <div className="day-events">
                {dayEvents.length === 0 ? (
                  <div className="no-events">
                    <span>Sin eventos</span>
                  </div>
                ) : (
                  dayEvents.map((event) => (
                    <div
                      key={event.id}
                      className="event-item"
                      style={{
                        borderLeftColor: event.eventCategory.color || '#3b82f6'
                      }}
                    >
                      <div className="event-title">{event.title}</div>
                      {!event.isAllDay && (
                        <div className="event-time">
                          {formatTime(event.startDate)}
                          {event.startDate !== event.endDate &&
                            ` - ${formatTime(event.endDate)}`
                          }
                        </div>
                      )}
                      {event.isAllDay && (
                        <div className="event-all-day">Todo el día</div>
                      )}
                      <div className="event-category">
                        <span
                          className="category-dot"
                          style={{ backgroundColor: event.eventCategory.color }}
                        ></span>
                        {event.eventCategory.name}
                      </div>
                      {event.location && (
                        <div className="event-location">📍 {event.location}</div>
                      )}
                    </div>
                  ))
                )}
              </div>
            </div>
          );
        })}
      </div>

      {/* Categories legend */}
      {weeklyData?.categories && weeklyData.categories.length > 0 && (
        <div className="categories-legend">
          <h3>Categorías:</h3>
          <div className="categories-list">
            {weeklyData.categories.map((category) => (
              <div key={category.id} className="category-item">
                <span
                  className="category-color"
                  style={{ backgroundColor: category.color }}
                ></span>
                <span className="category-name">{category.name}</span>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Summary */}
      {weeklyData && (
        <div className="week-summary">
          <p>
            📅 {weeklyData.events.length} evento{weeklyData.events.length !== 1 ? 's' : ''} esta semana
            {weeklyData.categories.length > 0 &&
              ` • ${weeklyData.categories.length} categoría${weeklyData.categories.length !== 1 ? 's' : ''} disponible${weeklyData.categories.length !== 1 ? 's' : ''}`
            }
          </p>
        </div>
      )}
    </div>
  );
};

export default WeeklyCalendar;