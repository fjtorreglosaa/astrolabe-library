import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import type { ColorScheme } from '../theme/tokens';

/**
 * Global UI state only. Server data belongs to TanStack Query and must never be duplicated here,
 * per GUIDELINES.md section 34.
 */
interface UiState {
  colorScheme: ColorScheme;

  /**
   * Expanded rather than hidden. The prototype's sidebar never disappears — it narrows to an icon
   * rail — so this is "wide or narrow", not "there or gone".
   */
  sidebarOpen: boolean;

  /**
   * Whether the quick actions button is put away. Separate from whether its menu is open, because
   * the prototype has both: `toggleFab` opens the dial and `hideFab` docks the button itself out of
   * the way. Persisted, so somebody who dismissed it does not get it back on every navigation.
   */
  quickActionsDocked: boolean;

  toggleColorScheme: () => void;
  setColorScheme: (scheme: ColorScheme) => void;
  toggleSidebar: () => void;
  setQuickActionsDocked: (docked: boolean) => void;
}

export const useUiStore = create<UiState>()(
  persist(
    (set) => ({
      colorScheme: 'light',
      sidebarOpen: true,
      quickActionsDocked: false,
      toggleColorScheme: () =>
        set((state) => ({ colorScheme: state.colorScheme === 'light' ? 'dark' : 'light' })),
      setColorScheme: (colorScheme) => set({ colorScheme }),
      toggleSidebar: () => set((state) => ({ sidebarOpen: !state.sidebarOpen })),
      setQuickActionsDocked: (quickActionsDocked) => set({ quickActionsDocked }),
    }),
    { name: 'astrolabe-ui' },
  ),
);
