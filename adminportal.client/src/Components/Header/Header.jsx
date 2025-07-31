//import { useState } from 'react';
import PropTypes from 'prop-types';
import UserWidget from '../UserWidget/UserWidget';
import toggleDots from '../../assets/Toggle_Dots.svg';
import {
    Logout,
    Return
} from '../../utils/api/sessions';

import { useAppContext } from '../../hooks/useAppContext';
import { usePopup } from '../../hooks/usePopup';
import { useNavigate } from 'react-router-dom';

import "./Header.css";

const Header = ({ 
    company, 
    title, 
    subtitle,
    currUser, 
    logoutButton, 
    root
}) => {
    const { 
        collapsed, setCollapsed, session
    } = useAppContext();
    /*session = {
        id: -1,
        username: "",
        powerunit: "",
        mfstdate: "",
        company: "",
        valid: false,
    }*/

    const { 
        popupType, /*setPopupType,*/
        openPopup, closePopup,
        popupVisible, /*setVisible,*/
    } = usePopup();

    const navigate = useNavigate();

    const toggleHeader = () => {
        setCollapsed(prevCollapsed => !prevCollapsed);
        // phase this out...
        const collapsedSS = sessionStorage.getItem("collapsed") === "true";
        sessionStorage.setItem("collapsed", !collapsedSS);
    }

    async function popupReturn(root) {
        if (root) {
            openPopup("return");
        }
        // navigate back a URL directory...
        const path = await Return(root, session.id);
        /*if (!root && path.endsWith('/')) { // ADD ANY RETURN LOGIC NEEDED IN ADMIN...
            // URL ends with /deliveries
            console.log("Attempting to release manifest access...");
            const response = await resetManifestAccess(session.username, session.powerunit, session.mfstdate, session.id);
            if (!response.success) {
                console.error("Failed to release manifest access hold.");
            }
        }*/

        navigate(path);
    }
        
    const popupLogout = () => {
        openPopup("logout");
        Logout(session);
    }

    return(
        <>
        <header className={`header ${collapsed ? "collapsed-header" : ''}`}>
            <div className={`buffer ${collapsed ? "collapsed-buffer" : ''}`}></div>
            
            <div className={`company_title ${collapsed ? "hidden" : ''}`}>
                {company.map((word, index) => (
                    <h4 className="company_list_title" key={index}>{word}</h4>
                ))}
            </div>
            <div className="sticky_header">
                <div className={`module_title ${collapsed ? "hidden" : ''}`}>
                    <h1>{title}</h1>
                    {/*<h2 id="title_dash">-</h2>*/}
                    <h2>{subtitle}</h2>
                </div>
                <div id="collapse_toggle_button" onClick={toggleHeader}>
                    <img id="toggle_dots" src={toggleDots} alt="toggle dots" />
                </div>
                <UserWidget 
                    currUser={currUser} 
                    logoutButton={logoutButton}
                    root={root}
                    popupReturn={popupReturn}
                    popupLogout={popupLogout}
                />
            </div>
        </header>
        {popupVisible && (
            <Popup 
                popupType={popupType}
                isVisible={popupVisible}
                closePopup={closePopup}
            />
        )}
        </>
    )
};

export default Header;

// *state variable actively managed in parent...
Header.propTypes = {
    company: PropTypes.array,// var company name {activeCompany}*
    title: PropTypes.string, // const module name
    subtitle: PropTypes.string, // const page title
    currUser: PropTypes.string, // var current username {currUser}*
    logoutButton: PropTypes.bool, // render logout button?
    root: PropTypes.bool,
};