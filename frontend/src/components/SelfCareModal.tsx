import { Loader2, Play, Wand2, X } from 'lucide-react';
import { useEffect, useState, type CSSProperties } from 'react';
import type {
  SelfCareFeedbackDto,
  SelfCareRecommendationDto,
  SelfCareRequestDto
} from '../services/apiService';
import { apiService, SelfCareFeedbackAction } from '../services/apiService';
import './SelfCareModal.css';
import { Button } from './ui/button';
import { Card } from './ui/card';

interface SelfCareModalProps {
  isOpen: boolean;
  onClose: () => void;
  currentMood?: number;
  onScheduleActivity: (recommendation: SelfCareRecommendationDto) => void;
  onStartTimer: (recommendation: SelfCareRecommendationDto) => void;
}

export default function SelfCareModal({
  isOpen,
  onClose,
  currentMood,
  onScheduleActivity,
  onStartTimer
}: SelfCareModalProps) {
  const [recommendations, setRecommendations] = useState<SelfCareRecommendationDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [refreshSuccess, setRefreshSuccess] = useState(false);
  const [hasLoaded, setHasLoaded] = useState(false);

  const getTypeBadgeClass = (typeDescription: string) => {
    const normalized = typeDescription?.toLowerCase() ?? '';

    if (normalized.includes('mental') || normalized.includes('mente')) {
      return 'self-care-type-chip self-care-type-chip--mental';
    }

    if (normalized.includes('creativ') || normalized.includes('arte')) {
      return 'self-care-type-chip self-care-type-chip--creative';
    }

    if (normalized.includes('descans') || normalized.includes('relaj')) {
      return 'self-care-type-chip self-care-type-chip--rest';
    }

    if (normalized.includes('fis') || normalized.includes('energ')) {
      return 'self-care-type-chip self-care-type-chip--physical';
    }

    if (normalized.includes('product') || normalized.includes('focus')) {
      return 'self-care-type-chip self-care-type-chip--productive';
    }

    return 'self-care-type-chip self-care-type-chip--default';
  };

  const normalizeConfidence = (value?: number | null) => {
    return Math.min(100, Math.max(0, value ?? 0));
  };

  const formatDuration = (minutes?: number | null) => {
    if (!minutes || minutes <= 0) return 'Flexible';
    if (minutes === 1) return '1 min';
    return `${minutes} min`;
  };

  // Cargar recomendaciones solo la primera vez que se abre el modal
  useEffect(() => {
    if (isOpen && !hasLoaded) {
      loadRecommendations();
      setHasLoaded(true);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isOpen]);

  const loadRecommendations = async () => {
    setLoading(true);
    setError(null);

    try {
      const request: SelfCareRequestDto = {
        currentMood,
        count: 5
      };

      const recs = await apiService.getSelfCareRecommendations(request);
      setRecommendations(recs);
    } catch (err) {
      console.error('Error loading self-care recommendations:', err);
      setError('No se pudieron cargar las sugerencias. Mostrando opciones genéricas.');

      // Fallback a sugerencias genéricas
      try {
        const genericRecs = await apiService.getGenericSelfCare(5);
        setRecommendations(genericRecs);
      } catch (fallbackErr) {
        console.error('Error loading generic recommendations:', fallbackErr);
        setError('No se pudieron cargar las sugerencias en este momento.');
      }
    } finally {
      setLoading(false);
    }
  };

  const handleRefresh = async () => {
    setRecommendations([]); // Limpia las actuales
    setRefreshSuccess(false);
    await loadRecommendations(); // Carga nuevas

    // Mostrar feedback de éxito
    setRefreshSuccess(true);
    setTimeout(() => setRefreshSuccess(false), 3000); // Ocultar después de 3s
  };

  const handleDismiss = async (recommendation: SelfCareRecommendationDto) => {
    const feedback: SelfCareFeedbackDto = {
      recommendationId: recommendation.id,
      action: SelfCareFeedbackAction.Dismissed,
      timestamp: new Date().toISOString()
    };

    try {
      await apiService.registerSelfCareFeedback(feedback);

      // Remover de la lista
      setRecommendations(prev => prev.filter(r => r.id !== recommendation.id));
    } catch (err) {
      console.error('Error registering feedback:', err);
    }
  };

  const handleSchedule = (recommendation: SelfCareRecommendationDto) => {
    const feedback: SelfCareFeedbackDto = {
      recommendationId: recommendation.id,
      action: SelfCareFeedbackAction.Scheduled,
      timestamp: new Date().toISOString()
    };

    apiService.registerSelfCareFeedback(feedback).catch((err: unknown) => {
      console.error('Error registering feedback:', err);
    });

    onScheduleActivity(recommendation);
    onClose();
  };

  const handleStartNow = (recommendation: SelfCareRecommendationDto) => {
    const feedback: SelfCareFeedbackDto = {
      recommendationId: recommendation.id,
      action: SelfCareFeedbackAction.CompletedNow,
      timestamp: new Date().toISOString()
    };

    apiService.registerSelfCareFeedback(feedback).catch((err: unknown) => {
      console.error('Error registering feedback:', err);
    });

    onStartTimer(recommendation);
    onClose();
  };

  if (!isOpen) return null;

  return (
    <div
      className="fixed inset-0 z-50 flex items-end sm:items-center justify-center self-care-modal-backdrop"
      onClick={onClose}
    >
      <div
        className="self-care-modal-container"
        onClick={(event) => event.stopPropagation()}
      >
        <div className="self-care-modal-surface">
          <button
            type="button"
            onClick={onClose}
            className="self-care-modal-close-btn"
            aria-label="Cerrar"
          >
            <X className="h-4 w-4" />
          </button>

          <header className="self-care-modal-header">
            <div className="self-care-header-left">
              <span className="self-care-plan-label">
                <span className="self-care-plan-indicator" aria-hidden="true" />
                Plan de bienestar
              </span>
              <h2 className="self-care-modal-title">Autocuidado</h2>
              <p className="self-care-modal-subtitle">
                Sugerencias personalizadas basadas en tus patrones y necesidades actuales
              </p>
            </div>
            <div className="self-care-header-right">
              <button
                type="button"
                onClick={handleRefresh}
                disabled={loading}
                className="self-care-refresh-btn"
              >
                <Wand2 className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />
                <span>{loading ? 'Generando...' : 'Más ideas'}</span>
              </button>
            </div>
          </header>

          <main className="self-care-modal-body self-care-scrollable" aria-live="polite">
            <div className="self-care-content">
              {refreshSuccess && !loading && (
                <div className="self-care-banner self-care-banner--success">
                  <span className="self-care-banner-icon" aria-hidden="true">✨</span>
                  <p>Sugerencias actualizadas</p>
                </div>
              )}

              {error && (
                <div className="self-care-banner self-care-banner--warning">
                  <span className="self-care-banner-icon" aria-hidden="true">⚠️</span>
                  <p>{error}</p>
                </div>
              )}

              {loading && (
                <div className="self-care-loading">
                  <Loader2 className="h-8 w-8 animate-spin" />
                  <p>Personalizando sugerencias...</p>
                </div>
              )}

              {!loading && recommendations.length === 0 && (
                <div className="self-care-empty-state">
                  <div className="self-care-empty-emoji" aria-hidden="true">🌿</div>
                  <h3>Sin sugerencias por ahora</h3>
                  <p>Actualizá la lista para descubrir nuevas ideas.</p>
                  <Button type="button" variant="ghost" onClick={loadRecommendations}>
                    Intentar de nuevo
                  </Button>
                </div>
              )}

              {!loading && recommendations.map((rec) => {
                const confidence = normalizeConfidence(rec.confidenceScore);
                const progressStyle = {
                  '--progress': `${confidence}`
                } as CSSProperties;

                return (
                  <Card key={rec.id} className="self-care-card">
                    <div className="self-care-card-shell">
                      <header className="self-care-card-top">
                        <div className="self-care-card-summary">
                          <div className="self-care-card-icon" aria-hidden="true">
                            {rec.icon}
                          </div>
                          <div className="self-care-card-heading">
                            <div className="self-care-card-title-row">
                              <h3 className="self-care-card-title">{rec.title}</h3>
                              {rec.typeDescription && (
                                <span className={getTypeBadgeClass(rec.typeDescription)}>
                                  {rec.typeDescription}
                                </span>
                              )}
                            </div>
                            <p className="self-care-card-description">{rec.description}</p>
                          </div>
                        </div>
                        <div className="self-care-progress" aria-hidden="true">
                          <div className="self-care-progress-circle" style={progressStyle}>
                            <span className="self-care-progress-value">{confidence}%</span>
                          </div>
                          <span className="self-care-progress-label">Confianza</span>
                        </div>
                      </header>

                      {rec.personalizedReason && (
                        <div className="self-care-card-reason">
                          <span className="self-care-card-reason-icon" aria-hidden="true">💡</span>
                          <p>{rec.personalizedReason}</p>
                        </div>
                      )}

                      <footer className="self-care-card-bottom">
                        <div className="self-care-card-meta">
                          <span className="self-care-meta-item">
                            <span className="self-care-meta-icon" aria-hidden="true">⏱️</span>
                            <span>{formatDuration(rec.durationMinutes)}</span>
                          </span>
                          {rec.historicalMoodImpact && (
                            <span className="self-care-meta-item">
                              <span aria-hidden="true">😊</span>
                              <span>Impacto {rec.historicalMoodImpact}/100</span>
                            </span>
                          )}
                          {rec.completionRate && (
                            <span className="self-care-meta-item">
                              <span aria-hidden="true">✓</span>
                              <span>{rec.completionRate}% completado</span>
                            </span>
                          )}
                        </div>
                        <div className="self-care-card-actions">
                          <Button
                            type="button"
                            variant="ghost"
                            onClick={() => handleStartNow(rec)}
                            className="self-care-action self-care-action--primary"
                          >
                            <Play className="h-4 w-4" />
                            Empezar
                          </Button>
                          <Button
                            type="button"
                            variant="ghost"
                            onClick={() => handleSchedule(rec)}
                            className="self-care-action self-care-action--secondary"
                          >
                            Agendar
                          </Button>
                          <button
                            type="button"
                            onClick={() => handleDismiss(rec)}
                            className="self-care-dismiss-btn"
                            aria-label="Descartar sugerencia"
                          >
                            <X className="h-4 w-4" />
                          </button>
                        </div>
                      </footer>
                    </div>
                  </Card>
                );
              })}
            </div>
          </main>
        </div>
      </div>
    </div>
  );
}
