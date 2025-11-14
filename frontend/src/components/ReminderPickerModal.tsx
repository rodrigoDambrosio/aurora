import { Clock, X } from 'lucide-react';
import { useEffect, useState } from 'react';
import type { CreateReminderDto, ReminderDto } from '../types/reminder.types';
import { ReminderType } from '../types/reminder.types';
import './ReminderPickerModal.css';

interface ReminderPickerModalProps {
  eventId: string;
  isOpen: boolean;
  onClose: () => void;
  onSave: (data: CreateReminderDto) => Promise<void>;
  editingReminder?: ReminderDto | null;
}

export function ReminderPickerModal({
  eventId,
  isOpen,
  onClose,
  onSave,
  editingReminder,
}: ReminderPickerModalProps) {
  const [selectedType, setSelectedType] = useState<ReminderType>(ReminderType.Minutes15);
  const [customHour, setCustomHour] = useState('09');
  const [customMinute, setCustomMinute] = useState('00');
  const [customBeforeHours, setCustomBeforeHours] = useState('0');
  const [customBeforeMinutes, setCustomBeforeMinutes] = useState('15');
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Pre-llenar valores cuando se edita un recordatorio
  useEffect(() => {
    if (editingReminder && isOpen) {
      setSelectedType(editingReminder.reminderType);

      if (editingReminder.reminderType === ReminderType.OneDayBefore) {
        setCustomHour((editingReminder.customTimeHours || 9).toString().padStart(2, '0'));
        setCustomMinute((editingReminder.customTimeMinutes || 0).toString().padStart(2, '0'));
      } else if (editingReminder.reminderType === ReminderType.Custom) {
        setCustomBeforeHours((editingReminder.customTimeHours || 0).toString());
        setCustomBeforeMinutes((editingReminder.customTimeMinutes || 15).toString());
      }
    } else if (isOpen && !editingReminder) {
      // Reset a valores por defecto cuando se abre para crear nuevo
      setSelectedType(ReminderType.Minutes15);
      setCustomHour('09');
      setCustomMinute('00');
      setCustomBeforeHours('0');
      setCustomBeforeMinutes('15');
    }
  }, [editingReminder, isOpen]);

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

      // Si es custom, agregar horas y minutos antes del evento
      if (selectedType === ReminderType.Custom) {
        reminderData.customTimeHours = parseInt(customBeforeHours, 10);
        reminderData.customTimeMinutes = parseInt(customBeforeMinutes, 10);
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
    <>
      {/* Backdrop */}
      <div className="reminder-modal-backdrop" onClick={onClose} />

      {/* Modal */}
      <div className="reminder-modal">
        {/* Header */}
        <div className="reminder-modal-header">
          <div className="reminder-modal-header-content">
            <div className="reminder-modal-icon">
              <Clock size={20} />
            </div>
            <h3>{editingReminder ? 'Editar recordatorio' : 'Agregar recordatorio'}</h3>
          </div>
          <button
            type="button"
            className="reminder-modal-close"
            onClick={onClose}
            disabled={isSaving}
            aria-label="Cerrar"
          >
            <X size={16} />
          </button>
        </div>

        {/* Body */}
        <div className="reminder-modal-body">
          <p className="reminder-modal-description">
            Selecciona cuándo quieres que te recordemos este evento:
          </p>

          {/* Opciones de recordatorio */}
          <div className="reminder-options">
            <label className={`reminder-option ${selectedType === ReminderType.Minutes15 ? 'selected' : ''}`}>
              <div className="reminder-option-header">
                <div className="reminder-option-radio" />
                <span className="reminder-option-label">15 minutos antes</span>
              </div>
              <input
                type="radio"
                name="reminderType"
                value={ReminderType.Minutes15}
                checked={selectedType === ReminderType.Minutes15}
                onChange={() => setSelectedType(ReminderType.Minutes15)}
                style={{ display: 'none' }}
              />
            </label>

            <label className={`reminder-option ${selectedType === ReminderType.Minutes30 ? 'selected' : ''}`}>
              <div className="reminder-option-header">
                <div className="reminder-option-radio" />
                <span className="reminder-option-label">30 minutos antes</span>
              </div>
              <input
                type="radio"
                name="reminderType"
                value={ReminderType.Minutes30}
                checked={selectedType === ReminderType.Minutes30}
                onChange={() => setSelectedType(ReminderType.Minutes30)}
                style={{ display: 'none' }}
              />
            </label>

            <label className={`reminder-option ${selectedType === ReminderType.OneDayBefore ? 'selected' : ''}`}>
              <div className="reminder-option-header">
                <div className="reminder-option-radio" />
                <span className="reminder-option-label">1 día antes a las:</span>
              </div>
              <input
                type="radio"
                name="reminderType"
                value={ReminderType.OneDayBefore}
                checked={selectedType === ReminderType.OneDayBefore}
                onChange={() => setSelectedType(ReminderType.OneDayBefore)}
                style={{ display: 'none' }}
              />

              {selectedType === ReminderType.OneDayBefore && (
                <div className="reminder-custom-time">
                  <input
                    type="number"
                    min="0"
                    max="23"
                    value={customHour}
                    onChange={(e) => setCustomHour(e.target.value.padStart(2, '0'))}
                    className="reminder-time-input"
                    placeholder="HH"
                  />
                  <span className="reminder-time-separator">:</span>
                  <input
                    type="number"
                    min="0"
                    max="59"
                    step="5"
                    value={customMinute}
                    onChange={(e) => setCustomMinute(e.target.value.padStart(2, '0'))}
                    className="reminder-time-input"
                    placeholder="MM"
                  />
                </div>
              )}
            </label>

            <label className={`reminder-option ${selectedType === ReminderType.Custom ? 'selected' : ''}`}>
              <div className="reminder-option-header">
                <div className="reminder-option-radio" />
                <span className="reminder-option-label">Personalizado:</span>
              </div>
              <input
                type="radio"
                name="reminderType"
                value={ReminderType.Custom}
                checked={selectedType === ReminderType.Custom}
                onChange={() => setSelectedType(ReminderType.Custom)}
                style={{ display: 'none' }}
              />

              {selectedType === ReminderType.Custom && (
                <div className="reminder-custom-time">
                  <input
                    type="number"
                    min="0"
                    max="72"
                    value={customBeforeHours}
                    onChange={(e) => setCustomBeforeHours(e.target.value)}
                    className="reminder-time-input"
                    placeholder="0"
                  />
                  <span className="reminder-time-separator">h</span>
                  <input
                    type="number"
                    min="0"
                    max="59"
                    step="5"
                    value={customBeforeMinutes}
                    onChange={(e) => setCustomBeforeMinutes(e.target.value)}
                    className="reminder-time-input"
                    placeholder="0"
                  />
                  <span className="reminder-time-separator">min antes</span>
                </div>
              )}
            </label>
          </div>

          {error && (
            <div className="reminder-modal-error">
              {error}
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="reminder-modal-footer">
          <button
            type="button"
            className="reminder-modal-button reminder-modal-button-cancel"
            onClick={onClose}
            disabled={isSaving}
          >
            Cancelar
          </button>
          <button
            type="button"
            className="reminder-modal-button reminder-modal-button-save"
            onClick={handleSave}
            disabled={isSaving}
          >
            {isSaving ? 'Guardando...' : (editingReminder ? 'Guardar cambios' : 'Agregar recordatorio')}
          </button>
        </div>
      </div>
    </>
  );
}
