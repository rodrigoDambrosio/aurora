import { AlertTriangle, Bell, Calendar, FileText, MapPin, Plus, Sparkles, Star, Trash2, X } from 'lucide-react';
import React, { useCallback, useEffect, useMemo, useState } from 'react';
import type { AIValidationResult, CreateEventDto, EventCategoryDto, EventDto, EventPriority } from '../services/apiService';
import { apiService } from '../services/apiService';
import type { CreateReminderDto } from '../types/reminder.types';
import { ReminderType } from '../types/reminder.types';
import './EventFormModal.css';
import { ReminderPickerModal } from './ReminderPickerModal';
import { ReminderSection } from './ReminderSection';
import { TimeInput } from './TimeInput';

interface EventPrefillData {
  title?: string;
  description?: string;
  durationMinutes?: number;
}

interface EventFormModalProps {
  isOpen: boolean;
  onClose: () => void;
  onEventCreated?: () => void;
  onEventUpdated?: () => void;
  initialDate?: Date;
  mode?: 'create' | 'edit';
  eventToEdit?: EventDto | null;
  prefillData?: EventPrefillData;
}

export const EventFormModal: React.FC<EventFormModalProps> = ({
  isOpen,
  onClose,
  onEventCreated,
  onEventUpdated,
  initialDate,
  mode = 'create',
  eventToEdit,
  prefillData
}) => {
  const [categories, setCategories] = useState<EventCategoryDto[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string>('');
  const [isValidating, setIsValidating] = useState(false);
  const [validationResult, setValidationResult] = useState<AIValidationResult | null>(null);
  const [validationError, setValidationError] = useState<string>('');
  const [timeFormat, setTimeFormat] = useState<'12h' | '24h'>('24h'); // Preferencia de formato de hora

  // Form state
  const [title, setTitle] = useState('');
  const [categoryId, setCategoryId] = useState<string>('');
  const [isAllDay, setIsAllDay] = useState(false);
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  const [startTime, setStartTime] = useState('09:00');
  const [endTime, setEndTime] = useState('10:00');
  const [location, setLocation] = useState('');
  const [description, setDescription] = useState('');
  const [priority, setPriority] = useState<EventPriority>(2);

  // Recordatorios para eventos nuevos
  const [pendingReminders, setPendingReminders] = useState<CreateReminderDto[]>([]);
  const [isReminderModalOpen, setIsReminderModalOpen] = useState(false);

  const isCreateMode = mode === 'create';

  const priorityOptions = useMemo(
    () => ([
      { value: 1 as EventPriority, label: 'Baja', description: 'Sin urgencia especial' },
      { value: 2 as EventPriority, label: 'Media', description: 'Prioritario pero manejable' },
      { value: 3 as EventPriority, label: 'Alta', description: 'Atención recomendada' },
      { value: 4 as EventPriority, label: 'Crítica', description: 'Requiere acción inmediata' }
    ]),
    []
  );

  const toDateInputValue = (isoDate?: string) => {
    if (!isoDate) return '';
    const date = new Date(isoDate);
    if (Number.isNaN(date.getTime())) return '';
    const tzOffset = date.getTimezoneOffset() * 60000;
    return new Date(date.getTime() - tzOffset).toISOString().slice(0, 10);
  };

  const toTimeInputValue = useCallback((isoDate?: string) => {
    if (!isoDate) return '';
    const date = new Date(isoDate);
    if (Number.isNaN(date.getTime())) return '';
    // El input type="time" siempre usa formato 24h (HH:MM) independientemente de la preferencia del usuario
    // El navegador se encarga de mostrarlo en el formato apropiado según la configuración regional
    const hours = date.getHours().toString().padStart(2, '0');
    const minutes = date.getMinutes().toString().padStart(2, '0');
    return `${hours}:${minutes}`;
  }, []);

  const combineLocalDateTimeToUtc = (datePart: string, timePart: string) => {
    const local = new Date(`${datePart}T${timePart}`);
    return Number.isNaN(local.getTime()) ? '' : local.toISOString();
  };

  const toUtcDayBoundary = (datePart: string, endOfDay = false) => {
    const time = endOfDay ? '23:59:59' : '00:00:00';
    const local = new Date(`${datePart}T${time}`);
    return Number.isNaN(local.getTime()) ? '' : local.toISOString();
  };

  // Funciones para manejar recordatorios pendientes
  const handleAddPendingReminder = async (data: CreateReminderDto) => {
    setPendingReminders(prev => [...prev, data]);
  };

  const handleRemovePendingReminder = (index: number) => {
    setPendingReminders(prev => prev.filter((_, i) => i !== index));
  };

  const getReminderTypeLabel = (reminder: CreateReminderDto): string => {
    switch (reminder.reminderType) {
      case ReminderType.Minutes15:
        return '15 minutos antes';
      case ReminderType.Minutes30:
        return '30 minutos antes';
      case ReminderType.OneDayBefore:
        return `1 día antes a las ${reminder.customTimeHours?.toString().padStart(2, '0')}:${reminder.customTimeMinutes?.toString().padStart(2, '0')}`;
      case ReminderType.Custom:
        const hours = reminder.customTimeHours || 0;
        const minutes = reminder.customTimeMinutes || 0;
        if (hours === 0 && minutes === 0) {
          return 'En el momento del evento';
        }
        const parts = [];
        if (hours > 0) parts.push(`${hours}h`);
        if (minutes > 0) parts.push(`${minutes}min`);
        return `${parts.join(' ')} antes`;
      default:
        return 'Personalizado';
    }
  };

  // Cargar preferencias de usuario
  useEffect(() => {
    const loadUserPreferences = async () => {
      try {
        const preferences = await apiService.getUserPreferences();
        setTimeFormat(preferences.timeFormat);
      } catch (err) {
        console.error('Error loading user preferences:', err);
        // Usar valor por defecto si falla
      }
    };

    loadUserPreferences();
  }, []);

  // Load categories on mount
  useEffect(() => {
    if (!isOpen) {
      return;
    }

    const loadCategories = async () => {
      try {
        const cats = await apiService.getEventCategories();
        setCategories(cats);
        if (cats.length > 0) {
          if (!isCreateMode && eventToEdit?.eventCategory?.id) {
            setCategoryId(eventToEdit.eventCategory.id);
          } else {
            setCategoryId((currentId) => currentId || cats[0].id);
          }
        }
      } catch (err) {
        console.error('Error loading categories:', err);
      }
    };

    loadCategories();
  }, [isOpen, isCreateMode, eventToEdit]);

  // Prefill form data when opening the modal
  useEffect(() => {
    if (!isOpen) {
      return;
    }

    if (!isCreateMode && eventToEdit) {
      setTitle(eventToEdit.title);
      setDescription(eventToEdit.description ?? '');
      setIsAllDay(eventToEdit.isAllDay);
      setStartDate(toDateInputValue(eventToEdit.startDate));
      setEndDate(toDateInputValue(eventToEdit.endDate));
      setStartTime(toTimeInputValue(eventToEdit.startDate) || '09:00');
      setEndTime(toTimeInputValue(eventToEdit.endDate) || '10:00');
      setLocation(eventToEdit.location ?? '');
      setCategoryId(eventToEdit.eventCategory?.id ?? '');
      setPriority(eventToEdit.priority ?? 2);
    } else {
      setTitle(prefillData?.title ?? '');
      setDescription(prefillData?.description ?? '');
      setIsAllDay(false);
      setLocation('');
      setPriority(2);

      const effectiveStart = initialDate ?? new Date();
      const validStart = Number.isNaN(effectiveStart.getTime()) ? new Date() : effectiveStart;
      const startOffsetMs = validStart.getTimezoneOffset() * 60000;
      const startLocal = new Date(validStart.getTime() - startOffsetMs);
      const startDateStr = startLocal.toISOString().split('T')[0];
      setStartDate(startDateStr);

      const startTimeStr = toTimeInputValue(validStart.toISOString()) || '09:00';
      setStartTime(startTimeStr);

      const durationMinutes = prefillData?.durationMinutes && prefillData.durationMinutes > 0
        ? prefillData.durationMinutes
        : 60;
      const endDateTime = new Date(validStart.getTime());
      endDateTime.setMinutes(endDateTime.getMinutes() + durationMinutes);
      const endOffsetMs = endDateTime.getTimezoneOffset() * 60000;
      const endLocal = new Date(endDateTime.getTime() - endOffsetMs);
      const endDateStr = endLocal.toISOString().split('T')[0];
      setEndDate(endDateStr);

      const endTimeStr = toTimeInputValue(endDateTime.toISOString()) || startTimeStr;
      setEndTime(endTimeStr);

      // Pre-seleccionar recordatorio de 15 minutos por defecto
      setPendingReminders([{
        reminderType: ReminderType.Minutes15,
        customTimeHours: null,
        customTimeMinutes: null
      }]);
    }
  }, [isOpen, isCreateMode, eventToEdit, initialDate, toTimeInputValue, prefillData]);

  // Reset form when modal closes
  useEffect(() => {
    if (!isOpen) {
      setTitle('');
      setLocation('');
      setDescription('');
      setIsAllDay(false);
      setStartTime('09:00');
      setEndTime('10:00');
      setStartDate('');
      setEndDate('');
      setError('');
      setCategoryId('');
      setPriority(2);
      setValidationResult(null);
      setValidationError('');
      setIsValidating(false);
      setPendingReminders([]);
      setIsReminderModalOpen(false);
    }
  }, [isOpen]);

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    setValidationResult(null);
    setValidationError('');
  }, [
    title,
    description,
    startDate,
    endDate,
    startTime,
    endTime,
    location,
    isAllDay,
    categoryId,
    priority,
    isOpen
  ]);

  const buildEventPayload = (): CreateEventDto => {
    if (!title.trim()) {
      throw new Error('El título es obligatorio');
    }

    if (!startDate || !endDate) {
      throw new Error('Debes seleccionar fecha de inicio y fin.');
    }

    let startDateTime: string;
    let endDateTime: string;

    if (isAllDay) {
      startDateTime = toUtcDayBoundary(startDate, false);
      endDateTime = toUtcDayBoundary(endDate, true);
    } else {
      startDateTime = combineLocalDateTimeToUtc(startDate, `${startTime}:00`);
      endDateTime = combineLocalDateTimeToUtc(endDate, `${endTime}:00`);
    }

    if (!startDateTime || !endDateTime) {
      throw new Error('No se pudo interpretar la fecha u hora seleccionada.');
    }

    // Validar que la fecha/hora de fin sea posterior a la de inicio
    const startDateObj = new Date(startDateTime);
    const endDateObj = new Date(endDateTime);

    if (endDateObj <= startDateObj) {
      if (isAllDay) {
        throw new Error('La fecha de fin debe ser posterior a la fecha de inicio.');
      } else {
        throw new Error('La hora de fin debe ser posterior a la hora de inicio.');
      }
    }

    return {
      title: title.trim(),
      description: description.trim() || undefined,
      startDate: startDateTime,
      endDate: endDateTime,
      location: location.trim() || undefined,
      eventCategoryId: categoryId,
      isAllDay,
      priority,
      timezoneOffsetMinutes: -new Date().getTimezoneOffset()
    };
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setValidationError('');
    setIsLoading(true);

    try {
      const eventDto = buildEventPayload();

      console.log('Creating event:', eventDto);

      if (isCreateMode) {
        const newEvent = await apiService.createEvent(eventDto);
        console.log('Event created successfully');

        // Crear recordatorios pendientes si hay alguno
        if (pendingReminders.length > 0) {
          for (const reminderData of pendingReminders) {
            try {
              await apiService.createReminder({ ...reminderData, eventId: newEvent.id });
            } catch (reminderError) {
              console.error('Error creating reminder:', reminderError);
            }
          }
        }

        onEventCreated?.();
      } else if (eventToEdit) {
        await apiService.updateEvent(eventToEdit.id, eventDto);
        console.log('Event updated successfully');
        onEventUpdated?.();
      }

      onClose();
    } catch (err) {
      const error = err as Error;
      console.error('Error saving event:', error);
      setError(
        error.message ||
        (isCreateMode ? 'Error al crear el evento' : 'Error al actualizar el evento')
      );
    } finally {
      setIsLoading(false);
    }
  };

  const handleManualValidation = async () => {
    setValidationError('');
    setIsValidating(true);

    try {
      const eventDto = buildEventPayload();
      const result = await apiService.validateEvent(eventDto);
      setValidationResult(result);
    } catch (err) {
      const error = err as { message?: string };
      setValidationResult(null);
      setValidationError(error?.message ?? 'No se pudo completar el análisis de IA.');
    } finally {
      setIsValidating(false);
    }
  };

  const handleCancel = () => {
    onClose();
  };

  const handleStartDateChange = (newStartDate: string) => {
    setStartDate(newStartDate);

    // Si la fecha de fin es anterior a la nueva fecha de inicio, ajustarla
    if (endDate && newStartDate > endDate) {
      setEndDate(newStartDate);
    }
  };

  const getSelectedCategory = () => {
    return categories.find(c => c.id === categoryId);
  };

  const normalizeSeverity = (severity: AIValidationResult['severity']) => {
    if (typeof severity === 'string') {
      return severity.toLowerCase();
    }

    switch (severity) {
      case 1:
        return 'warning';
      case 2:
        return 'critical';
      default:
        return 'info';
    }
  };

  const renderValidationResult = () => {
    if (!validationResult) {
      return null;
    }

    const usedAi = validationResult.usedAi ?? true;

    return (
      <div className={`event-validation-result event-validation-result-${normalizeSeverity(validationResult.severity)}`}>
        <div className="event-validation-header">
          {usedAi ? (
            <Sparkles size={18} aria-hidden="true" />
          ) : (
            <AlertTriangle size={18} aria-hidden="true" className="event-validation-fallback-icon" />
          )}
          <strong>{usedAi ? 'Análisis de IA' : 'Validación básica'}</strong>
        </div>
        {!usedAi && (
          <p className="event-validation-fallback-note">
            Este feedback se generó con reglas locales porque la IA no estuvo disponible.
          </p>
        )}
        <p className="event-validation-message">
          {validationResult.recommendationMessage ?? 'La IA no tiene observaciones para este evento.'}
        </p>
        {validationResult.suggestions?.length ? (
          <ul className="event-validation-suggestions">
            {validationResult.suggestions.map((suggestion) => (
              <li key={suggestion}>{suggestion}</li>
            ))}
          </ul>
        ) : null}
      </div>
    );
  };

  if (!isOpen) return null;

  return (
    <>
      {/* Backdrop */}
      <div className="event-modal-backdrop" onClick={handleCancel} />

      {/* Modal */}
      <div className="event-modal">
        {/* Header */}
        <div className="event-modal-header">
          <div className="event-modal-header-content">
            <div className="event-modal-icon">
              <Calendar size={20} />
            </div>
            <h2>{isCreateMode ? 'Crear Nuevo Evento' : 'Editar Evento'}</h2>
          </div>
          <button
            type="button"
            className="event-modal-close"
            onClick={handleCancel}
            aria-label="Cerrar"
          >
            <X size={16} />
          </button>
        </div>

        {/* Form */}
        <form className="event-modal-form" onSubmit={handleSubmit}>
          {/* Error message */}
          {error && (
            <div className="event-modal-error">
              {error}
            </div>
          )}

          {/* Title */}
          <div className="event-form-field">
            <label htmlFor="event-title">Título *</label>
            <input
              id="event-title"
              type="text"
              className="event-form-input"
              placeholder="Ej: Reunión de equipo"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              required
              autoFocus
            />
          </div>

          {/* Category */}
          <div className="event-form-field">
            <label htmlFor="event-category">Categoría</label>
            <select
              id="event-category"
              className="event-form-select"
              value={categoryId}
              onChange={(e) => setCategoryId(e.target.value)}
            >
              {categories.map((cat) => (
                <option key={cat.id} value={cat.id}>
                  {cat.name}
                </option>
              ))}
            </select>
            {getSelectedCategory() && (
              <div className="event-category-preview">
                <div
                  className="event-category-color"
                  style={{ backgroundColor: getSelectedCategory()?.color }}
                />
                <span>{getSelectedCategory()?.name}</span>
              </div>
            )}
          </div>

          {/* Priority */}
          <div className="event-form-field">
            <label>Prioridad</label>
            <div className="event-priority-options" role="group" aria-label="Seleccionar prioridad del evento">
              {priorityOptions.map((option) => {
                const isSelected = priority === option.value;
                return (
                  <button
                    key={option.value}
                    type="button"
                    className={`event-priority-chip ${isSelected ? 'selected' : ''}`}
                    onClick={() => setPriority(option.value)}
                    aria-pressed={isSelected}
                  >
                    <Star size={16} aria-hidden="true" />
                    <span>{option.label}</span>
                  </button>
                );
              })}
            </div>
            <p className="event-priority-helper">
              {priorityOptions.find((option) => option.value === priority)?.description}
            </p>
          </div>

          {/* All Day Toggle */}
          <div className="event-form-toggle">
            <label htmlFor="event-allday">Todo el día</label>
            <button
              type="button"
              role="switch"
              aria-checked={isAllDay}
              className={`event-toggle ${isAllDay ? 'active' : ''}`}
              onClick={() => setIsAllDay(!isAllDay)}
            >
              <span className="event-toggle-thumb" />
            </button>
          </div>

          {/* Date Range */}
          <div className="event-form-row">
            <div className="event-form-field">
              <label htmlFor="event-start-date">Fecha inicio</label>
              <input
                id="event-start-date"
                type="date"
                className="event-form-input"
                value={startDate}
                onChange={(e) => handleStartDateChange(e.target.value)}
                required
              />
            </div>
            <div className="event-form-field">
              <label htmlFor="event-end-date">Fecha fin</label>
              <input
                id="event-end-date"
                type="date"
                className="event-form-input"
                value={endDate}
                onChange={(e) => setEndDate(e.target.value)}
                min={startDate}
                required
              />
            </div>
          </div>

          {/* Time Range (only if not all day) */}
          {!isAllDay && (
            <div className="event-form-row">
              <div className="event-form-field">
                <label htmlFor="event-start-time">Hora inicio</label>
                <TimeInput
                  id="event-start-time"
                  value={startTime}
                  onChange={setStartTime}
                  timeFormat={timeFormat}
                />
              </div>
              <div className="event-form-field">
                <label htmlFor="event-end-time">Hora fin</label>
                <TimeInput
                  id="event-end-time"
                  value={endTime}
                  onChange={setEndTime}
                  timeFormat={timeFormat}
                />
              </div>
            </div>
          )}

          {/* Location */}
          <div className="event-form-field">
            <label htmlFor="event-location">Ubicación</label>
            <div className="event-form-input-with-icon">
              <MapPin size={16} className="event-form-icon" />
              <input
                id="event-location"
                type="text"
                className="event-form-input"
                placeholder="Ej: Sala de conferencias A"
                value={location}
                onChange={(e) => setLocation(e.target.value)}
              />
            </div>
          </div>

          {/* Description */}
          <div className="event-form-field">
            <label htmlFor="event-description">Descripción</label>
            <div className="event-form-textarea-wrapper">
              <FileText size={16} className="event-form-icon" />
              <textarea
                id="event-description"
                className="event-form-textarea"
                placeholder="Detalles adicionales del evento..."
                rows={4}
                value={description}
                onChange={(e) => setDescription(e.target.value)}
              />
            </div>
          </div>

          {/* Reminders Section */}
          {isCreateMode ? (
            <div className="event-reminders-section">
              <div className="event-reminders-header">
                <label className="event-reminders-label">
                  <Bell className="w-5 h-5 text-gray-400" />
                  Recordatorios
                </label>
                <button
                  type="button"
                  onClick={() => setIsReminderModalOpen(true)}
                  className="event-reminders-add-button"
                >
                  <Plus className="w-4 h-4" />
                  Agregar recordatorio
                </button>
              </div>

              <div className="event-reminders-list">
                {pendingReminders.length > 0 ? (
                  pendingReminders.map((reminder, index) => (
                    <div key={index} className="event-reminder-card">
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
                      </div>
                      <button
                        type="button"
                        onClick={() => handleRemovePendingReminder(index)}
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
            </div>
          ) : (
            eventToEdit && (
              <ReminderSection
                eventId={eventToEdit.id}
                eventStartDate={eventToEdit.startDate}
              />
            )
          )}

          {validationError && (
            <div className="event-validation-error" role="alert">
              {validationError}
            </div>
          )}

          {renderValidationResult()}

          {/* Actions */}
          <div className="event-form-actions">
            <button
              type="button"
              className="event-form-button event-form-button-cancel"
              onClick={handleCancel}
              disabled={isLoading}
            >
              Cancelar
            </button>
            <button
              type="button"
              className="event-form-button event-form-button-analyze"
              onClick={handleManualValidation}
              disabled={isLoading || isValidating || !title.trim()}
            >
              {isValidating ? 'Analizando...' : (
                <span className="event-form-button-analyze-content">
                  <Sparkles size={16} aria-hidden="true" />
                  <span>Analizar con IA</span>
                </span>
              )}
            </button>
            <button
              type="submit"
              className="event-form-button event-form-button-submit"
              disabled={isLoading || !title.trim()}
            >
              {isLoading
                ? isCreateMode
                  ? 'Creando...'
                  : 'Actualizando...'
                : isCreateMode
                  ? 'Crear Evento'
                  : 'Actualizar Evento'}
            </button>
          </div>
        </form >

        {/* Modal de selección de recordatorio - solo para modo creación */}
        {isCreateMode && (
          <ReminderPickerModal
            eventId="temp-event-id" // ID temporal para modo creación
            isOpen={isReminderModalOpen}
            onClose={() => setIsReminderModalOpen(false)}
            onSave={handleAddPendingReminder}
          />
        )}
      </div >
    </>
  );
};
