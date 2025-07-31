import unittest
import requests
import json
import argparse
import uuid
import sys
import os

from utils import (
    COLOR_DEFAULT, COLOR_SUCCESS, COLOR_FAIL, COLOR_PRIMARY, COLOR_SECONDARY, COLOR_WARN,
    BASE_API_URL, DEV_USERNAME, DEV_COMPANY, DEV_COMPANY_NAME, # api/user presets...
    printc, printv, print_header,
    get_authenticated_session, generate_unique_user_data, logout_via_api # log and session helpers...
)

''' User-Specific API Helper Functions (kept as standalone functions) '''

def create_user_via_api(session, user_data, verbose=False):
    """Helper to call POST /v1/users and return the response."""
    url = f"{BASE_API_URL}/users"
    printv(f"  > POST {url} - Creating user: {user_data['Username']}", verbose)
    response = session.post(url, json=user_data, verify=False)
    printv(f"  < Status: {response.status_code}", verbose)
    if verbose and response.status_code >= 400 and response.text:
        try:
            printv(f"  < Response: {json.dumps(response.json(), indent=2)}", verbose)
        except json.JSONDecodeError:
            printv(f"  < Response (text): {response.text}", verbose)
    return response

def get_user_via_api(session, username, verbose=False):
    """Helper to call GET /v1/users/{username} and return the response."""
    url = f"{BASE_API_URL}/users/{username}"
    printv(f"  > GET {url} - Getting user: {username}", verbose)
    response = session.get(url, verify=False)
    printv(f"  < Status: {response.status_code}", verbose)
    if verbose and response.status_code >= 400 and response.text:
        try:
            printv(f"  < Response: {json.dumps(response.json(), indent=2)}", verbose)
        except json.JSONDecodeError:
            printv(f"  < Response (text): {response.text}", verbose)
    return response

def update_user_via_api(session, prev_username, new_user_data, verbose=False):
    """Helper to call PUT /v1/users/{username} and return the response."""
    url = f"{BASE_API_URL}/users/{prev_username}"
    printv(f"  > PUT {url} - Updating user '{prev_username}' to '{new_user_data['Username']}'", verbose)
    response = session.put(url, json=new_user_data, verify=False)
    printv(f"  < Status: {response.status_code}", verbose)
    if verbose and response.status_code >= 400 and response.text:
        try:
            printv(f"  < Response: {json.dumps(response.json(), indent=2)}", verbose)
        except json.JSONDecodeError:
            printv(f"  < Response (text): {response.text}", verbose)
    return response

def delete_user_via_api(session, username, verbose=False, expect_fail=False):
    """Helper to call DELETE /v1/users/{username} and return the response."""
    url = f"{BASE_API_URL}/users/{username}"
    printv(f"  > DELETE {url} - Deleting user: {username}", verbose)
    response = session.delete(url, verify=False)
    printv(f"  < Status: {response.status_code}", verbose)
    if verbose and response.status_code != 204: # Delete usually returns 204 No Content
        if not expect_fail or verbose: # Only print response body if not expecting failure or if verbose
            try:
                printv(f"  < Response: {json.dumps(response.json(), indent=2)}", verbose)
            except json.JSONDecodeError:
                printv(f"  < Response (text): {response.text}", verbose)
    return response

class UserApiTests(unittest.TestCase):
    #_global_verbose = False

    @classmethod
    def setUpClass(cls):
        """Runs once before all tests in this class."""
        # You can perform class-level setup here if needed, e.g., creating a shared resource.
        #verbose_status = os.environ.get("API_TEST_VERBOSE") == '1'
        #print(f"DEBUG: {cls.__name__}.setUpClass - API_TEST_VERBOSE from env: {verbose_status}")
        pass

    def setUp(self):
        """Runs before each test method."""
        # Use the class-level _global_verbose_api set by argparse
        self.verbose = os.environ.get("API_TEST_VERBOSE") == '1'
        #print(f"DEBUG: {self.__class__.__name__}.setUp - self.verbose is: {self.verbose} (from env var)")
        if self.verbose:
            # unittest's default output already includes the test name when -v is used.
            # This custom print adds more detail/color if desired.
            printc(f"\n--- Running Test: {self._testMethodName} ---", COLOR_PRIMARY)

        self.session = get_authenticated_session(company=DEV_COMPANY, verbose=self.verbose)
        if not self.session:
            self.skipTest("Setup failed: Could not get authenticated session for DEV_COMPANY.")

        self.users_to_cleanup = set() # sets cleanly handle duplicates...

    def tearDown(self):
        """Runs after each test method."""
        cleanup_session = None
        try:
            cleanup_session = get_authenticated_session(company=DEV_COMPANY, verbose=False)
            if not cleanup_session:
                printc("WARNING: Failed to get cleanup session. Lingering users may remain.", COLOR_WARN)
                return # Can't clean up without a session
        except Exception as e:
            printc(f"WARNING: Exception getting cleanup session: {e}. Lingering users may remain.", COLOR_WARN)
            return

        for username in list(self.users_to_cleanup): # Iterate over a copy, as items might be removed
            printv(f"\n--- CLEANUP: Attempting to delete user '{username}' ---", self.verbose, COLOR_SECONDARY)
            try:
                # Use expect_fail=True in cleanup if you anticipate 404 for already deleted users
                delete_response = delete_user_via_api(cleanup_session, username, verbose=self.verbose, expect_fail=True)
                if delete_response.status_code == 204: # No Content - successful delete
                    printv(f"  < CLEANUP: User '{username}' deleted successfully.", self.verbose, COLOR_SUCCESS)
                elif delete_response.status_code == 404: # Not Found - user already gone
                    printv(f"  < CLEANUP: User '{username}' not found (likely already deleted).", self.verbose, COLOR_WARN)
                else:
                    printc(f"  < CLEANUP: Failed to delete user '{username}'. Status: {delete_response.status_code}. Response: {delete_response.text}", COLOR_FAIL)
            except Exception as e:
                printc(f"  < CLEANUP ERROR: Exception while deleting user '{username}': {e}", COLOR_FAIL)
        
        if cleanup_session: # Logout the cleanup session
            logout_via_api(cleanup_session, verbose=False)

        # Always logout the main test session at the end of tearDown
        if self.session:
            logout_via_api(self.session, verbose=False) # Silent cleanup

    # Helper to create user and track in cleanup list...
    def _create_and_track_user(self, user_data):
        response = create_user_via_api(self.session, user_data, self.verbose)
        if response.status_code == 201:
            self.users_to_cleanup.add(user_data["Username"])
        return response
    
    # Helper to update user and track in cleanup list...
    def _update_and_track_user(self, prev_username, new_user_data):
        response = update_user_via_api(self.session, prev_username, new_user_data, self.verbose)
        if response.status_code == 200: # Assuming 200 OK for successful update
            # If the username was changed, remove old and add new to cleanup list
            if prev_username in self.users_to_cleanup and prev_username != new_user_data["Username"]:
                self.users_to_cleanup.remove(prev_username)
                self.users_to_cleanup.add(new_user_data["Username"])
            elif prev_username not in self.users_to_cleanup:
                # If for some reason prev_username wasn't tracked, but update succeeded, track the new one
                self.users_to_cleanup.add(new_user_data["Username"])
        return response
    
    # Helper to delete user and remove from cleanup list...
    def _delete_and_untrack_user(self, username, expect_fail=False):
        response = delete_user_via_api(self.session, username, self.verbose, expect_fail)
        if response.status_code == 204 or (expect_fail and response.status_code == 404):
            # Only remove from tracking if deletion was expected to succeed or user was already not found
            if username in self.users_to_cleanup:
                self.users_to_cleanup.remove(username)
        return response

    # Test 1: Create a new user (Success)
    def test_1_create_user_success(self):
        # generate user data...
        user_data = generate_unique_user_data("newuser")
        try:
            printv("\n--- Attempting User Creation (Valid) ---", self.verbose, COLOR_SECONDARY)
            # generate new user in db...
            response = self._create_and_track_user(user_data)
            self.assertEqual(response.status_code, 201, f"Expected 201 Created, got {response.status_code}. Response: {response.text}")

            # parse response...
            response_json = response.json()

            # assert Username
            self.assertIn("Username", response_json, "Response (user object) is missing Username")
            self.assertEqual(response_json["Username"], user_data["Username"], "Created user username mismatch")

            # assert Permissions (inactive at this time)

            # assert Powerunit
            self.assertIn("Powerunit", response_json, "Response (user object) is missing Powerunit")
            self.assertEqual(response_json["Powerunit"], user_data["Powerunit"], "Created user powerunit mismatch")

            # assert ActiveCompany
            self.assertIn("ActiveCompany", response_json, "Response (user object) is missing ActiveCompany")
            self.assertEqual(response_json["ActiveCompany"], user_data["ActiveCompany"], "Created user ActiveCompany mismatch")

            # assert Companies (list comparison)
            self.assertIn("Companies", response_json, "Response (user object) is missing Companies")
            self.assertIsInstance(response_json["Companies"], list, "Companies field is not a list")
            self.assertListEqual(sorted(response_json["Companies"]), sorted(user_data["Companies"]), "Created user Companies mismatch")

            # assert Modules (list comparison)
            self.assertIn("Modules", response_json, "Response (user object) is missing Modules")
            self.assertIsInstance(response_json["Modules"], list, "Modules field is not a list")
            self.assertListEqual(sorted(response_json["Modules"]), sorted(user_data["Modules"]), "Created user Modules mismatch")

            printc("Test Create User (Success) PASSED.", COLOR_SUCCESS)
        except Exception as e:
            printc(f"Test Create User (Success) FAILED: {e}", COLOR_FAIL)
            raise # Re-raise to let unittest mark as failed

    # Test 2: Creating a Duplicate Username (Conflict)
    def test_2_create_user_duplicate_username(self):
        user_data = generate_unique_user_data("duplicate")

        # generate new user in db...
        printv("\n--- Attempting User Creation (Valid) ---", self.verbose, COLOR_SECONDARY)
        create_response = self._create_and_track_user(user_data) # Create it first (silently)
        self.assertEqual(create_response.status_code, 201, f"Expected 201 Created, got {create_response.status_code}. Response: {create_response.text}")

        try:
            printv("\n--- Attempting Duplicate User Creation ---", self.verbose, COLOR_SECONDARY)
            # generate duplicate user in db...
            duplicate_response = self._create_and_track_user(user_data)
            self.assertEqual(duplicate_response.status_code, 409, f"Expected 409 Conflict, got {duplicate_response.status_code}. Response: {duplicate_response.text}")

            # parse response...
            response_json = duplicate_response.json()
            self.assertIn("message", response_json, "Response missing 'message'")
            self.assertIn("Username is already in use", response_json["message"], "Duplicate message mismatch")

            printc("Test Create User (Duplicate Username) PASSED.", COLOR_SUCCESS)
        except Exception as e:
            printc(f"Test Create User (Duplicate Username) FAILED: {e}", COLOR_FAIL)
            raise

    # Test 3: Creating User w/ Invalid Data (Conflict)
    def test_3_create_user_invalid_data(self):
        invalid_user_data = {
            "Username": "", # Invalid: empty username
            "Password": "pw", # Invalid: too short
            "Powerunit": "abc",
            "ActiveCompany": "BRAUNS",
            "Companies": ["BRAUNS"],
            "Modules": ["admin"]
        }

        try:
            printv("\n--- Attempting User Creation (Invalid Data) ---", self.verbose, COLOR_SECONDARY)
            # generate new user in db...
            response = self._create_and_track_user(invalid_user_data)
            self.assertEqual(response.status_code, 400, f"Expected 400 Bad Request, got {response.status_code}. Response: {response.text}")

            # parse response...
            response_json = response.json()

            self.assertIn("message", response_json, "Response missing 'message'")
            self.assertIn("required.", response_json["message"], "Username error message mismatch")

            # expand to include additional assertions...
            #self.assertIn("Password must be at least 8 characters long", response_json.get("errors", {}).get("Password", [""])[0], "Password error message mismatch")
            
            printc("Test Create User (Invalid User Data) PASSED.", COLOR_SUCCESS)
        except Exception as e:
            printc(f"Test Create User (Invalid User Data) FAILED: {e}", COLOR_FAIL)
            raise

    # Test 4: Fetch User w/ Valid Data (Success)
    def test_4_get_user_success(self):
        user_data = generate_unique_user_data("gettest")

        printv("\n--- Attempting User Creation (Valid) ---", self.verbose, COLOR_SECONDARY)
        # generate new user in db...
        create_response = self._create_and_track_user(user_data)
        self.assertEqual(create_response.status_code, 201, f"Expected 201 Created, got {create_response.status_code}. Response: {create_response.text}")

        try:
            printv("\n--- Attempting Get User (Valid) ---", self.verbose, COLOR_SECONDARY)
            # fetch user from db...
            response = get_user_via_api(self.session, user_data["Username"], self.verbose)
            self.assertEqual(response.status_code, 200, f"Expected 200 OK, got {response.status_code}. Response: {response.text}")

            # parse response...
            response_json = response.json()
            self.assertEqual(response_json["Username"], user_data["Username"], "Fetched username mismatch")
            self.assertEqual(response_json["Powerunit"], user_data["Powerunit"], "Fetched powerunit mismatch")

            printc("Test Get User (Success) PASSED.", COLOR_SUCCESS)
        except Exception as e:
            printc(f"Test Get User (Success) FAILED: {e}", COLOR_FAIL)
            raise

    # Test 5: Get a User D.N.E (Not Found)
    def test_5_get_user_not_found(self):
        non_existent_username = "nonexistent_user_12345"

        try:
            printv("\n--- Attempting Get User (Not Found) ---", self.verbose, COLOR_SECONDARY)
            # fetch user from db...
            response = get_user_via_api(self.session, non_existent_username, self.verbose)
            self.assertEqual(response.status_code, 404, f"Expected 404 Not Found, got {response.status_code}. Response: {response.text}")
            self.assertIn(f"User with username '{non_existent_username}' not found.", response.text, "Not Found message mismatch")

            printc("Test Get User (Not Found) PASSED.", COLOR_SUCCESS)
        except Exception as e:
            printc(f"Test Get User (Not Found) FAILED: {e}", COLOR_FAIL)
            raise

    # Test 6: Update User (Success)
    def test_6_update_user_success(self):
        user_data = generate_unique_user_data("updatetest")

        # generate new user in db...
        printv("\n--- Attempting User Creation (Valid) ---", self.verbose, COLOR_SECONDARY)
        create_response = self._create_and_track_user(user_data)
        self.assertEqual(create_response.status_code, 201, f"Expected 201 Created, got {create_response.status_code}. Response: {create_response.text}")

        # init update data...
        unique_id = uuid.uuid4().hex[:8]
        updated_user_data = {
            **user_data, 
            "Username": f"{user_data['Username']}_{unique_id}",
            "Powerunit": f"{unique_id[:3]}"
        }
        updated_user_data["Companies"].append("TCS")
        updated_user_data["Modules"].remove("admin")
        
        try:
            printv("\n--- Attempting User Update (Valid) ---", self.verbose, COLOR_SECONDARY)
            # update user in db...
            response = self._update_and_track_user(user_data["Username"], updated_user_data)
            self.assertEqual(response.status_code, 200, f"Expected 200 OK, got {response.status_code}. Response: {response.text}")

            # parse response...
            response_json = response.json()

            # assert response is dictionary...
            self.assertIsInstance(response_json, dict)

            # assert key and values are present and valid...
            expected_keys = {
                "Username": str,
                "Password": str,
                "Powerunit": str,
                "Companies": list,
                "Modules": list
            }
            for key, expected_type in expected_keys.items():
                assert key in response_json, f"Missing key: {key}"
                assert isinstance(response_json[key], expected_type), f"{key} should be of type {expected_type.__name__}"
            
            # fetching the user again to verify...
            get_response = get_user_via_api(self.session, updated_user_data["Username"], verbose=False) # Silent get
            self.assertEqual(get_response.status_code, 200, "Failed to fetch user after update for verification.")

            # parse response...
            get_response_json = get_response.json()

            # assert updates were correctly set...
            self.assertEqual(get_response_json["Username"], f"{user_data['Username']}_{unique_id}", "Username not updated")
            self.assertEqual(get_response_json["Powerunit"], f"{unique_id[:3]}", "Powerunit not updated")
            self.assertIn("TCS", get_response_json["Companies"], "Updated companies list mismatch")
            self.assertNotIn("admin", get_response_json["Modules"], "Updated modules list mismatch")
            
            printc("Test Update User (Success) PASSED.", COLOR_SUCCESS)
        except Exception as e:
            printc(f"Test Update User (Success) FAILED: {e}", COLOR_FAIL)
            raise

    # Test 7: Update Non-Existent User (Not Found)
    def test_7_update_user_not_found(self):
        non_existent_username = "nonexistent_user_for_update"
        dummy_update_data = generate_unique_user_data(non_existent_username)

        try:
            printv("\n--- Attempting User Update (Not Found) ---", self.verbose, COLOR_SECONDARY)
            response = self._update_and_track_user(non_existent_username, dummy_update_data)
            self.assertEqual(response.status_code, 404, f"Expected 404 Not Found, got {response.status_code}. Response: {response.text}")
            #printc(f"response: {response}", COLOR_SUCCESS)

            self.assertIn(f"User with username '{non_existent_username}' not found.", response.text, "Not Found message mismatch")
            printc("Test Update User (Not Found) PASSED.", COLOR_SUCCESS)
        except Exception as e:
            printc(f"Test Update User (Not Found) FAILED: {e}", COLOR_FAIL)
            raise

    # Test 8: Update User w/ Duplicate Conflict (Conflict)
    def test_8_update_user_username_conflict(self):
        # Create two users: one to update, one to cause conflict
        printv("\n--- Attempting User Creations (Valid 2x) ---", self.verbose, COLOR_SECONDARY)
        user_to_update_data = generate_unique_user_data("update_this")
        conflict_username_data = generate_unique_user_data("conflict_with_this")
        while user_to_update_data["Powerunit"] == conflict_username_data["Powerunit"]:
            conflict_username_data = generate_unique_user_data("conflict_with_this")

        # Create users in DB...
        create_resp1 = self._create_and_track_user(user_to_update_data)
        create_resp2 = self._create_and_track_user(conflict_username_data)
        self.assertTrue(create_resp1.status_code == 201 and create_resp2.status_code == 201, "Setup failed for update conflict test.")

        # Try to update user_to_update's username to conflict_username
        new_data = user_to_update_data.copy()
        new_data["Username"] = conflict_username_data["Username"]

        try:
            printv("\n--- Attempting User Update (Username Conflict) ---", self.verbose, COLOR_SECONDARY)
            response = self._update_and_track_user(user_to_update_data["Username"], new_data)
            self.assertEqual(response.status_code, 409, f"Expected 409 Conflict, got {response.status_code}. Response: {response.text}")
            #printc(f"response: {response}", COLOR_SUCCESS)
            
            response_json = response.json()
            self.assertIn("Username is already in use", response_json.get("message", ""), "Conflict message mismatch")
            printc("Test Update User (Username Conflict) PASSED.", COLOR_SUCCESS)
        except Exception as e:
            printc(f"Test Update User (Username Conflict) FAILED: {e}", COLOR_FAIL)
            raise
        finally:
            # Clean up both users
            self._delete_and_untrack_user(user_to_update_data["Username"])
            self._delete_and_untrack_user(conflict_username_data["Username"])

    # Test 9: Delete User (Success)
    def test_9_delete_user_success(self):
        user_data = generate_unique_user_data("deletetest")

        # generate new user in db...
        printv("\n--- Attempting User Creation (Valid) ---", self.verbose, COLOR_SECONDARY)
        create_response = self._create_and_track_user(user_data)
        self.assertEqual(create_response.status_code, 201, "Setup failed: Could not create user for delete test.")

        try:
            printv("\n--- Attempting User Deletion (Valid) ---", self.verbose, COLOR_SECONDARY)
            response = self._delete_and_untrack_user(user_data["Username"])
            self.assertEqual(response.status_code, 204, f"Expected 204 No Content, got {response.status_code}. Response: {response.text}")
            #printc(f"response: {response}", COLOR_SUCCESS)

            # Verify deletion by trying to get the user again
            get_response = get_user_via_api(self.session, user_data["Username"], verbose=False)
            self.assertEqual(get_response.status_code, 404, "User found after deletion, expected 404")
            #printc(f"get_response: {get_response}", COLOR_SUCCESS)

            printc("Test Delete User (Success) PASSED.", COLOR_SUCCESS)
        except Exception as e:
            printc(f"Test Delete User (Success) FAILED: {e}", COLOR_FAIL)
            raise

    # Test 10: Delete Non-Existent User (Not Found)
    def test_10_delete_user_not_found(self):
        non_existent_username = "nonexistent_user_for_delete"

        try:
            printv("\n--- Attempting User Deletion (Not Found) ---", self.verbose, COLOR_SECONDARY)
            response = self._delete_and_untrack_user(non_existent_username, expect_fail=True)
            self.assertEqual(response.status_code, 404, f"Expected 404 Not Found, got {response.status_code}. Response: {response.text}")
            #printc(f"response: {response}", COLOR_SUCCESS)
            #printc(f"response.text: {response.text}", COLOR_SUCCESS)

            self.assertIn(f"User with username '{non_existent_username}' not found.", response.text, "Not Found message mismatch")
            printc("Test Delete User (Not Found) PASSED.", COLOR_SUCCESS)
        except Exception as e:
            printc(f"Test Delete User (Not Found) FAILED: {e}", COLOR_FAIL)
            raise

    # Test 11: Delete Self (Conflict)
    def test_11_delete_current_user_conflict(self):
        try:
            # attempt to delete the user whose cookie is currently active...
            printv("\n--- Attempting Delete Current User (Conflict) ---", self.verbose, COLOR_SECONDARY)
            response = self._delete_and_untrack_user(DEV_USERNAME, expect_fail=True)
            self.assertEqual(response.status_code, 409, f"Expected 409 Conflict, got {response.status_code}. Response: {response.text}")
            #printc(f"response: {response}", COLOR_SUCCESS)

            # parse response...
            response_json = response.json()
            self.assertIn("cannot be deleted while in use", response_json.get("message", ""), "Conflict message mismatch for active user deletion")
            #printc(f"response_json: {response_json}", COLOR_SUCCESS)

            printc("Test Delete Current User (Conflict) PASSED.", COLOR_SUCCESS)
        except Exception as e:
            printc(f"Test Delete Current User (Conflict) FAILED: {e}", COLOR_FAIL)
            raise

    def test_12_unauthorized_access(self):
        # Create a session with no authentication
        session_unauth = requests.Session()
        session_unauth.cookies.clear() # Ensure no residual cookies

        try:
            printv("\n--- Attempting Unauthorized User Access (GET /users/{username}) ---", self.verbose, COLOR_SECONDARY)
            response = get_user_via_api(session_unauth, DEV_USERNAME, self.verbose)
            self.assertEqual(response.status_code, 401, f"Expected 401 Unauthorized, got {response.status_code}. Response: {response.text}")
            printc("Test Unauthorized Access PASSED.", COLOR_SUCCESS)
        except Exception as e:
            printc(f"Test Unauthorized Access FAILED: {e}", COLOR_FAIL)
            raise

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