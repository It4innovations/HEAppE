#!/bin/bash
set -e

export PATH="$PATH:$HOME/.dotnet/tools:/root/.dotnet/tools"
export NUGET_PACKAGES="/root/.nuget/packages"

cleanup_permissions() {
    echo "==> Ensuring workspace permissions for host runner..."
    chmod -R 777 /workspace/TestResults 2>/dev/null || true
    rm -rf /workspace/.nuget 2>/dev/null || true
}
trap cleanup_permissions EXIT

echo "=========================================================="
echo "    HEAppE Middleware CI Phased Test & Coverage Runner"
echo "=========================================================="

cd /workspace

# Clean up any legacy .nuget folder in workspace
rm -rf /workspace/.nuget 2>/dev/null || true

echo "==> Restoring solution..."
dotnet restore "HEAppE Core.sln"

echo "==> Building solution..."
dotnet build "HEAppE Core.sln" -c Release --no-restore

mkdir -p /workspace/TestResults

echo "==> Waiting for mssql-ci database readiness..."
for i in {1..30}; do
    if nc -z mssql-ci 1433 2>/dev/null || (exec 6<>/dev/tcp/mssql-ci/1433) 2>/dev/null; then
        echo "Database mssql-ci is ready!"
        break
    fi
    echo "Waiting for mssql-ci:1433... ($i/30)"
    sleep 2
done

start_section() {
    local id="$1"
    local title="$2"
    echo -e "\e[0Ksection_start:$(date +%s):${id}[collapsed=false]\r\e[0K"
    echo -e "\033[1;36m=========================================================="
    echo -e "==> [PHASE] ${title}"
    echo -e "==========================================================\033[0m"
}

end_section() {
    local id="$1"
    echo -e "\e[0Ksection_end:$(date +%s):${id}\r\e[0K"
}

OVERALL_EXIT_CODE=0

run_phase() {
    local phase_id="$1"
    local phase_title="$2"
    local filter="$3"

    start_section "$phase_id" "$phase_title"
    set +e
    dotnet test "HEAppE Core.sln" \
        -c Release \
        --no-build \
        --filter "$filter" \
        --collect:"XPlat Code Coverage" \
        --settings ci/coverage.runsettings \
        --logger "junit;LogFilePath=/workspace/TestResults/${phase_id}_{assembly}_junit.xml" \
        --logger "trx;LogFileName=${phase_id}_{assembly}_results.trx" \
        --logger "console;verbosity=normal" \
        --results-directory /workspace/TestResults
    local exit_code=$?
    set -e

    if [ $exit_code -ne 0 ]; then
        echo -e "\033[1;31m❌ Phase '${phase_title}' FAILED (Exit Code: $exit_code)\033[0m"
        OVERALL_EXIT_CODE=$exit_code
    else
        echo -e "\033[1;32m✅ Phase '${phase_title}' PASSED\033[0m"
    fi
    end_section "$phase_id"
}

# Phase 1: Unit & Cryptography Tests
run_phase "phase1_unit" "Phase 1: Unit & Cryptography Tests" "Category!=Integration"

# Phase 2: Core API, Auth & Job Lifecycle Integration Tests
run_phase "phase2_api_core" "Phase 2: Core API, Authentication & Schedulers" "Category=Integration&FullyQualifiedName!~Management"

# Phase 3: Management CRUD Integration Tests
run_phase "phase3_management_crud" "Phase 3: Management CRUD Full Lifecycle Suite" "FullyQualifiedName~Management"

start_section "coverage_report" "Generating Code Coverage Reports"
if command -v reportgenerator &> /dev/null || [ -f "/root/.dotnet/tools/reportgenerator" ]; then
    REPORTGEN_BIN=$(command -v reportgenerator || echo "/root/.dotnet/tools/reportgenerator")
    rm -rf /workspace/TestResults/CoverageReport
    "$REPORTGEN_BIN" \
        -reports:"/workspace/TestResults/**/coverage.cobertura.xml" \
        -targetdir:"/workspace/TestResults/CoverageReport" \
        -reporttypes:"Html;Cobertura;TextSummary;Badges" \
        -assemblyfilters:"+RestApi;+BusinessLogicTier;+DataAccessTier;+ServiceTier;+CertificateGenerator;+DomainObjects;+ExtModels;+RestApiModels;+HpcConnectionFramework;+FileTransferFramework;+BackgroundThread;-*Test*;-*Tests*;-*.Migrations" \
        -classfilters:"-*Migrations*;-*Snapshot*" \
        -filefilters:"-*Migrations*" || true

    if [ -f "/workspace/TestResults/CoverageReport/Summary.txt" ]; then
        echo "==> Coverage Summary:"
        cat /workspace/TestResults/CoverageReport/Summary.txt
    fi

    # Clean up raw coverlet XML directories to prevent artifact bloat (saves multiple gigabytes)
    find /workspace/TestResults -mindepth 1 -maxdepth 1 -type d ! -name "CoverageReport" -exec rm -rf {} + 2>/dev/null || true
else
    echo "WARNING: reportgenerator binary not found in PATH or /root/.dotnet/tools"
fi
end_section "coverage_report"

# Ensure host gitlab-runner user can access and clean up TestResults
chmod -R 777 /workspace/TestResults 2>/dev/null || true
rm -rf /workspace/.nuget 2>/dev/null || true

if [ $OVERALL_EXIT_CODE -ne 0 ]; then
    echo -e "\033[1;31m❌ Test suite failed with exit code $OVERALL_EXIT_CODE\033[0m"
    exit $OVERALL_EXIT_CODE
else
    echo -e "\033[1;32m🎉 All test phases completed successfully!\033[0m"
fi
