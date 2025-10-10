import { Filter } from 'lucide-react';
import React, { useEffect, useMemo, useState } from 'react';
import type { EventDto, WeeklyEventsResponseDto } from '../services/apiService';
import { apiService } from '../services/apiService';
import './AuroraWeeklyCalendar.css';

interface AuroraWeeklyCalendarProps {
  onEventClick?: (event: EventDto) => void;
  onAddEvent?: (date: Date) => void;
}

const AuroraWeeklyCalendar: React.FC<AuroraWeeklyCalendarProps> = ({
  onEventClick,
  onAddEvent
}) => {
  const [currentDate, setCurrentDate] = useState(new Date());
  const [showFilters, setShowFilters] = useState(false);
  const [weeklyData, setWeeklyData] = useState<WeeklyEventsResponseDto | null>(null);
  const [loading, setLoading] = useState<boolean>(false);
  const [error, setError] = useState<string>('');

  // Get start of the week (Monday)
  const getWeekStart = (date: Date): Date => {
    const d = new Date(date);
    const day = d.getDay();
    const diff = d.getDate() - day + (day === 0 ? -6 : 1);
    return new Date(d.setDate(diff));
  };

  const weekStart = useMemo(() => getWeekStart(currentDate), [currentDate]);

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
      const response = await apiService.getWeeklyEvents(weekStartISO);
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

  // Load events when component mounts or current date changes
  useEffect(() => {
    loadWeeklyEvents(weekStart);
  }, [weekStart]);

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
  const formatMonth = (date: Date): string => {
    const months = [
      'enero', 'febrero', 'marzo', 'abril', 'mayo', 'junio',
      'julio', 'agosto', 'septiembre', 'octubre', 'noviembre', 'diciembre'
    ];
    return `${months[date.getMonth()]} ${date.getFullYear()}`;
  };

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
    const startTime = formatTime(event.startDate);
    const endTime = formatTime(event.endDate);
    return `${startTime} - ${endTime}`;
  };

  const getEventsByDate = (date: Date): EventDto[] => {
    if (!weeklyData?.events) return [];

    const dateStr = date.toISOString().split('T')[0];
    return weeklyData.events.filter((event: EventDto) =>
      event.startDate.startsWith(dateStr)
    );
  };

  const getCategoryColor = (event: EventDto): { bg: string, border: string, text: string } => {
    const category = event.eventCategory;
    if (!category) return { bg: '#e4edff', border: '#1447e6', text: '#1447e6' };

    // Category color mapping based on Aurora design
    const colorMap: Record<string, { bg: string, border: string, text: string }> = {
      'trabajo': { bg: '#dbeafe', border: '#1447e6', text: '#1447e6' },
      'personal': { bg: '#fef3c7', border: '#ca3500', text: '#ca3500' },
      'estudio': { bg: '#dcfce7', border: '#008236', text: '#008236' },
      'salud': { bg: '#ffedd4', border: '#ca3500', text: '#ca3500' },
      'ejercicio': { bg: '#ffedd4', border: '#ca3500', text: '#ca3500' },
    };

    return colorMap[category.name.toLowerCase()] || { bg: '#e4edff', border: '#1447e6', text: '#1447e6' };
  };

  const isToday = (date: Date): boolean => {
    const today = new Date();
    return date.toDateString() === today.toDateString();
  };

  const getWeekNumber = (date: Date): number => {
    const d = new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()));
    const dayNum = d.getUTCDay() || 7;
    d.setUTCDate(d.getUTCDate() + 4 - dayNum);
    const yearStart = new Date(Date.UTC(d.getUTCFullYear(), 0, 1));
    return Math.ceil((((d.getTime() - yearStart.getTime()) / 86400000) + 1) / 7);
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
      <div className="calendar-header">
        <div className="header-controls">
          <div className="nav-controls">
            <button className="nav-btn" onClick={goToPreviousWeek}>
              <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
                <path d="M10 12l-4-4 4-4" stroke="currentColor" strokeWidth="2" fill="none" />
              </svg>
            </button>
            <button className="nav-btn" onClick={goToNextWeek}>
              <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
                <path d="M6 4l4 4-4 4" stroke="currentColor" strokeWidth="2" fill="none" />
              </svg>
            </button>
            <h2 className="month-title">{formatMonth(currentDate)}</h2>
          </div>

          <div className="action-controls">
            <button className="today-btn" onClick={goToToday}>
              Hoy
            </button>
            <button
              className={`settings-btn ${showFilters ? 'bg-primary text-primary-foreground' : ''}`}
              onClick={() => setShowFilters(!showFilters)}
            >
              <Filter className="w-4 h-4" />
            </button>
            <button className="add-event-btn" onClick={() => onAddEvent?.(new Date())}>
              <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
                <path d="M8 3v10M3 8h10" stroke="currentColor" strokeWidth="2" />
              </svg>
              Evento
            </button>
          </div>
        </div>

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
      </div>

      {/* Calendar Grid */}
      <div className="calendar-grid">
        {weekDates.map((date, dayIndex) => {
          const dayEvents = getEventsByDate(date);

          return (
            <div key={dayIndex} className="day-column">
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
                    const colors = getCategoryColor(event);

                    return (
                      <div
                        key={event.id}
                        className="event-card"
                        style={{
                          backgroundColor: colors.bg,
                          borderLeftColor: colors.border,
                          color: colors.text
                        }}
                        onClick={() => onEventClick?.(event)}
                      >
                        <div className="event-title">{event.title}</div>
                        <div className="event-time">{getEventTimeRange(event)}</div>
                        <div className="event-priority">
                          {Array.from({ length: 4 }).map((_, i) => (
                            <svg key={i} width="12" height="12" viewBox="0 0 12 12" fill="none">
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
        <div className="week-badge">
          Semana {getWeekNumber(currentDate)}
        </div>
      </div>
    </div>
  );
};

export default AuroraWeeklyCalendar;