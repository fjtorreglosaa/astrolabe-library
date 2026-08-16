const STORAGE_KEY = 'astrolabe-device-id';

/**
 * A stable identifier for this browser, used to label sessions on the devices screen.
 *
 * It is a **label, not a credential**: the API never authorizes anything with it, so storing it in
 * localStorage is safe in a way that storing a token there would not be.
 */
export const getDeviceId = (): string => {
  const existing = localStorage.getItem(STORAGE_KEY);

  if (existing) {
    return existing;
  }

  const generated = crypto.randomUUID();
  localStorage.setItem(STORAGE_KEY, generated);

  return generated;
};
