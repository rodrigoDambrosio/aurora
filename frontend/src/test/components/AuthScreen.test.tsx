import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import AuthScreen from '../../components/Auth/AuthScreen';

describe('AuthScreen', () => {
  it('renders login mode by default', () => {
    render(<AuthScreen />);

    expect(screen.getByRole('heading', { level: 2, name: 'Bienvenido de vuelta' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Iniciar sesión' })).toBeInTheDocument();
  });

  it('switches to register mode and validates passwords', async () => {
    const user = userEvent.setup();

    render(<AuthScreen />);

    await user.click(screen.getByRole('button', { name: 'Registro' }));
    await user.type(screen.getByLabelText('Nombre'), 'Camila Tester');
    await user.type(screen.getByLabelText('Email'), 'user@example.com');
    await user.type(screen.getByLabelText('Contraseña'), '12345678');
    await user.type(screen.getByLabelText('Confirmar contraseña'), '87654321');

    await user.click(screen.getByRole('button', { name: 'Registrarme' }));

    expect(screen.getByText('Las contraseñas no coinciden.')).toBeInTheDocument();
  });

  it('invokes callback on successful submit', async () => {
    const onAuthSuccess = vi.fn();
    const simulateAuth = vi.fn().mockResolvedValue(undefined);

    render(<AuthScreen onAuthSuccess={onAuthSuccess} simulateAuth={simulateAuth} />);

    const emailInput = screen.getByLabelText('Email');
    const passwordInput = screen.getByLabelText('Contraseña');
    const submitButton = screen.getByRole('button', { name: 'Iniciar sesión' });

    fireEvent.change(emailInput, { target: { value: 'user@example.com' } });
    fireEvent.change(passwordInput, { target: { value: '12345678' } });

    const form = submitButton.closest('form');
    expect(form).not.toBeNull();
    await act(async () => {
      fireEvent.submit(form!);
    });

    await waitFor(() => {
      expect(simulateAuth).toHaveBeenCalledTimes(1);
    }, { timeout: 300 });

    await waitFor(() => {
      expect(onAuthSuccess).toHaveBeenCalledTimes(1);
    }, { timeout: 300 });
  });
});
