import { useEffect, useState } from 'react';
import type { ScheduleSuggestionDto } from '../services/apiService';
import { apiService, SuggestionStatus, SuggestionType } from '../services/apiService';
import './ScheduleSuggestionsPanel.css';
import { Button } from './ui/button';
import { Card } from './ui/card';

interface ScheduleSuggestionsPanelProps {
  onSuggestionAccepted?: () => void;
}

export const ScheduleSuggestionsPanel: React.FC<ScheduleSuggestionsPanelProps> = ({ onSuggestionAccepted }) => {
  const [suggestions, setSuggestions] = useState<ScheduleSuggestionDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [generating, setGenerating] = useState(false);
  const [respondingTo, setRespondingTo] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  useEffect(() => {
    loadSuggestions();
  }, []);

  const loadSuggestions = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await apiService.getScheduleSuggestions();
      setSuggestions(data);
    } catch (err) {
      setError('Error al cargar sugerencias');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const handleGenerate = async () => {
    try {
      setGenerating(true);
      setError(null);
      const data = await apiService.generateScheduleSuggestions();
      setSuggestions(data);
    } catch (err) {
      setError('Error al generar sugerencias');
      console.error(err);
    } finally {
      setGenerating(false);
    }
  };

  const handleRespond = async (suggestionId: string, status: SuggestionStatus) => {
    try {
      setRespondingTo(suggestionId);
      setError(null);
      setSuccessMessage(null);

      await apiService.respondToSuggestion(suggestionId, { status });

      // Mostrar mensaje de éxito
      if (status === SuggestionStatus.Accepted) {
        setSuccessMessage('✓ Sugerencia aplicada con éxito. El evento ha sido actualizado.');
      } else {
        setSuccessMessage('✓ Sugerencia descartada.');
      }

      // Actualizar la lista de sugerencias
      await loadSuggestions();

      // Si aceptó la sugerencia, notificar al padre para refrescar el calendario
      if (status === SuggestionStatus.Accepted && onSuggestionAccepted) {
        onSuggestionAccepted();
      }

      // Ocultar mensaje después de 5 segundos
      setTimeout(() => setSuccessMessage(null), 5000);
    } catch (err) {
      console.error('❌ Error al responder a la sugerencia:', err);
      setError('Error al responder a la sugerencia: ' + (err instanceof Error ? err.message : 'Error desconocido'));
    } finally {
      setRespondingTo(null);
    }
  };

  const getSuggestionIcon = (type: SuggestionType): string => {
    switch (type) {
      case SuggestionType.ResolveConflict:
        return '⚠️';
      case SuggestionType.MoveEvent:
        return '📅';
      case SuggestionType.OptimizeDistribution:
        return '📊';
      case SuggestionType.PatternAlert:
        return '🔔';
      case SuggestionType.SuggestBreak:
        return '☕';
      case SuggestionType.GeneralReorganization:
        return '🔄';
      default:
        return '💡';
    }
  };

  const getPriorityClass = (priority: number): string => {
    if (priority >= 5) return 'priority-critical';
    if (priority >= 4) return 'priority-high';
    if (priority >= 3) return 'priority-medium';
    return 'priority-low';
  };

  if (loading) {
    return (
      <Card className="suggestions-panel">
        <div className="suggestions-loading">
          <div className="spinner" />
          <p>Cargando sugerencias...</p>
        </div>
      </Card>
    );
  }

  return (
    <div className="suggestions-panel">
      <div className="suggestions-header">
        <h2>Sugerencias de Reorganización</h2>
        <Button
          onClick={handleGenerate}
          disabled={generating}
          variant="outline"
          className="generate-button"
        >
          <span className="button-icon">{generating ? '⏳' : '✨'}</span>
          {generating ? 'Generando...' : 'Generar Nuevas Sugerencias'}
        </Button>
      </div>

      {successMessage && (
        <div className="suggestions-success">
          {successMessage}
        </div>
      )}

      {error && (
        <div className="suggestions-error">
          {error}
        </div>
      )}

      {suggestions.length === 0 ? (
        <Card className="suggestions-empty">
          <p>No hay sugerencias pendientes</p>
          <p className="suggestions-empty-hint">
            Haz clic en "Generar Nuevas Sugerencias" para analizar tu calendario
          </p>
        </Card>
      ) : (
        <div className="suggestions-list">
          {suggestions.map((suggestion) => (
            <Card key={suggestion.id} className={`suggestion-card ${getPriorityClass(suggestion.priority)}`}>
              <div className="suggestion-header">
                <span className="suggestion-icon">{getSuggestionIcon(suggestion.type)}</span>
                <div className="suggestion-title-area">
                  <h3>{suggestion.description}</h3>
                  <span className="suggestion-type-badge">{suggestion.typeDescription}</span>
                </div>
                <div className="suggestion-confidence">
                  <span className="confidence-score">{suggestion.confidenceScore}%</span>
                  <span className="confidence-label">confianza</span>
                </div>
              </div>

              <div className="suggestion-body">
                <p className="suggestion-reason">
                  <strong>Motivo:</strong> {suggestion.reason}
                </p>

                {suggestion.eventTitle && (
                  <p className="suggestion-event">
                    <strong>Evento:</strong> {suggestion.eventTitle}
                  </p>
                )}

                {suggestion.suggestedDateTime && (
                  <p className="suggestion-time">
                    <strong>Hora sugerida:</strong>{' '}
                    {new Date(suggestion.suggestedDateTime).toLocaleString('es-AR', {
                      dateStyle: 'short',
                      timeStyle: 'short',
                    })}
                  </p>
                )}

                <p className="suggestion-help-text">
                  💡 <strong>Qué hace cada botón:</strong>
                  <br />
                  <span className="help-accept">• Aceptar = Aplica el cambio automáticamente</span>
                  <br />
                  <span className="help-reject">• Rechazar = Descarta la sugerencia</span>
                </p>
              </div>

              <div className="suggestion-actions">
                <Button
                  onClick={() => handleRespond(suggestion.id, SuggestionStatus.Accepted)}
                  disabled={respondingTo === suggestion.id}
                  variant="default"
                  size="sm"
                  className="action-accept"
                  title="Aplicar esta sugerencia: el evento se moverá automáticamente a la nueva fecha/hora"
                >
                  {respondingTo === suggestion.id ? (
                    <>
                      <span className="button-spinner">⏳</span> Aplicando...
                    </>
                  ) : (
                    <>
                      <span className="button-icon">✓</span> Aceptar
                    </>
                  )}
                </Button>
                <Button
                  onClick={() => handleRespond(suggestion.id, SuggestionStatus.Rejected)}
                  disabled={respondingTo === suggestion.id}
                  variant="ghost"
                  size="sm"
                  className="action-reject"
                  title="Descartar esta sugerencia: no se aplicará y desaparecerá de la lista"
                >
                  <span className="button-icon">✗</span> Rechazar
                </Button>
              </div>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
};
