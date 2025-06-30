const API_URL = import.meta.env.VIT_API_URL;

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