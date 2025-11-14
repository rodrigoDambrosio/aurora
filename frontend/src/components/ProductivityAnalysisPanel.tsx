import { AlertTriangle, Calendar, Clock, Lightbulb, RefreshCcw, Target, TrendingUp } from 'lucide-react';
import { useCallback, useEffect, useRef, useState } from 'react';
import { apiService, type ProductivityAnalysisDto } from '../services/apiService';
import './ProductivityAnalysisPanel.css';
import { Button } from './ui/button';
import { Card } from './ui/card';

const DEFAULT_ANALYSIS_DAYS = 30;
const MS_IN_DAY = 1000 * 60 * 60 * 24;

export function ProductivityAnalysisPanel() {
  const [analysis, setAnalysis] = useState<ProductivityAnalysisDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const isFetchingRef = useRef(false);

  const loadAnalysis = useCallback(async () => {
    if (isFetchingRef.current) {
      return;
    }

    isFetchingRef.current = true;

    try {
      setLoading(true);
      setError(null);
      const data = await apiService.getProductivityAnalysis(DEFAULT_ANALYSIS_DAYS);
      setAnalysis(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Error al cargar el análisis');
      console.error('Error loading productivity analysis:', err);
    } finally {
      setLoading(false);
      isFetchingRef.current = false;
    }
  }, []);

  useEffect(() => {
    void loadAnalysis();
  }, [loadAnalysis]);

  const getProductivityColor = (score: number): string => {
    if (score >= 70) return 'var(--color-success)';
    if (score >= 40) return 'var(--color-warning)';
    return 'var(--color-error)';
  };

  const getHeatMapColor = (score: number): string => {
    if (score === 0) return 'var(--color-surface)';
    if (score >= 80) return 'rgba(34, 197, 94, 0.8)'; // green-500
    if (score >= 60) return 'rgba(34, 197, 94, 0.6)';
    if (score >= 40) return 'rgba(251, 191, 36, 0.6)'; // amber-400
    if (score >= 20) return 'rgba(239, 68, 68, 0.4)'; // red-500
    return 'rgba(239, 68, 68, 0.2)';
  };

  const formatHour = (hour: number): string => {
    return `${hour.toString().padStart(2, '0')}:00`;
  };

  const isInitialLoad = loading && !analysis;

  if (isInitialLoad) {
    return (
      <div className="productivity-analysis-panel">
        <div className="analysis-loading">
          <div className="spinner" />
          <p>Analizando tus patrones de productividad...</p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="productivity-analysis-panel">
        <Card className="analysis-error">
          <p>{error}</p>
          <Button onClick={() => { void loadAnalysis(); }} variant="outline">
            Reintentar
          </Button>
        </Card>
      </div>
    );
  }

  if (!analysis) return null;

  const formatShortDate = (iso: string): string => {
    const date = new Date(iso);
    if (Number.isNaN(date.getTime())) {
      return '-';
    }

    return date
      .toLocaleDateString('es-AR', {
        day: '2-digit',
        month: 'short'
      })
      .replace('.', '');
  };

  const analysisStart = new Date(analysis.analysisPeriodStart);
  const analysisEnd = new Date(analysis.analysisPeriodEnd);
  const analysisDays = Math.max(1, Math.round((analysisEnd.getTime() - analysisStart.getTime()) / MS_IN_DAY) + 1);
  const analysisRangeLabel = `${formatShortDate(analysis.analysisPeriodStart)} - ${formatShortDate(analysis.analysisPeriodEnd)}`;
  const hasAnyData = analysis.totalEventsAnalyzed > 0;
  const hasHourlyData = analysis.hourlyProductivity.some(hour => hour.totalEvents > 0);
  const hasDailyData = analysis.dailyProductivity.some(day => day.totalEvents > 0);

  return (
    <div className="productivity-analysis-panel">
      <div className="analysis-header">
        <div className="header-content">
          <h1>
            <Target className="header-icon" />
            Análisis de Productividad
          </h1>
          <p className="header-subtitle">
            Descubre tus horas doradas y optimiza tu planificación
          </p>
        </div>

        <div className="header-actions">
          <Button
            className="refresh-button"
            variant="outline"
            size="sm"
            onClick={() => { void loadAnalysis(); }}
            disabled={loading}
          >
            <RefreshCcw className={`refresh-icon ${loading ? 'spinning' : ''}`} />
            {loading ? 'Actualizando...' : 'Actualizar'}
          </Button>
        </div>
      </div>

      <div className="analysis-stats">
        <Card className="stat-card">
          <div className="stat-icon">📊</div>
          <div className="stat-content">
            <span className="stat-value">{analysis.totalEventsAnalyzed}</span>
            <span className="stat-label">Eventos analizados</span>
          </div>
        </Card>

        <Card className="stat-card">
          <div className="stat-icon">⭐</div>
          <div className="stat-content">
            <span className="stat-value">{analysis.goldenHours.length}</span>
            <span className="stat-label">Horas doradas</span>
          </div>
        </Card>

        <Card className="stat-card">
          <div className="stat-icon">📅</div>
          <div className="stat-content">
            <span className="stat-value">{analysisDays}</span>
            <span className="stat-label">Días analizados</span>
            <span className="stat-range">{analysisRangeLabel}</span>
          </div>
        </Card>
      </div>
      {!hasAnyData ? (
        <Card className="analysis-empty">
          <h2>No hay suficiente actividad todavía</h2>
          <p>
            Registrá eventos de trabajo y calificá tu estado de ánimo para ver patrones de productividad.
          </p>
          <p className="analysis-empty-subtitle">
            La vista se actualiza automáticamente una vez que tengamos más datos recientes.
          </p>
        </Card>
      ) : (
        <>
          {/* Heat Map de Productividad por Hora */}
          <Card className="heat-map-card">
            <h2>
              <Clock />
              Productividad por Hora del Día
            </h2>
            <p className="section-description">
              Basado en tus eventos de trabajo de los últimos 7 días (sin contar hoy).
            </p>
            <div className="heat-map-container">
              {hasHourlyData ? (
                <>
                  <div className="heat-map-grid">
                    {analysis.hourlyProductivity.map((hour) => (
                      <div
                        key={hour.hour}
                        className="heat-map-cell"
                        style={{ backgroundColor: getHeatMapColor(hour.productivityScore) }}
                        title={`${formatHour(hour.hour)} - ${hour.productivityScore.toFixed(1)}% productividad`}
                      >
                        <div className="hour-label">{formatHour(hour.hour)}</div>
                        <div className="hour-score">{hour.productivityScore > 0 ? hour.productivityScore.toFixed(0) : '-'}</div>
                        {hour.totalEvents > 0 && (
                          <div className="hour-events">{hour.totalEvents} eventos</div>
                        )}
                      </div>
                    ))}
                  </div>

                  <div className="heat-map-legend">
                    <span className="legend-label">Baja</span>
                    <div className="legend-gradient" />
                    <span className="legend-label">Alta</span>
                  </div>
                </>
              ) : (
                <div className="section-empty">
                  <p>No registramos actividad reciente en esa ventana.</p>
                </div>
              )}
            </div>
          </Card>

          {/* Horas Doradas */}
          {analysis.goldenHours.length > 0 && (
            <Card className="golden-hours-card">
              <h2>
                <TrendingUp />
                Tus Horas Doradas
              </h2>
              <p className="section-description">
                Períodos donde tu productividad es máxima. Aprovéchalos para tareas importantes.
              </p>

              <div className="golden-hours-list">
                {analysis.goldenHours.map((hour, index) => (
                  <div key={index} className="golden-hour-item">
                    <div className="golden-hour-icon">⭐</div>
                    <div className="golden-hour-content">
                      <div className="golden-hour-time">{hour.description}</div>
                      <div className="golden-hour-score">
                        <div
                          className="score-bar"
                          style={{
                            width: `${hour.averageProductivityScore}%`,
                            backgroundColor: getProductivityColor(hour.averageProductivityScore),
                          }}
                        />
                        <span className="score-value">{hour.averageProductivityScore.toFixed(1)}%</span>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            </Card>
          )}

          {/* Horarios para ajustar */}
          {analysis.lowEnergyHours.length > 0 && (
            <Card className="low-energy-card">
              <h2>
                <AlertTriangle />
                Horarios para Ajustar
              </h2>
              <p className="section-description">
                Momentos donde tu productividad cae con frecuencia. Revisá qué actividades programás en estos espacios.
              </p>

              <div className="low-energy-list">
                {analysis.lowEnergyHours.map((hour, index) => (
                  <div key={index} className="low-energy-item">
                    <div className="low-energy-icon">⚠️</div>
                    <div className="low-energy-content">
                      <div className="low-energy-time">{hour.description}</div>
                    </div>
                  </div>
                ))}
              </div>
            </Card>
          )}

          {/* Productividad por Día de la Semana */}
          <Card className="daily-productivity-card">
            <h2>
              <Calendar />
              Productividad por Día de la Semana
            </h2>

            {hasDailyData ? (
              <div className="daily-chart">
                {analysis.dailyProductivity.map((day) => (
                  <div key={day.dayOfWeek} className="daily-bar-container">
                    <div className="daily-label">{day.dayName.substring(0, 3)}</div>
                    <div className="daily-bar-wrapper">
                      <div
                        className="daily-bar"
                        style={{
                          height: `${day.productivityScore}%`,
                          backgroundColor: getProductivityColor(day.productivityScore),
                        }}
                      />
                    </div>
                    <div className="daily-score">{day.productivityScore > 0 ? day.productivityScore.toFixed(0) : '0'}%</div>
                  </div>
                ))}
              </div>
            ) : (
              <div className="section-empty">
                <p>Todavía no hay suficientes eventos para graficar los días de la semana.</p>
              </div>
            )}
          </Card>

          {/* Recomendaciones */}
          {analysis.recommendations.length > 0 && (
            <Card className="recommendations-card">
              <h2>
                <Lightbulb />
                Recomendaciones Personalizadas
              </h2>

              <div className="recommendations-list">
                {analysis.recommendations.map((rec, index) => (
                  <div key={index} className={`recommendation-item priority-${rec.priority}`}>
                    <div className="recommendation-priority">
                      {'⭐'.repeat(rec.priority)}
                    </div>
                    <div className="recommendation-content">
                      <h3>{rec.title}</h3>
                      <p>{rec.description}</p>
                      {rec.suggestedHours.length > 0 && (
                        <div className="suggested-hours">
                          🕐 {rec.suggestedHours.map(h => formatHour(h)).join(', ')}
                        </div>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            </Card>
          )}
        </>
      )}
    </div>
  );
}
