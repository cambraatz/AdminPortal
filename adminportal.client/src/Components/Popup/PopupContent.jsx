import Success from '../../assets/success.svg';
import Fail from '../../assets/error.svg';
import "./Popup.css";
import { PopupContent_Company, 
        PopupContent_Users,
        PopupContent_Sessions } from './Content';

const PopupContent = ({ 
        popupType,
        //powerunit,
        credentials,
        company,
        //companies,
        //modules,
        checkedCompanies,
        checkedModules,
        inputErrors,
        handleUpdate,
        updateCompany,
        updateDriver,
        cancelDriver,
        checkboxChange,
        addDriver,
        removeDriver,
        pullDriver
    }) => {

    // GENERAL FAIL
    if (popupType === "fail"){
        return(
            <div className="popupContent">
                <img id="fail" src={Fail} alt="fail"/>
                <p>Oops! Something went wrong, please try again.</p>
            </div>
        )
    }

    // COMPANY
    if (popupType.startsWith("company")) {
        return (
            <PopupContent_Company 
                popupType={popupType}
                company={company}
                handleUpdate={handleUpdate}
                updateCompany={updateCompany}
                cancelDriver={cancelDriver}
                inputErrors={inputErrors}
            />
        )
    }

    // USERS
    if (popupType.startsWith("users")) {
        return (
            <PopupContent_Users
                popupType={popupType}
                credentials={credentials}
                checkedCompanies={checkedCompanies}
                checkedModules={checkedModules}
                checkboxChange={checkboxChange}
                handleUpdate={handleUpdate}
                addDriver={addDriver}
                updateDriver={updateDriver}
                removeDriver={removeDriver}
                cancelDriver={cancelDriver}
                pullDriver={pullDriver}
                inputErrors={inputErrors}
            />
        )
    }

    // SESSIONS
    if (popupType.startsWith("sessions")) {
        return (
            <PopupContent_Sessions 
                popupType={popupType}
                credentials={credentials}
            />
        )
    }

    switch (popupType) {
        case "fail":
            return (
                <>
                    <div className="popupContent">
                        <img id="fail" src={Fail} alt="fail"/>
                        <p>Oops! Something went wrong, please try again.</p>
                    </div>
                </>
            )
    }    
    
    if(popupType === null){
        return(
            <h5>this shouldnt happen...</h5>
        )
    }
};

export default PopupContent;