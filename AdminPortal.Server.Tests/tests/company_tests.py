import unittest
import requests
import json
import argparse
import time # For slight delays if needed, though not strictly required here
import urllib
from urllib.parse import urlparse
import sys # Import sys for unittest.main(argv=...)
import os

from utils import (
    COLOR_DEFAULT, COLOR_SUCCESS, COLOR_FAIL, COLOR_PRIMARY, COLOR_SECONDARY, COLOR_WARN,
    BASE_API_URL, DEV_USERNAME, DEV_COMPANY, DEV_COMPANY_NAME,
    printc, printv, print_header, # print_header might be less useful in unittest, but keep for now
    get_authenticated_session, logout_via_api # Assuming logout_via_api is now in utils
)

# --- Company-Specific API Helper Functions ---

def update_company_via_api(session, new_company_name, verbose=False,
                           override_cookies_in_header=None, # dict: {'cookie_name': 'value'} for specific cookies
                           omit_cookies_from_header=None):  # list: ['cookie_name1', 'cookie_name2'] to exclude
    """
    Helper to call PUT /v1/companies/{newName} to update the company.
    Allows overriding/omitting specific cookies in the 'Cookie' header for the request.
    """
    url = f"{BASE_API_URL}/companies/{new_company_name}"
    
    current_company_key = session.cookies.get("company", "N/A (cookie missing)")
    printv(f"   > PUT {url} - Updating company (cookie: '{current_company_key}') to '{new_company_name}'", verbose)
    
    # --- Start Manual Cookie Header Construction ---
    headers_to_send = {**session.headers} # Start with any default session headers (e.g., User-Agent)
    
    cookie_parts = []
    omit_cookies_from_header = omit_cookies_from_header or [] # Ensure it's a list even if None
    override_cookies_in_header = override_cookies_in_header or {} # Ensure it's a dict even if None

    # 1. Add all cookies from the session's jar that are NOT explicitly omitted or overridden
    for cookie in session.cookies:
        if cookie.name not in omit_cookies_from_header and cookie.name not in override_cookies_in_header:
            cookie_parts.append(f"{cookie.name}={cookie.value}")
    
    # 2. Add the explicitly provided (override) cookies from the new parameter
    for name, value in override_cookies_in_header.items():
        cookie_parts.append(f"{name}={value}")
            
    # 3. Assemble the final 'Cookie' header string
    if cookie_parts:
        headers_to_send['Cookie'] = "; ".join(cookie_parts)
    elif 'Cookie' in headers_to_send: # If no cookies should be sent, ensure no old 'Cookie' header remains
        del headers_to_send['Cookie']
    # --- End Manual Cookie Header Construction ---

    # Make the request with the meticulously crafted headers
    response = session.put(url, headers=headers_to_send, verify=False)
    
    printv(f"   < Status: {response.status_code}", verbose)
    if verbose and response.status_code >= 400 and response.text:
        try:
            printv(f"   < Response: {json.dumps(response.json(), indent=2)}", verbose)
        except json.JSONDecodeError:
            printv(f"   < Response (text): {response.text}", verbose)
    return response

# --- unittest.TestCase Class ---

class CompanyApiTests(unittest.TestCase):
    #_global_verbose = False

    @classmethod
    def setUpClass(cls):
        """Runs once before all tests in this class."""
        # No class-level setup strictly needed here, but can be used for shared resources.
        #verbose_status = os.environ.get('API_TEST_VERBOSE') == '1'
        #print(f"DEBUG: {cls.__name__}.setUpClass - API_TEST_VERBOSE from env: {verbose_status}")
        pass

    def setUp(self):
        """Runs before each test method."""
        # Use the class-level _global_verbose_api set by argparse
        self.verbose = os.environ.get('API_TEST_VERBOSE') == '1'
        #print(f"DEBUG: {self.__class__.__name__}.setUp - self.verbose is: {self.verbose} (from env var)")
        if self.verbose:
            # unittest's default output already includes the test name when -v is used.
            # This custom print adds more detail/color if desired.
            printc(f"\n--- Running Test: {self._testMethodName} ---", COLOR_PRIMARY)

        self.session = get_authenticated_session(company=DEV_COMPANY, verbose=self.verbose)
        if not self.session:
            self.skipTest("Setup failed: Could not get authenticated session for DEV_COMPANY.")

        # Store original DEV_COMPANY display name for cleanup in specific tests
        # This assumes "Brauns Express Inc" from your code. Adjust if it's dynamic.
        self.original_dev_company_display_name = DEV_COMPANY_NAME 

    def tearDown(self):
        """Runs after each test method."""
        if self.session:
            logout_via_api(self.session, verbose=False) # Silent cleanup

    # Test 1: Update Company (Success)
    def test_1_update_company_success(self):
        # We will update DEV_COMPANY, so get a session authenticated for it.
        # Session is already created in setUp.
        
        company_key_in_cookie = DEV_COMPANY # e.g., "BRAUNS"
        temp_display_name = f"{self.original_dev_company_display_name}_TEMP_{time.time_ns() % 1000}" # Ensure unique temp name

        try:
            printv("\n--- Attempting To Valid Update (Ok [200]) ---", self.verbose, color=COLOR_SECONDARY)
            # Perform the update
            response = update_company_via_api(self.session, temp_display_name, self.verbose)
            self.assertEqual(response.status_code, 200, f"Expected 200 OK, got {response.status_code}. Response: {response.text}")
            
            response_json = response.json()
            self.assertIn("message", response_json, "Response missing 'message'")
            self.assertIn("success", response_json["message"].lower(), f"Unexpected message: {response_json['message']}")

            # Verify the cookie 'company_mapping' was updated (optional, but good)
            updated_company_mapping_cookie = self.session.cookies.get("company_mapping")
            self.assertIsNotNone(updated_company_mapping_cookie, "company_mapping cookie not found after update")
            try:
                decoded_cookie_value = urllib.parse.unquote(updated_company_mapping_cookie)
                mapping = json.loads(decoded_cookie_value)
            
                # It updates the value, not the key. The key (DEV_COMPANY) stays the same.
                self.assertEqual(mapping.get(company_key_in_cookie), temp_display_name, 
                                   f"Company mapping for '{company_key_in_cookie}' not updated correctly. Expected '{temp_display_name}', got '{mapping.get(company_key_in_cookie)}'")

            except json.JSONDecodeError:
                self.fail(f"Could not decode company_mapping cookie: {updated_company_mapping_cookie}")

            printc("Test Update Company (Success) PASSED.", COLOR_SUCCESS)
        except Exception as e:
            printc(f"Test Update Company (Success) FAILED: {e}", COLOR_FAIL)
            raise # Re-raise to let unittest mark as failed
        finally:
            # --- Cleanup: Revert the company name ---
            # This cleanup MUST be robust, as subsequent tests might depend on original company name
            printv(f"\n*** CLEANUP: Reverting company from '{temp_display_name}' back to '{self.original_dev_company_display_name}' ***", self.verbose, COLOR_PRIMARY)
            
            # The session used in the test might have its 'company_mapping' cookie updated locally,
            # but the actual API call needs the original 'company' key in its cookie for the PUT to work correctly.
            # get_authenticated_session will ensure the 'company' cookie is correct for the DEV_COMPANY.
            cleanup_session = get_authenticated_session(company=DEV_COMPANY, verbose=self.verbose) 
            if cleanup_session:
                printv("\n--- Attempting To Reset Company (Ok [200]) ---", self.verbose, color=COLOR_SECONDARY)
                revert_response = update_company_via_api(cleanup_session, self.original_dev_company_display_name, self.verbose)
                if revert_response.status_code == 200:
                    printv("  < CLEANUP: Company name reverted successfully.", self.verbose, COLOR_SUCCESS)
                else:
                    printc(f"  < CLEANUP: Failed to revert company name. Status: {revert_response.status_code}. Response: {revert_response.text}", COLOR_FAIL)
                    # If cleanup fails, raise an error to indicate a critical issue
                    self.fail(f"Critical Cleanup Failure: Could not revert company name for {DEV_COMPANY}")
                logout_via_api(cleanup_session, verbose=False) # Logout cleanup session
            else:
                printc("  < CLEANUP: Failed to get cleanup session. Company name may not be reverted.", COLOR_FAIL)
                self.fail("Critical Cleanup Failure: Could not get cleanup session to revert company name.")

    # Test 2: Update Company (Invalid New Name)
    def test_2_update_company_invalid_new_name(self):
        # Session is already created in setUp.
        invalid_new_name = "" # Empty string

        try:
            printv("\n--- Attempting To Update With Invalid Company Name (Method Not Allowed [405]) ---", self.verbose, color=COLOR_SECONDARY)
            response = update_company_via_api(self.session, invalid_new_name, self.verbose)
            
            # As noted, this typically results in 405 Method Not Allowed if the routing can't match an empty string segment.
            # If your API has server-side validation that catches empty string before routing, it might return 400.
            # Adjust expectation based on actual API behavior.
            self.assertEqual(response.status_code, 405, f"Expected 405 Method Not Allowed, got {response.status_code}. Response: {response.text}")
            
            # 405 often returns empty/generic body for this type of route mismatch,
            # so no specific JSON message assertion needed.
            printc("Test Update Company (Invalid New Name) PASSED.", COLOR_SUCCESS)
        except Exception as e:
            printc(f"Test Update Company (Invalid New Name) FAILED: {e}", COLOR_FAIL)
            raise

    # Test 3: Update Company (Missing 'company' Cookie)
    def test_3_update_company_missing_company_cookie(self):
        # We need a session where the 'company' cookie is explicitly absent.
        # get_authenticated_session creates it, so we'll use override_cookies_in_header.
        new_name_attempt = "SomeNewNameForMissingCookieTest"

        try:
            printv("\n--- Attempting To Update With No Active Company Cookie (Bad Request [400]) ---", self.verbose, color=COLOR_SECONDARY)
            # Use override_cookies_in_header to set 'company' to an empty string, effectively removing it from consideration
            # by the backend if it expects a value. Or, use omit_cookies_from_header=['company'].
            response = update_company_via_api(
                self.session, 
                new_name_attempt, 
                verbose=self.verbose,
                omit_cookies_from_header=['company'] # This is cleaner than setting to empty string if the goal is to omit
            )
            
            self.assertEqual(response.status_code, 400, f"Expected 400 Bad Request, got {response.status_code}. Response: {response.text}")
            
            response_json = response.json()
            # The message "Current company name was not found in cookies." seems specific to your backend.
            self.assertIn("Current company name was not found in cookies.", response_json["message"], "Bad Request message mismatch")
            printc("Test Update Company (Missing 'company' Cookie) PASSED.", COLOR_SUCCESS)
        except Exception as e:
            printc(f"Test Update Company (Missing 'company' Cookie) FAILED: {e}", COLOR_FAIL)
            raise

    # Test 4: Update Company (Company Not Found in DB, via cookie)
    def test_4_update_company_not_found_in_db(self):
        # Session already available from setUp.
        non_existent_company_key = "NONEXISTENT_XYZ_C4T" # Ensure it's unique enough
        new_name_attempt = "ValidNewNameForNonExistentTest"

        try:
            printv("\n--- Attempting To Update Non-Existent Company Key (Not Found [404]) ---", verbose=self.verbose, color=COLOR_SECONDARY)
            # Use override_cookies_in_header to inject a non-existent company key into the request.
            response = update_company_via_api(
                self.session, 
                new_name_attempt, 
                verbose=self.verbose,
                override_cookies_in_header={'company': non_existent_company_key} 
            )
            
            self.assertEqual(response.status_code, 404, f"Expected 404 Not Found, got {response.status_code}. Response: {response.text}")
            
            response_json = response.json()
            expected_message_part = f"Company with name '{non_existent_company_key}' not found."
            self.assertIn(expected_message_part, response_json["message"], f"Not Found message mismatch: {response_json['message']}")
            printc("Test Update Company (Company Not Found in DB) PASSED.", COLOR_SUCCESS)
        except Exception as e:
            printc(f"Test Update Company (Company Not Found in DB) FAILED: {e}", COLOR_FAIL)
            raise

    # Test 5: Update Company (Unauthorized)
    def test_5_update_company_unauthorized(self):
        # Create a session with no authentication
        session_unauth = requests.Session()
        session_unauth.cookies.clear() # Ensure no residual cookies

        try:
            printv("\n--- Attempting Unauthorized Company Update ---", verbose=self.verbose, color=COLOR_SECONDARY)
            new_name_attempt = "AnotherNewNameUnauthorized"
            # It's good to include the 'company' cookie even for unauthorized, as it mimics a real client trying to act.
            session_unauth.cookies.set('company', DEV_COMPANY, domain=urlparse(BASE_API_URL).netloc, path='/') 
            
            response = update_company_via_api(session_unauth, new_name_attempt, self.verbose)
            
            # Expected 401 due to [Authorize] attribute on the endpoint
            self.assertEqual(response.status_code, 401, f"Expected 401 Unauthorized, got {response.status_code}. Response: {response.text}")
            
            # 401 often returns an empty body or a generic Unauthorized message, so no specific JSON message check needed.
            printc("Test Update Company (Unauthorized) PASSED.", COLOR_SUCCESS)
        except Exception as e:
            printc(f"Test Update Company (Unauthorized) FAILED: {e}", COLOR_FAIL)
            raise

    # Test 6: Update Company (Name Conflict) - Requires another existing company (e.g., NTS)
    '''
    def test_6_update_company_name_conflict(self):
        # Session already available from setUp.
        company_to_update_key = DEV_COMPANY # This is "BRAUNS" for DEV_COMPANY
        
        # NOTE: This test will only pass if 'NTS' is guaranteed to exist and is a valid target name.
        # If your dev environment only has one company ("BRAUNS"), this test will cause issues
        # or require creating a second temporary company which complicates cleanup.
        # Based on your comment, let's include it but keep the warning.
        conflicting_existing_name = "NTS" # Assuming "NTS" is another existing company

        try:
            printv("\n\t--- Attempting To Rename To Name Already In Use (Conflict [409]) ---", self.verbose, COLOR_SECONDARY)
            response = update_company_via_api(self.session, conflicting_existing_name, self.verbose)
            self.assertEqual(response.status_code, 409, f"Expected 409 Conflict, got {response.status_code}. Response: {response.text}")
            
            response_json = response.json()
            self.assertIn("Company name already exists", response_json["message"], "Conflict message mismatch")
            printc("Test Update Company (Name Conflict) PASSED.", COLOR_SUCCESS)
        except Exception as e:
            printc(f"Test Update Company (Name Conflict) FAILED: {e}", COLOR_FAIL)
            raise
    '''

# --- MAIN EXECUTION BLOCK ---
if __name__ == "__main__":
    # 1. Parse our custom arguments first
    parser = argparse.ArgumentParser(description="Run API tests for AdminPortal Company Controller.")
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

    # 2. Reconstruct sys.argv for unittest.main, removing our custom arguments
    #    unittest.main() expects the script name as the first argument,
    #    followed by any test-related arguments it understands (like -v, --failfast, etc.)
    #    We append the unknown_args, which are likely unittest's own arguments.
    sys.argv = [sys.argv[0]] + unknown_args

    # 3. Run unittest.main() with the modified argv
    #    unittest.main() will then parse its own -v (verbosity) flag.
    unittest.main()