import unittest
import os
import sys
import argparse

# set sys path...
current_dir = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(current_dir, 'tests'))

# import log formatting helpers...
from utils import (
    COLOR_SUCCESS, COLOR_FAIL, COLOR_SECONDARY, printc
)

# import unittest classes...
#from tests.company_tests import CompanyApiTests
#from tests.user_tests import UserApiTests

def run_all_tests_with_unittest(verbose=False):
    printc("\n--- Starting all tests using unittest discovery ---\n", COLOR_SECONDARY)

    #CompanyApiTests._global_verbose = verbose
    #UserApiTests._global_verbose = verbose

    # Debug: Confirm they are set immediately after setting
    #print(f"DEBUG: run_tests.py - CompanyApiTests._global_verbose is now: {CompanyApiTests._global_verbose}")
    #print(f"DEBUG: run_tests.py - UserApiTests._global_verbose is now: {UserApiTests._global_verbose}")

    # Discover tests in the 'tests' directory
    # If your test files are in the same directory as this runner, use start_dir='.'
    test_loader = unittest.TestLoader()
    test_suite = test_loader.discover(start_dir='tests', pattern='*_tests.py')

    # You can also explicitly load suites from individual files if discovery is tricky
    # from tests.company_tests import run_all_tests as run_company_tests
    # from tests.another_test_file import run_all_tests as run_another_tests
    # suite = unittest.TestSuite()
    # suite.addTest(unittest.FunctionTestCase(run_company_tests))
    # suite.addTest(unittest.FunctionTestCase(run_another_tests))

    # Run the discovered tests
    runner = unittest.TextTestRunner(verbosity=2) # verbosity=2 for more detailed output
    results = runner.run(test_suite)

    if results.wasSuccessful():
        printc("All tests passed successfully!", COLOR_SUCCESS)
    else:
        printc("Some tests failed!", COLOR_FAIL)
        sys.exit(1) # Exit with a non-zero code to indicate failure

if __name__ == "__main__":
    # You might pass arguments to your individual test functions like verbose.
    # unittest's TextTestRunner handles verbosity, but if `run_all_tests`
    # in your test files also respects a `verbose` argument, you'd need to adapt.
    # For now, let's assume your individual run_all_tests functions don't strictly require
    # command-line args when called this way.

    # 1. Parse your custom verbose argument in the main runner file
    parser = argparse.ArgumentParser(description="Run all API tests.")
    parser.add_argument(
        '-v_test', '--verbose',
        action='store_true',
        help='Enable verbose output for API requests and responses within test files. Use -v for unittest verbosity'
    )

    args = parser.parse_args()

    if args.verbose:
        os.environ['API_TEST_VERBOSE'] = '1'
        #print("DEBUG: run_tests.py - API_TEST_VERBOSE environment variable set to '1'")
    else:
        os.environ['API_TEST_VERBOSE'] = '0'
        #print("DEBUG: run_tests.py - API_TEST_VERBOSE environment variable set to '0'")

    run_all_tests_with_unittest()