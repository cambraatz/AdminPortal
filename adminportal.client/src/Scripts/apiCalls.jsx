import { SUCCESS_WAIT, FAIL_WAIT } from "./helperFunctions";
const API_URL = import.meta.env.VITE_API_URL;

export async function Return() {    
        const response = await fetch(`${API_URL}v1/sessions/return`, {
            method: "POST",
            headers: {
                'Content-Type': 'application/json; charset=UTF-8'
            },
            credentials: "include"
        });

        let success = false;

        if (response.ok) {
            console.log("Return Successful!");
            success = true;
        } else {
            console.error("Return cookie generation failed, return failure.");
        }
        setTimeout(() => {
            window.location.href = `https://login.tcsservices.com`;
        }, success ? SUCCESS_WAIT : FAIL_WAIT);
    }

export async function Logout() {
    localStorage.clear();
    sessionStorage.clear();

    const response = await fetch(`${API_URL}v1/sessions/logout`, {
        method: "POST",
        headers: {
            'Content-Type': 'application/json; charset=UTF-8'
        },
        credentials: "include",
    })
    if (response.ok) {
        console.log("Logout Successful!");
        setTimeout(() => {
            window.location.href = `https://login.tcsservices.com`;
        },SUCCESS_WAIT);
    } else {
        console.error("Cookie removal failed, Logout failure.");
        setTimeout(() => {
            window.location.href = `https://login.tcsservices.com`;
        },FAIL_WAIT);
    }
}