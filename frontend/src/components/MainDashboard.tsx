import React, { useCallback, useEffect, useImperativeHandle, useState } from 'react';
import { apiService, type EventCategoryDto, type EventDto } from '../services/apiService';
import AuroraMonthlyCalendar from './AuroraMonthlyCalendar';
import AuroraWeeklyCalendar from './AuroraWeeklyCalendar';
import EventDetailModal from './EventDetailModal';
import { EventFormModal } from './EventFormModal';
import { FloatingNLPInput } from './FloatingNLPInput';
import './MainDashboard.css';
import Navigation from './Navigation';
import SettingsScreen from './Settings/SettingsScreen';

interface MainDashboardRef {
  openEventById: (eventId: string) => Promise<void>;
}

interface MainDashboardProps {
  onViewEvent?: (openEventById: (eventId: string) => Promise<void>) => void;
}

const MainDashboard = React.forwardRef<MainDashboardRef, MainDashboardProps>(({ onViewEvent }, ref) => {
  const [activeView, setActiveView] = useState('calendar-week');
  const [refreshToken, setRefreshToken] = useState(0);
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

  const bumpRefreshToken = () => {
    setRefreshToken(prev => prev + 1);
  };

  const openEventById = useCallback(async (eventId: string) => {
    try {
      console.log('Abriendo evento por ID:', eventId);
      setDetailError('');

      // Obtener el evento por ID
      const event = await apiService.getEvent(eventId);

      // Abrir el modal de detalles
      setSelectedEvent(event);
      setIsDetailModalOpen(true);
    } catch (error) {
      console.error('Error al cargar evento:', error);
      setDetailError('No se pudo cargar el evento');
    }
  }, []);

  // Exponer la función openEventById a través del ref
  useImperativeHandle(ref, () => ({
    openEventById
  }), [openEventById]);

  // Informar al componente padre sobre la función openEventById
  useEffect(() => {
    if (onViewEvent) {
      onViewEvent(openEventById);
    }
  }, [onViewEvent, openEventById]); const handleViewChange = (view: string) => {
    setActiveView(view);
    console.log('Changing view to:', view);
  };

  const handleCategoryChange = (categoryId: string | null) => {
    setSelectedCategoryId(categoryId);
    console.log('Category filter changed:', categoryId);
  };

  const handleEventCreated = () => {
    console.log('Evento creado - refrescando calendario');
    bumpRefreshToken();
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
    bumpRefreshToken();
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
      bumpRefreshToken();
    } catch (error) {
      console.error('Error deleting event:', error);
      const message = error instanceof Error ? error.message : 'Error al eliminar el evento';
      setDetailError(message);
    } finally {
      setIsDeletingEvent(false);
    }
  };

  const showCalendarContainer = !['wellness', 'assistant', 'settings'].includes(activeView);
  const calendarView = activeView === 'calendar-month' ? 'calendar-month' : 'calendar-week';

  const renderPlaceholderContent = () => {
    switch (activeView) {
      case 'wellness':
        return (
          <div className="placeholder-view">
            <h2>Dashboard de Bienestar</h2>
            <p>Esta vista estará disponible pronto</p>
          </div>
        );
      case 'assistant':
        return (
          <div className="placeholder-view">
            <h2>Asistente IA</h2>
            <p>Esta vista estará disponible pronto</p>
          </div>
        );
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

        {!showCalendarContainer && renderPlaceholderContent()}
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
        isDeleting={isDeletingEvent}
        errorMessage={detailError}
      />
    </div>
  );
});

MainDashboard.displayName = 'MainDashboard';

export default MainDashboard;