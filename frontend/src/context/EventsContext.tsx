import React, { useState } from 'react';
import { EventsContext } from './eventsContextInternal';

export const EventsProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [refreshToken, setRefreshToken] = useState(0);

  const refreshEvents = () => {
    setRefreshToken(prev => prev + 1);
  };

  return (
    <EventsContext.Provider value={{ refreshToken, refreshEvents }}>
      {children}
    </EventsContext.Provider>
  );
};
