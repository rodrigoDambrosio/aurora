import { MessageSquare, Send, Sparkles, X } from 'lucide-react';
import { AnimatePresence, motion } from 'motion/react';
import { type FormEvent, useState } from 'react';
import type { AIValidationResult, CreateEventDto } from '../services/apiService';
import { apiService } from '../services/apiService';
import './FloatingNLPInput.css';

interface FloatingNLPInputProps {
  onEventCreated: () => void;
}

interface ParsedEvent extends CreateEventDto {
  categoryName: string;
}

export function FloatingNLPInput({ onEventCreated }: FloatingNLPInputProps) {
  const [isOpen, setIsOpen] = useState(false);
  const [input, setInput] = useState('');
  const [isProcessing, setIsProcessing] = useState(false);
  const [parsedEvent, setParsedEvent] = useState<ParsedEvent | null>(null);
  const [validation, setValidation] = useState<AIValidationResult | null>(null);
  const [isCreating, setIsCreating] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [isEditing, setIsEditing] = useState(false);

  const examples = [
    "reunión con cliente mañana a las 3pm",
    "gimnasio hoy 6:30pm por 1 hora",
    "estudiar React el miércoles de 2 a 4pm",
    "cena familiar el viernes 8pm"
  ];

  const resetState = () => {
    setInput('');
    setParsedEvent(null);
    setValidation(null);
    setIsProcessing(false);
    setIsCreating(false);
    setError(null);
    setIsEditing(false);
    setIsOpen(false);
  };

  const formatDate = (isoDate: string) => {
    try {
      return new Intl.DateTimeFormat('es-ES', {
        weekday: 'long',
        day: 'numeric',
        month: 'long'
      }).format(new Date(isoDate));
    } catch {
      return 'Fecha no disponible';
    }
  };

  const formatTime = (isoDate: string) => {
    try {
      return new Intl.DateTimeFormat('es-ES', {
        hour: '2-digit',
        minute: '2-digit'
      }).format(new Date(isoDate));
    } catch {
      return '--:--';
    }
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!input.trim()) return;

    console.log('[FloatingNLPInput] Iniciando análisis:', input);
    setError(null);
    setIsProcessing(true);
    setParsedEvent(null);
    setValidation(null);

    try {
      // Llamar al backend para parsear el texto con IA
      console.log('[FloatingNLPInput] Llamando a parseNaturalLanguage...');
      const response = await apiService.parseNaturalLanguage(input);
      console.log('[FloatingNLPInput] Respuesta recibida:', response);

      if (!response.success || !response.event) {
        throw new Error(response.errorMessage || 'No se pudo interpretar el texto');
      }

      // Obtener el nombre de la categoría para mostrar
      console.log('[FloatingNLPInput] Obteniendo categorías...');
      const categories = await apiService.getEventCategories();
      const category = categories.find(c => c.id === response.event.eventCategoryId);

      setParsedEvent({
        ...response.event,
        categoryName: category?.name || 'Sin categoría'
      });

      if (response.validation) {
        setValidation(response.validation);
      }

      console.log('[FloatingNLPInput] Análisis completado exitosamente');
    } catch (err) {
      console.error('[FloatingNLPInput] Error interpretando el mensaje:', err);
      const message = err instanceof Error
        ? err.message
        : 'No pudimos interpretar tu solicitud. Inténtalo nuevamente.';
      setError(message);
    } finally {
      console.log('[FloatingNLPInput] Finalizando análisis, setIsProcessing(false)');
      setIsProcessing(false);
    }
  };

  const handleConfirm = async () => {
    if (!parsedEvent || isCreating) return;

    setError(null);
    setIsCreating(true);

    try {
      // El evento ya viene parseado del backend, solo crearlo
      const { categoryName, ...eventData } = parsedEvent;
      await apiService.createEvent(eventData);
      onEventCreated();
      resetState();
    } catch (err) {
      console.error('Error creando el evento:', err);
      const message = err instanceof Error
        ? err.message
        : 'No pudimos crear el evento. Inténtalo nuevamente.';
      setError(message);
    } finally {
      setIsCreating(false);
    }
  };

  const handleCancel = () => {
    setInput('');
    setParsedEvent(null);
    setValidation(null);
    setIsProcessing(false);
    setIsEditing(false);
    setError(null);
  };

  return (
    <>
      {/* Floating Button */}
      <AnimatePresence>
        {!isOpen && (
          <motion.div
            initial={{ scale: 0, rotate: -180 }}
            animate={{ scale: 1, rotate: 0 }}
            exit={{ scale: 0, rotate: 180 }}
          >
            <button
              onClick={() => setIsOpen(true)}
              className="floating-nlp-button"
            >
              <MessageSquare size={20} />
            </button>
          </motion.div>
        )}
      </AnimatePresence>

      {/* Expanded Input */}
      <AnimatePresence>
        {isOpen && (
          <motion.div
            initial={{ scale: 0, opacity: 0, y: 20 }}
            animate={{ scale: 1, opacity: 1, y: 0 }}
            exit={{ scale: 0, opacity: 0, y: 20 }}
            className="floating-nlp-card"
          >
            {/* Header */}
            <div className="nlp-header">
              <div className="nlp-header-left">
                <div className="nlp-sparkle-badge">
                  <Sparkles size={16} />
                </div>
                <span className="nlp-title">Crear con IA</span>
              </div>
              <button
                onClick={() => setIsOpen(false)}
                className="nlp-close-button"
              >
                <X size={16} />
              </button>
            </div>

            {/* Error State */}
            {error && (
              <div className="nlp-feedback nlp-feedback-error">
                <p>{error}</p>
                <button onClick={() => setError(null)} className="nlp-feedback-close">
                  <X size={14} />
                </button>
              </div>
            )}

            {/* Input Form */}
            {!parsedEvent && !isProcessing && (
              <div className="nlp-form">
                <form onSubmit={handleSubmit}>
                  <div className="nlp-input-container">
                    <input
                      value={input}
                      onChange={(e) => setInput(e.target.value)}
                      placeholder="Ej: reunión mañana 3pm..."
                      className="nlp-input"
                      disabled={isProcessing}
                      autoFocus
                    />
                    <button
                      type="submit"
                      className="nlp-submit-button"
                      disabled={!input.trim() || isProcessing}
                    >
                      {isProcessing ? (
                        <div className="nlp-spinner nlp-spinner-small" />
                      ) : (
                        <Send size={16} />
                      )}
                    </button>
                  </div>
                </form>

                {/* Examples */}
                <div className="nlp-examples">
                  <span className="nlp-examples-label">Ejemplos:</span>
                  <div className="nlp-examples-container">
                    {examples.map((example, index) => (
                      <button
                        key={index}
                        onClick={() => setInput(example)}
                        className="nlp-example-button"
                        disabled={isProcessing}
                      >
                        {example}
                      </button>
                    ))}
                  </div>
                </div>
              </div>
            )}

            {/* Processing State */}
            {isProcessing && (
              <div className="nlp-processing">
                <div className="nlp-processing-content">
                  <div className="nlp-spinner" />
                  <p className="nlp-processing-text">
                    Analizando tu solicitud...
                  </p>
                </div>
              </div>
            )}

            {/* Confirmation */}
            {parsedEvent && !isProcessing && (
              <div className="nlp-confirmation">
                {/* Validation Feedback */}
                {validation && validation.recommendationMessage && (
                  <div className={`nlp-validation nlp-validation-${(validation.severity || 'Info').toLowerCase()}`}>
                    <div className="nlp-validation-header">
                      {validation.severity === 'Critical' && '⚠️'}
                      {validation.severity === 'Warning' && '⚡'}
                      {(!validation.severity || validation.severity === 'Info') && 'ℹ️'}
                      <span className="nlp-validation-severity">
                        {validation.severity === 'Critical' ? 'Conflicto' :
                          validation.severity === 'Warning' ? 'Advertencia' : 'Información'}
                      </span>
                    </div>
                    <p className="nlp-validation-message">{validation.recommendationMessage}</p>
                    {validation.suggestions && validation.suggestions.length > 0 && (
                      <ul className="nlp-validation-suggestions">
                        {validation.suggestions.map((suggestion, idx) => (
                          <li key={idx}>{suggestion}</li>
                        ))}
                      </ul>
                    )}
                  </div>
                )}

                {/* Event Preview or Edit Mode */}
                {!isEditing ? (
                  <div className="nlp-event-preview">
                    <div className="nlp-event-header">
                      <h4 className="nlp-event-title">{parsedEvent.title}</h4>
                      <span className="nlp-confidence-badge">
                        Parseado con IA ✨
                      </span>
                    </div>
                    <div className="nlp-event-details">
                      <p>📅 {formatDate(parsedEvent.startDate)}</p>
                      <p>🕐 {formatTime(parsedEvent.startDate)} - {formatTime(parsedEvent.endDate)}</p>
                      <p>🏷️ {parsedEvent.categoryName}</p>
                    </div>
                  </div>
                ) : (
                  <div className="nlp-edit-form">
                    <div className="nlp-form-field">
                      <label className="nlp-form-label">Título</label>
                      <input
                        type="text"
                        value={parsedEvent.title}
                        onChange={(e) => setParsedEvent({ ...parsedEvent, title: e.target.value })}
                        className="nlp-form-input"
                      />
                    </div>

                    <div className="nlp-form-row">
                      <div className="nlp-form-field">
                        <label className="nlp-form-label">Fecha</label>
                        <input
                          type="date"
                          value={parsedEvent.startDate.split('T')[0]}
                          onChange={(e) => {
                            const [, time] = parsedEvent.startDate.split('T');
                            const startTime = new Date(`${e.target.value}T${time}`);
                            const endTime = new Date(startTime);
                            endTime.setTime(new Date(parsedEvent.endDate).getTime());
                            setParsedEvent({
                              ...parsedEvent,
                              startDate: startTime.toISOString(),
                              endDate: endTime.toISOString()
                            });
                          }}
                          className="nlp-form-input"
                        />
                      </div>

                      <div className="nlp-form-field">
                        <label className="nlp-form-label">Hora inicio</label>
                        <input
                          type="time"
                          value={parsedEvent.startDate.split('T')[1]?.substring(0, 5) || ''}
                          onChange={(e) => {
                            const [date] = parsedEvent.startDate.split('T');
                            const startTime = new Date(`${date}T${e.target.value}:00.000Z`);
                            const duration = new Date(parsedEvent.endDate).getTime() - new Date(parsedEvent.startDate).getTime();
                            const endTime = new Date(startTime.getTime() + duration);
                            setParsedEvent({
                              ...parsedEvent,
                              startDate: startTime.toISOString(),
                              endDate: endTime.toISOString()
                            });
                          }}
                          className="nlp-form-input"
                        />
                      </div>
                    </div>

                    <div className="nlp-form-field">
                      <label className="nlp-form-label">Duración (minutos)</label>
                      <input
                        type="number"
                        min="15"
                        step="15"
                        value={Math.round((new Date(parsedEvent.endDate).getTime() - new Date(parsedEvent.startDate).getTime()) / 60000)}
                        onChange={(e) => {
                          const duration = parseInt(e.target.value) * 60000;
                          const endTime = new Date(new Date(parsedEvent.startDate).getTime() + duration);
                          setParsedEvent({ ...parsedEvent, endDate: endTime.toISOString() });
                        }}
                        className="nlp-form-input"
                      />
                    </div>
                  </div>
                )}

                <div className="nlp-actions">
                  <button
                    onClick={handleCancel}
                    className="nlp-cancel-button"
                    disabled={isCreating}
                  >
                    Cancelar
                  </button>

                  <button
                    onClick={() => setIsEditing(!isEditing)}
                    className="nlp-edit-toggle-button"
                    disabled={isCreating}
                  >
                    {isEditing ? '👁️ Ver' : '✏️ Editar'}
                  </button>

                  <button
                    onClick={handleConfirm}
                    className={`nlp-confirm-button ${!validation?.isApproved ? 'nlp-button-warning' : ''}`}
                    disabled={isCreating || !parsedEvent.title.trim()}
                  >
                    {isCreating ? 'Creando...' : (validation?.isApproved ? 'Confirmar' : 'Crear de todas formas')}
                  </button>
                </div>
              </div>
            )}
          </motion.div>
        )}
      </AnimatePresence>
    </>
  );
}