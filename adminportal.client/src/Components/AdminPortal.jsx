/*/////////////////////////////////////////////////////////////////////
 
Author: Cameron Braatz
Date: 11/15/2023
Update Date: 4/17/2025

*//////////////////////////////////////////////////////////////////////

import { useState, useEffect } from 'react';
import { useLocation } from "react-router-dom";
import Header from './Header/Header';
import Popup from './Popup/Popup';
import Footer from './Footer/Footer';
import MenuWindow from './MenuWindow/MenuWIndow';
import { //API_URL,
    showFailFlag,
    FAIL_WAIT, SUCCESS_WAIT } from '../Scripts/helperFunctions';
import { Logout } from '../Scripts/Logout';
import LoadingSpinner from './LoadingSpinner';

const API_URL = import.meta.env.VITE_API_URL;

/*/////////////////////////////////////////////////////////////////////
 
AdminPortal() - Administrative menu page and functionality

The following React functional component structures and renders the 
administrative menu user interface accessed only from successful 
login using admin credentials. The page allows users to add, edit and 
remove users from the driver database. Additionally, the page allows 
the admin user to change the name of the company to be dynamically 
rendered across the application.

*//////////////////////////////////////////////////////////////////////

const AdminPortal = () => {
    /* Page rendering, navigation and state initialization... */

    // location state...
    const location = useLocation();

    // loading state to prevent early renders...
    const [loading,setLoading] = useState(true);

    // header toggle state...
    //const [header,setHeader] = useState(location.state ? location.state.header : "open");

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
            validateUser();
    }, []);

    /* Page rendering helper functions... */

    const returnOnFail = (message) => {
        console.error(`Error: ${message}`);
        setPopup("Fail");
        setTimeout(() => {
            Logout();
        },FAIL_WAIT);
    }

    async function validateUser(){
        setLoading(true);

        // direct bypass to powerunit validation...
        //const response = await fetch(API_URL + "api/Admin/ValidateUser", {
        const response = await fetch(API_URL + "v1/sessions/me", {
            method: "GET",
            headers: {
                'Content-Type': 'application/json; charset=UTF-8'
            },
            credentials: 'include'
        });

        /*if (response.status === 401 || response.status === 403) {
            returnOnFail("Status 401/403 returned, logging out.");
        }*/

        //const data = await response.json();
        //console.log(data);

        if (response.ok) {
            const data = await response.json();

            sessionStorage.setItem("companies_map", data.companies);
            const company_map = JSON.parse(data.companies);
            console.log("companies_map: ", company_map);

            sessionStorage.setItem("modules_map", data.modules);
            const module_map = JSON.parse(data.modules);
            console.log("modules_map: ", module_map);

            setCompanies(company_map);
            setModules(module_map),

            setCompany(company_map[data.user.ActiveCompany]);
            setActiveCompany(company_map[data.user.ActiveCompany].split(' '));

            setCurrUser(data.user.Username);
            setLoading(false);
        } else {
            returnOnFail("Validation error, logging out.");
        }
    }

    /*/////////////////////////////////////////////////////////////////
    [void] : clearStyling() {
        helper script to remove error styling from all standard fields
    }
    *//////////////////////////////////////////////////////////////////

    const clearStyling = () => {
        const elements = ["username","password","powerunit","company"];
        elements.forEach(element => {
            const target = document.getElementById(element);
            if(target && target.classList.contains("invalid_input")) {
                target.classList.remove("invalid_input");
            }
        })
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
            comp_field.classList.add("invalid_input");
            showFailFlag("ff_admin_cc", "Company name is required!")
            return;
        }

        //const response = await fetch(API_URL + "api/Admin/SetCompany", {
        const response = await fetch(API_URL + "v1/companies/" + company, {
            method: "PUT",
            headers: {
                'Content-Type': 'application/json',
            },
            credentials: 'include'
        })

        if (response.status === 401 || response.status === 403) {
            returnOnFail("Status 401/403 returned, logging out.");
        }

        const data = await response.json();
        console.log("data: ",data);

        if (response.ok) {
            // set active, company is updated dynamically...
            setActiveCompany(company.split(' '));
            setPopup("Company Success");
            
            setTimeout(() => {
                closePopup();
            },SUCCESS_WAIT);
        }
        else {
            console.error(data.message);
            setPopup("Fail");

            setTimeout(() => {
                setPopup("Change Company");
            },FAIL_WAIT);
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
        const user_field = document.getElementById("username");
        const power_field = document.getElementById("powerunit");
        
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

        //const response = await fetch(API_URL + "api/Admin/AddDriver", {
        const response = await fetch(API_URL + "v1/users", {
            body: JSON.stringify(formData),
            method: "POST",
            headers: {
                'Content-Type': 'application/json; charset=UTF-8'
            },
            credentials: 'include'
        })

        if (response.status === 401 || response.status === 403) {
            returnOnFail("Status 401/403 returned, logging out.");
        }

        const data = await response.json();
        console.log(data);

        if (response.ok) {
            setPopup("Add Success");
            setTimeout(() => {
                closePopup();
            },SUCCESS_WAIT);
        } else {
            if (response.status === 409) {
                setPopup("Admin_Add Fail");
            } 
            else if (data.message.includes("Username is already in use")) {
                setPopup("Admin_Add Fail");
            }
            else {
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

        //const response = await fetch(API_URL + "api/Admin/PullDriver", {
        const response = await fetch(API_URL + "v1/users/" + credentials.USERNAME, {
            method: "GET",
            headers: {
                'Content-Type': 'application/json; charset=UTF-8'
            },
            credentials: 'include'
        })

        if (response.status === 401 || response.status === 403) {
            returnOnFail("Status 401/403 returned, logging out.");
            return;
        }

        // catch failed request and prevent behavior...
        if (response.ok) {
            const user = await response.json()
            console.log(user);

            //const user = data;
            setPreviousUser(credentials.USERNAME);
            setCredentials({
                USERNAME: user.Username,
                PASSWORD: user.Password,
                POWERUNIT: user.Powerunit
            });

            const checked_mods = {};
            Object.keys(modules).forEach((url) => {
                checked_mods[url] = user.Modules.includes(url);
            });
            setCheckedModules(checked_mods);

            const checked_comps = {};
            Object.keys(companies).forEach((key) => {
                checked_comps[key] = user.Companies.includes(key);
            });
            setCheckedCompanies(checked_comps);

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

        // package driver credentials and attempt to replace...
        const body_data = {
            ...credentials,
            //PREVUSER: previousUser,
            COMPANIES: Object.keys(checkedCompanies).filter(key => checkedCompanies[key]),
            MODULES: Object.keys(checkedModules).filter(key => checkedModules[key])
        }
        //const response = await fetch(API_URL + "api/Admin/ReplaceDriver", {
        const response = await fetch (API_URL + "v1/users/" + previousUser, {
            body: JSON.stringify(body_data),
            method: "PUT",
            headers: {
                'Content-Type': 'application/json; charset=UTF-8'
            },
            credentials: 'include'
        })

        if (response.status === 401 || response.status === 403) {
            returnOnFail("Status 401/403 returned, logging out.");
        }

        //const data = await response.json();
        //console.log(data);

        if (response.ok) {
            setPreviousUser("add new");
            setPopup("Update Success");
        }
        else {
            console.error(response.message);
            setPopup("UNPUConflictFail");
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
        //const response = await fetch(API_URL + "api/Admin/DeleteDriver?USERNAME=" + credentials.USERNAME, {
        const response = await fetch(API_URL + "v1/users/" + credentials.USERNAME, {
            method: "DELETE",
            credentials: 'include'
        })

        if (response.status === 401 || response.status === 403) {
            returnOnFail("Status 401/403 returned, logging out.");
        }

        //const data = await response.json();
        //console.log(data);

        // init popup duration and conditionally render popup response...
        if (response.ok) {
            setPopup("Delete Success");
            setTimeout(() => {
                closePopup();
            },SUCCESS_WAIT)
        } 
        else {
            const data = await response.json();
            console.error(data.message);
            if (data.duplicate) {
                setPopup("ActiveUserFail");
            }
            else { 
                setPopup("Fail");
            }
            setTimeout(() => {
                closePopup();
            },FAIL_WAIT)
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
        console.log(e.target);
        setCredentials({
            USERNAME: "",
            PASSWORD: "",
            POWERUNIT: ""
        })

        if (companies.length === 0 || modules.length === 0) {
            //collectOptions();
        }

        let message;
        // handle main admin button popup generation
        switch(e.target.innerText){
            case "Add New User":
                //setPopup("Add User");
                message = "Add User";
                setPreviousUser("add new");
                break;
            case "Change/Remove User":
                //setPopup("Find User");
                message = "Find User";
                break;
            case "Edit Company Name":
                //setPopup("Change Company");
                message = "Change Company";
                break;
            default:
                break;
        }
        openPopup(message);
    }

    /*/////////////////////////////////////////////////////////////////
    [void] : openPopup() {
        make popup window visible on screen
        enable on click behavior
    }

    [void] : closePopup() {
        self explanatory closing of "popupLoginWindow"
        setStatus("") and setMessage(null) - reset state data
    }
    *//////////////////////////////////////////////////////////////////

    const [popupVisible, setVisible] = useState(false);
    const openPopup = (message) => {
        setPopup(message);
        setVisible(true);
        console.log(`popupVisible: ${popupVisible}`);
    };

    const closePopup = () => {
        setVisible(false);

        clearStyling();
        setCheckedCompanies({});
        setCheckedModules({});
    };

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

    const header = location.state ? location.state.header : "open";
    const collapsed = header === "open" ? true : false;

    // render template...
    return(
        <div id="webpage">
            {loading ? (
                <>
                    <LoadingSpinner />
                </>
            ) : (
                <>
                    <Header
                        company={activeCompany ? activeCompany : "Transportation Computer Support, LLC."}
                        title="Admin Portal"
                        subtitle="Add/Update Company Records"
                        currUser={currUser}
                        logoutButton={true}
                        prompt="What would you like to do?"
                        collapsed={collapsed}
                    />
                    <MenuWindow 
                        prompt="What would you like to do?"
                        header="null"
                        pressButton={pressButton}
                    />
                    {popupVisible && (
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
                            closePopup={closePopup}
                            isVisible={popupVisible}
                        />
                    )}
                    <Footer id="footer"/>
                </>
            )}
        </div>
    )
};

export default AdminPortal;