import { Calendar, Clock, FileText, MapPin, Star, X } from 'lucide-react';
import React, { useEffect, useMemo, useRef, useState } from 'react';
import type { CreateEventDto, EventCategoryDto, EventDto, EventPriority } from '../services/apiService';
import { apiService } from '../services/apiService';
import './EventFormModal.css';

interface EventFormModalProps {
  isOpen: boolean;
  onClose: () => void;
  onEventCreated?: () => void;
  onEventUpdated?: () => void;
  initialDate?: Date;
  mode?: 'create' | 'edit';
  eventToEdit?: EventDto | null;
}

export const EventFormModal: React.FC<EventFormModalProps> = ({
  isOpen,
  onClose,
  onEventCreated,
  onEventUpdated,
  initialDate,
  mode = 'create',
  eventToEdit
}) => {
  const [categories, setCategories] = useState<EventCategoryDto[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string>('');

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
  const startTimeInputRef = useRef<HTMLInputElement>(null);
  const endTimeInputRef = useRef<HTMLInputElement>(null);
  const [supportsNativeTimePicker, setSupportsNativeTimePicker] = useState(true);

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

  const toTimeInputValue = (isoDate?: string) => {
    if (!isoDate) return '';
    const date = new Date(isoDate);
    if (Number.isNaN(date.getTime())) return '';
    return date.toLocaleTimeString('es-ES', {
      hour: '2-digit',
      minute: '2-digit',
      hour12: false
    });
  };

  const combineLocalDateTimeToUtc = (datePart: string, timePart: string) => {
    const local = new Date(`${datePart}T${timePart}`);
    return Number.isNaN(local.getTime()) ? '' : local.toISOString();
  };

  const toUtcDayBoundary = (datePart: string, endOfDay = false) => {
    const time = endOfDay ? '23:59:59' : '00:00:00';
    const local = new Date(`${datePart}T${time}`);
    return Number.isNaN(local.getTime()) ? '' : local.toISOString();
  };

  const openTimePicker = (input: HTMLInputElement | null) => {
    if (!input) {
      return;
    }

    const extended = input as HTMLInputElement & { showPicker?: () => void };
    if (typeof extended.showPicker === 'function') {
      extended.showPicker();
    } else {
      input.focus();
    }
  };

  const timeOptions = useMemo(() => {
    const options: string[] = [];
    for (let hour = 0; hour < 24; hour++) {
      for (let minute = 0; minute < 60; minute += 30) {
        const hh = String(hour).padStart(2, '0');
        const mm = String(minute).padStart(2, '0');
        options.push(`${hh}:${mm}`);
      }
    }
    return options;
  }, []);

  useEffect(() => {
    const hasPicker = typeof window !== 'undefined'
      && typeof HTMLInputElement !== 'undefined'
      && 'showPicker' in HTMLInputElement.prototype;

    setSupportsNativeTimePicker(hasPicker);
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
      setTitle('');
      setDescription('');
      setIsAllDay(false);
      setStartTime('09:00');
      setEndTime('10:00');
      setLocation('');
      setPriority(2);

      if (initialDate) {
        const offsetMs = initialDate.getTimezoneOffset() * 60000;
        const dateStr = new Date(initialDate.getTime() - offsetMs).toISOString().split('T')[0];
        setStartDate(dateStr);
        setEndDate(dateStr);
      } else {
        const now = new Date();
        const offsetMs = now.getTimezoneOffset() * 60000;
        const today = new Date(now.getTime() - offsetMs).toISOString().split('T')[0];
        setStartDate(today);
        setEndDate(today);
      }
    }
  }, [isOpen, isCreateMode, eventToEdit, initialDate]);

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
    }
  }, [isOpen]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setIsLoading(true);

    try {
      // Validate
      if (!title.trim()) {
        throw new Error('El título es obligatorio');
      }

      // Build DateTime strings
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

      const eventDto: CreateEventDto = {
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

      console.log('Creating event:', eventDto);

      if (isCreateMode) {
        await apiService.createEvent(eventDto);
        console.log('Event created successfully');
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
                {supportsNativeTimePicker ? (
                  <input
                    id="event-start-time"
                    type="time"
                    className="event-form-input"
                    value={startTime}
                    onChange={(e) => setStartTime(e.target.value)}
                    required
                    ref={startTimeInputRef}
                  />
                ) : (
                  <div className="event-form-time-picker">
                    <input
                      id="event-start-time"
                      type="time"
                      className="event-form-input event-form-time-input"
                      value={startTime}
                      onChange={(e) => setStartTime(e.target.value)}
                      required
                      ref={startTimeInputRef}
                      inputMode="numeric"
                      pattern="\\d{2}:\\d{2}"
                      list="event-start-time-options"
                    />
                    <button
                      type="button"
                      className="event-form-time-button"
                      onClick={() => openTimePicker(startTimeInputRef.current)}
                      aria-label="Seleccionar hora de inicio"
                    >
                      <Clock size={16} aria-hidden="true" />
                    </button>
                    <datalist id="event-start-time-options">
                      {timeOptions.map((option) => (
                        <option key={option} value={option} />
                      ))}
                    </datalist>
                  </div>
                )}
              </div>
              <div className="event-form-field">
                <label htmlFor="event-end-time">Hora fin</label>
                {supportsNativeTimePicker ? (
                  <input
                    id="event-end-time"
                    type="time"
                    className="event-form-input"
                    value={endTime}
                    onChange={(e) => setEndTime(e.target.value)}
                    required
                    ref={endTimeInputRef}
                  />
                ) : (
                  <div className="event-form-time-picker">
                    <input
                      id="event-end-time"
                      type="time"
                      className="event-form-input event-form-time-input"
                      value={endTime}
                      onChange={(e) => setEndTime(e.target.value)}
                      required
                      ref={endTimeInputRef}
                      inputMode="numeric"
                      pattern="\\d{2}:\\d{2}"
                      list="event-end-time-options"
                    />
                    <button
                      type="button"
                      className="event-form-time-button"
                      onClick={() => openTimePicker(endTimeInputRef.current)}
                      aria-label="Seleccionar hora de fin"
                    >
                      <Clock size={16} aria-hidden="true" />
                    </button>
                    <datalist id="event-end-time-options">
                      {timeOptions.map((option) => (
                        <option key={option} value={option} />
                      ))}
                    </datalist>
                  </div>
                )}
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
        </form>
      </div>
    </>
  );
};
