// src/Hooks/useAppContext.jsx (or src/Contexts/useAppContext.jsx)
import { useContext } from 'react';
import { AppContext } from '../contexts/AppContext'; // Import the context itself

export function useAppContext() {
  const context = useContext(AppContext);
  if (context === null) {
    throw new Error('useAppContext must be used within an AppProvider');
  }
  return context;
}