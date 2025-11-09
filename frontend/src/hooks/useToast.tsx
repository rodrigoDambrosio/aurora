import { useCallback, useState } from 'react';
import Toast from '../components/Toast';

type ToastType = 'success' | 'error' | 'info' | 'warning';

interface ToastState {
  id: string;
  message: string;
  type: ToastType;
}

const createToastId = () => Date.now().toString();

export const useToast = () => {
  const [toasts, setToasts] = useState<ToastState[]>([]);

  const removeToast = useCallback((id: string) => {
    setToasts(prev => prev.filter(toast => toast.id !== id));
  }, []);

  const showToast = useCallback(
    (message: string, type: ToastType = 'info') => {
      const id = createToastId();
      setToasts(prev => [...prev, { id, message, type }]);
    },
    [],
  );

  const ToastContainer = () => (
    <>
      {toasts.map(toast => (
        <Toast
          key={toast.id}
          message={toast.message}
          type={toast.type}
          onClose={() => removeToast(toast.id)}
        />
      ))}
    </>
  );

  return {
    showToast,
    showSuccess: (message: string) => showToast(message, 'success'),
    showError: (message: string) => showToast(message, 'error'),
    showWarning: (message: string) => showToast(message, 'warning'),
    showInfo: (message: string) => showToast(message, 'info'),
    ToastContainer,
  };
};
