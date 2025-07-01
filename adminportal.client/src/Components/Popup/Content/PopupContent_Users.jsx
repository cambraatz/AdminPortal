import Success from '../../../assets/success.svg';
import Fail from '../../../assets/error.svg';
import "../Popup.css";

const PopupContent_Users = ({ popupType, 
        credentials,  
        checkedCompanies, 
        checkedModules, 
        checkboxChange,
        handleUpdate,
        addDriver,
        updateDriver,
        removeDriver,
        cancelDriver,
        pullDriver,
        inputErrors 
    }) => {

    const companies = JSON.parse(sessionStorage.getItem("companies_map") || "{}");
    const modules = JSON.parse(sessionStorage.getItem("modules_map") || "{}");
    switch (popupType) {
        case "users_add":
            return (
                <div className="popupContent">
                    <div className="input_wrapper">
                        <label>Username</label>
                        <input 
                            type="text" 
                            id="username-input" 
                            value={credentials.USERNAME ?? ''} 
                            className={`input_form ${inputErrors?.username ? 'invalid_input' : ''}`} 
                            name="username"
                            onChange={handleUpdate} required
                            aria-invalid={!!inputErrors?.username}
                            aria-describedby={inputErrors?.username ? "ff_admin_au_un" : undefined}
                        />
                        {(inputErrors?.username && !inputErrors?.powerunit) && (
                            <div className={`aria_fail_flag ${inputErrors?.username ? 'visible' : ''}`} 
                                id="ff_admin_au_un"
                                role="alert">
                                <p>{inputErrors?.username}</p>
                            </div>
                        )}
                    </div>
                    <div className="input_wrapper">
                        <label>Power Unit</label>
                        <input 
                            type="text" 
                            id="powerunit-input" 
                            value={credentials.POWERUNIT ?? ''} 
                            className={`input_form ${inputErrors?.powerunit ? 'invalid_input' : ''}`} 
                            name="powerunit"
                            onChange={handleUpdate} required
                            aria-invalid={!!inputErrors?.powerunit}
                            aria-describedby={inputErrors?.powerunit ? "ff_admin_au_pu" : undefined}
                        />
                        {inputErrors.powerunit && (
                            <div className={`aria_fail_flag ${inputErrors?.powerunit ? 'visible' : ''}`} 
                                id="ff_admin_au_pu"
                                role="alert">
                                <p>{inputErrors?.powerunit}</p>
                            </div>
                        )}
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
                                        checked={!!checkedCompanies?.[key]}
                                        onChange={() => checkboxChange("company",key)}
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
                                        checked={!!checkedModules?.[url]}
                                        onChange={() => checkboxChange("module",url)}
                                    />
                                    <span className="checkmark"></span>
                                </label>
                            ))
                        )}
                    </div>

                    <div id="add_user">
                        <button type="button" className="popup_button" onClick={addDriver}>Add User</button>
                    </div>
                    <div id="cancel_user">
                        <button type="button" className="popup_button" onClick={cancelDriver}>Cancel</button>
                    </div>
                </div>
            )

        case "users_add_success":
            return (
                <div className="popupContent">
                    <img id="success" src={Success} alt="success"/>
                    <p>{credentials.USERNAME} was added successfully!</p>
                </div>
            )

        case "users_add_fail_conflict":
            return (
                <div className="popupContent">
                    <img id="fail" src={Fail} alt="fail"/>
                    <p>Oops! User already exists, please try again.</p>
                </div>
            )

        case "users_add_fail_invalid":
            return (
                <div className="popupContent">
                    <img id="fail" src={Fail} alt="fail"/>
                    <p>Invalid request, please try again.</p>
                </div>
            )

        case "users_fetch":
            return (
                <div className="popupContent">
                    <div className="input_wrapper">
                        <label>Username</label>
                        <input 
                            type="text" 
                            id="username-input" 
                            value={credentials.USERNAME ?? ''} 
                            className={`input_form ${inputErrors?.username ? 'invalid_input' : ''}`}
                            name="username"
                            onChange={handleUpdate} required
                            aria-invalid={!!inputErrors?.username}
                            aria-describedby={inputErrors?.username ? 'ff_admin_fu' : undefined}
                        />
                        {inputErrors?.username && (
                            <div className={`aria_fail_flag ${inputErrors?.username ? 'visible' : ''}`} 
                                id="ff_admin_fu"
                                role="alert">
                                <p>{inputErrors?.username}</p>
                            </div>
                        )}
                    </div>
                    <div id="find_user">
                        <button id="find_user" className="popup_button" onClick={pullDriver}>Find User</button>
                    </div>
                    <div id="cancel_user">
                        <button className="popup_button" onClick={cancelDriver}>Cancel</button>
                    </div>
                </div>
            )

        case "users_update":
            return (
                <div className="popupContent">
                    <div className="input_wrapper">
                        <label>Username</label>
                        <input 
                            type="text" 
                            id="username-input-uu" 
                            value={credentials.USERNAME ?? ''} 
                            className={`input_form ${inputErrors?.username ? 'invalid_input' : ''}`}
                            name="username" 
                            onChange={handleUpdate} required
                            aria-invalid={!!inputErrors.username}
                            aria-describedby={inputErrors?.username ? "ff_admin_eu_un" : undefined}
                        />
                        {(inputErrors?.username && !inputErrors?.powerunit) && (
                            <div className={`aria_fail_flag ${inputErrors?.username ? 'visible' : ''}`} 
                                id="ff_admin_eu_un"
                                role="alert">
                                <p>{inputErrors?.username}</p>
                            </div>
                        )}
                    </div>
                    <div>
                        <label>Password</label>
                        <input 
                            type="text" 
                            id="password-input-uu" 
                            value={credentials.PASSWORD ? credentials.PASSWORD : ""} 
                            className="input_form" 
                            onChange={handleUpdate}/>
                    </div>
                    <div className="input_wrapper">
                        <label>Power Unit</label>
                        <input 
                            type="text" 
                            id="powerunit-input-uu" 
                            value={credentials.POWERUNIT ?? ''}
                            className={`input_form ${inputErrors?.powerunit ? 'invalid_input' : ''}`}
                            name="powerunit"
                            onChange={handleUpdate} required
                            aria-invalid={!!inputErrors?.powerunit}
                            aria-describedby={inputErrors?.powerunit ? "ff_admin_au_pu" : undefined}
                        />
                        {inputErrors.powerunit && (
                            <div className={`aria_fail_flag ${inputErrors?.powerunit ? 'visible' : ''}`}
                                id="ff_admin_eu_pu"
                                role="alert">
                                <p>{inputErrors?.powerunit}</p>
                            </div>
                        )}
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
                                        checked={!!checkedCompanies?.[key]}
                                        onChange={() => checkboxChange("company",key)}
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
                                        checked={!!checkedModules?.[url]}
                                        onChange={() => checkboxChange("module",url)}
                                    />
                                    <span className="checkmark"></span>
                                </label>
                            ))
                        )}
                    </div>

                    <div id="submit_user">
                        <button className="popup_button" onClick={updateDriver}>Update User</button>
                    </div>
                    <div id="remove_user">
                        <button className="popup_button" onClick={removeDriver}>Remove User</button>
                    </div>
                    <div id="cancel_user" >
                        <button className="popup_button" onClick={cancelDriver}>Cancel</button>
                    </div>
                </div>
            )

        case "users_update_success":
            return (
                <div className="popupContent">
                    <img id="success" src={Success} alt="success"/>
                    <p>{credentials.USERNAME} successfully updated!</p>
                </div>
            )

        case "users_update_fail_notFound":
            return (
                <div className="popupContent">
                    <img id="fail" src={Fail} alt="fail"/>
                    <p>Update failed, {credentials.USERNAME} not found.</p>
                </div>
            )

        case "users_update_fail_duplicate":
            return (
                <div className="popupContent">
                    <img id="fail" src={Fail} alt="fail"/>
                    <p>Update failed, {credentials.USERNAME} already in use.</p>
                </div>
            )

        case "users_delete_success":
                        return (
                            <div className="popupContent">
                                <img id="success" src={Success} alt="success"/>
                                <p>'{credentials.USERNAME}' successfully deleted!</p>
                            </div>
                        )
                    case "users_delete_fail_active":
                        return (
                            <div className="popupContent">
                                <img id="fail" src={Fail} alt="fail"/>
                                <p>Cannot delete active user '{credentials.USERNAME}'.</p>
                            </div>
                        )
                    case "users_delete_fail_notFound":
                        return (
                            <div className="popupContent">
                                <img id="fail" src={Fail} alt="fail"/>
                                <p>'{credentials.USERNAME}' was not found in records.</p>
                            </div>
                        )
    }
};

export default PopupContent_Users;