import { Calendar, Clock, MapPin, Pencil, Star, Tag, Trash2, X } from 'lucide-react';
import React, { useEffect } from 'react';
import type { EventDto, EventPriority } from '../services/apiService';
import './EventDetailModal.css';

type EventDetailModalProps = {
  isOpen: boolean;
  event: EventDto | null;
  onClose: () => void;
  onEdit: (event: EventDto) => void;
  onDelete: (event: EventDto) => void;
  isDeleting?: boolean;
  errorMessage?: string;
};

const formatFullDate = (date: Date) => {
  return new Intl.DateTimeFormat('es-ES', {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
    year: 'numeric'
  }).format(date);
};

const formatTime = (date: Date) => {
  return new Intl.DateTimeFormat('es-ES', {
    hour: '2-digit',
    minute: '2-digit',
    hour12: false
  }).format(date);
};

const EventDetailModal: React.FC<EventDetailModalProps> = ({
  isOpen,
  event,
  onClose,
  onEdit,
  onDelete,
  isDeleting = false,
  errorMessage
}) => {
  useEffect(() => {
    if (!isOpen) {
      return;
    }

    const handleKeyDown = (keyboardEvent: KeyboardEvent) => {
      if (keyboardEvent.key === 'Escape') {
        onClose();
      }
    };

    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [isOpen, onClose]);

  if (!isOpen || !event) {
    return null;
  }

  const start = new Date(event.startDate);
  const end = new Date(event.endDate);
  const isSameDay = start.toDateString() === end.toDateString();
  const categoryColor = event.eventCategory?.color ?? '#1447e6';
  const priorityValue: EventPriority = event.priority ?? 2;

  const priorityInfo: Record<EventPriority, { label: string; description: string; color: string }> = {
    1: { label: 'Baja', description: 'Sin urgencia especial', color: '#38bdf8' },
    2: { label: 'Media', description: 'Prioritario pero manejable', color: '#6366f1' },
    3: { label: 'Alta', description: 'Atención recomendada', color: '#f59e0b' },
    4: { label: 'Crítica', description: 'Requiere acción inmediata', color: '#f87171' }
  };

  const currentPriority = priorityInfo[priorityValue] ?? priorityInfo[2];

  const primaryDateText = event.isAllDay
    ? formatFullDate(start)
    : `${formatFullDate(start)}${isSameDay ? '' : ` • ${formatFullDate(end)}`}`;

  const timeRangeText = event.isAllDay
    ? 'Todo el día'
    : `${formatTime(start)} - ${formatTime(end)}`;

  const handleEditClick = () => {
    onEdit(event);
  };

  const handleDeleteClick = () => {
    onDelete(event);
  };

  return (
    <div className="event-detail-portal" role="dialog" aria-modal="true" aria-labelledby="event-detail-title">
      <div className="event-detail-backdrop" onClick={onClose} data-testid="event-detail-backdrop" />
      <div className="event-detail-modal" onClick={(e) => e.stopPropagation()}>
        <div className="event-detail-header" style={{ borderColor: categoryColor }}>
          <div className="event-detail-header-info">
            <span className="event-detail-category-dot" style={{ backgroundColor: categoryColor }} aria-hidden="true" />
            <div>
              <p className="event-detail-category" style={{ color: categoryColor }}>
                {event.eventCategory?.name ?? 'Sin categoría'}
              </p>
              <h2 id="event-detail-title" className="event-detail-title">{event.title}</h2>
            </div>
          </div>
          <button
            type="button"
            className="event-detail-close"
            onClick={onClose}
            aria-label="Cerrar detalle del evento"
          >
            <X size={18} />
          </button>
        </div>

        <div className="event-detail-body">
          <div className="event-detail-row">
            <Calendar size={18} className="event-detail-icon" aria-hidden="true" />
            <div>
              <p className="event-detail-label">Fecha</p>
              <p className="event-detail-value">{primaryDateText}</p>
            </div>
          </div>

          <div className="event-detail-row">
            <Clock size={18} className="event-detail-icon" aria-hidden="true" />
            <div>
              <p className="event-detail-label">Horario</p>
              <p className="event-detail-value">{timeRangeText}</p>
            </div>
          </div>

          {event.location && (
            <div className="event-detail-row">
              <MapPin size={18} className="event-detail-icon" aria-hidden="true" />
              <div>
                <p className="event-detail-label">Ubicación</p>
                <p className="event-detail-value">{event.location}</p>
              </div>
            </div>
          )}

          <div className="event-detail-row">
            <Tag size={18} className="event-detail-icon" aria-hidden="true" />
            <div>
              <p className="event-detail-label">Todo el día</p>
              <p className="event-detail-value">{event.isAllDay ? 'Sí' : 'No'}</p>
            </div>
          </div>

          <div className="event-detail-row">
            <Star size={18} className="event-detail-icon" aria-hidden="true" />
            <div>
              <p className="event-detail-label">Prioridad</p>
              <div className="event-detail-priority" style={{ '--priority-color': currentPriority.color } as React.CSSProperties}>
                <div className="event-detail-priority-stars" role="img" aria-label={`Prioridad ${currentPriority.label}`}>
                  {Array.from({ length: 4 }).map((_, index) => (
                    <Star
                      key={index}
                      size={14}
                      className={index < priorityValue ? 'filled' : ''}
                      aria-hidden="true"
                    />
                  ))}
                </div>
                <span className="event-detail-priority-label" style={{ color: currentPriority.color }}>
                  {currentPriority.label}
                </span>
              </div>
              <p className="event-detail-priority-description">{currentPriority.description}</p>
            </div>
          </div>

          {event.description && (
            <div className="event-detail-section">
              <p className="event-detail-label">Descripción</p>
              <p className="event-detail-description">{event.description}</p>
            </div>
          )}

          {event.notes && (
            <div className="event-detail-section">
              <p className="event-detail-label">Notas</p>
              <p className="event-detail-description">{event.notes}</p>
            </div>
          )}
        </div>

        {errorMessage && (
          <div className="event-detail-error" role="alert">
            {errorMessage}
          </div>
        )}

        <div className="event-detail-actions">
          <button
            type="button"
            className="event-detail-button event-detail-button-secondary"
            onClick={handleEditClick}
            disabled={isDeleting}
          >
            <Pencil size={16} />
            Editar
          </button>
          <button
            type="button"
            className="event-detail-button event-detail-button-danger"
            onClick={handleDeleteClick}
            disabled={isDeleting}
          >
            <Trash2 size={16} />
            {isDeleting ? 'Eliminando…' : 'Eliminar'}
          </button>
        </div>
      </div>
    </div>
  );
};

export default EventDetailModal;
