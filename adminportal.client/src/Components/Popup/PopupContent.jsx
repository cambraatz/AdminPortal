import Success from '../../assets/success.svg';
import Fail from '../../assets/error.svg';
import "./Popup.css";
import { PopupContent_Company, 
        PopupContent_Users,
        PopupContent_Sessions } from './Content';

const PopupContent = ({ 
        popupType,
        powerunit,
        credentials,
        company,
        companies,
        modules,
        checkedCompanies,
        checkedModules,
        errors,
        inputErrors,
        handleUpdate,
        updateCompany,
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
                updateDriver={updateCompany}
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

    /*//
    //
    // ACTIVE
    //
    else if(props.popupType === "Update Success"){
        const user = props.credentials.USERNAME;
        return(
            <>
                <div className="popupContent">
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
    else if(props.popupType === "Delete Success"){
        const user = props.credentials.USERNAME;
        return(
            <>
                <div className="popupContent">
                    <img id="success" src={Success} alt="success"/>
                    <p>{user} successfully deleted!</p>
                </div>
            </>
        )
    }

    else if(props.popupType === "Success"){
        return(
            <>
                <div className="popupContent">
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
    
    else if(props.popupType === "ActiveUserFail"){
        return(
            <>
                <div className="popupContent">
                    <img id="fail" src={Fail} alt="fail"/>
                    <p>Cannot delete active user.</p>
                </div>
            </>
        )
    }
    else if(props.popupType === "UNPUConflictFail"){
        return(
            <>
                <div className="popupContent">
                    <img id="fail" src={Fail} alt="fail"/>
                    <p>Update failed, contact administrator.</p>
                </div>
            </>
        )
    }
    else if(props.popupType === "UNConflictFail"){
        return(
            <>
                <div className="popupContent">
                    <img id="fail" src={Fail} alt="fail"/>
                    <p>Username/Powerunit already in use.</p>
                </div>
            </>
        )
    }

    else if(props.popupType === "Logout Success"){
        const user = props.credentials.USERNAME;
        return(
            <>
                <div className="popupContent">
                    <img id="success" src={Success} alt="success"/>
                    <p>{user} was logged out successfully!</p>
                </div>
            </>
        )
    }

    else if(props.popupType === "Token Fail"){
        return(
            <>
                <div className="popupContent">
                    <img id="fail" src={Fail} alt="fail"/>
                    <p>Token validation failed, logging out.</p>
                </div>
            </>
        )
    }*/
};

export default PopupContent;