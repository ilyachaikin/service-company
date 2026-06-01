import React, { createContext, useContext, useState, useEffect } from 'react';

type ThemeMode = 'light' | 'dark';

interface ThemeContextType {
  themeMode: ThemeMode;
  toggleTheme: () => void;
  setTheme: (mode: ThemeMode) => void;
  isDark: boolean;
}

const ThemeContext = createContext<ThemeContextType | undefined>(undefined);

const STORAGE_KEY = 'sc_theme';

export const ThemeProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [themeMode, setThemeMode] = useState<ThemeMode>(() => {
    return (localStorage.getItem(STORAGE_KEY) as ThemeMode) || 'light';
  });

  useEffect(() => {
    localStorage.setItem(STORAGE_KEY, themeMode);

    document.body.classList.toggle('dark-theme', themeMode === 'dark');
  }, [themeMode]);

  const toggleTheme = () => setThemeMode(prev => prev === 'light' ? 'dark' : 'light');
  const setTheme = (mode: ThemeMode) => setThemeMode(mode);

  return (
    <ThemeContext.Provider value={{ themeMode, toggleTheme, setTheme, isDark: themeMode === 'dark' }}>
      {children}
    </ThemeContext.Provider>
  );
};

export const useTheme = () => {
  const ctx = useContext(ThemeContext);
  if (!ctx) throw new Error('useTheme must be used within ThemeProvider');
  return ctx;
};
