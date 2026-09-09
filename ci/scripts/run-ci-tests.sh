#!/bin/bash
set -e

export PATH="$PATH:$HOME/.dotnet/tools:/root/.dotnet/tools"

echo "=========================================================="
echo "    HEAppE Middleware CI Test & Coverage Runner"
echo "=========================================================="

cd /workspace

echo "==> Restoring solution..."
dotnet restore "HEAppE Core.sln"

echo "==> Building solution..."
dotnet build "HEAppE Core.sln" -c Release --no-restore

mkdir -p /workspace/TestResults

echo "==> Running tests with coverage..."
set +e
dotnet test "HEAppE Core.sln" \
    -c Release \
    --no-build \
    --collect:"XPlat Code Coverage" \
    --settings ci/coverage.runsettings \
    --logger "trx;LogFileName=test_results.trx" \
    --logger "console;verbosity=normal" \
    --results-directory /workspace/TestResults
TEST_EXIT_CODE=$?
set -e

echo "==> Generating coverage reports with reportgenerator..."
if command -v reportgenerator &> /dev/null || [ -f "/root/.dotnet/tools/reportgenerator" ]; then
    REPORTGEN_BIN=$(command -v reportgenerator || echo "/root/.dotnet/tools/reportgenerator")
    "$REPORTGEN_BIN" \
        -reports:"/workspace/TestResults/**/coverage.cobertura.xml" \
        -targetdir:"/workspace/TestResults/CoverageReport" \
        -reporttypes:"Html;Cobertura;TextSummary;Badges" \
        -assemblyfilters:"+RestApi;+BusinessLogicTier;+DataAccessTier;+ServiceTier;+CertificateGenerator;+DomainObjects;+ExtModels;+RestApiModels;+HpcConnectionFramework;+FileTransferFramework;+BackgroundThread;-*Test*;-*Tests*;-*.Migrations" || true

    if [ -f "/workspace/TestResults/CoverageReport/Summary.txt" ]; then
        echo "==> Coverage Summary:"
        cat /workspace/TestResults/CoverageReport/Summary.txt
    fi
else
    echo "WARNING: reportgenerator binary not found in PATH or /root/.dotnet/tools"
fi

if [ $TEST_EXIT_CODE -ne 0 ]; then
    echo "❌ Tests failed with exit code $TEST_EXIT_CODE"
    exit $TEST_EXIT_CODE
else
    echo "✅ All tests passed successfully!"
fi
