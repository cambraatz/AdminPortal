import PopupContent from './PopupContent';
import Success from '../../assets/success.svg';
import Fail from '../../assets/error.svg';
import "./Popup.css";

const Popup = (props) => {
    const popupClass = props.isVisible ? 'overlay-visible' : 'overlay-hidden';
    return (
        <div id="popupWindow" className={`overlay ${popupClass}`}>
            <div className="popupLogin">
                <div id="popupAddExit" className="content">
                    <h1 id="close_add" className="popupLoginWindow" onClick={props.closePopup}>&times;</h1>
                </div>
                <PopupContent 
                    message={props.message}
                    powerunit={props.powerunit}
                    handleUpdate={props.handleUpdate}
                    credentials={props.credentials}
                    company={props.company}
                    companies={props.companies}
                    modules={props.modules}
                    checkedCompanies={props.checkedCompanies}
                    checkedModules={props.checkedModules}
                    functions={props.functions}
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