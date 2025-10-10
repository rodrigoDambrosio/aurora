import React, { useState } from 'react';
import type { EventCategoryDto, EventDto } from '../services/apiService';
import AuroraMonthlyCalendar from './AuroraMonthlyCalendar';
import AuroraWeeklyCalendar from './AuroraWeeklyCalendar';
import { EventFormModal } from './EventFormModal';
import { FloatingNLPInput } from './FloatingNLPInput';
import './MainDashboard.css';
import Navigation from './Navigation';

const MainDashboard: React.FC = () => {
  const [activeView, setActiveView] = useState('calendar-week');
  const [refreshKey, setRefreshKey] = useState(0);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [selectedDate, setSelectedDate] = useState<Date | undefined>();
  const [selectedCategoryId, setSelectedCategoryId] = useState<string | null>(null);
  const [categories, setCategories] = useState<EventCategoryDto[]>([]);
  const [showFilters, setShowFilters] = useState(true);

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
    // Forzar re-render del calendario incrementando la key
    setRefreshKey(prev => prev + 1);
  };

  const handleEventClick = (event: EventDto) => {
    console.log('Event clicked:', event);
    // TODO: Open event details modal or navigate to event page
  };

  const handleAddEvent = (date: Date) => {
    console.log('Adding event for date:', date);
    setSelectedDate(date);
    setIsModalOpen(true);
  };

  const handleCategoriesLoaded = (loadedCategories: EventCategoryDto[]) => {
    setCategories(loadedCategories);
  };

  const renderMainContent = () => {
    switch (activeView) {
      case 'calendar-week':
        return (
          <AuroraWeeklyCalendar
            key={refreshKey}
            onEventClick={handleEventClick}
            onAddEvent={handleAddEvent}
            selectedCategoryId={selectedCategoryId}
            onCategoriesLoaded={handleCategoriesLoaded}
            showFilters={showFilters}
            onToggleFilters={() => setShowFilters(!showFilters)}
            categories={categories}
            onCategoryChange={handleCategoryChange}
          />
        );
      case 'calendar-month':
        return (
          <AuroraMonthlyCalendar
            key={refreshKey}
            onEventClick={handleEventClick}
            onAddEvent={handleAddEvent}
            selectedCategoryId={selectedCategoryId}
            onCategoriesLoaded={handleCategoriesLoaded}
            showFilters={showFilters}
            onToggleFilters={() => setShowFilters(!showFilters)}
            categories={categories}
            onCategoryChange={handleCategoryChange}
          />
        );
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
        return (
          <div className="placeholder-view">
            <h2>Configuración</h2>
            <p>Esta vista estará disponible pronto</p>
          </div>
        );
      default:
        return (
          <AuroraWeeklyCalendar
            onEventClick={handleEventClick}
            onAddEvent={handleAddEvent}
            selectedCategoryId={selectedCategoryId}
            onCategoriesLoaded={handleCategoriesLoaded}
          />
        );
    }
  };

  return (
    <div className="main-dashboard">
      <Navigation
        activeView={activeView}
        onViewChange={handleViewChange}
      />
      <main className="dashboard-content">
        {renderMainContent()}
      </main>

      {/* NLP Input */}
      <FloatingNLPInput onEventCreated={handleEventCreated} />

      {/* Event Form Modal */}
      <EventFormModal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        onEventCreated={handleEventCreated}
        initialDate={selectedDate}
      />
    </div>
  );
};

export default MainDashboard;