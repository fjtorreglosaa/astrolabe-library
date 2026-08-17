import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { SidebarNav } from './SidebarNav';
import type { NavigationSection } from '../routes/navigation';

/**
 * The box model of the sidebar entries.
 *
 * <p>
 * These read like styling trivia and are not. `ListItemButton` carries `flex-grow: 1` of its own,
 * because MUI expects it inside a `ListItem` — a row — where filling the width is right. Dropped
 * straight into the sidebar's flex <em>column</em>, that rule stretches every entry down the cross
 * axis instead, and the navigation ends up spread evenly from the top of the panel to the bottom.
 * </p>
 * <p>
 * Nothing errors when that happens, no test failed, and the source gives no hint: the growth comes
 * from a default two layers away. It cost three rounds of "the sidebar looks wrong" to find, so it
 * is pinned here rather than left to be rediscovered.
 * </p>
 */
const SECTIONS: NavigationSection[] = [
  {
    label: 'Discover',
    items: [
      { route: '/home', label: 'Home', icon: 'space_dashboard', visibleTo: ['member'] },
      { route: '/catalog', label: 'Catalog', icon: 'menu_book', visibleTo: ['member'] },
    ],
  },
  {
    label: 'My account',
    items: [
      { route: '/fines', label: 'Fines & payments', icon: 'receipt_long', visibleTo: ['member'] },
    ],
  },
];

const renderNav = (expanded = true) =>
  render(
    <MemoryRouter>
      <SidebarNav sections={SECTIONS} expanded={expanded} />
    </MemoryRouter>,
  );

describe('SidebarNav', () => {
  it('does not let entries stretch down the panel', () => {
    renderNav();

    for (const label of ['Home', 'Catalog', 'Fines & payments']) {
      const entry = screen.getByRole('link', { name: label });
      const style = getComputedStyle(entry);

      // The whole point. `1` here is the navigation spread over the sidebar's full height.
      expect(style.flexGrow).toBe('0');
      expect(style.flexShrink).toBe('0');
    }
  });

  it('holds every entry to the prototype height rather than letting content push it', () => {
    renderNav();

    const entry = screen.getByRole('link', { name: 'Home' });
    const style = getComputedStyle(entry);

    expect(style.height).toBe('44px');
    // MUI's own 8px, plus 4px of ListItemText margin, overflows a 44px row — and `height` is not a
    // cap, so the row grows instead of clipping.
    expect(style.paddingTop).toBe('0px');
    expect(style.paddingBottom).toBe('0px');
  });

  it('keeps the entries at the top, with the leftover height empty', () => {
    renderNav();

    const nav = screen.getByRole('navigation', { name: 'Main navigation' });
    const style = getComputedStyle(nav);

    expect(style.flexDirection).toBe('column');
    expect(style.justifyContent).toBe('flex-start');
    expect(style.gap).toBe('2px');
  });

  it('shows a section title per group when expanded, and none in the rail', () => {
    const { unmount } = renderNav(true);
    expect(screen.getByText('Discover')).toBeInTheDocument();
    expect(screen.getByText('My account')).toBeInTheDocument();
    unmount();

    renderNav(false);
    // The prototype sets `display:none` on the title in the rail, which takes no space at all —
    // so the groups run together rather than gaining a gap of their own.
    expect(screen.queryByText('Discover')).not.toBeInTheDocument();
  });

  it('still renders every route as a link in the rail', () => {
    renderNav(false);

    // The labels go, the destinations do not. A rail that dropped entries would be a navigation
    // that changes what it can reach depending on how wide it is.
    expect(screen.getAllByRole('link')).toHaveLength(3);
  });
});
