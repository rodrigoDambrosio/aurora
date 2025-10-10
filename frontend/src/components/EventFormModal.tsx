import { Calendar, Clock, FileText, MapPin, X } from 'lucide-react';
import React, { useEffect, useState } from 'react';
import type { CreateEventDto, EventCategoryDto } from '../services/apiService';
import { apiService } from '../services/apiService';
import './EventFormModal.css';

interface EventFormModalProps {
  isOpen: boolean;
  onClose: () => void;
  onEventCreated: () => void;
  initialDate?: Date;
}

export const EventFormModal: React.FC<EventFormModalProps> = ({
  isOpen,
  onClose,
  onEventCreated,
  initialDate
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

  // Load categories on mount
  useEffect(() => {
    const loadCategories = async () => {
      try {
        const cats = await apiService.getEventCategories();
        setCategories(cats);
        if (cats.length > 0 && !categoryId) {
          setCategoryId(cats[0].id);
        }
      } catch (err) {
        console.error('Error loading categories:', err);
      }
    };

    if (isOpen) {
      loadCategories();

      // Set initial date if provided
      if (initialDate) {
        const dateStr = initialDate.toISOString().split('T')[0];
        setStartDate(dateStr);
        setEndDate(dateStr);
      } else {
        const today = new Date().toISOString().split('T')[0];
        setStartDate(today);
        setEndDate(today);
      }
    }
  }, [isOpen, initialDate, categoryId]);

  // Reset form when modal closes
  useEffect(() => {
    if (!isOpen) {
      setTitle('');
      setLocation('');
      setDescription('');
      setIsAllDay(false);
      setStartTime('09:00');
      setEndTime('10:00');
      setError('');
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
        // All day events: start at 00:00, end at 23:59
        startDateTime = `${startDate}T00:00:00`;
        endDateTime = `${endDate}T23:59:59`;
      } else {
        startDateTime = `${startDate}T${startTime}:00`;
        endDateTime = `${endDate}T${endTime}:00`;
      }

      const eventDto: CreateEventDto = {
        title: title.trim(),
        description: description.trim() || undefined,
        startDate: startDateTime,
        endDate: endDateTime,
        location: location.trim() || undefined,
        eventCategoryId: categoryId,
        isAllDay
      };

      console.log('Creating event:', eventDto);

      await apiService.createEvent(eventDto);

      console.log('Event created successfully');
      onEventCreated();
      onClose();
    } catch (err) {
      const error = err as Error;
      console.error('Error creating event:', error);
      setError(error.message || 'Error al crear el evento');
    } finally {
      setIsLoading(false);
    }
  };

  const handleCancel = () => {
    onClose();
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
            <h2>Crear Nuevo Evento</h2>
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
                onChange={(e) => setStartDate(e.target.value)}
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
                required
              />
            </div>
          </div>

          {/* Time Range (only if not all day) */}
          {!isAllDay && (
            <div className="event-form-row">
              <div className="event-form-field">
                <label htmlFor="event-start-time">Hora inicio</label>
                <div className="event-form-input-with-icon">
                  <Clock size={16} className="event-form-icon" />
                  <input
                    id="event-start-time"
                    type="time"
                    className="event-form-input"
                    value={startTime}
                    onChange={(e) => setStartTime(e.target.value)}
                    required
                  />
                </div>
              </div>
              <div className="event-form-field">
                <label htmlFor="event-end-time">Hora fin</label>
                <div className="event-form-input-with-icon">
                  <Clock size={16} className="event-form-icon" />
                  <input
                    id="event-end-time"
                    type="time"
                    className="event-form-input"
                    value={endTime}
                    onChange={(e) => setEndTime(e.target.value)}
                    required
                  />
                </div>
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
              {isLoading ? 'Creando...' : 'Crear Evento'}
            </button>
          </div>
        </form>
      </div>
    </>
  );
};
