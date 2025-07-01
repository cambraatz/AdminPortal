import PopupContent from './PopupContent';
import Success from '../../assets/success.svg';
import Fail from '../../assets/error.svg';
import "./Popup.css";

const Popup = ({ 
        popupType,
        powerunit,
        credentials,
        company,
        companies,
        modules,
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
        pullDriver,
        isVisible,
        closePopup
    }) => {
    const popupClass = isVisible ? 'overlay-visible' : 'overlay-hidden';
    return (
        <div id="popupWindow" className={`overlay ${popupClass}`}>
            <div className="popupLogin">
                <div id="popupAddExit" className="content">
                    <h1 id="close_add" className="popupLoginWindow" onClick={closePopup}>&times;</h1>
                </div>
                <PopupContent 
                    popupType={popupType}
                    powerunit={powerunit}
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
                    checkboxChange={checkboxChange}
                    closePopup={closePopup}
                    isVisible={isVisible}
                    inputErrors={inputErrors}
                />
            </div>
        </div>
    )
};

export default Popup;

/* General framework for popup open/close logic in parent */
/*
const [popupVisible, setVisible] = useState(false);
const openPopup = () => {
    setVisible(true);
};

const closePopup = () => {
    setVisible(false);

    clearStyling();
    setCheckedCompanies({});
    setCheckedModules({});
};*/