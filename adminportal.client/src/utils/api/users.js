const API_URL = import.meta.env.VITE_API_URL;

async function parseErrorMessage(response) {
    let errorMessage = "An unknown error occurred";
    let errorType = "unknown";
    let errorStatus = ""

    // response objects are consumed on first use, temp clones...
    const responseJSON = response.clone();
    const responseTXT = response.clone();

    try {
        errorStatus = responseJSON.status;
        const errorBody = await responseJSON.json();
        if (errorBody && errorBody.message) {
            errorMessage = errorBody.message;
        }
        else if (errorBody && typeof errorBody === 'string') {
            errorMessage = errorBody;
        }
        else {
            errorMessage = `Server error (status: ${responseJSON.status}). Details: ${JSON.stringify(errorBody)}`;
        }
        errorType = 'json';
    // eslint-disable-next-line no-unused-vars
    } catch (ex) {
        try {
            errorStatus = responseTXT.status;
            const textError = await responseTXT.text();
            if (textError) {
                errorMessage = textError;
            } else {
                errorMessage = `HTTP Error! Status ${responseTXT.status} ${responseTXT.statusText || ''}`;
            }
            errorType = 'text';
        // eslint-disable-next-line no-unused-vars
        } catch (textParseEx) {
            errorMessage = `HTTP Error! Status: ${responseTXT.status} ${responseTXT.statusText || ''} (No response body)`;
            errorType = 'empty';
        }
    }

    console.error(`Error (${errorStatus} - ${errorType}):`, errorMessage);
    return {status: errorStatus, message: errorMessage };
}

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
        try {
            const parsedResponse = parseErrorMessage(response);
            console.error(`Adding user '${credentials.USERNAME}' failed, Status: ${parsedResponse.status} ${parsedResponse.message}`);
        } catch (ex) {
            console.error("Error: ", ex);
        }
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
        try {
            const parsedResponse = parseErrorMessage(response);
            console.error(`Fetching user '${username}' failed, Status: ${parsedResponse.status} ${parsedResponse.message}`);
        } catch (ex) {
            console.error("Error: ", ex);
        }
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
        try {
            const parsedResponse = parseErrorMessage(response);
            console.error(`Updating user '${prevUsername}' failed, Status: ${parsedResponse.status} ${parsedResponse.message}`);
        } catch (ex) {
            console.error("Error: ", ex);
        }
    }

    return response;
}

export async function removeUserFromDB(username) {
    const response = await fetch(API_URL + "v1/users/" + username, {
        method: "DELETE",
        credentials: 'include'
    });

    if (!response.ok) {
        try {
            const parsedResponse = parseErrorMessage(response);
            console.error(`Deleting user '${username}' failed, Status: ${parsedResponse.status} ${parsedResponse.message}`);
        } catch (ex) {
            console.error("Error: ", ex);
        }
    }

    return response;
}