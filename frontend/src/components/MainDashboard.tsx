import { Heart } from 'lucide-react';
import React, { useEffect, useState } from 'react';
import { useEvents } from '../context/EventsContext';
import type { SelfCareFeedbackDto } from '../services/apiService';
import { apiService, SelfCareFeedbackAction, type EventCategoryDto, type EventDto, type SelfCareRecommendationDto, type UpdateEventMoodDto } from '../services/apiService';
import RecommendationAssistant from './Assistant/RecommendationAssistant';
import AuroraMonthlyCalendar from './AuroraMonthlyCalendar';
import AuroraWeeklyCalendar from './AuroraWeeklyCalendar';
import EventDetailModal from './EventDetailModal';
import { EventFormModal } from './EventFormModal';
import { FloatingNLPInput } from './FloatingNLPInput';
import './MainDashboard.css';
import MonthlyMoodTracker from './MonthlyMoodTracker';
import Navigation from './Navigation';
import { ProductivityAnalysisPanel } from './ProductivityAnalysisPanel';
import { ScheduleSuggestionsPanel } from './ScheduleSuggestionsPanel';
import SelfCareModal from './SelfCareModal';
import SelfCareTimer from './SelfCareTimer';
import SettingsScreen from './Settings/SettingsScreen';
import WellnessDashboard from './WellnessDashboard';

type SelfCareEventPrefill = {
  title?: string;
  description?: string;
  durationMinutes?: number;
};

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

  // Self-care states
  const [isSelfCareModalOpen, setIsSelfCareModalOpen] = useState(false);
  const [selfCareTimer, setSelfCareTimer] = useState<SelfCareRecommendationDto | null>(null);
  const [selfCarePrefill, setSelfCarePrefill] = useState<SelfCareEventPrefill | null>(null);

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
    setSelfCarePrefill(null);
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
    setSelfCarePrefill(null);
  };

  const handleEventUpdated = () => {
    setIsEventFormOpen(false);
    setEventBeingEdited(null);
    setSelfCarePrefill(null);
    refreshEvents();
  };

  const handleCloseEventForm = () => {
    setIsEventFormOpen(false);
    setEventBeingEdited(null);
    setSelectedDate(undefined);
    setSelfCarePrefill(null);
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

  // Self-care handlers
  const handleScheduleActivity = (recommendation: SelfCareRecommendationDto) => {
    const suggestedStart = recommendation.suggestedDateTime
      ? new Date(recommendation.suggestedDateTime)
      : new Date();
    const startDate = Number.isNaN(suggestedStart.getTime()) ? new Date() : suggestedStart;

    const descriptionPieces = [recommendation.description?.trim()];
    if (recommendation.personalizedReason) {
      descriptionPieces.push(`Motivo sugerido: ${recommendation.personalizedReason.trim()}`);
    }

    setSelfCarePrefill({
      title: recommendation.title,
      description: descriptionPieces.filter(Boolean).join('\n\n'),
      durationMinutes: recommendation.durationMinutes
    });

    setSelectedDate(startDate);
    setEventFormMode('create');
    setEventBeingEdited(null);
    setIsEventFormOpen(true);
  };

  const handleStartTimer = (recommendation: SelfCareRecommendationDto) => {
    setSelfCareTimer(recommendation);
  };

  const handleTimerComplete = async (moodAfter: number) => {
    if (selfCareTimer) {
      const feedback: SelfCareFeedbackDto = {
        recommendationId: selfCareTimer.id,
        action: SelfCareFeedbackAction.CompletedNow,
        moodAfter: moodAfter > 0 ? moodAfter : undefined,
        timestamp: new Date().toISOString()
      };

      try {
        await apiService.registerSelfCareFeedback(feedback);
      } catch (error) {
        console.error('Error registering feedback:', error);
      }
    }

    setSelfCareTimer(null);
  };

  const handleTimerCancel = () => {
    setSelfCareTimer(null);
  };

  // Keyboard shortcut for self-care (Alt+C)
  useEffect(() => {
    const handleKeyPress = (event: KeyboardEvent) => {
      if (event.altKey && event.key.toLowerCase() === 'c') {
        event.preventDefault();
        setIsSelfCareModalOpen(true);
      }
    };

    window.addEventListener('keydown', handleKeyPress);
    return () => window.removeEventListener('keydown', handleKeyPress);
  }, []);

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
        prefillData={selfCarePrefill ?? undefined}
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

      {/* Self-Care Modal */}
      <SelfCareModal
        isOpen={isSelfCareModalOpen}
        onClose={() => setIsSelfCareModalOpen(false)}
        onScheduleActivity={handleScheduleActivity}
        onStartTimer={handleStartTimer}
      />

      {/* Self-Care Timer */}
      {selfCareTimer && (
        <SelfCareTimer
          recommendation={selfCareTimer}
          onComplete={handleTimerComplete}
          onCancel={handleTimerCancel}
        />
      )}

      {/* Self-Care FAB */}
      <button
        onClick={() => setIsSelfCareModalOpen(true)}
        className="fixed bottom-28 right-4 sm:bottom-[4.5rem] sm:right-4 w-12 h-12 rounded-full shadow-lg hover:shadow-xl transition-all duration-300 z-40 group flex items-center justify-center border-0"
        style={{
          background: 'linear-gradient(135deg, var(--gradient-primary-start) 0%, var(--gradient-primary-end) 100%)'
        }}
        title="Autocuidado (Alt+C)"
        aria-label="Abrir sugerencias de autocuidado"
      >
        <Heart className="h-5 w-5 text-white" fill="currentColor" />
        <span className="absolute -top-1 -right-1 bg-white text-purple-700 text-xs font-bold rounded-full h-5 w-5 flex items-center justify-center group-hover:scale-110 transition-transform shadow-sm">
          5
        </span>
      </button>
    </div>
  );
};

export default MainDashboard;