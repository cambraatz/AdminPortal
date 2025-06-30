//import Success from '../../../assets/success.svg';
import Fail from '../../../assets/error.svg';
import "../Popup.css";

const PopupContent_Sessions = (props) => {
    const { popupType, credentials } = props;

    switch (popupType) {
        case "sessions_validation_fail":
            return (
                <div className="popupContent">
                    <img id="fail" src={Fail} alt="fail"/>
                    <p>Session validation failed for {credentials.USERNAME}.</p>
                </div>
            )
    }
};

export default PopupContent_Sessions;