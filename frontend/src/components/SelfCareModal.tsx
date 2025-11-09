import {
  Calendar,
  EyeOff,
  Loader2,
  Play,
  RefreshCw,
  X
} from 'lucide-react';
import { useEffect, useState } from 'react';
import type {
  SelfCareFeedbackDto,
  SelfCareRecommendationDto,
  SelfCareRequestDto
} from '../services/apiService';
import { apiService, SelfCareFeedbackAction } from '../services/apiService';
import './SelfCareModal.css';
import { Badge } from './ui/badge';
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
      return 'self-care-badge self-care-badge-mental';
    }

    if (normalized.includes('creativ') || normalized.includes('arte')) {
      return 'self-care-badge self-care-badge-creative';
    }

    if (normalized.includes('descans') || normalized.includes('relaj')) {
      return 'self-care-badge self-care-badge-rest';
    }

    if (normalized.includes('fis') || normalized.includes('energ')) {
      return 'self-care-badge self-care-badge-physical';
    }

    if (normalized.includes('product') || normalized.includes('focus')) {
      return 'self-care-badge self-care-badge-productive';
    }

    return 'self-care-badge self-care-badge-default';
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
        className="self-care-modal-container w-full sm:max-w-[820px] rounded-[24px] sm:rounded-[32px] border shadow-[0_25px_60px_rgba(15,23,42,0.18)] flex flex-col h-[86vh] sm:h-auto sm:max-h-[86vh] overflow-hidden"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="self-care-modal-inner">
          {/* Header */}
          <div className="self-care-modal-header relative flex-shrink-0 border-b px-8 sm:px-12 py-8 sm:py-10 backdrop-blur">
            <div className="self-care-modal-header-content">
              <h2 className="self-care-modal-title">Autocuidado</h2>
              <p className="self-care-modal-subtitle">Sugerencias personalizadas para tu bienestar</p>
            </div>
            <div className="self-care-modal-actions">
              <button
                type="button"
                onClick={handleRefresh}
                disabled={loading}
                className="self-care-refresh-btn"
                aria-label="Actualizar sugerencias"
                title="Actualizar sugerencias"
              >
                <RefreshCw className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />
                <span>Más ideas</span>
              </button>
              <button
                onClick={onClose}
                type="button"
                className="self-care-modal-close-btn"
                aria-label="Cerrar"
              >
                <X className="h-4 w-4" />
              </button>
            </div>
          </div>

          {/* Content - Scrollable */}
          <div className="self-care-modal-content flex-1 overflow-y-auto px-10 sm:px-14 py-12 self-care-scrollable">
            <div className="flex flex-col gap-4 pt-4 pb-14">
              {/* Success message */}
              {refreshSuccess && !loading && (
                <div className="bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg p-3 flex items-center gap-2 animate-[slideDown_0.3s_ease-out]">
                  <div className="flex-shrink-0 w-5 h-5 rounded-full bg-green-500 flex items-center justify-center">
                    <svg className="w-3 h-3 text-white" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={3} d="M5 13l4 4L19 7" />
                    </svg>
                  </div>
                  <p className="text-sm font-medium text-green-800 dark:text-green-200">
                    ✨ Sugerencias actualizadas
                  </p>
                </div>
              )}

              {loading && (
                <div className="flex flex-col items-center justify-center py-12">
                  <Loader2 className="h-8 w-8 animate-spin text-primary-600 mb-2" />
                  <p className="text-sm text-gray-600 dark:text-gray-400">
                    Personalizando sugerencias...
                  </p>
                </div>
              )}

              {error && (
                <div className="bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded-lg p-3">
                  <p className="text-sm text-yellow-800 dark:text-yellow-200">
                    {error}
                  </p>
                </div>
              )}

              {!loading && recommendations.length === 0 && (
                <div className="text-center py-12">
                  <p className="text-gray-600 dark:text-gray-400">
                    No hay sugerencias disponibles en este momento.
                  </p>
                  <button
                    onClick={loadRecommendations}
                    className="mt-4 px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors"
                  >
                    Intentar de nuevo
                  </button>
                </div>
              )}

              {!loading && recommendations.length > 0 && (
                <div className="h-2" aria-hidden="true" />
              )}

              {!loading && recommendations.map((rec) => (
                <Card
                  key={rec.id}
                  className="self-care-recommendation-card self-care-card shadow-md transition-all hover:shadow-lg"
                >
                  <div className="space-y-5 p-6">
                    {/* Header de la tarjeta */}
                    <div className="flex items-start justify-between gap-4">
                      <div className="flex items-start gap-4 flex-1">
                        <div className="text-4xl flex-shrink-0">{rec.icon}</div>
                        <div className="flex-1 min-w-0">
                          <div className="flex items-baseline gap-2 flex-wrap mb-3">
                            <h3 className="font-semibold text-lg leading-tight text-foreground">
                              {rec.title}
                            </h3>
                            <Badge
                              variant="outline"
                              className={`${getTypeBadgeClass(rec.typeDescription)} px-3 py-1 text-[0.7rem] font-semibold uppercase tracking-[0.2em] leading-none`}
                            >
                              {rec.typeDescription}
                            </Badge>
                            {rec.id.startsWith('ai-') && (
                              <Badge className="self-care-ai-badge px-5 py-1.5 bg-gradient-to-r from-purple-500 to-pink-500 text-white border-transparent shadow-sm uppercase tracking-[0.35em] text-[0.7rem] font-semibold leading-none">
                                ✨ IA
                              </Badge>
                            )}
                          </div>
                          <p className="text-sm text-muted-foreground leading-relaxed">
                            {rec.description}
                          </p>
                        </div>
                      </div>
                      <div className="flex items-center gap-1 text-base font-semibold text-primary">
                        <span>{rec.confidenceScore}%</span>
                      </div>
                    </div>

                    {/* Razón personalizada */}
                    <div className="self-care-card-reason rounded-lg p-4">
                      <p className="text-sm text-muted-foreground leading-relaxed">
                        <span className="font-semibold text-primary">💡 Por qué ahora:</span>{' '}
                        {rec.personalizedReason}
                      </p>
                    </div>

                    {/* Metadata */}
                    <div className="self-care-card-meta flex flex-wrap items-center gap-4 pt-3 text-sm text-muted-foreground">
                      <span className="font-medium">⏱️ {rec.durationMinutes} min</span>
                      {rec.historicalMoodImpact && (
                        <span>
                          😊 Impacto: {rec.historicalMoodImpact}/100
                        </span>
                      )}
                      {rec.completionRate && (
                        <span>
                          ✓ {rec.completionRate}% completado
                        </span>
                      )}
                    </div>

                    {/* Acciones */}
                    <div className="flex flex-col sm:flex-row gap-3">
                      <Button
                        onClick={() => handleSchedule(rec)}
                        className="flex-1 gap-2 h-auto py-3 text-sm sm:text-base shadow-md bg-primary-gradient text-white hover:opacity-90 focus-visible:ring-primary"
                      >
                        <Calendar className="h-5 w-5" />
                        Agendar
                      </Button>
                      <Button
                        onClick={() => handleStartNow(rec)}
                        className="flex-1 gap-2 h-auto py-3 text-sm sm:text-base bg-emerald-500 text-white hover:bg-emerald-600 focus-visible:ring-emerald-500 shadow-md"
                      >
                        <Play className="h-5 w-5" />
                        Empezar ya
                      </Button>
                      <Button
                        onClick={() => handleDismiss(rec)}
                        className="flex-1 gap-2 h-auto py-3 text-sm sm:text-base bg-rose-500 text-white hover:bg-rose-600 focus-visible:ring-rose-500 shadow-md"
                        title="Ignorar"
                      >
                        <EyeOff className="h-4 w-4" />
                        <span className="hidden sm:inline">Ignorar</span>
                      </Button>
                    </div>
                  </div>
                </Card>
              ))}

              {!loading && recommendations.length > 0 && (
                <div className="h-6" aria-hidden="true" />
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
