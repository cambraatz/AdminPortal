import { SUCCESS_WAIT, FAIL_WAIT, scrapeDate } from "../../scripts/helperFunctions";
//import { useNavigate } from "react-router-dom";
const API_URL = import.meta.env.VITE_API_URL;

export async function validateSession(url) {
    const response = await fetch(url + "v1/sessions/me", {
        method: "GET",
        headers: {
            'Content-Type': 'application/json; charset=UTF-8'
        },
        credentials: 'include'
    });

    if (!response.ok) {
        console.error(`Session validation failed, Status: ${response.status} ${response.statusText}`);
        try {
            const data = await response.json();
            console.error(data.message);
        } catch (ex) {
            console.error(`Error: ${ex}`);
        }
    }

    return response;
}

const goBackOneDirectory = () => {
    const currPath = window.location.pathname;
    if (currPath === '/' || currPath ==='') {
        return '/';
    }
    const pathSegments = currPath.split('/');
    if (pathSegments.length > 0) {
        pathSegments.pop();
    }
    if (pathSegments.length > 1 && pathSegments[pathSegments.length - 1] === '') {
        pathSegments.pop();
    }

    let newPath = pathSegments.join('/');
    if (newPath === '') {
        newPath = '/';
    } else if (!newPath.startsWith('/')) {
        newPath = '/' + newPath;
    }

    return newPath;
}

export async function Return(root, sessionId) {    
    if (root) {
        const response = await fetch(`${API_URL}v1/sessions/return/${sessionId}`, {
            method: "POST",
            headers: {
                'Content-Type': 'application/json; charset=UTF-8'
            },
            credentials: "include"
        });

        if (response.ok) {
            console.log("Return Successful!");
            setTimeout(() => {
                //console.log("Logged Out... [dev]");
                window.location.href = `https://login.tcsservices.com`;
            }, SUCCESS_WAIT);
        } else {
            console.error("Return cookie generation failed, logging out.");
            Logout();
            return;
        }
    }
    else {
        const path = goBackOneDirectory();
        return path;
    }
}

export async function Logout(session=null) {
    localStorage.clear();
    sessionStorage.clear();

    const response = await fetch(`${API_URL}v1/sessions/logout/${session.id}`, {
        method: "POST",
        headers: {
            'Content-Type': 'application/json; charset=UTF-8'
        },
        body: JSON.stringify(session),
        credentials: "include",
    })
    if (response.ok) {
        console.log("Logout Successful!");
        setTimeout(() => {
            //console.log(`Logged Out... [${session.id}]`);
            window.location.href = `https://login.tcsservices.com`;
        },SUCCESS_WAIT);
    } else {
        console.error("Cookie removal failed, Logout failure.");
        setTimeout(() => {
            //console.log("Logged Out... [dev]");
            window.location.href = `https://login.tcsservices.com`;
        },FAIL_WAIT);
    }
}