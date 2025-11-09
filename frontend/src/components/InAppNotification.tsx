import { Bell, Calendar, Clock, X } from 'lucide-react';
import { useEffect, useState } from 'react';
import type { ReminderDto } from '../types/reminder.types';

interface InAppNotificationProps {
  reminder: ReminderDto;
  onDismiss: () => void;
  onViewEvent?: (eventId: string) => void | Promise<void>;
  stackIndex?: number;
}

export function InAppNotification({ reminder, onDismiss, onViewEvent, stackIndex = 0 }: InAppNotificationProps) {
  const [isVisible, setIsVisible] = useState(false);

  useEffect(() => {
    // Reproducir sonido de notificación
    playNotificationSound();

    // Mostrar con animación
    setIsVisible(true);

    // Auto-cerrar después de 10 segundos
    const timer = setTimeout(() => {
      handleDismiss();
    }, 10000);

    return () => clearTimeout(timer);
  }, []);

  const playNotificationSound = () => {
    try {
      // Crear un sonido simple usando Web Audio API
      const audioContext = new (window.AudioContext || (window as any).webkitAudioContext)();
      const oscillator = audioContext.createOscillator();
      const gainNode = audioContext.createGain();

      oscillator.connect(gainNode);
      gainNode.connect(audioContext.destination);

      // Configurar el sonido (tono suave)
      oscillator.frequency.setValueAtTime(800, audioContext.currentTime);
      oscillator.frequency.setValueAtTime(600, audioContext.currentTime + 0.1);

      gainNode.gain.setValueAtTime(0.3, audioContext.currentTime);
      gainNode.gain.exponentialRampToValueAtTime(0.01, audioContext.currentTime + 0.3);

      oscillator.start(audioContext.currentTime);
      oscillator.stop(audioContext.currentTime + 0.3);
    } catch (error) {
      console.warn('No se pudo reproducir el sonido de notificación:', error);
    }
  };

  const getTimeUntilEvent = (): string => {
    const start = new Date(reminder.eventStartDate);
    const now = new Date();
    const diffMilliseconds = start.getTime() - now.getTime();
    const diffMinutes = Math.floor(diffMilliseconds / (1000 * 60));

    if (diffMinutes < 0) {
      return 'El evento ya comenzó';
    } else if (diffMinutes === 0) {
      return 'Comienza ahora';
    } else if (diffMinutes < 60) {
      return `Comienza en ${diffMinutes} minuto${diffMinutes !== 1 ? 's' : ''}`;
    } else if (diffMinutes < 1440) { // menos de 24 horas
      const hours = Math.floor(diffMinutes / 60);
      const remainingMinutes = diffMinutes % 60;
      if (remainingMinutes > 0) {
        return `Comienza en ${hours} hora${hours !== 1 ? 's' : ''} y ${remainingMinutes} minuto${remainingMinutes !== 1 ? 's' : ''}`;
      } else {
        return `Comienza en ${hours} hora${hours !== 1 ? 's' : ''}`;
      }
    } else {
      const days = Math.floor(diffMinutes / 1440);
      return `Comienza en ${days} día${days !== 1 ? 's' : ''}`;
    }
  };

  const formatEventTime = (): string => {
    const start = new Date(reminder.eventStartDate);
    return start.toLocaleTimeString('es-ES', {
      hour: '2-digit',
      minute: '2-digit',
      hour12: false
    });
  };

  const formatEventDate = (): string => {
    const start = new Date(reminder.eventStartDate);
    return start.toLocaleDateString('es-ES', {
      weekday: 'long',
      day: 'numeric',
      month: 'long'
    });
  };

  const handleDismiss = () => {
    setIsVisible(false);
    setTimeout(() => {
      onDismiss();
    }, 300); // Esperar a que termine la animación
  };

  const handleViewEvent = async () => {
    try {
      console.log('InAppNotification.handleViewEvent called');
      console.log('reminder.eventId:', reminder.eventId);
      console.log('typeof reminder.eventId:', typeof reminder.eventId);
      console.log('onViewEvent function:', onViewEvent);

      await onViewEvent?.(reminder.eventId);
      handleDismiss();
    } catch (error) {
      console.error('Error al abrir evento desde notificación:', error);
      handleDismiss();
    }
  };

  return (
    <div
      className={`fixed z-[9999] transition-all duration-300 ${isVisible ? 'translate-x-0 opacity-100' : 'translate-x-full opacity-0'
        }`}
      style={{
        maxWidth: '400px',
        top: `${20 + stackIndex * 120}px`, // Apilar verticalmente
        right: '20px'
      }}
    >
      <div
        className="rounded-xl border shadow-2xl transition-all relative"
        style={{
          background: 'var(--color-surface)',
          borderColor: reminder.eventCategoryColor || 'var(--color-border)',
          borderWidth: '2px',
          padding: '24px'
        }}
      >
        {/* Botón cerrar */}
        <button
          onClick={handleDismiss}
          className="absolute top-3 right-3 p-1 border-0 cursor-pointer transition-colors rounded"
          style={{
            background: 'none',
            color: 'var(--color-text-muted)',
          }}
          onMouseEnter={(e) => {
            e.currentTarget.style.color = 'var(--color-text-primary)';
          }}
          onMouseLeave={(e) => {
            e.currentTarget.style.color = 'var(--color-text-muted)';
          }}
          aria-label="Cerrar"
        >
          <X className="w-4 h-4" />
        </button>

        {/* Header con ícono */}
        <div className="flex items-start gap-3 mb-4">
          <div
            className="p-2 rounded-full flex-shrink-0"
            style={{
              backgroundColor: reminder.eventCategoryColor + '20' || 'rgba(123, 104, 238, 0.2)'
            }}
          >
            <Bell
              className="w-5 h-5"
              style={{ color: reminder.eventCategoryColor || '#7b68ee' }}
            />
          </div>
          <div className="flex-1 min-w-0">
            <h3
              className="text-sm font-semibold m-0 mb-1"
              style={{ color: 'var(--color-text-primary)' }}
            >
              Recordatorio de evento
            </h3>
            <p
              className="text-xs m-0"
              style={{ color: 'var(--color-text-muted)' }}
            >
              {getTimeUntilEvent()}
            </p>
          </div>
        </div>

        {/* Información del evento */}
        <div className="mb-4">
          <h4
            className="text-lg font-bold m-0 mb-3 leading-tight"
            style={{ color: 'var(--color-text-primary)' }}
          >
            {reminder.eventTitle}
          </h4>

          <div className="flex items-center gap-2 mb-2">
            <Calendar
              className="w-4 h-4"
              style={{ color: 'var(--color-text-muted)' }}
            />
            <span
              className="text-sm"
              style={{ color: 'var(--color-text-muted)' }}
            >
              {formatEventDate()}
            </span>
          </div>

          <div className="flex items-center gap-2">
            <Clock
              className="w-4 h-4"
              style={{ color: 'var(--color-text-muted)' }}
            />
            <span
              className="text-sm"
              style={{ color: 'var(--color-text-muted)' }}
            >
              {formatEventTime()}
            </span>
          </div>
        </div>

        {/* Botones */}
        <div className="flex flex-col gap-3 mt-8">
          <button
            onClick={handleViewEvent}
            className="w-full px-4 py-3 text-sm font-semibold text-white border-0 rounded-lg cursor-pointer transition-all"
            style={{
              background: `linear-gradient(135deg, ${reminder.eventCategoryColor || 'var(--gradient-primary-start)'} 0%, ${reminder.eventCategoryColor ? reminder.eventCategoryColor + 'dd' : 'var(--gradient-primary-end)'} 100%)`,
              letterSpacing: '0.5px'
            }}
            onMouseEnter={(e) => {
              const hoverStart = reminder.eventCategoryColor ? reminder.eventCategoryColor + 'ee' : 'var(--gradient-primary-hover-start)';
              const hoverEnd = reminder.eventCategoryColor ? reminder.eventCategoryColor + 'bb' : 'var(--gradient-primary-hover-end)';
              e.currentTarget.style.background = `linear-gradient(135deg, ${hoverStart} 0%, ${hoverEnd} 100%)`;
              e.currentTarget.style.transform = 'translateY(-2px)';
              e.currentTarget.style.boxShadow = `0 6px 20px ${reminder.eventCategoryColor || '#4338ca'}40`;
            }}
            onMouseLeave={(e) => {
              e.currentTarget.style.background = `linear-gradient(135deg, ${reminder.eventCategoryColor || 'var(--gradient-primary-start)'} 0%, ${reminder.eventCategoryColor ? reminder.eventCategoryColor + 'dd' : 'var(--gradient-primary-end)'} 100%)`;
              e.currentTarget.style.transform = 'translateY(0)';
              e.currentTarget.style.boxShadow = 'none';
            }}
          >
            Ver evento
          </button>

          <button
            onClick={handleDismiss}
            className="w-full px-4 py-3 text-sm border-2 rounded-lg cursor-pointer transition-all font-medium"
            style={{
              background: 'transparent',
              color: 'var(--color-text-muted)',
              borderColor: 'var(--color-border)',
            }}
            onMouseEnter={(e) => {
              e.currentTarget.style.color = 'var(--color-text-primary)';
              e.currentTarget.style.borderColor = reminder.eventCategoryColor || 'var(--gradient-primary-start)';
            }}
            onMouseLeave={(e) => {
              e.currentTarget.style.color = 'var(--color-text-muted)';
              e.currentTarget.style.borderColor = 'var(--color-border)';
            }}
          >
            Cerrar
          </button>
        </div>
      </div>
    </div>
  );
}