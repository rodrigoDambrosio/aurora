import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import WellnessDashboard from '../../components/WellnessDashboard';
import { apiService } from '../../services/apiService';

vi.mock('../../services/apiService', () => ({
  apiService: {
    getWellnessSummary: vi.fn()
  }
}));

const getSummaryMock = vi.mocked(apiService.getWellnessSummary);

const buildSummary = () => ({
  year: 2025,
  month: 10,
  averageMood: 4.4,
  bestDay: {
    date: '2025-10-12T00:00:00Z',
    moodRating: 5,
    notes: 'Noche con amigos'
  },
  worstDay: {
    date: '2025-10-03T00:00:00Z',
    moodRating: 2,
    notes: 'Mucho trabajo'
  },
  moodTrend: [
    { date: '2025-10-01T00:00:00Z', averageMood: null, entries: 0 },
    { date: '2025-10-02T00:00:00Z', averageMood: 4, entries: 1 },
    { date: '2025-10-03T00:00:00Z', averageMood: 2, entries: 1 }
  ],
  moodDistribution: [
    { moodRating: 1, count: 0, percentage: 0 },
    { moodRating: 2, count: 1, percentage: 0.1 },
    { moodRating: 3, count: 1, percentage: 0.2 },
    { moodRating: 4, count: 4, percentage: 0.4 },
    { moodRating: 5, count: 3, percentage: 0.3 }
  ],
  streaks: {
    currentPositive: 2,
    longestPositive: 5,
    currentNegative: 0,
    longestNegative: 1
  },
  categoryImpacts: [
    {
      categoryId: 'fitness',
      categoryName: 'Entrenamiento',
      categoryColor: '#34d399',
      averageMood: 4.8,
      eventCount: 2,
      positiveCount: 2,
      negativeCount: 0
    }
  ],
  totalTrackedDays: 9,
  positiveDays: 5,
  neutralDays: 1,
  negativeDays: 1,
  trackingCoverage: 0.29,
  hasEventMoodData: true
});

describe('WellnessDashboard', () => {
  beforeEach(() => {
    getSummaryMock.mockReset();
    getSummaryMock.mockResolvedValue(buildSummary());
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  it('renders summary metrics after loading data', async () => {
    render(<WellnessDashboard />);

    await waitFor(() => {
      expect(apiService.getWellnessSummary).toHaveBeenCalled();
    });

    expect(screen.getByText('4.4')).toBeInTheDocument();
    expect(screen.getByText('Cobertura de registro')).toBeInTheDocument();
    expect(screen.getByText('29%')).toBeInTheDocument();
    expect(screen.getByText('Entrenamiento')).toBeInTheDocument();
    expect(screen.getByText('2 eventos con registro')).toBeInTheDocument();
  });

  it('handles API errors and allows retry', async () => {
    getSummaryMock
      .mockRejectedValueOnce(new Error('Network error'))
      .mockResolvedValueOnce(buildSummary());

    render(<WellnessDashboard />);

    await waitFor(() => {
      expect(screen.getByRole('alert')).toBeInTheDocument();
    });

    const retryButton = screen.getByRole('button', { name: 'Reintentar' });
    await userEvent.setup().click(retryButton);

    await waitFor(() => {
      expect(apiService.getWellnessSummary).toHaveBeenCalledTimes(2);
    });
  });

  it('requests a new summary when navigating months', async () => {
    const user = userEvent.setup();
    render(<WellnessDashboard />);

    await waitFor(() => {
      expect(apiService.getWellnessSummary).toHaveBeenCalled();
    });

    const initialCall = apiService.getWellnessSummary.mock.calls.at(-1);
    expect(initialCall).toBeDefined();

    await user.click(screen.getByLabelText('Mes siguiente'));

    await waitFor(() => {
      expect(apiService.getWellnessSummary.mock.calls.length).toBeGreaterThanOrEqual(2);
    });

    const latestCall = apiService.getWellnessSummary.mock.calls.at(-1);
    expect(latestCall).toBeDefined();

    if (initialCall && latestCall) {
      const initialMonth = initialCall[1];
      const latestMonth = latestCall[1];
      const expectedNextMonth = initialMonth === 12 ? 1 : initialMonth + 1;
      expect(latestMonth).toBe(expectedNextMonth);
    }
  });
});
