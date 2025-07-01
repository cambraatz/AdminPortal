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
import { 
    FAIL_WAIT, 
    SUCCESS_WAIT 
} from '../Scripts/helperFunctions';
import { Logout } from '../Scripts/apiCalls';
import LoadingSpinner from './LoadingSpinner';

import { updateUserInDB, removeUserFromDB, addUserToDB, fetchUserFromDB } from '../utils/api/users';
import { updateCompanyInDB } from '../utils/api/companies';
import { validateSession } from '../utils/api/sessions';

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
    const location = useLocation();
    const header = location.state ? location.state.header : "open";
    const collapsed = header === "open" ? true : false;

    const [loading,setLoading] = useState(true);
    const [credentials, setCredentials] = useState({
        USERNAME: "",
        PASSWORD: "",
        POWERUNIT: ""
    });

    // current/past user credentials...
    const [currUser, setCurrUser] = useState("admin");
    const [previousUser, setPreviousUser] = useState("");

    // company (forms) and activeCompany (rendered)...
    const [company, setCompany] = useState();
    const [activeCompany, setActiveCompany] = useState();

    const [inputErrors, setInputErrors] = useState({ 
        company: "",
        username: "",
        powerunit: ""
     });

    /* Page rendering helper functions... */
    const DEFAULT_POPUP = "users_update"
    const [popup, setPopup] = useState(DEFAULT_POPUP);
    const [popupVisible, setVisible] = useState(false);

    // handle opening popup state...
    const openPopup = (type) => {
        setPopup(type);
        setVisible(true);
    };

    // handle closing popup state...
    const closePopup = () => {
        setVisible(false);
        setPopup(DEFAULT_POPUP);
        clearStateStyling();
        setCheckedCompanies({});
        setCheckedModules({});
    };

    // set success prompt popup...
    const successPopup = (popupType) => {
        setPopup(popupType);
        setTimeout(() => {
            closePopup();
        },SUCCESS_WAIT)
    }

    // set failed prompt popup...
    const failedPopup = (popupType) => {
        setPopup(popupType);
        setTimeout(() => {
            closePopup();
        },FAIL_WAIT)
    }

    // set failed prompt AND send back to login...
    const returnOnFail = (popupType="fail") => {
        setPopup(popupType);
        setTimeout(() => {
            Logout();
        },FAIL_WAIT);
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

    // ensure company, token and navigation validity onLoad...
    useEffect(() => {  
            validateUser();
    }, []);

    async function validateUser(){
        setLoading(true);

        const response = await validateSession(API_URL);
        if (response.ok) {
            const data = await response.json();

            sessionStorage.setItem("companies_map", data.companies);
            const company_map = JSON.parse(data.companies);
            setCompanies(company_map);
            //console.log("companies_map: ", company_map);

            sessionStorage.setItem("modules_map", data.modules);
            const module_map = JSON.parse(data.modules);
            setModules(module_map);
            //console.log("modules_map: ", module_map);

            setCompany(company_map[data.user.ActiveCompany]);
            setActiveCompany(company_map[data.user.ActiveCompany].split(' '));

            setCurrUser(data.user.Username);
            setLoading(false);
            return;
        }

        returnOnFail("sessions_validation_fail");
    }

    /*/////////////////////////////////////////////////////////////////
    [void] : clearStyling() {
        helper script to remove error styling from all standard fields
    }
    *//////////////////////////////////////////////////////////////////

    /*const clearStyling = () => {
        const elements = ["username","password","powerunit","company"];
        elements.forEach(element => {
            const target = document.getElementById(element);
            if(target && target.classList.contains("invalid_input")) {
                target.classList.remove("invalid_input");
            }
        })
    }*/

    const clearStateStyling = () => {
        setInputErrors({ 
            company: "",
            username: "",
            powerunit: ""
        });
    };

    /* State management functions... */

    /*/////////////////////////////////////////////////////////////////
    // handle cancel, clear credentials and close popup...
    [void] : cancelDriver() {
        reset driver credentials to null
        close pop up
    }
    *//////////////////////////////////////////////////////////////////

    const cancelDriver = () => {
        setCredentials({
            USERNAME: "",
            PASSWORD: "",
            POWERUNIT: ""
        })
        closePopup();
    }

    /*/////////////////////////////////////////////////////////////////
    // handle changes to admin input fields...
    [void] : handleUpdate() {
        clear error styling on input fields
        handle input field changes
    }
    *//////////////////////////////////////////////////////////////////

    const handleUpdate = (e) => {
        clearStateStyling();
        let id = e.target.id;
        let val = e.target.value;
        switch(true){
            case id.startsWith("username"):
                setCredentials({
                    ...credentials,
                    USERNAME: val
                });
                break;
            case id.startsWith("password"):
                setCredentials({
                    ...credentials,
                    PASSWORD: val
                });
                break;
            case id.startsWith("powerunit"):
                setCredentials({
                    ...credentials,
                    POWERUNIT: val
                });
                break;
            case id.startsWith("company"):
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

    async function updateCompany(e) {
        /* start */
        e.preventDefault();

        let isValid = true;
        let errorMessage = "";

        if (company.trim() === "" || company.trim() === "Your Company Here") {
            errorMessage = "Company name is required!";
            isValid = false;
        }

        setInputErrors(prevErrors => ({ ...prevErrors, company: errorMessage }));

        if (!isValid) {
            console.error("Input validation error: ", errorMessage);
            //setPopup("fail");
            return;
        }
        /* end */
        /*const comp_field = document.getElementById("company");
        if (comp_field.value === "" || comp_field.value === "Your Company Here") {
            comp_field.classList.add("invalid_input");
            showFailFlag("ff_admin_cc", "Company name is required!")
            return;
        }*/

        const response = await updateCompanyInDB(company);
        if (response.ok) {
            // set active, company is updated dynamically...
            setActiveCompany(company.split(' '));
            successPopup("company_update_success");
        }
        else {
            failedPopup("fail");
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

    async function addDriver(e) {
        e.preventDefault();

        let isValid = true;
        let errorMessage = "";
        let errorCount = 0;

        if (credentials.USERNAME.trim() === "") {
            errorMessage = "Username is required!";
            isValid = false;
            errorCount += 1;
        }

        if (credentials.POWERUNIT.trim() === "") {
            errorMessage = "Powerunit is required!",
            isValid = false;
            errorCount += 1;
        }

        setInputErrors(prevErrors => ({ 
            ...prevErrors, 
            username: errorCount <= 1 ? errorMessage : "Username and Powerunit are both required!",
            powerunit: errorCount <= 1 ? errorMessage : "Username and Powerunit are both required!"
        }));

        if (!isValid) {
            console.error("Input validation error: ", errorMessage);
            return;
        }

        const response = await addUserToDB(credentials, activeCompany, checkedCompanies, checkedModules);
        if (response.ok) {
            successPopup("users_add_success");
            return;
        }

        let popupType = "fail";
        if (response.status === 409) {
            popupType = "users_add_fail_conflict";
        } 
        else if (response.status == 400) {
            popupType = "users_add_fail_invalid";
        }
        failedPopup(popupType);      
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

    async function pullDriver(e) {
        e.preventDefault();

        let isValid = true;
        let errorMessage = "";

        if (credentials.USERNAME.trim() === "") {
            errorMessage = "Username is required!";
            isValid = false;
        }

        setInputErrors(prevErrors => ({
            ...prevErrors,
            username: errorMessage
        }));

        if (!isValid) {
            console.error("Input validation error: ", errorMessage);
            return;
        }

        const response = await fetchUserFromDB(credentials.USERNAME);
        if (response.ok) {
            const user = await response.json();
            //console.log(user);

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

            setPopup("users_update");
        }
        else {
            //document.getElementById("username").className = "invalid_input";
            //showFailFlag("ff_admin_fu", "Username not found!")
            errorMessage = "Username not found!";
            setInputErrors(prevErrors => ({
                ...prevErrors,
                username: errorMessage
            }));
            console.error("Input validation error: ", errorMessage);
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

    async function updateDriver(e) {
        e.preventDefault();

        let isValid = true;
        let errorMessage = "";
        let errorCount = 0;

        if (credentials.USERNAME.trim() === "") {
            errorMessage = "Username is required!";
            isValid = false;
            errorCount += 1;
        }

        if (credentials.POWERUNIT.trim() === "") {
            errorMessage = "Powerunit is required!",
            isValid = false;
            errorCount += 1;
        }

        setInputErrors(prevErrors => ({ 
            ...prevErrors, 
            username: errorCount <= 1 ? errorMessage : "Username and Powerunit are both required!",
            powerunit: errorCount <= 1 ? errorMessage : "Username and Powerunit are both required!"
        }));

        if (!isValid) {
            console.error("Input validation error: ", errorMessage);
            return;
        }

        // package driver credentials and attempt to replace...
        const update_user = {
            USERNAME: credentials.USERNAME,
            PASSWORD: credentials.PASSWORD,
            POWERUNIT: credentials.POWERUNIT,
            COMPANIES: Object.keys(checkedCompanies).filter(key => checkedCompanies[key]),
            MODULES: Object.keys(checkedModules).filter(key => checkedModules[key])
        }
        const response = await updateUserInDB(previousUser, update_user);
        if (response.ok) {
            setPreviousUser("add new");
            successPopup("users_update_success");
        }
        else {
            let popupType = "fail";
            if (response.status == 404) {
                popupType = "users_update_fail_notFound";
            }
            else if (response.status == 409) {
                /* DIFFERENTIATE BETWEEN POWERUNIT + USERNAME CONFLICTS */
                popupType = "users_update_fail_duplicate";
            }
            returnOnFail(popupType);
        }
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
        const response = removeUserFromDB(credentials.USERNAME);
        if (response.ok) {
            successPopup("users_delete_success");
        } 
        else {
            let popupType = "fail";
            if (response.status == 409) {
                popupType = "users_delete_fail_active";
            }
            else if (response.status == 404) {
                popupType = "users_delete_fail_notFound";
            }
            returnOnFail(popupType);
        }
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

        let message;
        // handle main admin button popup generation
        switch(e.target.innerText){
            case "Add New User":
                //setPopup("Add User");
                message = "users_add";
                setPreviousUser("add new");
                break;
            case "Change/Remove User":
                //setPopup("Find User");
                message = "users_fetch";
                break;
            case "Edit Company Name":
                //setPopup("Change Company");
                message = "company_update";
                break;
        }
        openPopup(message);
    }

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
                            popupType={popup}
                            powerunit={null}
                            handleUpdate={handleUpdate}
                            updateCompany={updateCompany}
                            updateDriver={updateDriver}
                            addDriver={addDriver}
                            pullDriver={pullDriver}
                            removeDriver={removeDriver}
                            cancelDriver={cancelDriver}
                            credentials={credentials}
                            company={company}
                            companies={companies}
                            modules={modules}
                            checkedCompanies={checkedCompanies}
                            checkedModules={checkedModules}
                            checkboxChange={handleCheckboxChange}
                            closePopup={closePopup}
                            isVisible={popupVisible}
                            inputErrors={inputErrors}
                        />
                    )}
                    <Footer id="footer"/>
                </>
            )}
        </div>
    )
};

export default AdminPortal;