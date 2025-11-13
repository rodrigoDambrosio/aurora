import { ChevronLeft, ChevronRight, PenSquare, Smile, Sparkles, X } from 'lucide-react';
import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { formatMonthTitle } from '../lib/utils';
import {
  apiService,
  type DailyMoodEntryDto,
  type MonthlyMoodResponseDto,
  type UpsertDailyMoodRequestDto
} from '../services/apiService';
import './MonthlyMoodTracker.css';

interface MonthlyMoodTrackerProps {
  firstDayOfWeek?: number;
}

interface CalendarDay {
  date: Date;
  isCurrentMonth: boolean;
  isToday: boolean;
}

interface MoodOption {
  value: number;
  label: string;
  helper: string;
  emoji: string;
  color: string;
}

const MOOD_OPTIONS: MoodOption[] = [
  { value: 1, label: 'Muy mal', helper: 'Poquita energía', emoji: '😞', color: '#ef4444' },
  { value: 2, label: 'Mal', helper: 'Día desafiante', emoji: '🙁', color: '#f97316' },
  { value: 3, label: 'Neutral', helper: 'Todo normal', emoji: '😐', color: '#fbbf24' },
  { value: 4, label: 'Bien', helper: 'Estuviste en equilibrio', emoji: '🙂', color: '#34d399' },
  { value: 5, label: 'Excelente', helper: '¡Te sentiste genial!', emoji: '😄', color: '#22d3ee' }
];

const MAX_NOTES_LENGTH = 500;

const pad = (value: number) => value.toString().padStart(2, '0');

const normalizeDateKey = (value: Date | string) => {
  if (typeof value === 'string') {
    const parsed = new Date(value);
    return `${parsed.getUTCFullYear()}-${pad(parsed.getUTCMonth() + 1)}-${pad(parsed.getUTCDate())}`;
  }

  const utcDate = new Date(Date.UTC(value.getFullYear(), value.getMonth(), value.getDate()));
  return `${utcDate.getUTCFullYear()}-${pad(utcDate.getUTCMonth() + 1)}-${pad(utcDate.getUTCDate())}`;
};

const MonthlyMoodTracker: React.FC<MonthlyMoodTrackerProps> = ({ firstDayOfWeek = 1 }) => {
  const [currentMonth, setCurrentMonth] = useState(() => {
    const now = new Date();
    return new Date(now.getFullYear(), now.getMonth(), 1);
  });
  const [entries, setEntries] = useState<DailyMoodEntryDto[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [isEditorOpen, setIsEditorOpen] = useState(false);
  const [selectedDate, setSelectedDate] = useState<Date | null>(null);
  const [draftMoodRating, setDraftMoodRating] = useState<number | null>(null);
  const [draftNotes, setDraftNotes] = useState('');
  const [isSaving, setIsSaving] = useState(false);
  const [saveError, setSaveError] = useState<string | null>(null);

  const entriesByDate = useMemo(() => {
    const map = new Map<string, DailyMoodEntryDto>();
    entries.forEach(entry => {
      const key = normalizeDateKey(entry.entryDate);
      map.set(key, entry);
    });
    return map;
  }, [entries]);

  const weekDays = useMemo(() => {
    const allDays = ['Dom', 'Lun', 'Mar', 'Mié', 'Jue', 'Vie', 'Sáb'];
    return [...allDays.slice(firstDayOfWeek), ...allDays.slice(0, firstDayOfWeek)];
  }, [firstDayOfWeek]);

  const generateCalendarDays = useCallback((): CalendarDay[] => {
    const year = currentMonth.getFullYear();
    const month = currentMonth.getMonth();

    const firstDay = new Date(year, month, 1);
    const lastDay = new Date(year, month + 1, 0);

    let daysFromPrevMonth = firstDay.getDay() - firstDayOfWeek;
    if (daysFromPrevMonth < 0) {
      daysFromPrevMonth += 7;
    }

    const days: CalendarDay[] = [];
    const todayKey = normalizeDateKey(new Date());

    const prevMonthLastDay = new Date(year, month, 0).getDate();
    for (let i = daysFromPrevMonth - 1; i >= 0; i--) {
      const date = new Date(year, month - 1, prevMonthLastDay - i);
      days.push({
        date,
        isCurrentMonth: false,
        isToday: false
      });
    }

    for (let day = 1; day <= lastDay.getDate(); day++) {
      const date = new Date(year, month, day);
      const dateKey = normalizeDateKey(date);
      days.push({
        date,
        isCurrentMonth: true,
        isToday: dateKey === todayKey
      });
    }

    const remainingDays = 42 - days.length;
    for (let day = 1; day <= remainingDays; day++) {
      const date = new Date(year, month + 1, day);
      days.push({
        date,
        isCurrentMonth: false,
        isToday: false
      });
    }

    return days;
  }, [currentMonth, firstDayOfWeek]);

  const calendarDays = useMemo(() => generateCalendarDays(), [generateCalendarDays]);

  const loadMonthlyMood = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const response: MonthlyMoodResponseDto = await apiService.getMonthlyMoodEntries(
        currentMonth.getFullYear(),
        currentMonth.getMonth() + 1
      );
      setEntries(response.entries ?? []);
    } catch (loadError) {
      console.error('Error loading monthly mood entries', loadError);
      setError('No pudimos cargar el registro de estados de ánimo. Intentalo nuevamente.');
    } finally {
      setIsLoading(false);
    }
  }, [currentMonth]);

  useEffect(() => {
    loadMonthlyMood();
  }, [loadMonthlyMood]);

  const openEditorForDate = (date: Date) => {
    const key = normalizeDateKey(date);
    const existing = entriesByDate.get(key);
    setSelectedDate(date);
    setDraftMoodRating(existing?.moodRating ?? null);
    setDraftNotes(existing?.notes ?? '');
    setSaveError(null);
    setIsEditorOpen(true);
  };

  const closeEditor = () => {
    setIsEditorOpen(false);
    setSelectedDate(null);
    setDraftMoodRating(null);
    setDraftNotes('');
    setSaveError(null);
  };

  const handleSave = async () => {
    if (!selectedDate) {
      return;
    }

    if (draftMoodRating === null) {
      setSaveError('Seleccioná cómo te sentiste en el día.');
      return;
    }

    const payload: UpsertDailyMoodRequestDto = {
      entryDate: new Date(Date.UTC(
        selectedDate.getFullYear(),
        selectedDate.getMonth(),
        selectedDate.getDate()
      )).toISOString(),
      moodRating: draftMoodRating,
      notes: draftNotes.trim() ? draftNotes.trim() : null
    };

    try {
      setIsSaving(true);
      const updatedEntry = await apiService.upsertDailyMood(payload);
      setEntries(prev => {
        const selectedKey = normalizeDateKey(selectedDate);
        const existingIndex = prev.findIndex(entry => normalizeDateKey(entry.entryDate) === selectedKey);
        if (existingIndex >= 0) {
          const clone = [...prev];
          clone[existingIndex] = updatedEntry;
          return clone;
        }
        return [...prev, updatedEntry];
      });
      closeEditor();
    } catch (err) {
      console.error('Error saving mood entry', err);
      setSaveError(err instanceof Error ? err.message : 'No pudimos guardar el registro. Intentalo de nuevo.');
    } finally {
      setIsSaving(false);
    }
  };

  const handleDelete = async () => {
    if (!selectedDate) {
      return;
    }

    try {
      setIsSaving(true);
      await apiService.deleteDailyMood(new Date(Date.UTC(
        selectedDate.getFullYear(),
        selectedDate.getMonth(),
        selectedDate.getDate()
      )).toISOString());

      setEntries(prev => prev.filter(entry => normalizeDateKey(entry.entryDate) !== normalizeDateKey(selectedDate)));
      closeEditor();
    } catch (err) {
      console.error('Error deleting mood entry', err);
      setSaveError(err instanceof Error ? err.message : 'No pudimos eliminar el registro. Intentalo de nuevo.');
    } finally {
      setIsSaving(false);
    }
  };

  const goToPreviousMonth = () => {
    setCurrentMonth(prev => new Date(prev.getFullYear(), prev.getMonth() - 1, 1));
  };

  const goToNextMonth = () => {
    setCurrentMonth(prev => new Date(prev.getFullYear(), prev.getMonth() + 1, 1));
  };

  const goToToday = () => {
    const now = new Date();
    setCurrentMonth(new Date(now.getFullYear(), now.getMonth(), 1));
  };

  const renderMoodIndicator = (day: Date) => {
    const entry = entriesByDate.get(normalizeDateKey(day));
    if (!entry) {
      return null;
    }

    const option = MOOD_OPTIONS.find(opt => opt.value === entry.moodRating);
    if (!option) {
      return null;
    }

    return (
      <div
        className="mood-calendar-chip"
        style={{ backgroundColor: `${option.color}1f`, borderColor: `${option.color}66` }}
      >
        <span aria-hidden="true">{option.emoji}</span>
      </div>
    );
  };

  return (
    <div className="monthly-mood-tracker" aria-live="polite">
      <header className="mood-header">
        <div className="mood-header-left">
          <button type="button" className="mood-nav-btn" onClick={goToPreviousMonth} aria-label="Mes anterior">
            <ChevronLeft size={18} />
          </button>
          <button type="button" className="mood-nav-btn" onClick={goToNextMonth} aria-label="Mes siguiente">
            <ChevronRight size={18} />
          </button>
          <h2 className="mood-title">
            <Smile size={18} aria-hidden="true" />
            {formatMonthTitle(currentMonth)}
          </h2>
        </div>
        <div className="mood-header-right">
          <button type="button" className="mood-today-btn" onClick={goToToday}>
            Hoy
          </button>
        </div>
      </header>

      <section className="mood-intro" role="note">
        <Sparkles size={18} aria-hidden="true" />
        <div>
          <h3>Tu clima emocional del mes</h3>
          <p>Registrá cómo te sentiste cada día y agregá una nota breve para detectar patrones.</p>
        </div>
      </section>

      {error && (
        <div className="mood-error-banner" role="alert">
          {error}
        </div>
      )}

      <div className="mood-weekdays">
        {weekDays.map(day => (
          <span key={day} className="mood-weekday" aria-hidden="true">
            {day}
          </span>
        ))}
      </div>

      <div className="mood-grid" data-loading={isLoading}>
        {calendarDays.map((day, index) => {
          const dateKey = normalizeDateKey(day.date);
          const entry = entriesByDate.get(dateKey);
          const option = entry ? MOOD_OPTIONS.find(opt => opt.value === entry.moodRating) : undefined;

          return (
            <button
              key={dateKey + index}
              type="button"
              className={`mood-grid-day ${day.isCurrentMonth ? '' : 'is-other-month'} ${day.isToday ? 'is-today' : ''}`}
              onClick={() => day.isCurrentMonth && openEditorForDate(day.date)}
              aria-label={`Registrar estado de ánimo para el ${day.date.toLocaleDateString('es-ES')}${option ? `, actualmente ${option.label}` : ''}`}
            >
              <span className="mood-day-number">{day.date.getDate()}</span>
              {renderMoodIndicator(day.date)}
              {entry && (
                <span className="mood-day-label" style={{ color: option?.color ?? '#3b82f6' }}>
                  {option?.label ?? 'Sin registro'}
                </span>
              )}
            </button>
          );
        })}
      </div>

      <footer className="mood-legend" aria-hidden="true">
        {MOOD_OPTIONS.map(option => (
          <div key={option.value} className="mood-legend-item">
            <span className="mood-legend-emoji" aria-hidden="true">{option.emoji}</span>
            <span className="mood-legend-label">{option.label}</span>
          </div>
        ))}
      </footer>

      {isEditorOpen && selectedDate && (
        <div className="mood-editor-overlay" role="dialog" aria-modal="true">
          <div className="mood-editor-card">
            <header className="mood-editor-header">
              <div>
                <p className="mood-editor-title">{selectedDate.toLocaleDateString('es-ES', { weekday: 'long', day: 'numeric', month: 'long' })}</p>
                <span className="mood-editor-subtitle">¿Cómo te sentiste este día?</span>
              </div>
              <button type="button" className="mood-editor-close" onClick={closeEditor} aria-label="Cerrar">
                <X size={18} />
              </button>
            </header>

            <div className="mood-option-grid">
              {MOOD_OPTIONS.map(option => (
                <button
                  key={option.value}
                  type="button"
                  className={`mood-option-btn ${draftMoodRating === option.value ? 'is-selected' : ''}`}
                  onClick={() => {
                    setDraftMoodRating(option.value);
                    setSaveError(null);
                  }}
                >
                  <span className="mood-option-emoji" aria-hidden="true">{option.emoji}</span>
                  <div className="mood-option-texts">
                    <span className="mood-option-label">{option.label}</span>
                    <span className="mood-option-helper">{option.helper}</span>
                  </div>
                </button>
              ))}
            </div>

            <label className="mood-notes-field">
              <span>Nota del día (opcional)</span>
              <textarea
                value={draftNotes}
                onChange={(event) => {
                  const value = event.target.value.slice(0, MAX_NOTES_LENGTH);
                  setDraftNotes(value);
                }}
                placeholder="Anotá lo que influyó en tu día…"
                maxLength={MAX_NOTES_LENGTH}
              />
              <span className="mood-char-count">{draftNotes.length}/{MAX_NOTES_LENGTH}</span>
            </label>

            {saveError && (
              <div className="mood-editor-error" role="alert">
                {saveError}
              </div>
            )}

            <div className="mood-editor-actions">
              <button type="button" className="mood-save-btn" onClick={handleSave} disabled={isSaving}>
                {isSaving ? 'Guardando…' : 'Guardar registro'}
              </button>
              <button type="button" className="mood-cancel-btn" onClick={closeEditor} disabled={isSaving}>
                Cancelar
              </button>
              {entriesByDate.has(normalizeDateKey(selectedDate)) && (
                <button type="button" className="mood-delete-btn" onClick={handleDelete} disabled={isSaving}>
                  Quitar registro
                </button>
              )}
            </div>

            <div className="mood-editor-reminder">
              <PenSquare size={16} aria-hidden="true" />
              <p>Registrar tu estado de ánimo a diario te ayuda a detectar patrones y planificar mejor tus semanas.</p>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default MonthlyMoodTracker;
