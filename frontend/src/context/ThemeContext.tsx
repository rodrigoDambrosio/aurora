import { useEffect, useMemo, useState, type ReactNode } from 'react';
import { applyTheme, detectPreferredTheme, THEME_STORAGE_KEY, type Theme } from './themeUtils';
import { ThemeContext, type ThemeContextValue } from './themeContextInternal';

export function ThemeProvider({ children }: { children: ReactNode }) {
  const [theme, setThemeState] = useState<Theme>(() => detectPreferredTheme());

  useEffect(() => {
    applyTheme(theme);
    if (typeof window !== 'undefined') {
      window.localStorage.setItem(THEME_STORAGE_KEY, theme);
    }
  }, [theme]);

  useEffect(() => {
    if (!window.matchMedia) {
      return;
    }

    const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
    const handler = (event: MediaQueryListEvent) => {
  const storedTheme = window.localStorage.getItem(THEME_STORAGE_KEY) as Theme | null;
      if (storedTheme === 'light' || storedTheme === 'dark') {
        return;
      }
      setThemeState(event.matches ? 'dark' : 'light');
    };

    mediaQuery.addEventListener('change', handler);
    return () => mediaQuery.removeEventListener('change', handler);
  }, []);

  const contextValue = useMemo<ThemeContextValue>(() => ({
    theme,
    toggleTheme: () => setThemeState((prev) => (prev === 'light' ? 'dark' : 'light')),
    setTheme: (nextTheme) => setThemeState(nextTheme),
  }), [theme]);

  return (
    <ThemeContext.Provider value={contextValue}>
      {children}
    </ThemeContext.Provider>
  );
}

