import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import type { ColorScheme } from '../theme/tokens';

/**
 * Global UI state only. Server data belongs to TanStack Query and must never be duplicated here,
 * per GUIDELINES.md section 34.
 */
interface UiState {
  colorScheme: ColorScheme;
  sidebarOpen: boolean;
  toggleColorScheme: () => void;
  setColorScheme: (scheme: ColorScheme) => void;
  toggleSidebar: () => void;
}

export const useUiStore = create<UiState>()(
  persist(
    (set) => ({
      colorScheme: 'light',
      sidebarOpen: true,
      toggleColorScheme: () =>
        set((state) => ({ colorScheme: state.colorScheme === 'light' ? 'dark' : 'light' })),
      setColorScheme: (colorScheme) => set({ colorScheme }),
      toggleSidebar: () => set((state) => ({ sidebarOpen: !state.sidebarOpen })),
    }),
    { name: 'astrolabe-ui' },
  ),
);
