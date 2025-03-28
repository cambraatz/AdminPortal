/*import React, { useEffect, useState } from 'react';
import { translateDate } from '../Scripts/helperFunctions';*/
import { useState, useEffect } from 'react';
import PropTypes from 'prop-types';
import UserWidget from './UserWidget';

//const Header = (props) => {
const Header = (props) => {
    //const company = props.company && props.company !== "" ? props.company.split(' ') : [""];
    const [company, setCompany] = useState(["Transportation", "Computer Support", "LLC."]);
    const [user, setUser] = useState(props.currUser);

    useEffect(() => {
        if (props.company && props.company != "") {
            setCompany(props.company.split(' '));
        }
        setUser(props.currUser);
    }, [props.company, props.currUser]);

    /*
    const deliveryHeader = () => {
        let header = (
            <>
                <div id="title_div">
                    {company.map((word, index) => (<h4 className="TCS_title" key={index}>{word}</h4>))}
                </div>
                <div className="sticky_header" onClick={props.onClick}>
                    <div id="main_title">
                        <h1>Driver Manifest</h1>
                        <h2 id="title_dash">-</h2>
                        <h2>{props.title}</h2>
                    </div>
                    <UserWidget 
                        company={company}
                        driver={user} 
                        status={props.status} 
                        header={props.header} 
                        MFSTDATE={props.MFSTDATE} 
                        POWERUNIT={props.POWERUNIT} 
                        toggle={props.toggle}/>
                </div>
            </>
        );
        
        return header;
    }*/

    const deliveryCondition = () => {
        let condition = (
            <div id="widgetHeader">
                <h4 className="prompt">{props.alt}</h4>
            </div>
        );

        if (props.header === "Manifest" || props.header === "Admin"){
            condition = null;
        }
        else if (props.header === "Full"){
            condition = (
                <>
                    <div id="SPM_Row">
                        <div className="SPM_Col">
                            <h5>Stop No:</h5>
                            <h5 className="weak">{props.STOP}</h5>
                        </div>
                        <div className="SPM_Col">
                            <h5>Pro No:</h5>
                            <h5 className="weak">{props.PRONUMBER}</h5>
                        </div>
                        <div className="SPM_Col">
                            <h5>Manifest Key:</h5>
                            <h5 className="weak">{props.MFSTKEY}</h5>
                        </div>
                    </div>                
                </>
            )
        }
        return condition;
    };
    

    return(
        <>
        <header id="Header">
            <div id="buffer"></div>
            
            <div id="title_div">
                {company.map((word, index) => (
                    <h4 className="TCS_title" key={index}>{word}</h4>
                ))}
            </div>
            <div className="sticky_header" onClick={props.onClick}>
                <div id="main_title">
                    <h1>Driver Manifest</h1>
                    <h2 id="title_dash">-</h2>
                    <h2>{props.title}</h2>
                </div>
                <UserWidget 
                    company={company}
                    driver={user} 
                    status={props.status} 
                    header={props.header} 
                    MFSTDATE={props.MFSTDATE} 
                    POWERUNIT={props.POWERUNIT} 
                    toggle={props.toggle}/>
                    
            </div>
        </header>
        {deliveryCondition()}
        </>
    )
};

export default Header;

Header.propTypes = {
    alt: PropTypes.string,
    header: PropTypes.string,
    STOP: PropTypes.string,
    PRONUMBER: PropTypes.string,
    MFSTKEY: PropTypes.string,
    MFSTDATE: PropTypes.string,
    POWERUNIT: PropTypes.string,
    company: PropTypes.string,
    title: PropTypes.string,
    status: PropTypes.string,
    toggle: PropTypes.string,
    currUser: PropTypes.string,
    onClick: PropTypes.func,
    setPopup: PropTypes.func,
};