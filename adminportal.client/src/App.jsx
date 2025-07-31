//import { useEffect, useState } from 'react';
import './App.css';
import { BrowserRouter, Route, Routes } from 'react-router-dom';
import AdminPortal from './Components/AdminPortal.jsx';

// app context is overkill for admin, but in place for scalability...
import { AppProvider } from './contexts/AppContext.jsx';

function App() {
    return (
        <div className="App">
            <AppProvider>
                <BrowserRouter>
                    <Routes>
                        <Route path="/" element={ <AdminPortal /> } />
                    </Routes>
                </BrowserRouter>
            </AppProvider>
        </div>
    );
}

export default App;