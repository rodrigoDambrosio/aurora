import { MessageSquare, Send, Sparkles, X } from 'lucide-react';
import { AnimatePresence, motion } from 'motion/react';
import { useState } from 'react';
import './FloatingNLPInput.css';

interface FloatingNLPInputProps {
  onEventCreated: () => void;
}

interface ParsedEvent {
  title: string;
  date: string;
  time: string;
  category: string;
  confidence: number;
}

export function FloatingNLPInput({ onEventCreated }: FloatingNLPInputProps) {
  const [isOpen, setIsOpen] = useState(false);
  const [input, setInput] = useState('');
  const [isProcessing, setIsProcessing] = useState(false);
  const [parsedEvent, setParsedEvent] = useState<ParsedEvent | null>(null);

  const examples = [
    "reunión con cliente mañana a las 3pm",
    "gimnasio hoy 6:30pm por 1 hora",
    "estudiar React el miércoles de 2 a 4pm",
    "cena familiar el viernes 8pm"
  ];

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!input.trim()) return;

    setIsProcessing(true);

    // Simular procesamiento de NLP
    setTimeout(() => {
      // Mock parsing result
      const mockResult: ParsedEvent = {
        title: input.charAt(0).toUpperCase() + input.slice(1),
        date: 'Mañana',
        time: '15:00',
        category: 'Trabajo',
        confidence: 0.85
      };

      setParsedEvent(mockResult);
      setIsProcessing(false);
    }, 1500);
  };

  const handleConfirm = () => {
    // Simular creación del evento
    setTimeout(() => {
      onEventCreated();
      setInput('');
      setParsedEvent(null);
      setIsOpen(false);
    }, 500);
  };

  const handleCancel = () => {
    setInput('');
    setParsedEvent(null);
    setIsProcessing(false);
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

            {/* Input Form */}
            {!parsedEvent && (
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
                <div className="nlp-event-preview">
                  <div className="nlp-event-header">
                    <h4 className="nlp-event-title">{parsedEvent.title}</h4>
                    <span className="nlp-confidence-badge">
                      {(parsedEvent.confidence * 100).toFixed(0)}% seguro
                    </span>
                  </div>
                  <div className="nlp-event-details">
                    <p>📅 {parsedEvent.date}</p>
                    <p>🕐 {parsedEvent.time}</p>
                    <p>🏷️ {parsedEvent.category}</p>
                  </div>
                </div>

                <div className="nlp-actions">
                  <button
                    onClick={handleConfirm}
                    className="nlp-confirm-button"
                  >
                    Crear Evento
                  </button>
                  <button
                    onClick={handleCancel}
                    className="nlp-cancel-button"
                  >
                    Cancelar
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