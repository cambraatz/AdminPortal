import React from 'react';
import '../LoadingSpinner.css';

const LoadingSpinner = () => {
    return (
        <div className="spinner-cont">
            <div className="loading-spinner"></div>
            <p className="lading-text">Loading...</p>
        </div>
    );
};

export default LoadingSpinner;