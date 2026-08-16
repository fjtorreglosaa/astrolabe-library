import { CssBaseline, ThemeProvider } from '@mui/material';
import { useMemo, type ReactNode } from 'react';
import { createAstrolabeTheme } from './createAstrolabeTheme';
import { useUiStore } from '../app/uiStore';

/**
 * Applies the Astrolabe theme and keeps it in sync with the user's chosen colour scheme.
 * The prototype exposes a scheme selector in the header, so light and dark are both first class.
 */
export const ThemeModeProvider = ({ children }: { children: ReactNode }) => {
  const scheme = useUiStore((state) => state.colorScheme);
  const theme = useMemo(() => createAstrolabeTheme(scheme), [scheme]);

  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      {children}
    </ThemeProvider>
  );
};
