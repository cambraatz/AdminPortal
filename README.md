# AdminPortal Microservice

---

## Project Overview

This repository contains a dedicated microservice focused on **administrative user management and company configuration** for a trucking logistics company. It forms a crucial part of a larger, microservices-based web application designed to empower administrators with the tools to manage user accounts and system-wide settings securely and efficiently.

### Repository Structure

This repository is organized into distinct projects:

* **`adminportal.client`**: The **React-based frontend** (JavaScript, CSS, HTML) responsible for the intuitive user interface of the Admin Portal.
* **`AdminPortal.Server`**: The **.NET backend** (C#) that constitutes this specific microservice, handling secure and efficient database transactions related to administrative operations.
* **`AdminPortal.Server.Tests`**: Contains comprehensive **backend unit and integration tests** for the `AdminPortal.Server` microservice (specific to AdminPortal functionalities).

---

## Architecture and Integration

This microservice operates within a broader ecosystem of services, notably interacting with a separate **Login Portal Microservice**. Access to the Admin Portal is highly restricted, requiring full administrative rights.

* **Authentication & Authorization (SSO):** This service does *not* handle user login directly. Instead, it receives a validated session through the Login Portal Microservice. Requests to this service's protected endpoints expect a valid **JWT (JSON Web Token)** to be present, which is issued and managed by the Login Portal. Importantly, access to this AdminPortal is **only granted upon successful authentication with full administrative rights**. **Single Sign-On (SSO)** is utilized to prevent compromising sensitive administrative data with duplicate sessions.
* **Token Validation:** This microservice includes its own `TokenService` to validate incoming JWTs against a shared secret key and configuration. This allows it to independently verify the authenticity and authorization of administrative requests, including refreshing access tokens if the refresh token is valid and needs to be renewed.
* **Data Interaction:** It directly interfaces with the user and company configuration databases, ensuring all administrative operations are performed securely and efficiently.

---

## Core Functionality & Workflow

This microservice provides a secure interface for administrators to manage user accounts and central company settings.

### UX/UI Workflow (Admin Process Flow)

The following figure illustrates an administrator's interaction with the components managed by this microservice.

![Admin Interface](README_Assets/Admin/DM_AdminMenu_mobile.png)
*Figure 1.0: Admin Menu*

### 1. Admin Menu Navigation

Upon successful login with administrative rights, users are redirected to the Admin Menu. This menu presents three primary administrative functions, each handled by this microservice's backend:

1.  **Add New User**: For onboarding new employees.
2.  **Change/Remove Existing Users**: For managing existing employee records.
3.  **Edit Active Company Name**: For updating the company's display name.

### 2. Add New User

This feature allows administrators to securely create new user accounts.

* **Input Form:** The UI presents an input form for collecting essential new user details.
* **User Details:** Administrators provide a **username** and assign a **power unit** (vehicle ID) to the new user.
* **Access Control:** The form also includes options to specify **lists of companies and services** the new user will have access to.
* **Password Management:** For enhanced security and convenience, the **password for a new user is deliberately left blank** and is designed to be set by the user themselves upon their initial login.

![Add User Interface](README_Assets/Admin/DM_Admin_AddUser_mobile.png)
*Figure 2.0: Add User Interface*

### 3. Change/Remove Existing Users

This robust feature enables administrators to manage current employee records.

* **User Search:** Administrators can search for existing users by providing a **username**.
* **Record Fetching:** Upon valid input, the microservice fetches the employee's current details from the database.
* **Update Capabilities:** The retrieved user information, including **username, an anonymous representation of the password, power unit, and assigned companies/services**, is displayed for editing. This ensures sensitive password data is never exposed directly.
* **User Removal:** Administrators also have the capability to **remove users entirely** from the system using this feature, ensuring proper user lifecycle management.

![Find User Interface](README_Assets/Admin/DM_Admin_FindUser_mobile.png)
*Figure 3.0: Find User Interface*

![Change/Remove User Interface](README_Assets/Admin/DM_Admin_ChangeUser_mobile.png)
*Figure 3.1: Change/Remove User Interface*

### 4. Edit Active Company Name

This function provides flexibility for company branding and identification within the application.

* **Company Name Configuration:** The portal allows administrators to update the **verbose name of the active company** that is displayed across the application.
* **Immutable Company Keys:** It's critical to note that companies are correlated with **unique, immutable company keys** that identify their respective databases.
* **Dynamic Naming:** Only the **front-facing/verbose names can be changed**. This ensures that all instances of that company within the application refer to the new name without impacting the underlying database connections or data integrity.

![Company Name Interface](README_Assets/Admin/DM_Admin_EditCompany_mobile.png)
*Figure 4.0: Company Name Interface*

---

## Visual Feedback for Database Changes

Similar to other microservices in this ecosystem, the AdminPortal provides the data and status updates that drive the client-side visual feedback, using custom graphic icons and specific feedback messages to visually confirm the outcome of administrative database interactions.

### Success Icons

Processed responses from this service trigger success icons for successful database updates, ensuring administrators receive immediate confirmation.

![Success Iconography](README_Assets/Admin/DM_Admin_UpdateSuccess_mobile.png)
*Figure 5.0: Success Iconography*

### Error Icons

Failed requests handled by this service result in error icons with accompanying messages to guide corrective actions.

![Fail Iconography](README_Assets/Admin/DM_Admin_UpdateFail_mobile.png)
*Figure 5.1: Fail Iconography*

### Validation Icons

Input validation performed by this service (or related client-side logic) contributes to displaying icons indicating invalid or missing inputs for required fields, prompting administrators to address errors before proceeding.

![Form/Field Validation](README_Assets/Admin/DM_Admin_AddUser_Error_mobile.png)
*Figure 5.2: Form/Field Validation*