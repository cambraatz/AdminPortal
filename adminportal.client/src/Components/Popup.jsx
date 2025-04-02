import Success from '../assets/success.svg';
import Fail from '../assets/error.svg';

const Popup = (props) => {
    if(props.message === null){
        return(
            <h5>this shouldnt happen...</h5>
        )
    }
    //
    //
    // ACTIVE
    //
    else if(props.message === "Edit User"){
        const companies = JSON.parse(sessionStorage.getItem("companies_map") || "{}");
        const modules = JSON.parse(sessionStorage.getItem("modules_map") || "{}");
        return(
            <>
                <div className="popupLoginContent">
                    <div className="input_wrapper">
                        <label>Username</label>
                        <input type="text" id="username" value={props.credentials.USERNAME} className="input_form" onChange={props.handleUpdate}/>
                        <div className="fail_flag" id="ff_admin_eu_un">
                            <p>Username is required!</p>
                        </div>
                    </div>
                    <div>
                        <label>Password</label>
                        <input type="text" id="password" value={props.credentials.PASSWORD ? props.credentials.PASSWORD : ""} className="input_form" onChange={props.handleUpdate}/>
                    </div>
                    <div className="input_wrapper">
                        <label>Power Unit</label>
                        <input type="text" id="powerunit" value={props.credentials.POWERUNIT} className="input_form" onChange={props.handleUpdate}/>
                        <div className="fail_flag" id="ff_admin_eu_pu">
                            <p>Powerunit is required!</p>
                        </div>
                        <div className="fail_flag" id="ff_admin_eu_up">
                            <p>Username and Powerunit are required!</p>
                        </div>
                    </div>

                    <div id="cm-checkbox" className="checkbox-div">
                        <div className="checkbox-header">
                            <h5 className="checkbox-header-text">Companies</h5>
                        </div>
                        {companies && (
                            Object.entries(companies).map(([key,company]) => (
                                <label key={key} className="check-container">
                                    {company}
                                    <input 
                                        type="checkbox" 
                                        checked={!!props.checkedCompanies?.[key]}
                                        onChange={() => props.functions.checkboxChange("company",key)}
                                    />
                                    <span className="checkmark"></span>
                                </label>
                            ))
                        )}
                    </div>
                    <div id="md-checkbox" className="checkbox-div">
                        <div className="checkbox-header">
                            <h5 className="checkbox-header-text">Services</h5>
                        </div>
                        {modules && (
                            Object.entries(modules).map(([url,name]) => (
                                <label key={url} className="check-container">
                                    {name}
                                    <input 
                                        type="checkbox" 
                                        checked={!!props.checkedModules?.[url]}
                                        onChange={() => props.functions.checkboxChange("module",url)}
                                    />
                                    <span className="checkmark"></span>
                                </label>
                            ))
                        )}
                    </div>

                    <div id="submit_user">
                        <button className="popup_button" onClick={props.functions.updateDriver}>Update User</button>
                    </div>
                    <div id="remove_user">
                        <button className="popup_button" onClick={props.functions.removeDriver}>Remove User</button>
                    </div>
                    <div id="cancel_user" >
                        <button className="popup_button" onClick={props.functions.cancelDriver}>Cancel</button>
                    </div>
                </div>
            </>
        )
    }
    //
    //
    // ACTIVE
    //
    else if(props.message === "Add User"){
        const companies = JSON.parse(sessionStorage.getItem("companies_map") || "{}");
        const modules = JSON.parse(sessionStorage.getItem("modules_map") || "{}");
        return(
            <div className="popupLoginContent">
                <div className="input_wrapper">
                    <label>Username</label>
                    <input type="text" id="username" value={props.credentials.USERNAME} className="input_form" onChange={props.handleUpdate} required/>
                    <div className="fail_flag" id="ff_admin_au_un">
                        <p>Username is required!</p>
                    </div>
                </div>
                <div className="input_wrapper">
                    <label>Power Unit</label>
                    <input type="text" id="powerunit" value={props.credentials.POWERUNIT} className="input_form" onChange={props.handleUpdate} required/>
                    <div className="fail_flag" id="ff_admin_au_pu">
                        <p>Powerunit is required!</p>
                    </div>
                    <div className="fail_flag" id="ff_admin_au_up">
                        <p>Username and Powerunit are required!</p>
                    </div>
                </div>
                <div id="cm-checkbox" className="checkbox-div">
                    <div className="checkbox-header">
                        <h5 className="checkbox-header-text">Companies</h5>
                    </div>
                    {companies && (
                        Object.entries(companies).map(([key,company]) => (
                            <label key={key} className="check-container">
                                {company}
                                <input 
                                    type="checkbox" 
                                    checked={!!props.checkedCompanies?.[key]}
                                    onChange={() => props.functions.checkboxChange("company",key)}
                                />
                                <span className="checkmark"></span>
                            </label>
                        ))
                    )}
                </div>
                <div id="md-checkbox" className="checkbox-div">
                    <div className="checkbox-header">
                        <h5 className="checkbox-header-text">Services</h5>
                    </div>
                    {modules && (
                        Object.entries(modules).map(([url,module]) => (
                            <label key={url} className="check-container">
                                {module}
                                <input 
                                    type="checkbox" 
                                    checked={!!props.checkedModules?.[url]}
                                    onChange={() => props.functions.checkboxChange("module",url)}
                                />
                                <span className="checkmark"></span>
                            </label>
                        ))
                    )}
                </div>

                <div id="add_user">
                    <button type="button" className="popup_button" onClick={props.functions.addDriver}>Add User</button>
                </div>
                <div id="cancel_user">
                    <button type="button" className="popup_button" onClick={props.functions.cancelDriver}>Cancel</button>
                </div>
            </div>
        )
    }
    //
    //
    // ACTIVE
    //
    else if(props.message === "Find User"){
        return(
            <>
                <div className="popupLoginContent">
                    <div className="input_wrapper">
                        <label>Username</label>
                        <input type="text" id="username" value={props.credentials.USERNAME} className="input_form" onChange={props.handleUpdate}/>
                        <div className="fail_flag" id="ff_admin_fu">
                            <p>Username was not found!</p>
                        </div>
                    </div>
                    <div id="find_user">
                        <button id="find_user" className="popup_button" onClick={props.functions.pullDriver}>Find User</button>
                    </div>
                    <div id="cancel_user">
                        <button className="popup_button" onClick={props.functions.cancelDriver}>Cancel</button>
                    </div>
                </div>
            </>
        )
    }
    //
    //
    // ACTIVE
    //
    else if(props.message === "Change Company"){
        return(
            <>
                <div className="popupLoginContent">
                    <div className="input_wrapper">
                        <label>Company Name</label>
                        <input type="text" id="company" value={props.company} className="input_form" onChange={props.handleUpdate}/>
                        <div className="fail_flag" id="ff_admin_cc">
                            <p>Company name is required!</p>
                        </div>
                    </div>
                    <div id="submit_company">
                        <button className="popup_button" onClick={props.functions.updateCompany}>Update Company</button>
                    </div>
                    <div id="cancel_user">
                        <button className="popup_button" onClick={props.functions.cancelDriver}>Cancel</button>
                    </div>
                </div>
            </>
        )
    }
    //
    //
    // ACTIVE
    //
    else if(props.message === "Add Success"){
        const user = props.credentials.USERNAME;
        return(
            <>
                <div className="popupLoginContent">
                    <img id="success" src={Success} alt="success"/>
                    <p>{user} was added successfully!</p>
                </div>
            </>
        )
    }
    //
    //
    // ACTIVE
    //
    else if(props.message === "Update Success"){
        const user = props.credentials.USERNAME;
        return(
            <>
                <div className="popupLoginContent">
                    <img id="success" src={Success} alt="success"/>
                    <p>{user} successfully updated!</p>
                </div>
            </>
        )
    }
    //
    //
    // ACTIVE
    //
    else if(props.message === "Delete Success"){
        const user = props.credentials.USERNAME;
        return(
            <>
                <div className="popupLoginContent">
                    <img id="success" src={Success} alt="success"/>
                    <p>{user} successfully deleted!</p>
                </div>
            </>
        )
    }
    //
    //
    // ACTIVE
    //
    else if(props.message === "Company Success"){
        //const company = props.company;
        return(
            <>
                <div className="popupLoginContent">
                    <img id="success" src={Success} alt="success"/>
                    <p>Company successfully updated!</p>
                </div>
            </>
        )
    }
    else if(props.message === "Success"){
        return(
            <>
                <div className="popupLoginContent">
                    <img id="success" src={Success} alt="success"/>
                    <p>Delivery successfully updated!</p>
                </div>
            </>
        )
    }
    //
    //
    // ACTIVE
    //
    else if(props.message === "Fail"){
        return(
            <>
                <div className="popupLoginContent">
                    <img id="fail" src={Fail} alt="fail"/>
                    <p>Oops! Something went wrong, please try again.</p>
                </div>
            </>
        )
    }
    //
    //
    // ACTIVE
    //
    else if(props.message === "Admin_Add Fail"){
        return(
            <>
                <div className="popupLoginContent">
                    <img id="fail" src={Fail} alt="fail"/>
                    <p>Oops! User already exists, please try again.</p>
                </div>
            </>
        )
    }

    else if(props.message === "Logout Success"){
        const user = props.credentials.USERNAME;
        return(
            <>
                <div className="popupLoginContent">
                    <img id="success" src={Success} alt="success"/>
                    <p>{user} was logged out successfully!</p>
                </div>
            </>
        )
    }

    else if(props.message === "Token Fail"){
        return(
            <>
                <div className="popupLoginContent">
                    <img id="fail" src={Fail} alt="fail"/>
                    <p>Token validation failed, logging out.</p>
                </div>
            </>
        )
    }
};

export default Popup;