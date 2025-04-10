/*/////////////////////////////////////////////////////////////////////
 
Author: Cameron Braatz
Date: 11/15/2023
Update Date: 1/8/2025

*//////////////////////////////////////////////////////////////////////

import { useState, useEffect } from 'react';
import { useLocation } from "react-router-dom";
import Header from './Header';
import Popup from './Popup';
import Footer from './Footer';
import { API_URL,
    showFailFlag,
    FAIL_WAIT, SUCCESS_WAIT } from '../Scripts/helperFunctions';
import { Logout } from '../Scripts/Logout';

/*/////////////////////////////////////////////////////////////////////
 
AdminPortal() - Administrative menu page and functionality

The following React functional component structures and renders the 
administrative menu user interface accessed only from successful 
login using admin credentials. The page allows users to add, edit and 
remove users from the driver database. Additionally, the page allows 
the admin user to change the name of the company to be dynamically 
rendered across the application.

///////////////////////////////////////////////////////////////////////

BASIC STRUCTURE:
// initialize rendered page...
    initialize date, navigation and states
    verify company name for rendering
    verify location.state to catch improper navigation

    useEffect() => 
        check delivery validity onLoad and after message state change

// page rendering helper functions...
    renderCompany() => 
        retrieve company name from database when not in memory
    collapseHeader() => 
        open/close collapsible header
    openPopup() => 
        open popup for delivery confirmation
    closePopup() => 
        close popup for delivery confirmation
    clearStyling() =>
        remove error styling from all input fields (if present)

// state management functions...
    handleUpdate() => 
        handle input field changes

// API requests + functions...
    getCompany() =>
        retrieve the active company name from DB
    updateCompany() =>
        update active company name with provided user input

    addDriver() =>
        adds a new driver and conveys errors that occur
    pullDriver() => 
        retrieve user credentials for a given username (edit/remove)
    updateDriver() =>
        collect credentials/prev user (scraped) and update in DB
    removeDriver() =>
        remove record from DB that matches credentials.USERNAME
    cancelDriver() => 
        handle cancel, clear credentials and close popup

    pressButton() =>
        handle button clicks to render respective popup selections

    [inactive] dumpUsers() =>
        debug function to dump all active users to console.log()

// render template + helpers...
    package popup helper functions
    return render template

*//////////////////////////////////////////////////////////////////////

const AdminPortal = () => {
    /* Page rendering, navigation and state initialization... */

    // location state...
    const location = useLocation();

    // loading state to prevent early renders...
    const [loading,setLoading] = useState(true);

    // header toggle state...
    const [header,setHeader] = useState(location.state ? location.state.header : "open");

    // driver credentials state...
    const [credentials, setCredentials] = useState({
        USERNAME: "",
        PASSWORD: "",
        POWERUNIT: ""
    });

    // current/past user credentials...
    const [currUser, setCurrUser] = useState("admin");
    const [previousUser, setPreviousUser] = useState("");

    // "Edit User", "Find User", "Change Company"...
    const [popup, setPopup] = useState("Edit User");

    // company (forms) and activeCompany (rendered)...
    const [company, setCompany] = useState();
    const [activeCompany, setActiveCompany] = useState();

    // ensure company, token and navigation validity onLoad...
    useEffect(() => {  
        //console.log("useEffect, waiting for cookies to set...") 
        //setTimeout(() => {
            validateUser();
        //}, 7500);
    }, []);

    /* Page rendering helper functions... */

    async function validateUser(){
        setLoading(true);

        // direct bypass to powerunit validation...
        const response = await fetch(API_URL + "api/Admin/ValidateUser", {
            //body: JSON.stringify(username),
            method: "POST",
            headers: {
                'Content-Type': 'application/json; charset=UTF-8'
            },
            credentials: 'include'
        });

        const data = await response.json();
        console.log(data);

        if (data.success) {
            let mappings = {};
            const mapping_response = await fetch(`${API_URL}api/Admin/FetchMappings`, {
                method: "POST",
                headers: {
                    'Content-Type': 'application/json; charset=UTF-8'
                },
                credentials: 'include'
            });

            if(mapping_response.ok) {
                mappings = await mapping_response.json();
                sessionStorage.setItem("companies_map", mappings.companies);
                console.log("companies_map: ", mappings.companies);
                
                sessionStorage.setItem("modules_map", mappings.modules);
                console.log("modules_map: ", mappings.modules);              
            } else {
                console.error("Error setting mapping cookies.")
            };

            const COMPANIES = JSON.parse(mappings.companies);
            setCompanies(COMPANIES);
            //console.log(COMPANIES);

            const MODULES = JSON.parse(mappings.modules);
            setModules(MODULES);
            //console.log(MODULES);
            
            setCompany(COMPANIES[data.user.ActiveCompany]);
            setActiveCompany(COMPANIES[data.user.ActiveCompany]);
    
            setCurrUser(data.user.Username);
            //console.log(`data.user.Username: ${data.user.Username}`);
        } else {
            console.error("ERROR: Validation error, logging out.");
            // Create the button element
            const logoutButton = document.createElement('button');
            logoutButton.innerText = 'Logout';
            logoutButton.style.cssText = 'background-color: red; color: white; padding: 10px; margin-top: 10px;';

            // Attach a click event to trigger Logout function
            logoutButton.onclick = function() {
                Logout();
            };

            // Append the button to the body or a specific container
            document.body.appendChild(logoutButton);

            //setTimeout(() => {
            //    Logout();
            //},15000);
        }
        setLoading(false);
    }

    /*/////////////////////////////////////////////////////////////////
    // initialize and manage collapsible header behavior...
    [void] : collapseHeader(event) {
        if (e.target.id === "collapseToggle" or "toggle_dots"):
            open/close header - do opposite of current "header" state
    }
    *//////////////////////////////////////////////////////////////////

    const collapseHeader = (e) => {
        if (e.target.id === "collapseToggle" || e.target.id === "toggle_dots") {
            setHeader(prev => (prev === "open" ? "close" : "open"));
        }
    }

    /*/////////////////////////////////////////////////////////////////
    [void] : openPopup() {
        make popup window visible on screen
        enable on click behavior
    }
    *//////////////////////////////////////////////////////////////////

    const openPopup = () => {
        document.getElementById("popupAddWindow").style.visibility = "visible";
        document.getElementById("popupAddWindow").style.opacity = 1;
        document.getElementById("popupAddWindow").style.pointerEvents = "auto";  
    };

    /*/////////////////////////////////////////////////////////////////
    [void] : closePopup() {
        self explanatory closing of "popupLoginWindow"
        setStatus("") and setMessage(null) - reset state data
    }
    *//////////////////////////////////////////////////////////////////

    const closePopup = () => {
        document.getElementById("popupAddWindow").style.visibility = "hidden";
        document.getElementById("popupAddWindow").style.opacity = 0;
        document.getElementById("popupAddWindow").style.pointerEvents = "none";
        clearStyling();
        setCheckedCompanies({});
        setCheckedModules({});
    };

    /*async function Logout() {
        clearMemory();
        const response = await fetch(`${API_URL}api/Admin/Logout`, {
            method: "POST",
            headers: {
                'Content-Type': 'application/json; charset=UTF-8'
            },
        })
        if (response.ok) {
            console.log("Logout Successful!");
            setTimeout(() => {
                window.location.href = `https://www.login.tcsservices.com`;
            },1000)
        } else {
            console.alert("Cookie removal failed, Logout failure.")
        }
    }*/

    /*/////////////////////////////////////////////////////////////////
    [void] : clearStyling() {
        helper script to remove error styling from all standard fields
    }
    *//////////////////////////////////////////////////////////////////

    const clearStyling = () => {
        if (document.getElementById("username") && document.getElementById("username").classList.contains("invalid_input")) {
            document.getElementById("username").classList.remove("invalid_input");
        }
        if (document.getElementById("password") && document.getElementById("password").classList.contains("invalid_input")) {
            document.getElementById("password").classList.remove("invalid_input");
        }
        if (document.getElementById("powerunit") && document.getElementById("powerunit").classList.contains("invalid_input")) {
            document.getElementById("powerunit").classList.remove("invalid_input");
        }
        if (document.getElementById("company") && document.getElementById("company").classList.contains("invalid_input")) {
            document.getElementById("company").classList.remove("invalid_input");
        }
    }

    /* State management functions... */

    /*/////////////////////////////////////////////////////////////////
    // handle changes to admin input fields...
    [void] : handleUpdate() {
        clear error styling on input fields
        handle input field changes
    }
    *//////////////////////////////////////////////////////////////////

    const handleUpdate = (e) => {
        clearStyling();
        let val = e.target.value;
        switch(e.target.id){
            case 'username':
                setCredentials({
                    ...credentials,
                    USERNAME: val
                });
                break;
            case 'password':
                setCredentials({
                    ...credentials,
                    PASSWORD: val
                });
                break;
            case 'powerunit':
                setCredentials({
                    ...credentials,
                    POWERUNIT: val
                });
                break;
            case "company":
                setCompany(val);
                break;
            default:
                break;
        }
    }

    /* API Calls and Functionality... */

    /*/////////////////////////////////////////////////////////////////
    // update active company name with provided user input...
    [void] : updateCompany() {
        trigger input error styling when invalid + render flag
        validate token credentials
        update the company name in DB

        if (!success):
            render error popup + reset popup
        else:
            set company state
            render success popup + close popup
    }
    *//////////////////////////////////////////////////////////////////

    async function updateCompany() {
        const comp_field = document.getElementById("company");
        if (comp_field.value === "" || comp_field.value === "Your Company Here") {
            document.getElementById("company").classList.add("invalid_input");
            showFailFlag("ff_admin_cc", "Company name is required!")
            return;
        }

        const response = await fetch(API_URL + "api/Admin/SetCompany", {
            body: JSON.stringify(company),
            method: "PUT",
            headers: {
                'Content-Type': 'application/json',
            },
            credentials: 'include'
        })

        const data = await response.json();
        //console.log("data: ",data);

        if (!data.success) {
            console.error("company name value mismatch");
            setPopup("Fail");

            setTimeout(() => {
                setPopup("Change Company");
            },FAIL_WAIT);
        }
        else {
            // set active, company is updated dynamically...
            setActiveCompany(data.company);
            setPopup("Company Success");
            
            setTimeout(() => {
                closePopup();
            },SUCCESS_WAIT);
        }
    }

    /*/////////////////////////////////////////////////////////////////
    // adds a new driver, convey any errors in response...
    [void] : addDriver() {
        define input field alert status
        apply error styling and render flag when present

        package driver credentials into form for request body
        add driver credentials to database
        if (success):
            render success popup notification
        else:
            if (pk violation):
                render duplicate driver fail popup
            else:
                render add driver fail popup
    }
    *//////////////////////////////////////////////////////////////////

    async function addDriver() {
        const user_field = document.getElementById("username")
        const power_field = document.getElementById("powerunit")
        
        let code = -1;
        const messages = [
            "Username is required!", 
            "Powerunit is required!", 
            "Username and Powerunit are required!"
        ]
        const alerts = {
            0: "ff_admin_au_un", // Username is required!...
            1: "ff_admin_au_pu", // Powerunit is required!...
            2: "ff_admin_au_up" // Username and Powerunit are required!...
        }

        if (user_field.value === "" || user_field.value == null){
            user_field.classList.add("invalid_input");
            code += 1;

        } 
        
        if (power_field.value === "" || power_field.value == null){
            power_field.classList.add("invalid_input");
            code += 2;
        }

        if (code >= 0) {
            showFailFlag(alerts[code], messages[code])
            return;
        }

        let COMPANIES = JSON.parse(sessionStorage.getItem("companies_mapping") || "{}");

        const formData = {
            Username: credentials.USERNAME,
            Permissions: null,
            Powerunit: credentials.POWERUNIT,
            ActiveCompany: Object.keys(COMPANIES).find(key => COMPANIES[key] === activeCompany),
            Companies: Object.keys(checkedCompanies).filter(key => checkedCompanies[key]),
            Modules: Object.keys(checkedModules).filter(key => checkedModules[key])
        };

        //console.log("checkedCompanies: ", checkedCompanies);
        //console.log("checkedModules: ", checkedModules);
        //console.log("formData: ", formData);

        const response = await fetch(API_URL + "api/Admin/AddDriver", {
            body: JSON.stringify(formData),
            method: "PUT",
            headers: {
                'Content-Type': 'application/json; charset=UTF-8'
            },
            credentials: 'include'
        })

        const data = await response.json();
        //console.log(data);
        if (data.success) {
            setPopup("Add Success");
            setTimeout(() => {
                closePopup();
            },SUCCESS_WAIT);
        } else {
            if (data.error.includes("Violation of PRIMARY KEY")) {
                setPopup("Admin_Add Fail");
            } else {
                setPopup("Fail");
            }
            setTimeout(() => {
                closePopup();
            },FAIL_WAIT);
        }
        
    }

    /*/////////////////////////////////////////////////////////////////
    // adds a new driver, convey any errors in response...
    [void] : pullDriver() {
        define input field alert status
        apply error styling and render flag when present

        package driver credentials into form for request body
        add driver credentials to database
        if (success):
            render success popup notification
        else:
            if (pk violation):
                render duplicate driver fail popup
            else:
                render add driver fail popup
    }
    *//////////////////////////////////////////////////////////////////

    async function pullDriver() {
        //console.log("companies: ", companies);
        //console.log("modules: ", modules);

        if (document.getElementById("username").value === "") {
            document.getElementById("username").classList.add("invalid_input");

            showFailFlag("ff_admin_fu","Username is required!");
            return;
        }

        const body_data ={
            USERNAME: credentials.USERNAME,
            PASSWORD: null,
            POWERUNIT: null
        }

        const response = await fetch(API_URL + "api/Admin/PullDriver", {
            body: JSON.stringify(body_data),
            method: "POST",
            headers: {
                'Content-Type': 'application/json; charset=UTF-8'
            },
            credentials: 'include'
        })

        const data = await response.json()
        //console.log(data);

        // catch failed request and prevent behavior...
        if (data.success) {
            const user = data.user;
            setPreviousUser(credentials.USERNAME);
            setCredentials({
                USERNAME: user.USERNAME,
                PASSWORD: user.PASSWORD,
                POWERUNIT: user.POWERUNIT
            });

            console.log(`modules: ${modules}`);
            console.log(`companies: ${companies}`);
            console.log(`user.MODULES: ${user.MODULES}`);

            const check_mods = {};
            Object.entries(modules).forEach(([url,name]) => {
                console.log(`check_mods, url: ${url}, name: ${name}`);
                check_mods[url] = user.MODULES.includes(url);
            });
            setCheckedModules(check_mods);
            console.log("check_mods: ", check_mods);

            const check_comps = {};
            Object.entries(companies).forEach(([key,name]) => {
                console.log(`check_mods, key: ${key}, name: ${name}`);
                check_comps[key] = user.COMPANIES.includes(key);
            });
            setCheckedCompanies(check_comps);
            console.log("check_comps: ", check_comps);


            setPopup("Edit User");
        }
        else {
            document.getElementById("username").className = "invalid_input";
            document.getElementById("ff_admin_fu").classList.add("visible");
            setTimeout(() => {
                document.getElementById("ff_admin_fu").classList.remove("visible");
            },1500)
        }
    }

    /*/////////////////////////////////////////////////////////////////
    // collect credentials/prev user (scraped) and update in DB...
    [void] : updateDriver() {
        define input field alert status
        apply error styling and render flag when present
        validate token and refresh as needed

        package driver credentials into form for request body
        add driver credentials to database
        if (success):
            render success popup notification
        else:
            render failure popup notification
    }
    *//////////////////////////////////////////////////////////////////

    async function updateDriver() {
        const user_field = document.getElementById("username")
        const power_field = document.getElementById("powerunit")
        
        // map empty field cases to messages...
        let code = -1;
        const messages = [
            "Username is required!", 
            "Powerunit is required!", 
            "Username and Powerunit are required!"
        ]
        const alerts = {
            0: "ff_admin_eu_un", // Username is required!...
            1: "ff_admin_eu_pu", // Powerunit is required!...
            2: "ff_admin_eu_up" // Username and Powerunit are required!...
        }
        // flag empty username...
        if (user_field.value === "" || user_field.value == null){
            user_field.classList.add("invalid_input");
            code += 1;
        } 
        // flag empty powerunit...
        if (power_field.value === "" || power_field.value == null){
            power_field.classList.add("invalid_input");
            code += 2;
        }

        // catch and alert user to incomplete fields...
        if (code >= 0) {
            showFailFlag(alerts[code],messages[code]);
            return;
        }

        // request token from memory, refresh as needed...
        /*const token = await requestAccess(credentials.USERNAME);
        
        // handle invalid token on login...
        if (!token) {
            //navigate('/');
            Logout();
            return;
        }*/

        // package driver credentials and attempt to replace...
        const body_data = {
            ...credentials,
            PREVUSER: previousUser,
            COMPANIES: Object.keys(checkedCompanies).filter(key => checkedCompanies[key]),
            MODULES: Object.keys(checkedModules).filter(key => checkedModules[key])
        }
        const response = await fetch(API_URL + "api/Admin/ReplaceDriver", {
            body: JSON.stringify(body_data),
            method: "PUT",
            headers: {
                //"Authorization": `Bearer ${token}`,
                'Content-Type': 'application/json; charset=UTF-8'
            },
            credentials: 'include'
        })

        const data = await response.json();
        //console.log(data);

        if (data.success) {
            setPreviousUser("add new");
            setPopup("Update Success");
        }
        else {
            console.trace("update driver failed");
            setPopup("Fail");
        }

        setTimeout(() => {
            closePopup();
        },1000)
    }

    /*/////////////////////////////////////////////////////////////////
    // remove record from DB that matches credentials.USERNAME...
    [void] : removeDriver() {
        validate token and refresh as needed

        remove driver from database
        if (success):
            render success popup notification
        else:
            render failure popup notification
    }
    *//////////////////////////////////////////////////////////////////

    async function removeDriver() {
        const response = await fetch(API_URL + "api/Admin/DeleteDriver?USERNAME=" + credentials.USERNAME, {
            method: "DELETE",
            credentials: 'include'
        })

        const data = await response.json();
        //console.log(data);

        if (data.success) {
            setPopup("Delete Success");
            setTimeout(() => {
                closePopup();
            },1000)
        }
        else {
            console.error("delete driver failed");
            setPopup("Fail");
            setTimeout(() => {
                closePopup();
            },2000)
        }
    }

    /*/////////////////////////////////////////////////////////////////
    // handle cancel, clear credentials and close popup...
    [void] : cancelDriver() {
        reset driver credentials to null
        close pop up
    }
    *//////////////////////////////////////////////////////////////////

    async function cancelDriver() {
        setCredentials({
            USERNAME: "",
            PASSWORD: "",
            POWERUNIT: ""
        })
        closePopup();
    }

    /*/////////////////////////////////////////////////////////////////
    // handle unique behavior of collection of admin buttons...
    [void] : pressButton() {
        reset driver credentials to null
        handle admin button clicks to set popup state
        opens popup
    }
    *//////////////////////////////////////////////////////////////////

    async function pressButton(e) {
        setCredentials({
            USERNAME: "",
            PASSWORD: "",
            POWERUNIT: ""
        })

        if (companies.length === 0 || modules.length === 0) {
            //collectOptions();
        }

        // handle main admin button popup generation
        switch(e.target.innerText){
            case "Add New User":
                setPopup("Add User");
                setPreviousUser("add new");
                break;
            case "Change/Remove User":
                setPopup("Find User");
                break;
            case "Edit Company Name":
                setPopup("Change Company");
                break;
            default:
                break;
        }
        openPopup();
    }

    const [companies,setCompanies] = useState([]);
    const [modules,setModules] = useState([]);

    const [checkedCompanies,setCheckedCompanies] = useState({});
    const [checkedModules,setCheckedModules] = useState({});

    const handleCheckboxChange = (type,name) => {
        if (type === "company") {
            setCheckedCompanies({
                ...checkedCompanies,
                [name]: !checkedCompanies[name]
        });
        } else if (type === "module") {
            setCheckedModules({
                ...checkedModules,
                [name]: !checkedModules[name]
        });
        }
    }

    // package helper functions to organize popup functions...
    const functions = {
        "addDriver": addDriver,
        "pullDriver": pullDriver,
        "updateDriver": updateDriver,
        "removeDriver": removeDriver,
        "cancelDriver": cancelDriver,
        "updateCompany": updateCompany,
        "checkboxChange": handleCheckboxChange
    };

    // render template...
    return(
        <div id="webpage">
            {loading ? (
                <div className="loading-container">
                    <p>Loading...</p>
                </div>
            ) : (
                <>
                    <Header
                        company={activeCompany}
                        title="Admin Portal"
                        alt="What would you like to do?"
                        status="admin"
                        currUser={currUser}
                        MFSTDATE={null} 
                        POWERUNIT={null}
                        STOP={null}
                        PRONUMBER={null}
                        MFSTKEY={null}
                        toggle={header}
                        onClick={collapseHeader}
                        setPopup={setPopup}
                    />
                    <div id="admin_div">
                        <button type="button" onClick={pressButton}>Add New User</button>
                        <button type="button" onClick={pressButton}>Change/Remove User</button>
                        <button type="button" onClick={pressButton}>Edit Company Name</button>
                    </div>
                    <div id="popupAddWindow" className="overlay">
                        <div className="popupLogin">
                            <div id="popupAddExit" className="content">
                                <h1 id="close_add" className="popupLoginWindow" onClick={closePopup}>&times;</h1>
                            </div>
                            <Popup 
                                message={popup}
                                powerunit={null}
                                handleUpdate={handleUpdate}
                                credentials={credentials}
                                company={company}
                                companies={companies}
                                modules={modules}
                                checkedCompanies={checkedCompanies}
                                checkedModules={checkedModules}
                                functions={functions}
                            />
                        </div>
                    </div>
                    <Footer id="footer"/>
                </>
            )}
        </div>
    )
};

export default AdminPortal;