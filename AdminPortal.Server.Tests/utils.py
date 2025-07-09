import requests
import json
import uuid
import urllib3
import urllib.parse # For unquoting cookie values

# Suppress the InsecureRequestWarning that comes with verify=False
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

# --- ANSI ESCAPE CODES FOR COLORS ---
#COLOR_RESET = "\033[0m"   
COLOR_DEFAULT = "\033[0m" # Reset to default color/formatting
COLOR_SUCCESS = "\033[92m"  # Green
COLOR_FAIL = "\033[91m"    # Red
COLOR_PRIMARY = "\033[93m" # Yellow 
COLOR_SECONDARY = "\033[96m" # Cyan
COLOR_WARN = "\033[93m"    # Yellow

# --- CONFIGURATION ---
BASE_API_URL = "https://localhost:7242/v1"
DEV_USERNAME = "cbraatz" # Ensure this user exists in your local DB for dev-login
DEV_COMPANY = "BRAUNS" # Default company for dev login
DEV_COMPANY_NAME = "Brauns Express Inc"

# --- SHARED HELPER FUNCTIONS ---

def printc(message, color=COLOR_DEFAULT):
    """Prints a message with the specified color."""
    print(f"{color}{message}{COLOR_DEFAULT}")

def printv(message, verbose=False, color=COLOR_DEFAULT):
    """Prints a message only if verbose is True."""
    if verbose:
        printc(message, color)

def print_header(test_number, test_name, test_scenario, color=COLOR_PRIMARY):
    """Prints a standardized test header."""
    header_text = f"\n--- Test {test_number}: {test_name} ({test_scenario}) ---"
    printc(header_text, color)
    printc("-" * len(header_text), color) # Match length of header

def get_authenticated_session(username=DEV_USERNAME, company=DEV_COMPANY, verbose=False):
    """
    Logs in via dev-login endpoint and returns a requests.Session
    configured with the necessary cookies for authentication.
    Allows specifying username/company for login.
    """
    dev_login_url = f"{BASE_API_URL}/sessions/dev-login?username={username}&company={company}"
    #printv(f"\n--- Attempting Dev Login for {username} (Company: {company}) ---", verbose, COLOR_SECONDARY) # Changed to secondary color
    try:
        response = requests.get(dev_login_url, allow_redirects=False, verify=False)
        #printv(f"  > Dev Login Request: GET {dev_login_url}", verbose) # Added verbose detail
        #printv(f"  < Dev Login Status: {response.status_code}", verbose)

        if response.status_code in [302, 303, 307, 308]:
            session = requests.Session()
            session.cookies.update(response.cookies)
            
            access_token = session.cookies.get("access_token")
            if not access_token:
                #printc("Warning: access_token not found in dev-login response cookies.", COLOR_WARN)
                raise Exception("Access token missing from dev login cookies.")

            #printv("Dev Login Successful!", verbose, color=COLOR_SUCCESS) # Changed to printv as printc is too verbose here
            return session
        '''else:
            printc(f"Dev Login Failed: Expected redirect (302) but got {response.status_code}.", COLOR_FAIL)
            if response.text: # Only print if there's content
                printv(f"  < Response body: {response.text}", verbose)
            raise Exception(f"Dev Login Failed: {response.status_code}")'''
        
    except requests.exceptions.RequestException as e:
        #printc(f"Network error during dev login: {e}", COLOR_FAIL)
        raise

def generate_unique_user_data(prefix="testuser"):
    """Generates unique user data for testing."""
    unique_id = uuid.uuid4().hex[:8] # Short unique ID
    return {
        "Username": f"{prefix}_{unique_id}",
        "Password": "TestPassword123!",
        "Powerunit": f"{unique_id[:3]}", # Assuming 3 chars fit your DB
        "ActiveCompany": "BRAUNS", # Default to DEV_COMPANY
        "Companies": ["BRAUNS", "NTS"], # Example list
        "Modules": ["admin", "deliverymanager"]
    }

def logout_via_api(session, verbose=False):
    """Helper to call POST /v1/sessions/logout and return the response."""
    url = f"{BASE_API_URL}/sessions/logout"
    # Keeping cleanup silent in verbose mode too, as per original intention
    # printv(f"  > POST {url} - Logging out current session (cleanup)", verbose)
    response = session.post(url, verify=False)
    # printv(f"  < Status: {response.status_code} (cleanup)", verbose)
    return response