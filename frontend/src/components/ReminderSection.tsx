import { Bell, Plus, Trash2 } from 'lucide-react';
import { useState } from 'react';
import { useReminders } from '../hooks/useReminders';
import type { CreateReminderDto, ReminderDto } from '../types/reminder.types';
import { ReminderType } from '../types/reminder.types';
import './EventFormModal.css'; // Importar estilos de Aurora
import { ReminderPickerModal } from './ReminderPickerModal';

interface ReminderSectionProps {
  eventId: string | null;
  eventStartDate?: string;
  eventEndDate?: string;
}

export function ReminderSection({ eventId, eventStartDate }: ReminderSectionProps) {
  const { reminders, isLoading, addReminder, removeReminder } = useReminders(eventId);
  const [isModalOpen, setIsModalOpen] = useState(false);

  // No mostrar recordatorios para eventos pasados
  const isEventInPast = eventStartDate && new Date(eventStartDate) < new Date();

  const handleAddReminder = async (data: CreateReminderDto) => {
    await addReminder(data);
  };

  const handleDeleteReminder = async (id: string) => {
    if (confirm('¿Estás seguro de que quieres eliminar este recordatorio?')) {
      await removeReminder(id);
    }
  };

  const getReminderTypeLabel = (reminder: ReminderDto): string => {
    switch (reminder.reminderType) {
      case ReminderType.Minutes15:
        return '15 minutos antes';
      case ReminderType.Minutes30:
        return '30 minutos antes';
      case ReminderType.OneDayBefore:
        return `1 día antes a las ${reminder.customTimeHours?.toString().padStart(2, '0')}:${reminder.customTimeMinutes?.toString().padStart(2, '0')}`;
      default:
        return 'Personalizado';
    }
  };

  if (isEventInPast) {
    return null; // No mostrar sección para eventos pasados
  }

  if (!eventId) {
    return (
      <div className="event-reminders-section">
        <div className="event-reminders-header">
          <label className="event-reminders-label">
            <Bell className="w-5 h-5 text-gray-400" />
            Recordatorios
          </label>
        </div>
        <div className="event-reminders-empty">
          Guarda el evento primero para agregar recordatorios
        </div>
      </div>
    );
  }

  return (
    <div className="event-reminders-section">
      <div className="event-reminders-header">
        <label className="event-reminders-label">
          <Bell className="w-5 h-5 text-gray-400" />
          Recordatorios
        </label>
        <button
          type="button"
          onClick={() => setIsModalOpen(true)}
          disabled={isLoading}
          className="event-reminders-add-button"
        >
          <Plus className="w-4 h-4" />
          Agregar recordatorio
        </button>
      </div>

      {/* Lista de recordatorios */}
      <div className="event-reminders-list">
        {reminders.length > 0 ? (
          reminders.map((reminder) => (
            <div key={reminder.id} className="event-reminder-card">
              <div className="event-reminder-content">
                <svg
                  className="event-reminder-icon"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth="2"
                    d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"
                  />
                </svg>
                <span className="event-reminder-text">
                  {getReminderTypeLabel(reminder)}
                </span>
                {reminder.isSent && (
                  <span className="event-reminder-status">
                    Enviado
                  </span>
                )}
              </div>
              <button
                type="button"
                onClick={() => handleDeleteReminder(reminder.id)}
                className="event-reminder-delete"
                aria-label="Eliminar recordatorio"
              >
                <Trash2 className="w-4 h-4" />
              </button>
            </div>
          ))
        ) : (
          <div className="event-reminders-empty">
            No hay recordatorios configurados
          </div>
        )}
      </div>

      {/* Modal de selección de recordatorio */}
      {eventId && (
        <ReminderPickerModal
          eventId={eventId}
          isOpen={isModalOpen}
          onClose={() => setIsModalOpen(false)}
          onSave={handleAddReminder}
        />
      )}
    </div>
  );
}
