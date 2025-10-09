import React from 'react';
import { cn } from '../../lib/utils';

interface ToastProps extends React.HTMLAttributes<HTMLDivElement> {
  variant?: 'default' | 'success' | 'error' | 'warning';
  children: React.ReactNode;
}

export const Toast = React.forwardRef<HTMLDivElement, ToastProps>(
  ({ className, variant = 'default', ...props }, ref) => {
    const variantClasses = {
      default: 'bg-background text-foreground border',
      success: 'bg-green-500 text-white border-green-600',
      error: 'bg-red-500 text-white border-red-600',
      warning: 'bg-yellow-500 text-black border-yellow-600'
    };

    return (
      <div
        ref={ref}
        className={cn(
          'fixed top-4 right-4 z-50 rounded-lg p-4 shadow-lg transition-all duration-300',
          variantClasses[variant],
          className
        )}
        {...props}
      />
    );
  }
);

Toast.displayName = 'Toast';