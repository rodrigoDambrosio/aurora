import React, { useEffect, useState } from 'react';
import { useEvents } from '../context/EventsContext';
import { apiService, type EventCategoryDto, type EventDto, type UpdateEventMoodDto } from '../services/apiService';
import RecommendationAssistant from './Assistant/RecommendationAssistant';
import AuroraMonthlyCalendar from './AuroraMonthlyCalendar';
import AuroraWeeklyCalendar from './AuroraWeeklyCalendar';
import EventDetailModal from './EventDetailModal';
import { EventFormModal } from './EventFormModal';
import { FloatingNLPInput } from './FloatingNLPInput';
import './MainDashboard.css';
import MonthlyMoodTracker from './MonthlyMoodTracker';
import Navigation from './Navigation';
import { ScheduleSuggestionsPanel } from './ScheduleSuggestionsPanel';
import SettingsScreen from './Settings/SettingsScreen';
import WellnessDashboard from './WellnessDashboard';
import { ProductivityAnalysisPanel } from './ProductivityAnalysisPanel';

const MainDashboard: React.FC = () => {
  const [activeView, setActiveView] = useState('calendar-week');
  const { refreshToken, refreshEvents } = useEvents();
  const [isEventFormOpen, setIsEventFormOpen] = useState(false);
  const [selectedDate, setSelectedDate] = useState<Date | undefined>();
  const [selectedCategoryId, setSelectedCategoryId] = useState<string | null>(null);
  const [categories, setCategories] = useState<EventCategoryDto[]>([]);
  const [showFilters, setShowFilters] = useState(true);
  const [selectedEvent, setSelectedEvent] = useState<EventDto | null>(null);
  const [isDetailModalOpen, setIsDetailModalOpen] = useState(false);
  const [eventFormMode, setEventFormMode] = useState<'create' | 'edit'>('create');
  const [eventBeingEdited, setEventBeingEdited] = useState<EventDto | null>(null);
  const [isDeletingEvent, setIsDeletingEvent] = useState(false);
  const [detailError, setDetailError] = useState('');
  const [firstDayOfWeek, setFirstDayOfWeek] = useState<number>(1); // Default to Monday (1)

  // Load user preferences on component mount
  useEffect(() => {
    const loadUserPreferences = async () => {
      try {
        const preferences = await apiService.getUserPreferences();
        if (preferences.firstDayOfWeek !== undefined) {
          setFirstDayOfWeek(preferences.firstDayOfWeek);
          console.log('Loaded user preference - firstDayOfWeek:', preferences.firstDayOfWeek);
        }
      } catch (error) {
        console.error('Error loading user preferences:', error);
        // Keep default value (Monday) if there's an error
      }
    };

    loadUserPreferences();
  }, []);

  const handleViewChange = (view: string) => {
    setActiveView(view);
    console.log('Changing view to:', view);
  };

  const handleCategoryChange = (categoryId: string | null) => {
    setSelectedCategoryId(categoryId);
    console.log('Category filter changed:', categoryId);
  };

  const handleEventCreated = () => {
    console.log('Evento creado - refrescando calendario');
    refreshEvents();
  };

  const handleEventClick = (event: EventDto) => {
    setSelectedEvent(event);
    setDetailError('');
    setIsDetailModalOpen(true);
  };

  const handleAddEvent = (date: Date) => {
    console.log('Adding event for date:', date);
    setSelectedDate(date);
    setEventFormMode('create');
    setEventBeingEdited(null);
    setIsEventFormOpen(true);
  };

  const handleCategoriesLoaded = (loadedCategories: EventCategoryDto[]) => {
    setCategories(loadedCategories);
  };

  const closeEventDetailModal = () => {
    setIsDetailModalOpen(false);
    setSelectedEvent(null);
  };

  const handleEditEvent = (event: EventDto) => {
    setEventFormMode('edit');
    setEventBeingEdited(event);
    setIsEventFormOpen(true);
    setIsDetailModalOpen(false);
    setSelectedDate(undefined);
  };

  const handleEventUpdated = () => {
    setIsEventFormOpen(false);
    setEventBeingEdited(null);
    refreshEvents();
  };

  const handleCloseEventForm = () => {
    setIsEventFormOpen(false);
    setEventBeingEdited(null);
    setSelectedDate(undefined);
  };

  const handleDeleteEvent = async (event: EventDto) => {
    const confirmed = window.confirm('¿Eliminar este evento? Esta acción no se puede deshacer.');
    if (!confirmed) {
      return;
    }

    try {
      setIsDeletingEvent(true);
      setDetailError('');
      await apiService.deleteEvent(event.id);
      closeEventDetailModal();
      refreshEvents();
    } catch (error) {
      console.error('Error deleting event:', error);
      const message = error instanceof Error ? error.message : 'Error al eliminar el evento';
      setDetailError(message);
    } finally {
      setIsDeletingEvent(false);
    }
  };

  const handleUpdateEventMood = async (eventId: string, mood: UpdateEventMoodDto) => {
    try {
      const updatedEvent = await apiService.updateEventMood(eventId, mood);
      setSelectedEvent(updatedEvent);
      refreshEvents();
      return updatedEvent;
    } catch (error) {
      const message = error instanceof Error ? error.message : 'No se pudo guardar el estado de ánimo';
      throw new Error(message);
    }
  };

  const showCalendarContainer = activeView === 'calendar-week' || activeView === 'calendar-month';
  const calendarView = activeView === 'calendar-month' ? 'calendar-month' : 'calendar-week';
  const showMonthlyMood = activeView === 'mood-month';
  const showWellness = activeView === 'wellness';
  const showProductivity = activeView === 'productivity';
  const showAssistant = activeView === 'assistant';
  const showPlaceholder = !showCalendarContainer && !showMonthlyMood && !showWellness && !showProductivity && !showAssistant;

  const renderPlaceholderContent = () => {
    switch (activeView) {
      case 'suggestions':
        return <ScheduleSuggestionsPanel onSuggestionAccepted={refreshEvents} />;
      case 'settings':
        return <SettingsScreen />;
      default:
        return null;
    }
  };

  return (
    <div className="main-dashboard">
      <Navigation
        activeView={activeView}
        onViewChange={handleViewChange}
      />
      <main className="dashboard-content">
        <div
          className={`calendar-views-container ${showCalendarContainer ? 'is-active' : 'is-hidden'}`}
          aria-hidden={!showCalendarContainer}
        >
          <section
            className={`calendar-view-panel ${calendarView === 'calendar-week' ? 'is-active' : ''}`}
            aria-hidden={calendarView !== 'calendar-week'}
          >
            <AuroraWeeklyCalendar
              onEventClick={handleEventClick}
              onAddEvent={handleAddEvent}
              selectedCategoryId={selectedCategoryId}
              onCategoriesLoaded={handleCategoriesLoaded}
              showFilters={showFilters}
              onToggleFilters={() => setShowFilters(!showFilters)}
              categories={categories}
              onCategoryChange={handleCategoryChange}
              refreshToken={refreshToken}
              firstDayOfWeek={firstDayOfWeek}
            />
          </section>
          <section
            className={`calendar-view-panel ${calendarView === 'calendar-month' ? 'is-active' : ''}`}
            aria-hidden={calendarView !== 'calendar-month'}
          >
            <AuroraMonthlyCalendar
              onEventClick={handleEventClick}
              onAddEvent={handleAddEvent}
              selectedCategoryId={selectedCategoryId}
              onCategoriesLoaded={handleCategoriesLoaded}
              showFilters={showFilters}
              onToggleFilters={() => setShowFilters(!showFilters)}
              categories={categories}
              onCategoryChange={handleCategoryChange}
              refreshToken={refreshToken}
              firstDayOfWeek={firstDayOfWeek}
            />
          </section>
        </div>

        {showMonthlyMood && (
          <section className="mood-view-panel" aria-label="Seguimiento mensual de ánimo">
            <MonthlyMoodTracker firstDayOfWeek={firstDayOfWeek} />
          </section>
        )}

        {showWellness && (
          <section className="wellness-view-panel" aria-label="Dashboard de bienestar">
            <WellnessDashboard />
          </section>
        )}

        {showAssistant && (
          <section className="assistant-view-panel" aria-label="Asistente de recomendaciones">
            <RecommendationAssistant />
          </section>
        )}

        {showProductivity && (
          <section className="productivity-view-panel" aria-label="Análisis de productividad">
            <ProductivityAnalysisPanel />
          </section>
        )}

        {showPlaceholder && renderPlaceholderContent()}
      </main>

      {/* NLP Input */}
      <FloatingNLPInput onEventCreated={handleEventCreated} />

      {/* Event Form Modal */}
      <EventFormModal
        isOpen={isEventFormOpen}
        onClose={handleCloseEventForm}
        onEventCreated={handleEventCreated}
        onEventUpdated={handleEventUpdated}
        initialDate={selectedDate}
        mode={eventFormMode}
        eventToEdit={eventBeingEdited}
      />

      {/* Event Detail Modal */}
      <EventDetailModal
        isOpen={isDetailModalOpen}
        event={selectedEvent}
        onClose={closeEventDetailModal}
        onEdit={handleEditEvent}
        onDelete={handleDeleteEvent}
        onUpdateMood={handleUpdateEventMood}
        isDeleting={isDeletingEvent}
        errorMessage={detailError}
      />
    </div>
  );
};

export default MainDashboard;