import { Clock, X } from 'lucide-react';
import { useState } from 'react';
import type { CreateReminderDto } from '../types/reminder.types';
import { ReminderType } from '../types/reminder.types';

interface ReminderPickerModalProps {
  eventId: string;
  isOpen: boolean;
  onClose: () => void;
  onSave: (data: CreateReminderDto) => Promise<void>;
}

export function ReminderPickerModal({
  eventId,
  isOpen,
  onClose,
  onSave,
}: ReminderPickerModalProps) {
  const [selectedType, setSelectedType] = useState<ReminderType>(ReminderType.Minutes15);
  const [customHour, setCustomHour] = useState('09');
  const [customMinute, setCustomMinute] = useState('00');
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSave = async () => {
    setError(null);
    setIsSaving(true);

    try {
      const reminderData: CreateReminderDto = {
        eventId,
        reminderType: selectedType,
      };

      // Si es "un día antes", agregar la hora personalizada
      if (selectedType === ReminderType.OneDayBefore) {
        reminderData.customTimeHours = parseInt(customHour, 10);
        reminderData.customTimeMinutes = parseInt(customMinute, 10);
      }

      await onSave(reminderData);
      onClose();

      // Reset form
      setSelectedType(ReminderType.Minutes15);
      setCustomHour('09');
      setCustomMinute('00');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Error al guardar recordatorio');
    } finally {
      setIsSaving(false);
    }
  };

  if (!isOpen) {
    return null;
  }

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-xl shadow-xl max-w-md w-full">
        {/* Header */}
        <div className="flex items-center justify-between p-4 border-b border-gray-200">
          <div className="flex items-center gap-2">
            <Clock className="w-5 h-5 text-primary-600" />
            <h3 className="text-lg font-semibold text-gray-900">
              Agregar recordatorio
            </h3>
          </div>
          <button
            onClick={onClose}
            className="p-1 text-gray-400 hover:text-gray-600 hover:bg-gray-100 rounded transition-colors"
            disabled={isSaving}
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Body */}
        <div className="p-4 space-y-3">
          <p className="text-sm text-gray-600 mb-4">
            Selecciona cuándo quieres que te recordemos este evento:
          </p>

          {/* Opciones de recordatorio */}
          <label className="flex items-center gap-3 p-3 border border-gray-200 rounded-lg cursor-pointer hover:bg-gray-50 transition-colors">
            <input
              type="radio"
              name="reminderType"
              value={ReminderType.Minutes15}
              checked={selectedType === ReminderType.Minutes15}
              onChange={() => setSelectedType(ReminderType.Minutes15)}
              className="w-4 h-4 text-primary-600 focus:ring-primary-500"
            />
            <span className="text-sm font-medium text-gray-900">
              15 minutos antes
            </span>
          </label>

          <label className="flex items-center gap-3 p-3 border border-gray-200 rounded-lg cursor-pointer hover:bg-gray-50 transition-colors">
            <input
              type="radio"
              name="reminderType"
              value={ReminderType.Minutes30}
              checked={selectedType === ReminderType.Minutes30}
              onChange={() => setSelectedType(ReminderType.Minutes30)}
              className="w-4 h-4 text-primary-600 focus:ring-primary-500"
            />
            <span className="text-sm font-medium text-gray-900">
              30 minutos antes
            </span>
          </label>

          <label className="flex flex-col gap-2 p-3 border border-gray-200 rounded-lg cursor-pointer hover:bg-gray-50 transition-colors">
            <div className="flex items-center gap-3">
              <input
                type="radio"
                name="reminderType"
                value={ReminderType.OneDayBefore}
                checked={selectedType === ReminderType.OneDayBefore}
                onChange={() => setSelectedType(ReminderType.OneDayBefore)}
                className="w-4 h-4 text-primary-600 focus:ring-primary-500"
              />
              <span className="text-sm font-medium text-gray-900">
                1 día antes a las:
              </span>
            </div>

            {selectedType === ReminderType.OneDayBefore && (
              <div className="flex items-center gap-2 ml-7">
                <input
                  type="number"
                  min="0"
                  max="23"
                  value={customHour}
                  onChange={(e) => setCustomHour(e.target.value.padStart(2, '0'))}
                  className="w-16 px-2 py-1 text-center border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                  placeholder="HH"
                />
                <span className="text-gray-600">:</span>
                <input
                  type="number"
                  min="0"
                  max="59"
                  step="5"
                  value={customMinute}
                  onChange={(e) => setCustomMinute(e.target.value.padStart(2, '0'))}
                  className="w-16 px-2 py-1 text-center border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                  placeholder="MM"
                />
              </div>
            )}
          </label>

          {error && (
            <div className="p-3 bg-red-50 border border-red-200 rounded-lg">
              <p className="text-sm text-red-800">{error}</p>
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="flex items-center justify-end gap-2 p-4 border-t border-gray-200">
          <button
            onClick={onClose}
            disabled={isSaving}
            className="px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-100 rounded-lg transition-colors disabled:opacity-50"
          >
            Cancelar
          </button>
          <button
            onClick={handleSave}
            disabled={isSaving}
            className="px-4 py-2 text-sm font-medium text-white bg-primary-600 hover:bg-primary-700 rounded-lg transition-colors disabled:opacity-50"
          >
            {isSaving ? 'Guardando...' : 'Agregar recordatorio'}
          </button>
        </div>
      </div>
    </div>
  );
}
