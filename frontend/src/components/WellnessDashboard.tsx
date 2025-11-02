import { AlertTriangle, BarChart3, Calendar, ChevronLeft, ChevronRight, Flame, PieChart, Sparkles, TrendingUp } from 'lucide-react';
import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { formatMonthTitle } from '../lib/utils';
import {
  apiService,
  type CategoryMoodImpactDto,
  type MoodDaySnapshotDto,
  type MoodDistributionSliceDto,
  type WellnessSummaryDto
} from '../services/apiService';
import './WellnessDashboard.css';

interface MoodDescriptor {
  label: string;
  helper: string;
  emoji: string;
  tone: 'positive' | 'neutral' | 'negative';
}

const MOOD_DESCRIPTORS: Record<number, MoodDescriptor> = {
  1: { label: 'Muy mal', helper: 'Necesitás recargar energías', emoji: '😞', tone: 'negative' },
  2: { label: 'Mal', helper: 'Semana desafiante', emoji: '🙁', tone: 'negative' },
  3: { label: 'Neutral', helper: 'Clima emocional estable', emoji: '😐', tone: 'neutral' },
  4: { label: 'Bien', helper: 'Vas por buen camino', emoji: '🙂', tone: 'positive' },
  5: { label: 'Excelente', helper: 'Tu energía estuvo arriba', emoji: '😄', tone: 'positive' }
};

const getMoodDescriptor = (value: number | null | undefined): MoodDescriptor | null => {
  if (!value) {
    return null;
  }

  const rounded = Math.max(1, Math.min(5, Math.round(value)));
  return MOOD_DESCRIPTORS[rounded] ?? null;
};

const clampPercentage = (value: number) => {
  if (Number.isNaN(value)) {
    return 0;
  }
  return Math.max(0, Math.min(100, Math.round(value)));
};

const formatSnapshot = (snapshot: MoodDaySnapshotDto | null | undefined) => {
  if (!snapshot) {
    return {
      headline: 'Sin datos',
      description: 'No registraste estados de ánimo en este período.'
    };
  }

  const date = new Date(snapshot.date);
  const descriptor = getMoodDescriptor(snapshot.moodRating);
  const formattedDate = date.toLocaleDateString('es-ES', {
    weekday: 'long',
    day: 'numeric',
    month: 'long'
  });

  const headline = `${descriptor?.emoji ?? '🙂'} ${descriptor?.label ?? 'Estado registrado'}`;
  const description = `${formattedDate.charAt(0).toUpperCase() + formattedDate.slice(1)} · ${snapshot.moodRating}/5`;

  return { headline, description };
};

const emptySummary: WellnessSummaryDto = {
  year: new Date().getFullYear(),
  month: new Date().getMonth() + 1,
  averageMood: 0,
  bestDay: null,
  worstDay: null,
  moodTrend: [],
  moodDistribution: [],
  streaks: {
    currentPositive: 0,
    longestPositive: 0,
    currentNegative: 0,
    longestNegative: 0
  },
  categoryImpacts: [],
  totalTrackedDays: 0,
  positiveDays: 0,
  neutralDays: 0,
  negativeDays: 0,
  trackingCoverage: 0,
  hasEventMoodData: false
};

const WellnessDashboard: React.FC = () => {
  const [currentMonth, setCurrentMonth] = useState(() => {
    const now = new Date();
    return new Date(now.getFullYear(), now.getMonth(), 1);
  });
  const [summary, setSummary] = useState<WellnessSummaryDto>(emptySummary);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loadSummary = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const data = await apiService.getWellnessSummary(
        currentMonth.getFullYear(),
        currentMonth.getMonth() + 1
      );
      setSummary(data);
    } catch (err) {
      console.error('Error loading wellness summary', err);
      setError('No pudimos cargar tu dashboard de bienestar. Intentalo nuevamente.');
    } finally {
      setIsLoading(false);
    }
  }, [currentMonth]);

  useEffect(() => {
    loadSummary();
  }, [loadSummary]);

  const goToPreviousMonth = () => {
    setCurrentMonth(prev => new Date(prev.getFullYear(), prev.getMonth() - 1, 1));
  };

  const goToNextMonth = () => {
    setCurrentMonth(prev => new Date(prev.getFullYear(), prev.getMonth() + 1, 1));
  };

  const goToCurrentMonth = () => {
    const now = new Date();
    setCurrentMonth(new Date(now.getFullYear(), now.getMonth(), 1));
  };

  const coveragePercentage = useMemo(
    () => clampPercentage((summary?.trackingCoverage ?? 0) * 100),
    [summary?.trackingCoverage]
  );

  const averageMoodDescriptor = useMemo(
    () => getMoodDescriptor(summary?.averageMood),
    [summary?.averageMood]
  );

  const hasTrackedDays = summary?.totalTrackedDays > 0;

  const bestDay = useMemo(() => formatSnapshot(summary?.bestDay), [summary?.bestDay]);
  const worstDay = useMemo(() => formatSnapshot(summary?.worstDay), [summary?.worstDay]);

  const trendPoints = useMemo(() => {
    const points = summary?.moodTrend ?? [];
    if (!points.length) {
      return '';
    }

    const maxIndex = Math.max(points.length - 1, 1);
    return points
      .map((point, index) => {
        if (point.averageMood === null || point.averageMood === undefined) {
          return null;
        }
        const x = (index / maxIndex) * 100;
        const normalized = (point.averageMood - 1) / 4; // 0 a 1
        const y = (1 - normalized) * 100;
        return `${x.toFixed(2)},${y.toFixed(2)}`;
      })
      .filter(Boolean)
      .join(' ');
  }, [summary?.moodTrend]);

  const hasTrendData = useMemo(
    () => (summary?.moodTrend ?? []).some(point => point.averageMood !== null),
    [summary?.moodTrend]
  );

  const distributionSlices = useMemo<MoodDistributionSliceDto[]>(
    () => summary?.moodDistribution ?? [],
    [summary?.moodDistribution]
  );

  const categoryImpacts = useMemo<CategoryMoodImpactDto[]>(
    () => summary?.categoryImpacts ?? [],
    [summary?.categoryImpacts]
  );

  const positiveStreak = summary?.streaks?.longestPositive ?? 0;
  const negativeStreak = summary?.streaks?.longestNegative ?? 0;

  return (
    <div className="wellness-dashboard" aria-live="polite">
      <header className="wellness-header">
        <div className="wellness-header-left">
          <button
            type="button"
            className="wellness-nav-btn"
            onClick={goToPreviousMonth}
            aria-label="Mes anterior"
          >
            <ChevronLeft size={18} />
          </button>
          <button
            type="button"
            className="wellness-nav-btn"
            onClick={goToNextMonth}
            aria-label="Mes siguiente"
          >
            <ChevronRight size={18} />
          </button>
          <h2 className="wellness-title">
            <Calendar size={18} aria-hidden="true" />
            {formatMonthTitle(currentMonth)}
          </h2>
        </div>
        <div className="wellness-header-right">
          <button type="button" className="wellness-today-btn" onClick={goToCurrentMonth}>
            Mes actual
          </button>
        </div>
      </header>

      <section className="wellness-intro" role="note">
        <Sparkles size={18} aria-hidden="true" />
        <div>
          <h3>Tu radar emocional</h3>
          <p>Explorá cómo estuvo tu energía este mes y qué actividades influyeron en tus ánimos.</p>
        </div>
      </section>

      {error && (
        <div className="wellness-error" role="alert">
          <span>{error}</span>
          <button type="button" onClick={loadSummary}>
            Reintentar
          </button>
        </div>
      )}

      <div className="wellness-content" data-loading={isLoading}>
        {isLoading && (
          <div className="wellness-loading" role="status">
            <span>Cargando métricas…</span>
          </div>
        )}

        {!isLoading && (
          <>
            <section className="wellness-hero-card">
              <div className="hero-score" data-tone={averageMoodDescriptor?.tone ?? 'neutral'}>
                <span className="hero-score-value">{summary?.averageMood?.toFixed(1) ?? '0.0'}</span>
                <div className="hero-score-texts">
                  <span className="hero-score-label">Promedio mensual</span>
                  <span className="hero-score-helper">
                    {averageMoodDescriptor ? `${averageMoodDescriptor.emoji} ${averageMoodDescriptor.helper}` : 'Registrá más estados de ánimo para ver tendencias.'}
                  </span>
                </div>
              </div>

              <div className="hero-highlights">
                <div className="hero-highlight">
                  <span className="hero-highlight-title">Mejor día</span>
                  <strong>{bestDay.headline}</strong>
                  <span className="hero-highlight-helper">{bestDay.description}</span>
                </div>
                <div className="hero-highlight divider" aria-hidden="true" />
                <div className="hero-highlight">
                  <span className="hero-highlight-title">Día a mejorar</span>
                  <strong>{worstDay.headline}</strong>
                  <span className="hero-highlight-helper">{worstDay.description}</span>
                </div>
              </div>
            </section>

            <section className="wellness-metrics-grid">
              <article className="metric-card">
                <header>
                  <BarChart3 size={16} aria-hidden="true" />
                  <span>Cobertura de registro</span>
                </header>
                <div className="metric-value">
                  <span>{coveragePercentage}%</span>
                  <small>{summary?.totalTrackedDays ?? 0} días con registro</small>
                </div>
                <div className="coverage-progress">
                  <div
                    className="coverage-progress-bar"
                    style={{ width: `${coveragePercentage}%` }}
                    aria-hidden="true"
                  />
                </div>
              </article>

              <article className="metric-card">
                <header>
                  <TrendingUp size={16} aria-hidden="true" />
                  <span>Días positivos</span>
                </header>
                <div className="metric-value">
                  <span>{summary?.positiveDays ?? 0}</span>
                  <small>Calificación ≥ 4</small>
                </div>
              </article>

              <article className="metric-card">
                <header>
                  <PieChart size={16} aria-hidden="true" />
                  <span>Días neutros</span>
                </header>
                <div className="metric-value">
                  <span>{summary?.neutralDays ?? 0}</span>
                  <small>Calificación 3</small>
                </div>
              </article>

              <article className="metric-card warning">
                <header>
                  <AlertTriangle size={16} aria-hidden="true" />
                  <span>Días desafiantes</span>
                </header>
                <div className="metric-value">
                  <span>{summary?.negativeDays ?? 0}</span>
                  <small>Calificación ≤ 2</small>
                </div>
              </article>
            </section>

            <section className="wellness-panels">
              <article className="panel-card trend-card">
                <header>
                  <TrendingUp size={18} aria-hidden="true" />
                  <div>
                    <h4>Evolución diaria</h4>
                    <span>Seguimiento del estado de ánimo a lo largo del mes</span>
                  </div>
                </header>

                {hasTrackedDays && hasTrendData ? (
                  <div className="trend-chart" role="img" aria-label="Gráfico de evolución diaria del estado de ánimo">
                    <svg viewBox="0 0 100 100" preserveAspectRatio="none">
                      <polyline
                        points={trendPoints}
                        fill="none"
                        stroke="var(--gradient-primary-start)"
                        strokeWidth={2.5}
                        strokeLinecap="round"
                      />
                    </svg>
                    <div className="trend-scale" aria-hidden="true">
                      <span>5</span>
                      <span>3</span>
                      <span>1</span>
                    </div>
                  </div>
                ) : (
                  <p className="panel-empty">Registrá al menos dos días con estado de ánimo para ver la tendencia.</p>
                )}

                <footer className="trend-footnote">
                  Cada punto representa el promedio del día. Si no registraste un día, lo mostramos como un espacio en blanco.
                </footer>
              </article>

              <article className="panel-card distribution-card">
                <header>
                  <PieChart size={18} aria-hidden="true" />
                  <div>
                    <h4>Distribución de estados</h4>
                    <span>Cómo se repartieron tus calificaciones</span>
                  </div>
                </header>

                {hasTrackedDays ? (
                  <ul className="distribution-list">
                    {distributionSlices.map(slice => {
                      const descriptor = MOOD_DESCRIPTORS[slice.moodRating as keyof typeof MOOD_DESCRIPTORS];
                      const percentage = clampPercentage((slice.percentage ?? 0) * 100);
                      return (
                        <li key={slice.moodRating}>
                          <div className="distribution-label">
                            <span aria-hidden="true">{descriptor?.emoji ?? '🙂'}</span>
                            <div>
                              <strong>{descriptor?.label ?? `Nivel ${slice.moodRating}`}</strong>
                              <span>{slice.count} días</span>
                            </div>
                          </div>
                          <div className="distribution-bar" aria-hidden="true">
                            <div style={{ width: `${percentage}%` }} />
                          </div>
                          <span className="distribution-percentage">{percentage}%</span>
                        </li>
                      );
                    })}
                  </ul>
                ) : (
                  <p className="panel-empty">Todavía no hay registros suficientes para calcular la distribución.</p>
                )}
              </article>
            </section>

            <section className="wellness-panels">
              <article className="panel-card streaks-card">
                <header>
                  <Flame size={18} aria-hidden="true" />
                  <div>
                    <h4>Rachas emocionales</h4>
                    <span>Detectá tus momentos sostenidos</span>
                  </div>
                </header>

                {hasTrackedDays ? (
                  <div className="streaks-grid">
                    <div className="streak-card positive">
                      <span className="streak-label">Racha positiva más larga</span>
                      <strong>{positiveStreak} días</strong>
                      <span className="streak-helper">Estados de ánimo ≥ 4</span>
                    </div>
                    <div className="streak-card negative">
                      <span className="streak-label">Racha desafiante más larga</span>
                      <strong>{negativeStreak} días</strong>
                      <span className="streak-helper">Estados de ánimo ≤ 2</span>
                    </div>
                  </div>
                ) : (
                  <p className="panel-empty">Completá al menos una semana con registros para ver tus rachas.</p>
                )}
              </article>

              <article className="panel-card categories-card">
                <header>
                  <BarChart3 size={18} aria-hidden="true" />
                  <div>
                    <h4>Impacto por categoría</h4>
                    <span>Eventos que más influyeron en tu energía</span>
                  </div>
                </header>

                {summary?.hasEventMoodData && categoryImpacts.length > 0 ? (
                  <ul className="category-list">
                    {categoryImpacts.map(category => {
                      const descriptor = getMoodDescriptor(category.averageMood);
                      return (
                        <li key={category.categoryId}>
                          <div className="category-color" style={{ backgroundColor: category.categoryColor ?? 'var(--gradient-primary-start)' }} aria-hidden="true" />
                          <div className="category-info">
                            <strong>{category.categoryName}</strong>
                            <span>{category.eventCount} eventos con registro</span>
                          </div>
                          <div className="category-score" data-tone={descriptor?.tone ?? 'neutral'}>
                            <span>{descriptor?.emoji ?? '🙂'}</span>
                            <strong>{category.averageMood.toFixed(1)}</strong>
                          </div>
                        </li>
                      );
                    })}
                  </ul>
                ) : (
                  <p className="panel-empty">Calificá tus eventos con estado de ánimo para descubrir cómo impactan.</p>
                )}
              </article>
            </section>
          </>
        )}
      </div>
    </div>
  );
};

export default WellnessDashboard;
