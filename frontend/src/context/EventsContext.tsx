import React, { createContext, useContext, useState } from 'react';

interface EventsContextType {
  refreshToken: number;
  refreshEvents: () => void;
}

const EventsContext = createContext<EventsContextType | undefined>(undefined);

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

export const useEvents = () => {
  const context = useContext(EventsContext);
  if (context === undefined) {
    throw new Error('useEvents must be used within an EventsProvider');
  }
  return context;
};
