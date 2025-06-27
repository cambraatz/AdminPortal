import { useState } from 'react';
import PropTypes from 'prop-types';
import UserWidget from '../UserWidget/UserWidget';
import toggleDots from '../../assets/Toggle_Dots.svg';
import "./Header.css";

const Header = (props) => {
    const company = props.company;
    const [collapsed, setCollapsed] = useState(props.collapsed);
    
    const collapseHeader = (e) => {
        if (e.target.id === "collapseToggle" || e.target.id === "toggle_dots") {
            if (collapsed) {
                document.getElementById("main_title").style.display = "flex";
                document.getElementById("title_div").style.display = "flex";
                document.getElementById("buffer").style.height = "20px";
            } else {
                document.getElementById("main_title").style.display = "none";
                document.getElementById("title_div").style.display = "none";
                document.getElementById("buffer").style.height = "10px";
            }
            
            setCollapsed(!collapsed);
        }
    }

    return(
        <header id="Header">
            <div id="buffer"></div>
            
            <div id="title_div">
                {company.map((word, index) => (
                    <h4 className="TCS_title" key={index}>{word}</h4>
                ))}
            </div>
            <div className="sticky_header">
                <div id="main_title">
                    <h1>{props.title}</h1>
                    <h2 id="title_dash">-</h2>
                    <h2>{props.subtitle}</h2>
                </div>
                <div id="collapseToggle" onClick={collapseHeader}>
                    <img id="toggle_dots" src={toggleDots} alt="toggle dots" />
                </div>
                <UserWidget 
                    currUser={props.currUser} 
                    logoutButton={props.logoutButton}
                />
            </div>
        </header>
    )
};

export default Header;

// *state variable actively managed in parent...
Header.propTypes = {
    company: PropTypes.string, // var company name {activeCompany}*
    title: PropTypes.string, // const module name
    subtitle: PropTypes.string, // const page title
    currUser: PropTypes.string, // var current username {currUser}*
    logoutButton: PropTypes.bool, // render logout button?
    prompt: PropTypes.string, // const string page prompt
    collapsed: PropTypes.bool, // header render state

    //header: PropTypes.string, // inactive in admin page
    //STOP: PropTypes.string, // inactive in admin page
    //PRONUMBER: PropTypes.string, // inactive in admin page
    //MFSTKEY: PropTypes.string, // inactive in admin page
    //MFSTDATE: PropTypes.string, // inactive in admin page
    //POWERUNIT: PropTypes.string, // inactive in admin page
    //setPopup: PropTypes.func, // inactive in admin page
};