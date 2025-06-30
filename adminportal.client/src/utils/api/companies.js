const API_URL = import.meta.env.VITE_API_URL;

export async function updateCompanyInDB(companyName) {
    const response = await fetch(API_URL + "v1/companies/" + companyName, {
        method: "PUT",
        headers: {
            'Content-Type': 'application/json',
        },
        credentials: 'include'
    })

    if (!response.ok) {
        try {
            console.error(`Updating company '${companyName}' failed, Status: ${response.status} ${response.statusText}`);
            //const data = await response.json();
            //console.error(data.message);
        } catch (ex) {
            console.error(`Error: ${ex}`);
        }
    }

    return response;
}
