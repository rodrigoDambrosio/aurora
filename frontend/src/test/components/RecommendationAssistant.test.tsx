import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import RecommendationAssistant from '../../components/Assistant/RecommendationAssistant';
import { apiService, type RecommendationDto } from '../../services/apiService';

vi.mock('../../services/apiService', () => ({
  apiService: {
    getRecommendations: vi.fn(),
    submitRecommendationFeedback: vi.fn(),
    getRecommendationFeedbackSummary: vi.fn()
  }
}));

const getRecommendationsMock = vi.mocked(apiService.getRecommendations);
const submitFeedbackMock = vi.mocked(apiService.submitRecommendationFeedback);
const getSummaryMock = vi.mocked(apiService.getRecommendationFeedbackSummary);

const createRecommendation = (overrides: Partial<RecommendationDto> = {}): RecommendationDto => ({
  id: 'rec-1',
  title: 'Reunión con equipo',
  subtitle: 'Alianza semanal',
  reason: 'Detectamos que los martes tenés mayor energía para reuniones.',
  recommendationType: 'focus',
  suggestedStart: '2025-11-02T14:00:00Z',
  suggestedDurationMinutes: 45,
  confidence: 0.82,
  categoryId: 'team',
  categoryName: 'Trabajo en equipo',
  moodImpact: 'Reduce el estrés acumulado',
  summary: 'Sincronizá objetivos con el equipo y definí próximos pasos.',
  ...overrides
});

describe('RecommendationAssistant', () => {
  beforeEach(() => {
    getRecommendationsMock.mockReset();
    submitFeedbackMock.mockReset();
    getSummaryMock.mockReset();
  });

  it('muestra recomendaciones después de cargar datos', async () => {
    getRecommendationsMock.mockResolvedValue([createRecommendation()]);
    getSummaryMock.mockResolvedValue({
      totalFeedback: 3,
      acceptedCount: 2,
      rejectedCount: 1,
      acceptanceRate: 66.7,
      averageMoodAfter: 4.5,
      periodStartUtc: '2025-10-01T00:00:00Z',
      periodEndUtc: '2025-10-31T00:00:00Z'
    });

    render(<RecommendationAssistant />);

    await waitFor(() => {
      expect(getRecommendationsMock).toHaveBeenCalledTimes(1);
    });

    expect(screen.getByText('Reunión con equipo')).toBeInTheDocument();
    expect(screen.getByText(/45 minutos/)).toBeInTheDocument();
  expect(screen.getByText('Feedback total')).toBeInTheDocument();
  expect(screen.getByText(/66\.7%/)).toBeInTheDocument();
    expect(getSummaryMock).toHaveBeenCalled();
  });

  it('informa cuando no hay sugerencias', async () => {
  getRecommendationsMock.mockResolvedValue([]);
    getSummaryMock.mockResolvedValue({
      totalFeedback: 0,
      acceptedCount: 0,
      rejectedCount: 0,
      acceptanceRate: 0,
      averageMoodAfter: null,
      periodStartUtc: '2025-10-01T00:00:00Z',
      periodEndUtc: '2025-10-31T00:00:00Z'
    });

    render(<RecommendationAssistant />);

    await waitFor(() => {
      expect(screen.getByRole('status')).toHaveTextContent('No encontramos sugerencias');
    });
  });

  it('permite reintentar después de un error', async () => {
    getRecommendationsMock
      .mockRejectedValueOnce(new Error('falló'))
  .mockResolvedValueOnce([createRecommendation({ id: 'rec-2', title: 'Bloque creativo' })]);

    getSummaryMock.mockResolvedValue({
      totalFeedback: 0,
      acceptedCount: 0,
      rejectedCount: 0,
      acceptanceRate: 0,
      averageMoodAfter: null,
      periodStartUtc: '2025-10-01T00:00:00Z',
      periodEndUtc: '2025-10-31T00:00:00Z'
    });

    render(<RecommendationAssistant />);

    await waitFor(() => {
      expect(screen.getByRole('alert')).toBeInTheDocument();
    });

    await userEvent.setup().click(screen.getByRole('button', { name: 'Reintentar' }));

    await waitFor(() => {
      expect(getRecommendationsMock).toHaveBeenCalledTimes(2);
    });

    expect(screen.getByText('Bloque creativo')).toBeInTheDocument();
  });

  it('envía feedback cuando el usuario acepta una sugerencia', async () => {
  getRecommendationsMock.mockResolvedValue([createRecommendation()]);
    submitFeedbackMock.mockResolvedValue();
    getSummaryMock.mockResolvedValue({
      totalFeedback: 1,
      acceptedCount: 1,
      rejectedCount: 0,
      acceptanceRate: 100,
      averageMoodAfter: 5,
      periodStartUtc: '2025-10-01T00:00:00Z',
      periodEndUtc: '2025-10-31T00:00:00Z'
    });

    render(<RecommendationAssistant />);

    const user = userEvent.setup();

    await waitFor(() => {
      expect(screen.getByText('Reunión con equipo')).toBeInTheDocument();
    });

    await user.click(screen.getByRole('button', { name: 'Me sirve' }));

    await waitFor(() => {
      expect(submitFeedbackMock).toHaveBeenCalledWith(
        expect.objectContaining({ recommendationId: 'rec-1', accepted: true })
      );
    });

    expect(screen.getByRole('button', { name: '¡Gracias!' })).toBeDisabled();
  });
});
