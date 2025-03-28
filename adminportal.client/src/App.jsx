//import { useEffect, useState } from 'react';
import './App.css';
import { BrowserRouter, Route, Routes } from 'react-router-dom';
import AdminPortal from './Components/AdminPortal.jsx';

function App() {
    return (
        <div className="App">
            <BrowserRouter>
                <Routes>
                    <Route path="/" element={ <AdminPortal /> } />
                </Routes>
            </BrowserRouter>
        </div>
    );
}

export default App;