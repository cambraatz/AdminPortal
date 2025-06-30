import Success from '../../../assets/success.svg';
import Fail from '../../../assets/error.svg';
import "../Popup.css";

const PopupContent_Company = ({ 
    popupType, 
    company, 
    handleUpdate, 
    updateCompany, 
    cancelDriver, 
    inputErrors 
}) => {
    switch (popupType) {
        case "company_update":
            return(
                <div className="popupContent">
                    <div className="input_wrapper">
                        <label htmlFor='company-name-input'>Company Name</label>
                        <input 
                            type="text" 
                            id="company-name-input" 
                            className={`input_form ${inputErrors?.company ? 'invalid_input' : ''}`}
                            name="company"
                            value={company ?? ''} 
                            onChange={handleUpdate}
                            aria-invalid={!!inputErrors?.company}
                            aria-describedby={inputErrors?.company ? 'ff_admin_cc' : undefined}
                        />
                        {inputErrors?.company && (
                            <div className={`aria_fail_flag ${inputErrors?.company ? 'visible' : ''}`} 
                                id="ff_admin_cc" 
                                role="alert">
                                <p>{inputErrors?.company || ' '}</p>
                            </div>
                        )}
                    </div>
                    <div id="submit_company">
                        <button className="popup_button" onClick={updateCompany}>Update Company</button>
                    </div>
                    <div id="cancel_user">
                        <button className="popup_button" onClick={cancelDriver}>Cancel</button>
                    </div>
                </div>
            )
        case "company_update_success":
            return (
                <div className="popupContent">
                    <img id="success" src={Success} alt="success"/>
                    <p>Company successfully updated!</p>
                </div>
            )
        default:
            console.error("THIS SHOULD NOT BE HIT!");
            return null;
    }
};

export default PopupContent_Company;