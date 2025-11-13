import { createContext } from 'react';

export interface EventsContextValue {
  refreshToken: number;
  refreshEvents: () => void;
}

export const EventsContext = createContext<EventsContextValue | undefined>(undefined);
