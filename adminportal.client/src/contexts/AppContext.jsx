// eslint-disable-next-line no-unused-vars
import React, { createContext, useState } from 'react';
import PropTypes from 'prop-types';
//import { validateSession } from '../utils/api/sessions'; // Assuming this path is correct

// 1. Create the Context
export const AppContext = createContext(null);

// 2. Create the Provider Component
export function AppProvider({ children }) {
  // Global States
  const [loading, setLoading] = useState(true); // loading state to prevent early rendering
  const [collapsed, setCollapsed] = useState(false); // default to open header state
  const [session,setSession] = useState({ // valid date/powerunit combination, assume to be active if not null
    id: -1,
    username: "",
    powerunit: "",
    mfstdate: "",
    company: "",
    valid: false,
  });

  // The value object contains all global states and functions to be shared
  const contextValue = {
    loading, setLoading, // [bool] trigger loading spinner
    collapsed, setCollapsed, // [bool] collapsed header status
    session, setSession // [obj] valid date/powerunit combo in use
  };

  return (
    <AppContext.Provider value={contextValue}>
      {children}
    </AppContext.Provider>
  );
}

AppProvider.propTypes = {
    children: PropTypes.any,
}