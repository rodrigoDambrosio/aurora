import { Bell, Plus, Trash2 } from 'lucide-react';
import { useState } from 'react';
import { useReminders } from '../hooks/useReminders';
import type { CreateReminderDto, ReminderDto } from '../types/reminder.types';
import { ReminderType } from '../types/reminder.types';
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
      <div className="space-y-2">
        <label className="flex items-center gap-2 text-sm font-medium text-gray-700">
          <Bell className="w-4 h-4" />
          Recordatorios
        </label>
        <p className="text-sm text-gray-500">
          Guarda el evento primero para agregar recordatorios
        </p>
      </div>
    );
  }

  return (
    <div className="space-y-3">
      <label className="flex items-center gap-2 text-sm font-medium text-gray-700">
        <Bell className="w-4 h-4" />
        Recordatorios
      </label>

      {/* Lista de recordatorios */}
      {reminders.length > 0 && (
        <div className="space-y-2">
          {reminders.map((reminder) => (
            <div
              key={reminder.id}
              className="flex items-center justify-between p-3 bg-gray-50 rounded-lg border border-gray-200"
            >
              <div className="flex items-center gap-2">
                <Bell className="w-4 h-4 text-primary-600" />
                <span className="text-sm text-gray-900">
                  {getReminderTypeLabel(reminder)}
                </span>
                {reminder.isSent && (
                  <span className="text-xs text-gray-500 bg-gray-200 px-2 py-0.5 rounded">
                    Enviado
                  </span>
                )}
              </div>

              <button
                onClick={() => handleDeleteReminder(reminder.id)}
                className="p-1 text-gray-400 hover:text-red-600 hover:bg-red-50 rounded transition-colors"
                aria-label="Eliminar recordatorio"
              >
                <Trash2 className="w-4 h-4" />
              </button>
            </div>
          ))}
        </div>
      )}

      {/* Botón para agregar recordatorio */}
      <button
        type="button"
        onClick={() => setIsModalOpen(true)}
        disabled={isLoading}
        className="flex items-center gap-2 px-3 py-2 text-sm font-medium text-primary-700 bg-primary-50 hover:bg-primary-100 rounded-lg transition-colors disabled:opacity-50"
      >
        <Plus className="w-4 h-4" />
        Agregar recordatorio
      </button>

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
