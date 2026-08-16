import { deviceIconName } from './DevicesAndSessionsPage';
import { RevocationScope, type DeviceType } from '../api/sessionsApi';

/**
 * Regression for `GLOBAL-011`.
 *
 * The device icon was chosen by comparing `deviceType` against numeric codes, while the API sends
 * the enumeration by name. Nothing ever matched, every row rendered the generic fallback, and
 * TypeScript could not see it because the declared type said `number` and the declared type was
 * simply untrue. Confirmed against the running API, which answers `"deviceType": "Web"`.
 */
describe('device icons', () => {
  it.each<[DeviceType, string]>([
    ['Mobile', 'smartphone'],
    ['Tablet', 'tablet'],
    ['Web', 'computer'],
    ['Desktop', 'computer'],
  ])('gives %s its own icon', (deviceType, expected) => {
    expect(deviceIconName(deviceType)).toBe(expected);
  });

  it('falls back only for a genuinely unknown device', () => {
    expect(deviceIconName('Unknown')).toBe('devices');
  });

  it('does not answer the fallback for every known device', () => {
    // The shape of the original defect: every branch collapsing to the default. Asserting one icon
    // at a time would have passed happily while the switch matched nothing.
    const icons = (['Mobile', 'Tablet', 'Web', 'Desktop'] as DeviceType[]).map(deviceIconName);

    expect(new Set(icons).has('devices')).toBe(false);
    expect(new Set(icons).size).toBeGreaterThan(1);
  });
});

describe('revocation scope', () => {
  it('is sent by name, so a reordered enum cannot silently revoke the wrong thing', () => {
    // The same failure mode as the icons, with worse consequences: a numeric literal keeps
    // compiling against a reordered enum and starts ending every session instead of the others.
    expect(RevocationScope.AllOthers).toBe('AllOthers');
    expect(RevocationScope.All).toBe('All');
    expect(RevocationScope.Specified).toBe('Specified');
  });
});
