import { AlertTriangle, Calendar, CheckCircle2, Clock, Target, TrendingUp, X } from 'lucide-react';
import { AnimatePresence, motion } from 'motion/react';
import { useState } from 'react';
import type { CreateEventDto, GeneratePlanResponseDto } from '../services/apiService';
import { apiService } from '../services/apiService';
import './GeneratePlanModal.css';

interface GeneratePlanModalProps {
  isOpen: boolean;
  onClose: () => void;
  onPlanCreated: () => void;
}

interface PlanOptions {
  startDate?: string;
  durationWeeks?: number;
  sessionsPerWeek?: number;
  sessionDurationMinutes?: number;
  preferredTimeOfDay?: string;
}

export function GeneratePlanModal({ isOpen, onClose, onPlanCreated }: GeneratePlanModalProps) {
  const [step, setStep] = useState<'input' | 'preview' | 'creating'>('input');
  const [goal, setGoal] = useState('');
  const [options, setOptions] = useState<PlanOptions>({});
  const [plan, setPlan] = useState<GeneratePlanResponseDto | null>(null);
  const [isGenerating, setIsGenerating] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [editedEvents, setEditedEvents] = useState<CreateEventDto[]>([]);

  const examples = [
    "Aprender a tocar la guitarra desde cero",
    "Entrenar para correr una maratón de 42km",
    "Dominar desarrollo web con React y Node.js",
    "Mejorar mi nivel de inglés conversacional",
    "Crear hábito de meditación diaria"
  ];

  const resetState = () => {
    setStep('input');
    setGoal('');
    setOptions({});
    setPlan(null);
    setError(null);
    setEditedEvents([]);
    setIsGenerating(false);
  };

  const handleClose = () => {
    resetState();
    onClose();
  };

  const handleGeneratePlan = async () => {
    if (!goal.trim()) {
      setError('Por favor ingresa un objetivo');
      return;
    }

    setIsGenerating(true);
    setError(null);

    try {
      const generatedPlan = await apiService.generatePlan(goal, options);
      setPlan(generatedPlan);
      setEditedEvents([...generatedPlan.events]);
      setStep('preview');
    } catch (err) {
      console.error('Error al generar plan:', err);
      setError(err instanceof Error ? err.message : 'Error al generar el plan con IA');
    } finally {
      setIsGenerating(false);
    }
  };

  const handleCreatePlan = async () => {
    if (!plan || editedEvents.length === 0) return;

    setStep('creating');
    setError(null);

    try {
      // Crear todos los eventos en lote
      const promises = editedEvents.map(event => apiService.createEvent(event));
      await Promise.all(promises);

      // Notificar éxito y cerrar
      onPlanCreated();
      handleClose();
    } catch (err) {
      console.error('Error al crear eventos del plan:', err);
      setError(err instanceof Error ? err.message : 'Error al crear los eventos');
      setStep('preview');
    }
  };

  const handleEventEdit = (index: number, field: keyof CreateEventDto, value: unknown) => {
    const updated = [...editedEvents];
    (updated[index] as unknown as Record<string, unknown>)[field] = value;
    setEditedEvents(updated);
  };

  const handleRemoveEvent = (index: number) => {
    const updated = editedEvents.filter((_, i) => i !== index);
    setEditedEvents(updated);
  };

  return (
    <AnimatePresence>
      {isOpen && (
        <>
          {/* Backdrop */}
          <motion.div
            className="generate-plan-backdrop"
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            onClick={handleClose}
          />

          {/* Modal */}
          <motion.div
            className="generate-plan-modal"
            initial={{ opacity: 0, scale: 0.95, y: 20 }}
            animate={{ opacity: 1, scale: 1, y: 0 }}
            exit={{ opacity: 0, scale: 0.95, y: 20 }}
            transition={{ type: 'spring', damping: 25, stiffness: 300 }}
          >
            {/* Header */}
            <div className="generate-plan-header">
              <div className="generate-plan-header-content">
                <Target className="generate-plan-icon" />
                <div>
                  <h2 className="generate-plan-title">Generar Plan Multi-Día</h2>
                  <p className="generate-plan-subtitle">
                    {step === 'input' && 'Transforma tu objetivo en un plan estructurado'}
                    {step === 'preview' && 'Revisa y ajusta tu plan antes de crearlo'}
                    {step === 'creating' && 'Creando eventos del plan...'}
                  </p>
                </div>
              </div>
              <button className="generate-plan-close" onClick={handleClose} aria-label="Cerrar">
                <X size={20} />
              </button>
            </div>

            {/* Content */}
            <div className="generate-plan-content">
              {/* Step 1: Input Goal */}
              {step === 'input' && (
                <div className="generate-plan-input-step">
                  <div className="generate-plan-goal-section">
                    <label htmlFor="goal-input" className="generate-plan-label">
                      ¿Qué objetivo querés alcanzar?
                    </label>
                    <textarea
                      id="goal-input"
                      className="generate-plan-textarea"
                      placeholder="Ej: Aprender a tocar la guitarra desde cero"
                      value={goal}
                      onChange={(e) => setGoal(e.target.value)}
                      rows={3}
                      disabled={isGenerating}
                    />

                    {/* Examples */}
                    <div className="generate-plan-examples">
                      <p className="generate-plan-examples-label">Ejemplos:</p>
                      <div className="generate-plan-examples-list">
                        {examples.map((example, idx) => (
                          <button
                            key={idx}
                            className="generate-plan-example-chip"
                            onClick={() => setGoal(example)}
                            disabled={isGenerating}
                          >
                            {example}
                          </button>
                        ))}
                      </div>
                    </div>
                  </div>

                  {/* Options */}
                  <div className="generate-plan-options">
                    <p className="generate-plan-options-title">Preferencias (opcional)</p>
                    <div className="generate-plan-options-grid">
                      <div className="generate-plan-option">
                        <label htmlFor="start-date">
                          <Calendar size={16} />
                          Fecha de inicio
                        </label>
                        <input
                          id="start-date"
                          type="date"
                          min={new Date().toISOString().split('T')[0]}
                          value={options.startDate ?? ''}
                          onChange={(e) => setOptions({ ...options, startDate: e.target.value || undefined })}
                          disabled={isGenerating}
                        />
                      </div>

                      <div className="generate-plan-option">
                        <label htmlFor="duration-weeks">
                          <Calendar size={16} />
                          Duración (semanas)
                        </label>
                        <input
                          id="duration-weeks"
                          type="number"
                          min="1"
                          max="52"
                          placeholder="Auto"
                          value={options.durationWeeks ?? ''}
                          onChange={(e) => setOptions({ ...options, durationWeeks: e.target.value ? parseInt(e.target.value) : undefined })}
                          disabled={isGenerating}
                        />
                      </div>

                      <div className="generate-plan-option">
                        <label htmlFor="sessions-per-week">
                          <TrendingUp size={16} />
                          Sesiones/semana
                        </label>
                        <input
                          id="sessions-per-week"
                          type="number"
                          min="1"
                          max="14"
                          placeholder="Auto"
                          value={options.sessionsPerWeek ?? ''}
                          onChange={(e) => setOptions({ ...options, sessionsPerWeek: e.target.value ? parseInt(e.target.value) : undefined })}
                          disabled={isGenerating}
                        />
                      </div>

                      <div className="generate-plan-option">
                        <label htmlFor="session-duration">
                          <Clock size={16} />
                          Duración sesión (min)
                        </label>
                        <input
                          id="session-duration"
                          type="number"
                          min="15"
                          max="480"
                          step="15"
                          placeholder="Auto"
                          value={options.sessionDurationMinutes ?? ''}
                          onChange={(e) => setOptions({ ...options, sessionDurationMinutes: e.target.value ? parseInt(e.target.value) : undefined })}
                          disabled={isGenerating}
                        />
                      </div>

                      <div className="generate-plan-option">
                        <label htmlFor="preferred-time">
                          <Clock size={16} />
                          Horario preferido
                        </label>
                        <input
                          id="preferred-time"
                          type="time"
                          placeholder="Auto"
                          value={options.preferredTimeOfDay ?? ''}
                          onChange={(e) => setOptions({ ...options, preferredTimeOfDay: e.target.value || undefined })}
                          disabled={isGenerating}
                        />
                      </div>
                    </div>
                  </div>

                  {/* Error */}
                  {error && (
                    <div className="generate-plan-error">
                      <AlertTriangle size={16} />
                      {error}
                    </div>
                  )}

                  {/* Actions */}
                  <div className="generate-plan-actions">
                    <button
                      className="generate-plan-button secondary"
                      onClick={handleClose}
                      disabled={isGenerating}
                    >
                      Cancelar
                    </button>
                    <button
                      className="generate-plan-button primary"
                      onClick={handleGeneratePlan}
                      disabled={isGenerating || !goal.trim()}
                    >
                      {isGenerating ? (
                        <>
                          <span className="generate-plan-spinner" />
                          Generando plan...
                        </>
                      ) : (
                        <>
                          <Target size={18} />
                          Generar Plan
                        </>
                      )}
                    </button>
                  </div>
                </div>
              )}

              {/* Step 2: Preview Plan */}
              {step === 'preview' && plan && (
                <div className="generate-plan-preview-step">
                  {/* Plan Summary */}
                  <div className="generate-plan-summary">
                    <h3 className="generate-plan-summary-title">{plan.planTitle}</h3>
                    <p className="generate-plan-summary-description">{plan.planDescription}</p>

                    <div className="generate-plan-summary-stats">
                      <div className="generate-plan-stat">
                        <Calendar size={16} />
                        <span>{plan.durationWeeks} semanas</span>
                      </div>
                      <div className="generate-plan-stat">
                        <TrendingUp size={16} />
                        <span>{editedEvents.length} sesiones</span>
                      </div>
                    </div>

                    {plan.additionalTips && (
                      <div className="generate-plan-tips">
                        <CheckCircle2 size={16} />
                        <p>{plan.additionalTips}</p>
                      </div>
                    )}

                    {/* Conflicts Warning */}
                    {plan.hasPotentialConflicts && (
                      <div className="generate-plan-conflicts">
                        <AlertTriangle size={16} />
                        <div>
                          <p className="generate-plan-conflicts-title">Posibles conflictos detectados:</p>
                          <ul className="generate-plan-conflicts-list">
                            {plan.conflictWarnings.map((warning, idx) => (
                              <li key={idx}>{warning}</li>
                            ))}
                          </ul>
                        </div>
                      </div>
                    )}
                  </div>

                  {/* Events List */}
                  <div className="generate-plan-events">
                    <h4 className="generate-plan-events-title">
                      Sesiones del plan ({editedEvents.length})
                    </h4>
                    <div className="generate-plan-events-list">
                      {editedEvents.map((event, idx) => (
                        <div key={idx} className="generate-plan-event-card">
                          <div className="generate-plan-event-header">
                            <span className="generate-plan-event-number">#{idx + 1}</span>
                            <button
                              className="generate-plan-event-remove"
                              onClick={() => handleRemoveEvent(idx)}
                              aria-label="Eliminar sesión"
                            >
                              <X size={16} />
                            </button>
                          </div>

                          <input
                            type="text"
                            className="generate-plan-event-title"
                            value={event.title}
                            onChange={(e) => handleEventEdit(idx, 'title', e.target.value)}
                          />

                          <textarea
                            className="generate-plan-event-description"
                            value={event.description || ''}
                            onChange={(e) => handleEventEdit(idx, 'description', e.target.value)}
                            rows={2}
                            placeholder="Descripción..."
                          />

                          <div className="generate-plan-event-datetime">
                            <div className="generate-plan-event-datetime-group">
                              <label>
                                <Calendar size={14} />
                                Fecha
                              </label>
                              <input
                                type="date"
                                value={new Date(event.startDate).toISOString().split('T')[0]}
                                onChange={(e) => {
                                  const newDate = e.target.value;
                                  const oldStart = new Date(event.startDate);
                                  const oldEnd = new Date(event.endDate);

                                  // Mantener la hora, cambiar solo la fecha
                                  const [year, month, day] = newDate.split('-').map(Number);
                                  const newStart = new Date(oldStart);
                                  newStart.setFullYear(year, month - 1, day);

                                  const newEnd = new Date(oldEnd);
                                  newEnd.setFullYear(year, month - 1, day);

                                  handleEventEdit(idx, 'startDate', newStart.toISOString());
                                  handleEventEdit(idx, 'endDate', newEnd.toISOString());
                                }}
                              />
                            </div>

                            <div className="generate-plan-event-datetime-group">
                              <label>
                                <Clock size={14} />
                                Inicio
                              </label>
                              <input
                                type="time"
                                value={new Date(event.startDate).toISOString().slice(11, 16)}
                                onChange={(e) => {
                                  const [hours, minutes] = e.target.value.split(':').map(Number);
                                  const newStart = new Date(event.startDate);
                                  newStart.setHours(hours, minutes);
                                  handleEventEdit(idx, 'startDate', newStart.toISOString());
                                }}
                              />
                            </div>

                            <div className="generate-plan-event-datetime-group">
                              <label>
                                <Clock size={14} />
                                Fin
                              </label>
                              <input
                                type="time"
                                value={new Date(event.endDate).toISOString().slice(11, 16)}
                                onChange={(e) => {
                                  const [hours, minutes] = e.target.value.split(':').map(Number);
                                  const newEnd = new Date(event.endDate);
                                  newEnd.setHours(hours, minutes);
                                  handleEventEdit(idx, 'endDate', newEnd.toISOString());
                                }}
                              />
                            </div>
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>

                  {/* Error */}
                  {error && (
                    <div className="generate-plan-error">
                      <AlertTriangle size={16} />
                      {error}
                    </div>
                  )}

                  {/* Actions */}
                  <div className="generate-plan-actions">
                    <button
                      className="generate-plan-button secondary"
                      onClick={() => setStep('input')}
                    >
                      Volver
                    </button>
                    <button
                      className="generate-plan-button primary"
                      onClick={handleCreatePlan}
                      disabled={editedEvents.length === 0}
                    >
                      <CheckCircle2 size={18} />
                      Crear {editedEvents.length} evento{editedEvents.length !== 1 ? 's' : ''}
                    </button>
                  </div>
                </div>
              )}

              {/* Step 3: Creating */}
              {step === 'creating' && (
                <div className="generate-plan-creating-step">
                  <div className="generate-plan-spinner-large" />
                  <p className="generate-plan-creating-text">
                    Creando {editedEvents.length} eventos del plan...
                  </p>
                </div>
              )}
            </div>
          </motion.div>
        </>
      )}
    </AnimatePresence>
  );
}
