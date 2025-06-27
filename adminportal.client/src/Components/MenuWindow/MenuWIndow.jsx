import PropTypes from 'prop-types';
import MenuWindowContent from "./MenuWindowContent";
import "./MenuWindow.css";

const MenuWindow = (props) => {
    return(
        <>
        {props.prompt && (
            <div className="mw_header">
                <h4 className="prompt">{props.prompt}</h4>
            </div>
        )}
        {props.header === "Full" && (
            <div className="mw_spm_row">
                <div className="mw_spm_col">
                    <h5>Stop No:</h5>
                    <h5 className="weak">{props.STOP}</h5>
                </div>
                <div className="mw_spm_col">
                    <h5>Pro No:</h5>
                    <h5 className="weak">{props.PRONUMBER}</h5>
                </div>
                <div className="mw_spm_col">
                    <h5>Manifest Key:</h5>
                    <h5 className="weak">{props.MFSTKEY}</h5>
                </div>
            </div>                
        )}
        <MenuWindowContent pressButton={props.pressButton} />
        </>
    )
};

export default MenuWindow;

MenuWindow.propTypes = {
    prompt: PropTypes.string, // const string page prompt
    header: PropTypes.string, // inactive in admin page
    pressButton: PropTypes.func, // pressButton helper
    STOP: PropTypes.string, // inactive in admin page
    PRONUMBER: PropTypes.string, // inactive in admin page
    MFSTKEY: PropTypes.string, // inactive in admin page
};