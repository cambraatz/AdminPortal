const API_URL = import.meta.env.VITE_API_URL;

export async function addUserToDB(credentials, activeCompany, checkedCompanies, checkedModules) {
    let COMPANIES = JSON.parse(sessionStorage.getItem("companies_mapping") || "{}");
    const formData = {
        Username: credentials.USERNAME,
        Permissions: null,
        Powerunit: credentials.POWERUNIT,
        ActiveCompany: Object.keys(COMPANIES).find(key => COMPANIES[key] === activeCompany),
        Companies: Object.keys(checkedCompanies).filter(key => checkedCompanies[key]),
        Modules: Object.keys(checkedModules).filter(key => checkedModules[key])
    };

    const response = await fetch(API_URL + "v1/users", {
        body: JSON.stringify(formData),
        method: "POST",
        headers: {
            'Content-Type': 'application/json; charset=UTF-8'
        },
        credentials: 'include'
    })

    if (!response.ok) {
        console.error(`Adding user '${credentials.USERNAME}' failed, Status: ${response.status} ${response.statusText}`);
        const data = await response.json();
        console.error(data.message);
    }

    return response;
}

export async function fetchUserFromDB(username) {
    const response = await fetch(API_URL + "v1/users/" + username, {
            method: "GET",
            headers: {
                'Content-Type': 'application/json; charset=UTF-8'
            },
            credentials: 'include'
        })

    if (!response.ok) {
        console.error(`Fetching user '${username}' failed, Status: ${response.status} ${response.statusText}`);
        const data = await response.json();
        console.error(data.message);
    }

    return response;
}

export async function updateUserInDB(prevUsername, userUpdate) {
    const response = await fetch (API_URL + "v1/users/" + prevUsername, {
        body: JSON.stringify(userUpdate),
        method: "PUT",
        headers: {
            'Content-Type': 'application/json; charset=UTF-8'
        },
        credentials: 'include'
    })
    
    if (!response.ok) {
        console.error(`Updating user '${prevUsername}' failed, Status: ${response.status} ${response.statusText}`);
        const data = await response.json();
        console.error(data.message);
    }

    return response;
}

export async function removeUserFromDB(username) {
    const response = await fetch(API_URL + "v1/users/" + username, {
        method: "DELETE",
        credentials: 'include'
    });

    if (!response.ok) {
        console.error(`Deleting user '${username}' failed, Status: ${response.status} ${response.statusText}`);
        const data = await response.json();
        console.error(data.message);
    }

    return response;
}