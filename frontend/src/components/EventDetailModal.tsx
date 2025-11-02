import { Calendar, Clock, MapPin, Pencil, Sparkles, Star, Tag, Trash2, X } from 'lucide-react';
import React, { useEffect, useState } from 'react';
import type { EventDto, EventPriority, UpdateEventMoodDto } from '../services/apiService';
import './EventDetailModal.css';

type EventDetailModalProps = {
  isOpen: boolean;
  event: EventDto | null;
  onClose: () => void;
  onEdit: (event: EventDto) => void;
  onDelete: (event: EventDto) => void;
  onUpdateMood?: (eventId: string, payload: UpdateEventMoodDto) => Promise<EventDto>;
  isDeleting?: boolean;
  errorMessage?: string;
};

type MoodOption = {
  value: number;
  label: string;
  helper: string;
  emoji: string;
  color: string;
};

const MOOD_OPTIONS: MoodOption[] = [
  { value: 1, label: 'Muy mal', helper: 'La actividad te dejó con poca energía.', emoji: '😞', color: '#ef4444' },
  { value: 2, label: 'Mal', helper: 'No fue una experiencia agradable.', emoji: '🙁', color: '#f97316' },
  { value: 3, label: 'Neutral', helper: 'Fue una experiencia aceptable.', emoji: '😐', color: '#fbbf24' },
  { value: 4, label: 'Bien', helper: 'Terminaste con buena sensación.', emoji: '🙂', color: '#34d399' },
  { value: 5, label: 'Excelente', helper: 'Te sentiste genial al finalizar.', emoji: '😄', color: '#22d3ee' }
];

const MAX_MOOD_NOTES_LENGTH = 500;
const MOOD_REMINDER_WINDOW_HOURS = 48;

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
  onUpdateMood,
  isDeleting = false,
  errorMessage
}) => {
  const [displayMoodRating, setDisplayMoodRating] = useState<number | null>(event?.moodRating ?? null);
  const [displayMoodNotes, setDisplayMoodNotes] = useState<string>(event?.moodNotes ?? '');
  const [isMoodEditorVisible, setIsMoodEditorVisible] = useState(false);
  const [moodRatingDraft, setMoodRatingDraft] = useState<number | null>(event?.moodRating ?? null);
  const [moodNotesDraft, setMoodNotesDraft] = useState<string>(event?.moodNotes ?? '');
  const [isSavingMood, setIsSavingMood] = useState(false);
  const [moodError, setMoodError] = useState('');

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

  useEffect(() => {
    if (!event) {
      return;
    }

    setDisplayMoodRating(event.moodRating ?? null);
    setDisplayMoodNotes(event.moodNotes ?? '');
    setMoodRatingDraft(event.moodRating ?? null);
    setMoodNotesDraft(event.moodNotes ?? '');
    setIsMoodEditorVisible(false);
    setIsSavingMood(false);
    setMoodError('');
  }, [event]);

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

  const now = new Date();
  const eventFinished = end.getTime() <= now.getTime();
  const hoursSinceEnd = (now.getTime() - end.getTime()) / (1000 * 60 * 60);
  const showMoodReminder = !displayMoodRating && eventFinished && hoursSinceEnd >= 0 && hoursSinceEnd <= MOOD_REMINDER_WINDOW_HOURS;

  const activeMoodOption = MOOD_OPTIONS.find(option => option.value === displayMoodRating) ?? null;
  const editingMoodOption = MOOD_OPTIONS.find(option => option.value === moodRatingDraft) ?? null;

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

  const handleOpenMoodEditor = () => {
    setMoodRatingDraft(displayMoodRating);
    setMoodNotesDraft(displayMoodNotes);
    setMoodError('');
    setIsMoodEditorVisible(true);
  };

  const handleMoodOptionSelect = (value: number) => {
    setMoodRatingDraft(value);
    setMoodError('');
  };

  const handleCancelMood = () => {
    setIsMoodEditorVisible(false);
    setMoodRatingDraft(displayMoodRating);
    setMoodNotesDraft(displayMoodNotes);
    setMoodError('');
  };

  const handleSaveMood = async () => {
    if (moodRatingDraft === null) {
      setMoodError('Seleccioná cómo te sentiste durante la actividad.');
      return;
    }

    const trimmedNotes = moodNotesDraft.trim();
    const payload: UpdateEventMoodDto = {
      moodRating: moodRatingDraft,
      moodNotes: trimmedNotes.length > 0 ? trimmedNotes : null
    };

    try {
      setIsSavingMood(true);

      let updatedEvent: EventDto | null = null;
      if (onUpdateMood) {
        updatedEvent = await onUpdateMood(event.id, payload);
      }

      const resultingRating = updatedEvent?.moodRating ?? payload.moodRating ?? null;
      const resultingNotes = updatedEvent?.moodNotes ?? payload.moodNotes ?? null;

      setDisplayMoodRating(resultingRating);
      setDisplayMoodNotes(resultingNotes ?? '');
      setMoodRatingDraft(resultingRating);
      setMoodNotesDraft(resultingNotes ?? '');
      setIsMoodEditorVisible(false);
      setMoodError('');
    } catch (saveError) {
      const message = saveError instanceof Error
        ? saveError.message
        : 'No se pudo guardar el estado de ánimo. Intentalo nuevamente.';
      setMoodError(message);
    } finally {
      setIsSavingMood(false);
    }
  };

  const handleClearMood = async () => {
    try {
      setIsSavingMood(true);

      if (onUpdateMood) {
        await onUpdateMood(event.id, { moodRating: null, moodNotes: null });
      }

      setDisplayMoodRating(null);
      setDisplayMoodNotes('');
      setMoodRatingDraft(null);
      setMoodNotesDraft('');
      setIsMoodEditorVisible(false);
      setMoodError('');
    } catch (clearError) {
      const message = clearError instanceof Error
        ? clearError.message
        : 'No se pudo limpiar el registro.';
      setMoodError(message);
    } finally {
      setIsSavingMood(false);
    }
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

          <div className="event-detail-section event-detail-mood-section">
            <div className="event-detail-mood-header">
              <p className="event-detail-label">Estado de ánimo</p>
              {!isMoodEditorVisible && (
                <button
                  type="button"
                  className="event-detail-mood-edit"
                  onClick={handleOpenMoodEditor}
                >
                  {displayMoodRating ? 'Actualizar' : 'Registrar'}
                </button>
              )}
            </div>

            {showMoodReminder && !isMoodEditorVisible && (
              <div className="event-detail-mood-reminder" role="note">
                <Sparkles size={16} aria-hidden="true" />
                <div>
                  <p className="event-detail-mood-reminder-title">¿Cómo te sentiste después de este evento?</p>
                  <p className="event-detail-mood-reminder-text">Registrar tu estado de ánimo nos ayuda a ajustar futuras recomendaciones.</p>
                </div>
                <button
                  type="button"
                  className="event-detail-mood-reminder-button"
                  onClick={handleOpenMoodEditor}
                >
                  Registrar
                </button>
              </div>
            )}

            {isMoodEditorVisible ? (
              <div className="event-detail-mood-editor">
                <div className="event-detail-mood-options" role="group" aria-label="Seleccioná tu estado de ánimo">
                  {MOOD_OPTIONS.map(option => (
                    <button
                      key={option.value}
                      type="button"
                      className={`event-detail-mood-option${moodRatingDraft === option.value ? ' is-selected' : ''}`}
                      onClick={() => handleMoodOptionSelect(option.value)}
                      aria-pressed={moodRatingDraft === option.value}
                    >
                      <span className="event-detail-mood-emoji" aria-hidden="true">{option.emoji}</span>
                      <span className="event-detail-mood-option-info">
                        <span className="event-detail-mood-option-label">{option.label}</span>
                        <span className="event-detail-mood-option-helper">{option.helper}</span>
                      </span>
                    </button>
                  ))}
                </div>

                <label className="event-detail-mood-notes">
                  <span>Notas (opcional)</span>
                  <textarea
                    value={moodNotesDraft}
                    onChange={(e) => setMoodNotesDraft(e.target.value.slice(0, MAX_MOOD_NOTES_LENGTH))}
                    maxLength={MAX_MOOD_NOTES_LENGTH}
                    placeholder="Anota algo que quieras recordar de cómo te sentiste…"
                  />
                  <span className="event-detail-mood-charcount">
                    {moodNotesDraft.length}/{MAX_MOOD_NOTES_LENGTH}
                  </span>
                </label>

                {editingMoodOption && (
                  <p className="event-detail-mood-selected-hint">
                    {editingMoodOption.emoji} {editingMoodOption.helper}
                  </p>
                )}

                {moodError && (
                  <div className="event-detail-mood-error" role="alert">
                    {moodError}
                  </div>
                )}

                <div className="event-detail-mood-actions">
                  <button
                    type="button"
                    className="event-detail-mood-save"
                    onClick={handleSaveMood}
                    disabled={isSavingMood}
                  >
                    {isSavingMood ? 'Guardando…' : 'Guardar'}
                  </button>
                  <button
                    type="button"
                    className="event-detail-mood-cancel"
                    onClick={handleCancelMood}
                    disabled={isSavingMood}
                  >
                    Cancelar
                  </button>
                  {displayMoodRating !== null && (
                    <button
                      type="button"
                      className="event-detail-mood-clear"
                      onClick={handleClearMood}
                      disabled={isSavingMood}
                    >
                      Quitar registro
                    </button>
                  )}
                </div>
              </div>
            ) : (
              <div className="event-detail-mood-summary" aria-live="polite">
                {displayMoodRating ? (
                  <>
                    <div
                      className="event-detail-mood-chip"
                      style={{
                        borderColor: activeMoodOption?.color ?? '#6366f1',
                        backgroundColor: `${activeMoodOption?.color ?? '#6366f1'}14`
                      }}
                    >
                      <span className="event-detail-mood-emoji" aria-hidden="true">{activeMoodOption?.emoji ?? '🙂'}</span>
                      <div className="event-detail-mood-chip-texts">
                        <span className="event-detail-mood-chip-label">{activeMoodOption?.label ?? 'Registrado'}</span>
                        <span className="event-detail-mood-chip-helper">{activeMoodOption?.helper}</span>
                      </div>
                    </div>
                    {displayMoodNotes && (
                      <p className="event-detail-mood-notes-text">{displayMoodNotes}</p>
                    )}
                  </>
                ) : (
                  <p className="event-detail-mood-empty">Aún no registraste cómo te sentiste después de este evento.</p>
                )}
              </div>
            )}
          </div>
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
