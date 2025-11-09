import { Bell, X } from 'lucide-react';
import { useState } from 'react';
import { useNotifications } from '../context/NotificationContext';
import { notificationService } from '../services/notificationService';
import { ReminderType } from '../types/reminder.types';

interface NotificationPermissionBannerProps {
  onPermissionGranted?: () => void;
}

export function NotificationPermissionBanner({ onPermissionGranted }: NotificationPermissionBannerProps) {
  const [isVisible, setIsVisible] = useState(true);
  const { showNotification } = useNotifications();

  const handleEnable = async () => {
    const permission = await notificationService.requestPermission();

    if (permission === 'granted') {
      onPermissionGranted?.();
      setIsVisible(false);
    } else if (permission === 'denied') {
      // Mostrar mensaje de ayuda
      alert('Has denegado los permisos de notificaciones. Para habilitarlos, ve a la configuración de tu navegador.');
    }
  };

  const handleTestNotification = async () => {
    // Crear un recordatorio de prueba con todos los campos requeridos
    const testReminder = {
      id: 'test-' + Date.now(),
      eventId: 'test-event',
      eventTitle: '🎯 Reunión importante - Prueba',
      eventStartDate: new Date(Date.now() + 15 * 60 * 1000).toISOString(), // 15 minutos desde ahora
      eventCategoryColor: '#7b68ee',
      reminderType: ReminderType.Minutes15,
      triggerDateTime: new Date().toISOString(),
      isSent: false,
      createdAt: new Date().toISOString()
    };

    // Mostrar la notificación interna en lugar de la del navegador
    showNotification(testReminder);

    // Mostrar mensaje de confirmación
    // alert('¡Notificación de prueba enviada! Aparecerá en la esquina superior derecha con sonido.');
  };

  const handleRemindLater = () => {
    setIsVisible(false);
  };

  const handleDismiss = () => {
    notificationService.dismissBanner();
    setIsVisible(false);
  };

  if (!isVisible) {
    return null;
  }

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center p-4"
      style={{
        backgroundColor: 'rgba(0, 0, 0, 0.6)',
        backdropFilter: 'blur(4px)'
      }}
      onClick={(e) => {
        if (e.target === e.currentTarget) {
          handleRemindLater();
        }
      }}
    >
      <div
        className="w-full max-w-sm rounded-xl border shadow-2xl transition-all relative"
        style={{
          background: 'var(--color-surface)',
          borderColor: 'var(--color-border)',
          padding: '30px',
          textAlign: 'center'
        }}
        onClick={(e) => e.stopPropagation()}
      >
        {/* Botón cerrar en esquina superior derecha */}
        <button
          onClick={handleDismiss}
          className="absolute top-4 right-4 p-2 border-0 cursor-pointer transition-colors rounded"
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
          <X className="w-5 h-5" />
        </button>

        {/* Ícono grande centrado */}
        <div className="mb-6 flex flex-col items-center">
          <div
            className="mb-4 p-4 rounded-full"
            style={{
              background: 'linear-gradient(135deg, #facc15 0%, var(--gradient-primary-end) 100%)',
              width: '80px',
              height: '80px',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center'
            }}
          >
            <Bell
              className="w-10 h-10"
              style={{ color: 'white' }}
            />
          </div>
          <h2
            className="text-2xl font-bold m-0"
            style={{
              color: 'var(--color-text-primary)',
              letterSpacing: '0.5px'
            }}
          >
            Permisos de notificación
          </h2>
        </div>

        {/* Contenido */}
        <div className="mb-8">
          <p
            className="text-base mb-4 leading-relaxed"
            style={{ color: 'var(--color-text-primary)' }}
          >
            Aurora necesita tu permiso para recordarte tus eventos importantes.
          </p>
          <p
            className="text-sm leading-relaxed"
            style={{ color: 'var(--color-text-muted)' }}
          >
            Recibe notificaciones cuando se acerque un evento y nunca te olvides de lo que es importante para ti.
          </p>
        </div>

        {/* Botones apilados verticalmente */}
        <div className="flex flex-col gap-4">
          <button
            onClick={handleEnable}
            className="w-full px-6 py-4 text-base font-bold text-white border-0 rounded-lg cursor-pointer transition-all"
            style={{
              background: 'linear-gradient(135deg, var(--gradient-primary-start) 0%, var(--gradient-primary-end) 100%)',
              letterSpacing: '0.5px'
            }}
            onMouseEnter={(e) => {
              e.currentTarget.style.background = 'linear-gradient(135deg, var(--gradient-primary-hover-start) 0%, var(--gradient-primary-hover-end) 100%)';
              e.currentTarget.style.transform = 'translateY(-2px)';
              e.currentTarget.style.boxShadow = '0 6px 20px rgba(67, 56, 202, 0.4)';
            }}
            onMouseLeave={(e) => {
              e.currentTarget.style.background = 'linear-gradient(135deg, var(--gradient-primary-start) 0%, var(--gradient-primary-end) 100%)';
              e.currentTarget.style.transform = 'translateY(0)';
              e.currentTarget.style.boxShadow = 'none';
            }}
          >
            Habilitar notificaciones
          </button>

          <button
            onClick={handleTestNotification}
            className="w-full px-6 py-3 text-sm border-2 rounded-lg cursor-pointer transition-all font-medium"
            style={{
              background: 'var(--color-surface-alt)',
              color: 'var(--color-text-primary)',
              borderColor: 'var(--gradient-primary-start)',
            }}
            onMouseEnter={(e) => {
              e.currentTarget.style.background = 'var(--gradient-primary-start)';
              e.currentTarget.style.color = 'white';
              e.currentTarget.style.transform = 'translateY(-1px)';
            }}
            onMouseLeave={(e) => {
              e.currentTarget.style.background = 'var(--color-surface-alt)';
              e.currentTarget.style.color = 'var(--color-text-primary)';
              e.currentTarget.style.transform = 'translateY(0)';
            }}
          >
            🧪 Probar notificación
          </button>

          <button
            onClick={handleRemindLater}
            className="w-full px-6 py-3 text-base border-2 rounded-lg cursor-pointer transition-all font-medium"
            style={{
              background: 'transparent',
              color: 'var(--color-text-muted)',
              borderColor: 'var(--color-border)',
            }}
            onMouseEnter={(e) => {
              e.currentTarget.style.color = 'var(--color-text-primary)';
              e.currentTarget.style.borderColor = 'var(--gradient-primary-start)';
            }}
            onMouseLeave={(e) => {
              e.currentTarget.style.color = 'var(--color-text-muted)';
              e.currentTarget.style.borderColor = 'var(--color-border)';
            }}
          >
            Después
          </button>
        </div>
      </div>
    </div>
  );
}
