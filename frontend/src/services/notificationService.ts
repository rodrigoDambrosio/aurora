import type { ReminderDto } from '../types/reminder.types';

/**
 * Servicio para gestionar las notificaciones del navegador
 */
class NotificationService {
  /**
   * Verifica si las notificaciones están soportadas por el navegador
   */
  isSupported(): boolean {
    return 'Notification' in window;
  }

  /**
   * Obtiene el estado actual de los permisos de notificaciones
   */
  getPermissionStatus(): NotificationPermission {
    if (!this.isSupported()) {
      return 'denied';
    }
    return Notification.permission;
  }

  /**
   * Solicita permisos para enviar notificaciones
   */
  async requestPermission(): Promise<NotificationPermission> {
    if (!this.isSupported()) {
      console.warn('Las notificaciones no están soportadas en este navegador');
      return 'denied';
    }

    if (Notification.permission === 'granted') {
      return 'granted';
    }

    if (Notification.permission === 'denied') {
      return 'denied';
    }

    try {
      const permission = await Notification.requestPermission();
      return permission;
    } catch (error) {
      console.error('Error al solicitar permisos de notificación:', error);
      return 'denied';
    }
  }

  /**
   * Muestra una notificación del navegador para un recordatorio
   */
  showNotification(reminder: ReminderDto): void {
    if (Notification.permission !== 'granted') {
      console.warn('No hay permisos para mostrar notificaciones');
      return;
    }

    const timeUntilEvent = this.getTimeUntilEvent(reminder.eventStartDate);

    const notification = new Notification(
      `${reminder.eventTitle}`,
      {
        body: timeUntilEvent,
        icon: '/aurora-icon.png', // Asegúrate de tener este ícono en public/
        badge: '/aurora-badge.png',
        tag: reminder.id, // Previene notificaciones duplicadas
        requireInteraction: false,
        silent: false,
      }
    );

    this.setupClickHandler(notification, reminder.eventId);
  }

  /**
   * Calcula el tiempo restante hasta el evento
   */
  private getTimeUntilEvent(eventStartDate: string): string {
    const start = new Date(eventStartDate);
    const now = new Date();
    const diffMinutes = Math.floor((start.getTime() - now.getTime()) / 1000 / 60);

    if (diffMinutes < 0) {
      return 'El evento ya comenzó';
    } else if (diffMinutes === 0) {
      return 'Comienza ahora';
    } else if (diffMinutes < 60) {
      return `Comienza en ${diffMinutes} minuto${diffMinutes !== 1 ? 's' : ''}`;
    } else if (diffMinutes < 1440) { // menos de 24 horas
      const hours = Math.floor(diffMinutes / 60);
      return `Comienza en ${hours} hora${hours !== 1 ? 's' : ''}`;
    } else {
      const days = Math.floor(diffMinutes / 1440);
      return `Comienza en ${days} día${days !== 1 ? 's' : ''}`;
    }
  }

  /**
   * Configura el manejador de click en la notificación
   */
  private setupClickHandler(notification: Notification, eventId: string): void {
    notification.onclick = () => {
      window.focus();
      // Navegar al evento (asumiendo que tienes una ruta para ver eventos)
      window.location.href = `/events/${eventId}`;
      notification.close();
    };

    // Auto-cerrar después de 10 segundos
    setTimeout(() => {
      notification.close();
    }, 10000);
  }

  /**
   * Verifica si el usuario ha ocultado el banner de permisos
   */
  hasUserDismissedBanner(): boolean {
    return localStorage.getItem('notificationBannerDismissed') === 'true';
  }

  /**
   * Marca el banner como descartado
   */
  dismissBanner(): void {
    localStorage.setItem('notificationBannerDismissed', 'true');
  }

  /**
   * Resetea el estado del banner (útil para testing)
   */
  resetBannerDismissal(): void {
    localStorage.removeItem('notificationBannerDismissed');
  }
}

export const notificationService = new NotificationService();
