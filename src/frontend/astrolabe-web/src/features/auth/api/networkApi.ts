import { httpClient } from '../../../shared/api/httpClient';

export interface Country {
  id: string;
  name: string;
  isoCode: string;
}

export interface City {
  id: string;
  countryId: string;
  name: string;
  homeLibraryId: string | null;
}

/**
 * Countries offered at registration.
 *
 * The API derives this from active libraries rather than a flag, so a country never appears here
 * unless a member registering into it could actually borrow something (BR-NET-004).
 */
export const getRegistrationCountries = async (): Promise<Country[]> => {
  const { data } = await httpClient.get<Country[]>('/api/v1/network/countries');
  return data;
};

export const getCitiesByCountry = async (countryId: string): Promise<City[]> => {
  const { data } = await httpClient.get<City[]>(`/api/v1/network/countries/${countryId}/cities`);
  return data;
};
