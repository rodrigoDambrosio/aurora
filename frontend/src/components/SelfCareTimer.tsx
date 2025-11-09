import { Pause, Play, RotateCcw, Star, X } from 'lucide-react';
import { useEffect, useState } from 'react';
import type { SelfCareRecommendationDto } from '../services/apiService';

interface SelfCareTimerProps {
  recommendation: SelfCareRecommendationDto;
  onComplete: (moodAfter: number) => void;
  onCancel: () => void;
}

export default function SelfCareTimer({
  recommendation,
  onComplete,
  onCancel
}: SelfCareTimerProps) {
  const [timeLeft, setTimeLeft] = useState(recommendation.durationMinutes * 60); // en segundos
  const [isRunning, setIsRunning] = useState(false);
  const [showFeedback, setShowFeedback] = useState(false);
  const [selectedMood, setSelectedMood] = useState<number | null>(null);

  useEffect(() => {
    if (!isRunning || showFeedback) return;

    const interval = setInterval(() => {
      setTimeLeft((prev) => {
        if (prev <= 1) {
          setIsRunning(false);
          setShowFeedback(true);

          // Notificación del navegador
          if ('Notification' in window && Notification.permission === 'granted') {
            new Notification('¡Tiempo completado!', {
              body: `Has terminado: ${recommendation.title}`,
              icon: recommendation.icon
            });
          }

          return 0;
        }
        return prev - 1;
      });
    }, 1000);

    return () => clearInterval(interval);
  }, [isRunning, showFeedback, recommendation.title, recommendation.icon]);

  const minutes = Math.floor(timeLeft / 60);
  const seconds = timeLeft % 60;

  const handleStart = () => {
    setIsRunning(true);

    // Pedir permiso para notificaciones
    if ('Notification' in window && Notification.permission === 'default') {
      Notification.requestPermission();
    }
  };

  const handlePause = () => {
    setIsRunning(false);
  };

  const handleReset = () => {
    setIsRunning(false);
    setTimeLeft(recommendation.durationMinutes * 60);
  };

  const handleSubmitFeedback = () => {
    if (selectedMood) {
      onComplete(selectedMood);
    }
  };

  const handleSkipFeedback = () => {
    onComplete(0); // 0 indica que no quiso dar feedback
  };

  if (showFeedback) {
    return (
      <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm">
        <div className="bg-white dark:bg-gray-800 rounded-lg shadow-xl p-6 max-w-md w-full mx-4">
          <div className="text-center space-y-4">
            <div className="text-6xl">{recommendation.icon}</div>
            <h3 className="text-xl font-semibold text-gray-900 dark:text-white">
              ¡Completaste la actividad!
            </h3>
            <p className="text-gray-600 dark:text-gray-400">
              {recommendation.title}
            </p>

            <div className="pt-4">
              <p className="text-sm text-gray-700 dark:text-gray-300 mb-3">
                ¿Cómo te sentiste después?
              </p>
              <div className="flex justify-center gap-2">
                {[1, 2, 3, 4, 5].map((mood) => (
                  <button
                    key={mood}
                    onClick={() => setSelectedMood(mood)}
                    className={`p-3 rounded-lg transition-all ${selectedMood === mood
                        ? 'bg-yellow-400 text-white scale-110'
                        : 'bg-gray-100 dark:bg-gray-700 text-gray-400 hover:bg-gray-200 dark:hover:bg-gray-600'
                      }`}
                  >
                    <Star
                      className="h-6 w-6"
                      fill={selectedMood === mood ? 'currentColor' : 'none'}
                    />
                  </button>
                ))}
              </div>
              <p className="text-xs text-gray-500 dark:text-gray-400 mt-2">
                1 = Mal · 5 = Excelente
              </p>
            </div>

            <div className="flex gap-2 pt-4">
              <button
                onClick={handleSkipFeedback}
                className="flex-1 px-4 py-2 text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg transition-colors"
              >
                Omitir
              </button>
              <button
                onClick={handleSubmitFeedback}
                disabled={!selectedMood}
                className={`flex-1 px-4 py-2 rounded-lg font-medium transition-colors ${selectedMood
                    ? 'bg-primary-600 text-white hover:bg-primary-700'
                    : 'bg-gray-300 dark:bg-gray-700 text-gray-500 cursor-not-allowed'
                  }`}
              >
                Enviar
              </button>
            </div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm">
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow-xl p-6 max-w-md w-full mx-4">
        {/* Header */}
        <div className="flex justify-between items-start mb-6">
          <div className="flex items-center gap-3">
            <div className="text-4xl">{recommendation.icon}</div>
            <div>
              <h3 className="font-semibold text-gray-900 dark:text-white">
                {recommendation.title}
              </h3>
              <p className="text-sm text-gray-600 dark:text-gray-400">
                {recommendation.durationMinutes} minutos
              </p>
            </div>
          </div>
          <button
            onClick={onCancel}
            className="p-2 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-full transition-colors"
            aria-label="Cancelar"
          >
            <X className="h-5 w-5 text-gray-500 dark:text-gray-400" />
          </button>
        </div>

        {/* Timer Display */}
        <div className="text-center mb-6">
          <div className="text-6xl font-bold text-primary-600 dark:text-primary-400 tabular-nums">
            {String(minutes).padStart(2, '0')}:{String(seconds).padStart(2, '0')}
          </div>
          <div className="mt-2 h-2 bg-gray-200 dark:bg-gray-700 rounded-full overflow-hidden">
            <div
              className="h-full bg-primary-600 dark:bg-primary-500 transition-all duration-1000"
              style={{
                width: `${((recommendation.durationMinutes * 60 - timeLeft) / (recommendation.durationMinutes * 60)) * 100}%`
              }}
            />
          </div>
        </div>

        {/* Controls */}
        <div className="flex gap-2">
          {!isRunning ? (
            <button
              onClick={handleStart}
              className="flex-1 flex items-center justify-center gap-2 px-4 py-3 bg-green-600 text-white rounded-lg hover:bg-green-700 transition-colors font-medium"
            >
              <Play className="h-5 w-5" />
              {timeLeft === recommendation.durationMinutes * 60 ? 'Comenzar' : 'Continuar'}
            </button>
          ) : (
            <button
              onClick={handlePause}
              className="flex-1 flex items-center justify-center gap-2 px-4 py-3 bg-yellow-600 text-white rounded-lg hover:bg-yellow-700 transition-colors font-medium"
            >
              <Pause className="h-5 w-5" />
              Pausar
            </button>
          )}
          <button
            onClick={handleReset}
            className="px-4 py-3 bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 rounded-lg hover:bg-gray-200 dark:hover:bg-gray-600 transition-colors"
            title="Reiniciar"
          >
            <RotateCcw className="h-5 w-5" />
          </button>
        </div>

        {/* Motivational Message */}
        <div className="mt-4 p-3 bg-blue-50 dark:bg-blue-900/20 rounded-lg">
          <p className="text-sm text-blue-800 dark:text-blue-200 text-center">
            💡 {recommendation.personalizedReason}
          </p>
        </div>
      </div>
    </div>
  );
}
