import unittest
import requests
import json
import argparse
import sys
import os
import jwt

from utils import (
    COLOR_DEFAULT, COLOR_SUCCESS, COLOR_FAIL, COLOR_PRIMARY, COLOR_SECONDARY, COLOR_WARN,
    BASE_API_URL, DEV_USERNAME, DEV_COMPANY,
    printc, printv, # print_header is less common in unittest methods
    get_authenticated_session, logout_via_api # Ensure logout_via_api is in utils and works as expected
)

# --- API HELPER FUNCTIONS FOR SESSIONS CONTROLLER ---

def get_current_driver_via_api(session, verbose=False):
    """
    Helper to call GET /v1/sessions/me and return the response.
    """
    url = f"{BASE_API_URL}/sessions/me"
    printv(f"  > GET {url} - Getting current driver session", verbose)
    response = session.get(url, verify=False)
    printv(f"  < Status: {response.status_code}", verbose)
    try:
        printv(f"  < Response: {json.dumps(response.json(), indent=2)}", verbose)
    except json.JSONDecodeError:
        if response.text:
            printv(f"  < Response (text): {response.text}", verbose)
    return response

# Note: logout_via_api is assumed to be in utils.py and imported.
# If it's only defined here, you'd need to ensure it's accessible or move it to utils.

def return_session_via_api(session, session_id, verbose=False):
    """
    Helper to call POST /v1/sessions/return/{userId} and return the response.
    The userId parameter is expected to be the sessionId.
    """
    url = f"{BASE_API_URL}/sessions/return/{session_id}"
    printv(f"  > POST {url} - Initializing return session", verbose)
    response = session.post(url, verify=False)
    printv(f"  < Status: {response.status_code}", verbose)
    try:
        printv(f"  < Response: {json.dumps(response.json(), indent=2)}", verbose)
    except json.JSONDecodeError:
        if response.text:
            printv(f"  < Response (text): {response.text}", verbose)
    return response

def post_credentials_via_api(session, verbose=False):
    """
    Helper to call POST /v1/sessions/me and return the response.
    """
    url = f"{BASE_API_URL}/sessions/me"
    printv(f"  > POST {url} - Attempting credentials refresh", verbose)
    response = session.post(url, verify=False)
    printv(f"  < Status: {response.status_code}", verbose)
    try:
        printv(f"  < Response: {json.dumps(response.json(), indent=2)}", verbose)
    except json.JSONDecodeError:
        if response.text:
            printv(f"  < Response (text): {response.text}", verbose)
    return response

def logout_with_id_via_api(session, session_id, verbose=False, session_body=None):
    """
    Helper to call the POST /v1/sessions/logout/{userId} endpoint,
    with the userId parameter being the session ID.
    """
    url = f"{BASE_API_URL}/sessions/logout/{session_id}"
    printv(f"  > POST {url} - Logging out session with ID {session_id}", verbose)
    
    headers = {'Content-Type': 'application/json'}
    body = session_body if session_body is not None else {}
    
    response = session.post(url, json=body, headers=headers, verify=False)
    printv(f"  < Status: {response.status_code}", verbose)
    try:
        printv(f"  < Response: {json.dumps(response.json(), indent=2)}", verbose)
    except json.JSONDecodeError:
        if response.text:
            printv(f"  < Response (text): {response.text}", verbose)
    return response

# --- unittest.TestCase Class ---

class SessionApiTests(unittest.TestCase):
    # No _global_verbose class variable needed, as it's read from env var in setUp

    @classmethod
    def setUpClass(cls):
        """Runs once before all tests in this class."""
        # This is where _global_verbose_api would already be set by run_tests.py
        # You can add debug prints here if needed:
        # verbose_status = os.environ.get('API_TEST_VERBOSE') == '1'
        # print(f"DEBUG: {cls.__name__}.setUpClass - API_TEST_VERBOSE from env: {verbose_status}")
        pass

    def setUp(self):
        """Runs before each test method."""
        # Read the verbose status from the environment variable
        self.verbose = os.environ.get('API_TEST_VERBOSE') == '1'
        if self.verbose:
            printc(f"\n--- Running Test: {self._testMethodName} ---", COLOR_PRIMARY)

        # Get an authenticated session for tests that require it
        self.session = get_authenticated_session(company=DEV_COMPANY, verbose=self.verbose)
        if not self.session:
            self.skipTest("Setup failed: Could not get authenticated session for DEV_COMPANY.")

    def tearDown(self):
        """Runs after each test method."""
        # Ensure the session is logged out, especially if the test itself didn't log out
        # or if it failed before logging out.
        if self.session:
            printv(f"\n--- CLEANUP: Logging out session for {self.session.cookies.get('username', 'N/A')} ---", self.verbose, COLOR_SECONDARY)
            try:
                # Use a fresh session for cleanup if self.session might be compromised
                # For logout, it's often fine to use the existing session unless the test
                # explicitly invalidates it in a way that prevents subsequent logout calls.
                # However, for maximum robustness, you could get a new auth session here.
                # For simplicity, we'll use the existing self.session.
                logout_response = logout_via_api(self.session, verbose=self.verbose)
                if logout_response.status_code == 200:
                    printv("  < CLEANUP: Session logged out successfully.", self.verbose, COLOR_SUCCESS)
                else:
                    # If logout fails (e.g., already logged out, invalid session), log a warning.
                    # Don't fail the tearDown unless it's a critical issue preventing further tests.
                    printc(f"  < CLEANUP: Failed to logout session. Status: {logout_response.status_code}", COLOR_WARN)
            except Exception as e:
                printc(f"  < CLEANUP ERROR: Exception during session logout: {e}", COLOR_FAIL)
        
        # Clear cookies locally to ensure absolute isolation for the next setUp,
        # even if logout API didn't clear them from the session object.
        if self.session:
            self.session.cookies.clear()


    # Test 1: Get Current Driver (Success)
    def test_1_get_current_driver_success(self):
        # Session is already created in setUp.
        try:
            printv(f"\n--- Attempting Get Current Driver (Valid) ---", self.verbose, COLOR_SECONDARY)
            response = get_current_driver_via_api(self.session, self.verbose)
            self.assertEqual(response.status_code, 200, f"Expected 200 OK, got {response.status_code}. Response: {response.text}")
            
            response_json = response.json()
            self.assertIn("user", response_json, "Response missing 'user' object")
            self.assertEqual(response_json["user"]["Username"], DEV_USERNAME, "Fetched user username mismatch")
            self.assertIn("companies", response_json, "Response missing 'companies' mapping")
            self.assertIn("modules", response_json, "Response missing 'modules' mapping")
            
            printc("Test Get Current Driver (Success) PASSED.", COLOR_SUCCESS)
        except Exception as e:
            printc(f"Test Get Current Driver (Success) FAILED: {e}", COLOR_FAIL)
            raise # Re-raise to let unittest mark as failed

    # Test 2: Get Current Driver (Missing Cookie - Unauthorized/BadRequest)
    def test_2_get_current_driver_missing_cookie(self):
        # Create a session with no cookies for this specific test
        session_no_cookies = requests.Session()
        session_no_cookies.cookies.clear()

        try:
            printv(f"\n--- Attempting Get Current Driver (Unauthorized) ---", self.verbose, COLOR_SECONDARY)
            response = get_current_driver_via_api(session_no_cookies, self.verbose)
            
            # The API has [Authorize] and then checks cookies.
            # If no cookie is sent, [Authorize] will return 401 Unauthorized.
            # If cookie is sent but empty, then the controller's BadRequest will trigger.
            # Assert for either 401 or 400.
            self.assertIn(response.status_code, [401, 400], f"Expected 401 or 400, got {response.status_code}. Response: {response.text}")
            
            if response.status_code == 400:
                response_json = response.json()
                self.assertIn("Username cookies is missing or empty", response_json["message"], "Bad Request message mismatch")
            # For 401, the body might be empty or a generic ProblemDetails from ASP.NET Core
            
            printc("Test Get Current Driver (Missing Cookie) PASSED.", COLOR_SUCCESS)
        except Exception as e:
            printc(f"Test Get Current Driver (Missing Cookie) FAILED: {e}", COLOR_FAIL)
            raise

    # Test 3: Logout (Success)
    def test_3_logout_success(self):
        # Session is already created in setUp.
        # Get the session ID from the token before we can log out
        access_token = self.session.cookies.get("access_token")
        if not access_token:
            self.fail("Access token cookie not found. Cannot proceed with logout test.")

        try:
            # Decode the access token (without verifying signature for test purposes)
            decoded_token = jwt.decode(access_token, options={"verify_signature": False})
            session_id = decoded_token.get("sessionId")
            
            if session_id is None:
                self.fail("Session ID claim not found in access token.")

            printv(f"\n--- Attempting Logout ---", self.verbose, COLOR_SECONDARY)

            # The C# endpoint takes a SessionModel body, so we'll pass one
            session_body = {
                "Username": DEV_USERNAME,
                "AccessToken": access_token,
                "RefreshToken": self.session.cookies.get("refresh_token")
            }
            
            response = logout_with_id_via_api(self.session, session_id, self.verbose, session_body)
            self.assertEqual(response.status_code, 200, f"Expected 200 OK, got {response.status_code}. Response: {response.text}")
            
            response_json = response.json()
            self.assertIn("Logged out successfully", response_json["message"], "Logout message mismatch")
            
            # Verify that all cookies are cleared
            self.assertNotIn("access_token", self.session.cookies, "Access token cookie was not cleared.")
            self.assertNotIn("refresh_token", self.session.cookies, "Refresh token cookie was not cleared.")
            
            printc("Test Logout with Session ID (Success) PASSED.", COLOR_SUCCESS)

        except Exception as e:
            printc(f"Test Logout with Session ID (Success) FAILED: {e}", COLOR_FAIL)
            raise

    # Test 4: Full Return and Refresh Process (Success)
    def test_4_return_session_success(self):
        """Test a successful POST to the new /return/{userId} endpoint."""
        # Get the session ID from the token before we can return
        access_token = self.session.cookies.get("access_token")
        if not access_token:
            self.fail("Access token cookie not found. Cannot proceed with return test.")
        
        try:
            # Decode the access token (without verifying signature for test purposes)
            decoded_token = jwt.decode(access_token, options={"verify_signature": False})
            session_id = decoded_token.get("sessionId")
            
            if session_id is None:
                self.fail("Session ID claim not found in access token.")
            
            printv(f"\n--- Attempting Valid Return to /return/{session_id} ---", self.verbose, COLOR_SECONDARY)
            response = return_session_via_api(self.session, session_id, self.verbose)

            self.assertEqual(response.status_code, 200, f"Expected 200 OK, got {response.status_code}. Response: {response.text}")
            
            response_json = response.json()
            self.assertIn("Returning, cookies extension completed successfully.", response_json["message"], "Return message mismatch")
            
            # Verify 'return' cookie is set (though its value is 'true', it's a flag)
            self.assertEqual(self.session.cookies.get("return"), "true", "Return cookie not set or not 'true'")

            printc("Test Return Session (Success) PASSED.", COLOR_SUCCESS)
        except Exception as e:
            printc(f"Test Return Session (Success) FAILED: {e}", COLOR_FAIL)
            raise

    # Test 5: Return Session (Unauthorized)
    def test_5_return_session_unauthorized(self):
        # Create a session with no cookies (unauthenticated) for this test
        session_no_cookies = requests.Session()
        session_no_cookies.cookies.clear()

        try:
            printv(f"\n--- Attempting Unauthorized Return ---", self.verbose, COLOR_SECONDARY)
            response = return_session_via_api(session_no_cookies, self.verbose)
            # This endpoint has [Authorize], so it should return 401 Unauthorized
            self.assertEqual(response.status_code, 401, f"Expected 401 Unauthorized, got {response.status_code}. Response: {response.text}")
            
            # For 401, the body might be empty or a generic ProblemDetails from ASP.NET Core
            # You can add more specific assertions if your 401 response has a predictable body.
            
            printc("Test Return Session (Unauthorized) PASSED.", COLOR_SUCCESS)
        except Exception as e:
            printc(f"Test Return Session (Unauthorized) FAILED: {e}", COLOR_FAIL)
            raise

# --- MAIN EXECUTION BLOCK ---
if __name__ == "__main__":
    # 1. Parse custom arguments first
    parser = argparse.ArgumentParser(description="Run API tests for AdminPortal Sessions Controller.")
    parser.add_argument(
        '-v_test', '--verbose',
        action='store_true',
        help='Enable verbose output for API requests and responses (for custom printv). Use -v for unittest verbosity.'
    )
    
    # Use parse_known_args to let unittest.main handle its own arguments
    args, unknown_args = parser.parse_known_args()

    # Set environment variable for direct execution too
    if args.verbose:
        os.environ['API_TEST_VERBOSE'] = '1'
    else:
        os.environ['API_TEST_VERBOSE'] = '0'

    # 2. Reconstruct sys.argv for unittest.main, removing custom arguments
    sys.argv = [sys.argv[0]] + unknown_args

    # 3. Run unittest.main() with the modified argv
    unittest.main()