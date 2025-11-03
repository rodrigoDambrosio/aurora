import { RefreshCcw, Sparkles, ThumbsDown, ThumbsUp } from 'lucide-react';
import React, { useCallback, useEffect, useMemo, useState } from 'react';
import {
  apiService,
  type RecommendationDto,
  type RecommendationFeedbackDto,
  type RecommendationFeedbackSummaryDto
} from '../../services/apiService';
import './RecommendationAssistant.css';

type FeedbackState = 'idle' | 'sending' | 'sent' | 'error';

type Filters = {
  currentMood?: number;
  externalContext?: string;
};

const buildFeedbackPayload = (
  recommendationId: string,
  accepted: boolean,
  extra?: Partial<RecommendationFeedbackDto>
): RecommendationFeedbackDto => ({
  recommendationId,
  accepted,
  notes: extra?.notes,
  moodAfter: extra?.moodAfter,
  submittedAtUtc: extra?.submittedAtUtc
});

const formatDateTime = (isoDate: string): string => {
  if (!isoDate) {
    return 'Sin horario sugerido';
  }

  const date = new Date(isoDate);

  if (Number.isNaN(date.getTime())) {
    return 'Sin horario sugerido';
  }

  return new Intl.DateTimeFormat('es-AR', {
    weekday: 'short',
    day: '2-digit',
    month: 'short',
    hour: '2-digit',
    minute: '2-digit'
  }).format(date);
};

const RecommendationAssistant: React.FC = () => {
  const [filters, setFilters] = useState<Filters>({});
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [recommendations, setRecommendations] = useState<RecommendationDto[]>([]);
  const [feedbackStatus, setFeedbackStatus] = useState<Record<string, FeedbackState>>({});
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [summary, setSummary] = useState<RecommendationFeedbackSummaryDto | null>(null);
  const [summaryError, setSummaryError] = useState<string | null>(null);

  const activeFiltersLabel = useMemo(() => {
    if (!filters.currentMood && !filters.externalContext) {
      return 'Sin filtros adicionales';
    }

    const parts: string[] = [];

    if (filters.currentMood) {
      parts.push(`Ánimo: ${filters.currentMood}/5`);
    }

    if (filters.externalContext) {
      parts.push(`Contexto: ${filters.externalContext}`);
    }

    return parts.join(' · ');
  }, [filters]);

  const loadRecommendations = useCallback(async () => {
    setIsLoading(true);
    setErrorMessage(null);

    try {
      const payload = {
        referenceDate: new Date().toISOString(),
        limit: 5,
        currentMood: filters.currentMood,
        externalContext: filters.externalContext?.trim() || undefined
      };

      const result = await apiService.getRecommendations(payload);
      setRecommendations(result);
    } catch (error) {
      const message =
        error instanceof Error ? error.message : 'No pudimos cargar las recomendaciones.';
      setErrorMessage(message);
    } finally {
      setIsLoading(false);
    }
  }, [filters.currentMood, filters.externalContext]);

  const loadFeedbackSummary = useCallback(async (days = 30) => {
    try {
      setSummaryError(null);
      const result = await apiService.getRecommendationFeedbackSummary(days);
      setSummary(result);
    } catch (error) {
      const message = error instanceof Error ? error.message : 'No pudimos recuperar tu historial.';
      setSummaryError(message);
    }
  }, []);

  useEffect(() => {
    void loadRecommendations();
    void loadFeedbackSummary();
  }, [loadFeedbackSummary, loadRecommendations]);

  const handleMoodChange = (event: React.ChangeEvent<HTMLSelectElement>) => {
    const value = event.target.value;
    setFilters((prev) => ({
      ...prev,
      currentMood: value === '' ? undefined : Number.parseInt(value, 10)
    }));
  };

  const handleContextChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const value = event.target.value;
    setFilters((prev) => ({
      ...prev,
      externalContext: value
    }));
  };

  const handleClearFilters = () => {
    setFilters({});
  };

  const sendFeedback = async (recommendationId: string, accepted: boolean) => {
    setFeedbackStatus((prev) => ({
      ...prev,
      [recommendationId]: 'sending'
    }));

    try {
      const payload = buildFeedbackPayload(recommendationId, accepted);
      await apiService.submitRecommendationFeedback(payload);

      setFeedbackStatus((prev) => ({
        ...prev,
        [recommendationId]: 'sent'
      }));

      void loadFeedbackSummary();
    } catch (error) {
      console.error('Error enviando feedback', error);
      setFeedbackStatus((prev) => ({
        ...prev,
        [recommendationId]: 'error'
      }));
    }
  };

  const renderContent = () => {
    if (isLoading) {
      return (
        <div className="recommendations-loading" data-testid="recommendations-loading">
          {Array.from({ length: 3 }).map((_, index) => (
            <div key={index} className="recommendation-skeleton">
              <div className="skeleton shimmer" />
            </div>
          ))}
        </div>
      );
    }

    if (errorMessage) {
      return (
        <div className="recommendations-error" role="alert">
          <p>{errorMessage}</p>
          <button type="button" onClick={() => void loadRecommendations()}>
            Reintentar
          </button>
        </div>
      );
    }

    if (recommendations.length === 0) {
      return (
        <div className="recommendations-empty" role="status">
          <p>No encontramos sugerencias en este momento.</p>
          <button type="button" onClick={() => void loadRecommendations()}>
            Volver a intentar
          </button>
        </div>
      );
    }

    return (
      <div className="recommendations-grid">
        {recommendations.map((item) => {
          const status = feedbackStatus[item.id] ?? 'idle';
          const isSending = status === 'sending';
          const isSent = status === 'sent';
          const isError = status === 'error';

          return (
            <article key={item.id} className="recommendation-card">
              <header className="recommendation-card-header">
                <div className="recommendation-card-title">
                  <Sparkles aria-hidden="true" size={18} />
                  <div>
                    <h3>{item.title}</h3>
                    {item.subtitle && <p className="recommendation-subtitle">{item.subtitle}</p>}
                  </div>
                </div>
                <span className="recommendation-badge">{Math.round(item.confidence * 100)}% match</span>
              </header>

              <p className="recommendation-reason">{item.reason}</p>

              <dl className="recommendation-meta">
                <div>
                  <dt>Horario sugerido</dt>
                  <dd>{formatDateTime(item.suggestedStart)}</dd>
                </div>
                <div>
                  <dt>Duración</dt>
                  <dd>{item.suggestedDurationMinutes} minutos</dd>
                </div>
                {item.categoryName && (
                  <div>
                    <dt>Categoría</dt>
                    <dd>{item.categoryName}</dd>
                  </div>
                )}
                {item.moodImpact && (
                  <div>
                    <dt>Impacto esperado</dt>
                    <dd>{item.moodImpact}</dd>
                  </div>
                )}
                {item.summary && (
                  <div>
                    <dt>Resumen</dt>
                    <dd>{item.summary}</dd>
                  </div>
                )}
              </dl>

              <footer className="recommendation-actions">
                <button
                  type="button"
                  className="action-button is-positive"
                  disabled={isSending || isSent}
                  onClick={() => void sendFeedback(item.id, true)}
                >
                  <ThumbsUp size={16} aria-hidden="true" />
                  {isSent ? '¡Gracias!' : 'Me sirve'}
                </button>
                <button
                  type="button"
                  className="action-button is-negative"
                  disabled={isSending || isSent}
                  onClick={() => void sendFeedback(item.id, false)}
                >
                  <ThumbsDown size={16} aria-hidden="true" />
                  Pasar
                </button>
              </footer>

              {isError && (
                <p className="recommendation-feedback-error" role="status">
                  No pudimos guardar tu feedback. Probá de nuevo.
                </p>
              )}
            </article>
          );
        })}
      </div>
    );
  };

  return (
    <section className="recommendation-assistant" aria-labelledby="recommendation-assistant-title">
      <header className="assistant-header">
        <div>
          <h2 id="recommendation-assistant-title">Asistente de recomendaciones</h2>
          <p>Recomendaciones personalizadas para organizar tu día.</p>
        </div>
        <button
          type="button"
          className="refresh-button"
          onClick={() => void loadRecommendations()}
          disabled={isLoading}
        >
          <RefreshCcw size={16} aria-hidden="true" />
          Actualizar
        </button>
      </header>

      <form
        className="assistant-filters"
        onSubmit={(event) => {
          event.preventDefault();
          void loadRecommendations();
        }}
      >
        <div className="filter-group">
          <label htmlFor="mood-filter">¿Cómo te sentís hoy?</label>
          <select
            id="mood-filter"
            value={filters.currentMood?.toString() ?? ''}
            onChange={handleMoodChange}
          >
            <option value="">Sin filtro</option>
            <option value="5">5 - Energía total</option>
            <option value="4">4 - Muy bien</option>
            <option value="3">3 - Neutral</option>
            <option value="2">2 - Bajo</option>
            <option value="1">1 - Necesito calma</option>
          </select>
        </div>

        <div className="filter-group">
          <label htmlFor="context-filter">Contexto (opcional)</label>
          <input
            id="context-filter"
            type="text"
            placeholder="Ej: Lluvioso, trabajo remoto, sin gimnasio"
            value={filters.externalContext ?? ''}
            onChange={handleContextChange}
          />
        </div>

        <div className="filters-actions">
          <span className="filters-summary">{activeFiltersLabel}</span>
          <div className="filters-buttons">
            <button type="submit" className="apply-button" disabled={isLoading}>
              Ver sugerencias
            </button>
            <button type="button" className="clear-button" onClick={handleClearFilters} disabled={isLoading}>
              Limpiar
            </button>
          </div>
        </div>
      </form>

      {renderContent()}

      <aside className="assistant-summary" aria-label="Resumen de feedback">
        <header className="assistant-summary-header">
          <h3>Tu interacción con el asistente</h3>
          <button type="button" onClick={() => void loadFeedbackSummary()}>
            Actualizar
          </button>
        </header>

        {summaryError && (
          <p className="assistant-summary-error" role="status">{summaryError}</p>
        )}

        {summary && (
          <div className="assistant-summary-grid">
            <div>
              <span className="assistant-summary-label">Feedback total</span>
              <span className="assistant-summary-value">{summary.totalFeedback}</span>
            </div>
            <div>
              <span className="assistant-summary-label">Aceptadas</span>
              <span className="assistant-summary-value">{summary.acceptedCount}</span>
            </div>
            <div>
              <span className="assistant-summary-label">Tasa de acierto</span>
              <span className="assistant-summary-value">{summary.acceptanceRate.toFixed(1)}%</span>
            </div>
            <div>
              <span className="assistant-summary-label">Ánimo post recomendación</span>
              <span className="assistant-summary-value">{summary.averageMoodAfter?.toFixed(1) ?? 'Sin datos'}</span>
            </div>
          </div>
        )}
      </aside>
    </section>
  );
};

export default RecommendationAssistant;
