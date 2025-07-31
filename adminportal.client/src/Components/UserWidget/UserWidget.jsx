import PropTypes from 'prop-types';
import userIcon from "../../assets/userIcon.png";

import './UserWidget.css';

const UserWidget = ({ logoutButton, currUser, root, popupReturn, popupLogout }) => {
    const showLogoutButton = logoutButton !== false;
    return (
        <>
            <div id="uw_div">
                <div id="uw_content">
                    <div id="uw_icon_div">
                        <img id="uw_icon" src={userIcon} alt="User Icon" />
                        <p>{currUser}</p>
                    </div>
                    <div id="uw_navButtons">
                        <div id="uw_return">
                            <button onClick={() => popupReturn(root)}>Go Back</button>
                        </div>
                        {showLogoutButton && (
                            <div id="uw_logout">
                                <button onClick={popupLogout}>Log Out</button>
                            </div>
                        )}
                    </div>
                </div>
            </div>
        </>
    );
};

export default UserWidget;

UserWidget.propTypes = {
    logoutButton: PropTypes.bool, // render logout button?
    currUser: PropTypes.string, // current username
    root: PropTypes.bool,
    popupReturn: PropTypes.func,
    popupLogout: PropTypes.func
};