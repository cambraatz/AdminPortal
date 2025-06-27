import "./MenuWindow.css";

const MenuWindowContent = (props) => {
    return (
        <div className="admin_div">
            <button type="button" onClick={props.pressButton}>Add New User</button>
            <button type="button" onClick={props.pressButton}>Change/Remove User</button>
            <button type="button" onClick={props.pressButton}>Edit Company Name</button>
        </div>
    )
}

export default MenuWindowContent;