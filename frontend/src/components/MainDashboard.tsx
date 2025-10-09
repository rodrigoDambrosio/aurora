import React, { useState } from 'react';
import type { EventDto } from '../services/apiService';
import AuroraWeeklyCalendar from './AuroraWeeklyCalendar';
import { FloatingNLPInput } from './FloatingNLPInput';
import './MainDashboard.css';
import Navigation from './Navigation';

const MainDashboard: React.FC = () => {
  const [activeView, setActiveView] = useState('calendar-week');

  const handleViewChange = (view: string) => {
    setActiveView(view);
    console.log('Changing view to:', view);
  };

  const handleEventCreated = () => {
    console.log('Evento creado mediante IA');
    // TODO: Refresh calendar data or show success notification
  };

  const handleEventClick = (event: EventDto) => {
    console.log('Event clicked:', event);
    // TODO: Open event details modal or navigate to event page
  };

  const handleAddEvent = (date: Date) => {
    console.log('Adding event for date:', date);
    // TODO: Open add event modal or navigate to create event page
  };

  const renderMainContent = () => {
    switch (activeView) {
      case 'calendar-week':
        return (
          <AuroraWeeklyCalendar
            onEventClick={handleEventClick}
            onAddEvent={handleAddEvent}
          />
        );
      case 'calendar-month':
        return (
          <div className="placeholder-view">
            <h2>Vista Mensual</h2>
            <p>Esta vista estará disponible pronto</p>
          </div>
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
      <FloatingNLPInput onEventCreated={handleEventCreated} />
    </div>
  );
};

export default MainDashboard;