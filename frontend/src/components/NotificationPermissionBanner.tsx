import { Bell, X } from 'lucide-react';
import { useState } from 'react';
import { notificationService } from '../services/notificationService';

interface NotificationPermissionBannerProps {
  onPermissionGranted?: () => void;
}

export function NotificationPermissionBanner({ onPermissionGranted }: NotificationPermissionBannerProps) {
  const [isVisible, setIsVisible] = useState(true);

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
    <div className="bg-amber-50 border-b border-amber-200 px-4 py-3 sticky top-0 z-50 shadow-sm">
      <div className="max-w-4xl mx-auto flex items-start gap-3">
        <Bell className="w-5 h-5 text-amber-600 flex-shrink-0 mt-0.5" />

        <div className="flex-1 min-w-0">
          <p className="text-sm text-amber-900 font-medium">
            Aurora necesita tu permiso para enviarte recordatorios de tus eventos
          </p>
          <p className="text-xs text-amber-700 mt-1">
            Recibe notificaciones cuando se acerque un evento importante
          </p>
        </div>

        <div className="flex items-center gap-2 flex-shrink-0">
          <button
            onClick={handleEnable}
            className="px-3 py-1.5 bg-amber-600 text-white text-sm font-medium rounded-lg hover:bg-amber-700 transition-colors"
          >
            Habilitar
          </button>

          <button
            onClick={handleRemindLater}
            className="px-3 py-1.5 text-amber-700 text-sm font-medium hover:bg-amber-100 rounded-lg transition-colors"
          >
            Después
          </button>

          <button
            onClick={handleDismiss}
            className="p-1 text-amber-600 hover:bg-amber-100 rounded transition-colors"
            aria-label="Cerrar"
          >
            <X className="w-4 h-4" />
          </button>
        </div>
      </div>
    </div>
  );
}
