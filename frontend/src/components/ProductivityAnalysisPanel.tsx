import { useEffect, useState } from 'react';
import { Card } from './ui/card';
import { Button } from './ui/button';
import { Clock, TrendingUp, TrendingDown, Lightbulb, Calendar, Target } from 'lucide-react';
import { apiService, type ProductivityAnalysisDto } from '../services/apiService';
import './ProductivityAnalysisPanel.css';

export function ProductivityAnalysisPanel() {
  const [analysis, setAnalysis] = useState<ProductivityAnalysisDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [periodDays, setPeriodDays] = useState(30);

  const loadAnalysis = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await apiService.getProductivityAnalysis(periodDays);
      setAnalysis(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Error al cargar el análisis');
      console.error('Error loading productivity analysis:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadAnalysis();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [periodDays]);

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

  if (loading) {
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
          <Button onClick={loadAnalysis} variant="outline">
            Reintentar
          </Button>
        </Card>
      </div>
    );
  }

  if (!analysis) return null;

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

        <div className="period-selector">
          <Button
            variant={periodDays === 7 ? 'default' : 'outline'}
            size="sm"
            onClick={() => setPeriodDays(7)}
          >
            7 días
          </Button>
          <Button
            variant={periodDays === 30 ? 'default' : 'outline'}
            size="sm"
            onClick={() => setPeriodDays(30)}
          >
            30 días
          </Button>
          <Button
            variant={periodDays === 90 ? 'default' : 'outline'}
            size="sm"
            onClick={() => setPeriodDays(90)}
          >
            90 días
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
            <span className="stat-value">{periodDays}</span>
            <span className="stat-label">Días analizados</span>
          </div>
        </Card>
      </div>

      {/* Heat Map de Productividad por Hora */}
      <Card className="heat-map-card">
        <h2>
          <Clock />
          Productividad por Hora del Día
        </h2>
        <div className="heat-map-container">
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

      {/* Horas de Baja Energía */}
      {analysis.lowEnergyHours.length > 0 && (
        <Card className="low-energy-card">
          <h2>
            <TrendingDown />
            Horas de Baja Energía
          </h2>
          <p className="section-description">
            Períodos donde tu energía disminuye. Ideal para tareas ligeras o descansos.
          </p>

          <div className="low-energy-list">
            {analysis.lowEnergyHours.map((hour, index) => (
              <div key={index} className="low-energy-item">
                <div className="low-energy-icon">💤</div>
                <div className="low-energy-content">
                  <div className="low-energy-time">{hour.description}</div>
                  <div className="low-energy-score">
                    <div
                      className="score-bar low"
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

      {/* Productividad por Día de la Semana */}
      <Card className="daily-productivity-card">
        <h2>
          <Calendar />
          Productividad por Día de la Semana
        </h2>

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
      </Card>

      {/* Productividad por Categoría */}
      {analysis.categoryProductivity.length > 0 && (
        <Card className="category-productivity-card">
          <h2>
            <Target />
            Productividad por Tipo de Actividad
          </h2>

          <div className="category-list">
            {analysis.categoryProductivity.map((cat) => (
              <div key={cat.categoryId} className="category-item">
                <div
                  className="category-color"
                  style={{ backgroundColor: cat.categoryColor }}
                />
                <div className="category-content">
                  <div className="category-name">{cat.categoryName}</div>
                  <div className="category-details">
                    {cat.optimalHours.length > 0 && (
                      <span className="optimal-hours">
                        🕐 Mejor a las: {cat.optimalHours.map(h => formatHour(h)).join(', ')}
                      </span>
                    )}
                  </div>
                </div>
                <div className="category-score">
                  {cat.averageProductivityScore.toFixed(0)}%
                </div>
              </div>
            ))}
          </div>
        </Card>
      )}

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
    </div>
  );
}
