# HEAppE REST API Python Test Suite

This directory contains the automated REST API test suite for HEAppE built using **Python 3.12, `pytest`, and `schemathesis`**.

## Key Features

1. **Dynamic 100% Swagger Coverage (`schemathesis`)**:
   - Uses the **`SwaggerReference/RestApi-py4heappe.json`** specification (174 endpoints) or live URL `${HEAPPE_URL}/swagger/py4heappe/swagger.json`.
   - `schemathesis` dynamically discovers and tests 100% of defined OpenAPI endpoints, including any newly added or modified routes without needing manual test boilerplate.
2. **`X-Api-Key` Authentication**:
   - Requests propagate the `X-Api-Key` HTTP header.
3. **Sequential End-to-End Slurm Job Lifecycle (`tests/test_e2e_slurm_lifecycle.py`)**:
   - Verifies the complete workflow: Create Slurm Cluster -> Create Project -> Create CommandTemplate -> Submit Job to Slurm -> Monitor Execution -> Fetch Output Files -> Teardown & Clean up.
4. **Zero Compilation Overhead**:
   - Executes instantly via `pytest` without requiring build steps.

## Local Execution

### 1. Install Dependencies
```bash
cd Tests/RestApiTests
pip install -r requirements.txt
```

### 2. Run Tests
```bash
# Run all tests
pytest

# Run with verbose output and generate JUnit XML report
pytest -v --junitxml=TestResults.xml

# Run only the 100% OpenAPI coverage audit test
pytest -v -s -k "test_verify_100_percent_endpoint_coverage"

# Run only the sequential E2E Slurm job lifecycle scenario
pytest tests/test_e2e_slurm_lifecycle.py
```

## Environment Configuration

Configure using environment variables:
- `HEAPPE_URL`: Base URL of the HEAppE REST API (Default: `http://localhost:5000`)
- `HEAPPE_API_KEY`: Value for the `X-Api-Key` header (Default: `test-api-key`)
- `CLUSTER_NAME`: Name for the test Slurm cluster (Default: `SlurmTestCluster`)
- `PROJECT_NAME`: Name for the test project (Default: `TestProject`)
