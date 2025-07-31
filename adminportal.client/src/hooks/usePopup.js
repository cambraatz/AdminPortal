// src/hooks/usePopup.js
import { useState, useCallback } from 'react';
import { FAIL_WAIT, SUCCESS_WAIT } from '../scripts/helperFunctions';

const DEFAULT_POPUP_TYPE = "Success"; // Or whatever your default is

export const usePopup = () => {
    const [popupType, setPopupType] = useState(DEFAULT_POPUP_TYPE);
    const [popupVisible, setVisible] = useState(false);

    // Use useCallback to memoize these functions, preventing unnecessary re-renders
    // in consuming components if they are passed down as props.
    const openPopup = useCallback((type) => {
        setPopupType(type);
        setVisible(true);
    }, []); // Empty dependency array means these functions only get created once

    const closePopup = useCallback(() => {
        setVisible(false);
        setPopupType(DEFAULT_POPUP_TYPE); // Reset to default when closing
    }, []);

    const successPopup = useCallback((popupType) => {
        setPopupType(popupType);
        setVisible(true);
        setTimeout(() => {
            setVisible(false);
        }, SUCCESS_WAIT)
    }, [])

    const failPopup = useCallback((message) => {
        console.error(message);
        setPopupType("fail");
        setVisible(true);
        setTimeout(() => {
            setVisible(false);
        }, FAIL_WAIT)
    }, [])

    // Return the state and functions that consuming components will need
    return { popupType, setPopupType, popupVisible, setVisible, openPopup, closePopup, successPopup, failPopup };
};