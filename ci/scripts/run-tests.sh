#!/bin/bash
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/../.." && pwd)"
CI_DIR="$ROOT_DIR/ci"

echo "=========================================================="
echo "    HEAppE Middleware End-to-End Test & Coverage Runner    "
echo "=========================================================="

cd "$CI_DIR"

SKIP_DOCKER=false
FILTER=""

while [[ "$#" -gt 0 ]]; do
    case $1 in
        --unit-only) FILTER="Category!=Integration"; SKIP_DOCKER=true; shift ;;
        --integration-only) FILTER="Category=Integration"; shift ;;
        --skip-docker) SKIP_DOCKER=true; shift ;;
        --filter) FILTER="$2"; shift 2 ;;
        *) echo "Unknown option: $1"; exit 1 ;;
    esac
done

# Step 1: Start Docker infrastructure if needed
if [ "$SKIP_DOCKER" = false ]; then
    echo "==> Starting Docker Compose CI infrastructure..."
    docker compose -f docker-compose.ci.yml up -d --build mssql-ci slurm-ci sshnode-ci pbs-mock-ci qscheduler-ci
    
    echo "==> Waiting for all services to become healthy..."
    bash "$CI_DIR/scripts/wait-for-healthy.sh" 120
fi

# Step 2: Build solution
echo "==> Building HEAppE solution..."
cd "$ROOT_DIR"
dotnet build "HEAppE Core.sln" -c Release

# Step 3: Run tests with coverage
echo "==> Executing test suites with Coverlet coverage instrumentation..."
mkdir -p "$ROOT_DIR/TestResults"

TEST_ARGS=(
    --configuration Release
    --no-build
    --logger "trx;LogFileName=test_results.trx"
    --logger "console;verbosity=normal"
    --collect:"XPlat Code Coverage"
    --settings "$CI_DIR/coverage.runsettings"
    --results-directory "$ROOT_DIR/TestResults"
)

if [ -n "$FILTER" ]; then
    TEST_ARGS+=(--filter "$FILTER")
fi

# Run tests on solution (all test projects)
dotnet test "HEAppE Core.sln" "${TEST_ARGS[@]}" || TEST_FAILED=true

# Step 4: Generate HTML and combined Cobertura report
if command -v reportgenerator &> /dev/null; then
    echo "==> Generating HTML code coverage report..."
    reportgenerator \
        -reports:"$ROOT_DIR/TestResults/**/coverage.cobertura.xml" \
        -targetdir:"$ROOT_DIR/TestResults/CoverageReport" \
        -reporttypes:"Html;Cobertura;TextSummary"
    
    if [ -f "$ROOT_DIR/TestResults/CoverageReport/Summary.txt" ]; then
        echo "==> Coverage Summary:"
        cat "$ROOT_DIR/TestResults/CoverageReport/Summary.txt"
    fi
else
    echo "NOTE: reportgenerator not installed locally. Coverage XMLs collected in TestResults/"
fi

echo "=========================================================="
if [ "$TEST_FAILED" = true ]; then
    echo "❌ Some tests failed!"
    exit 1
else
    echo "✅ All tests passed successfully!"
fi
echo "=========================================================="
