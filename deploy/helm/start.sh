#!/bin/bash
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

echo "============================================================"
echo "  Starting HEAppE Kubernetes Stack"
echo "============================================================"

# 1. Start Minikube if not running
if ! minikube status >/dev/null 2>&1; then
    echo "--> Starting Minikube..."
    minikube start --cpus=4 --memory=8192 --driver=docker
else
    echo "--> Minikube is already running."
fi

# 2. Configure Docker CLI to use Minikube's Docker daemon
echo "--> Configuring Docker environment for Minikube..."
eval $(minikube docker-env)

# 3. Build Docker images inside Minikube
cd "$REPO_ROOT"
echo "--> Building Docker images..."
echo "    1/3 Building heappe:6.5.0..."
docker build -t heappe:6.5.0 -f RestApi/Dockerfile .

echo "    2/3 Building datastagingapi:6.5.0..."
docker build -t datastagingapi:6.5.0 -f DataStagingAPI/Dockerfile .

echo "    3/3 Building sshagent:latest..."
docker build -t sshagent:latest -f SshAgent/Dockerfile .

# 4. Create Kubernetes namespace
echo "--> Creating namespace 'heappe'..."
kubectl create namespace heappe 2>/dev/null || true

# 5. Deploy / Upgrade Helm Release
echo "--> Deploying HEAppE Helm chart..."
helm upgrade --install heappe "$SCRIPT_DIR/heappe" \
  --namespace heappe \
  --set mssql.saPassword="Passw0rd123!" \
  --set api.image.pullPolicy=Never \
  --set datastaging.image.pullPolicy=Never \
  --set sshAgent.image.pullPolicy=Never \
  --set-file api.seedData="$SCRIPT_DIR/heappe/seed.njson"

# 6. Wait for pods to be ready
echo "--> Waiting for pods to become ready (timeout 5m)..."
kubectl wait --for=condition=Ready pods --all -n heappe --timeout=300s || true

echo ""
echo "============================================================"
echo "  HEAppE Stack Status:"
echo "============================================================"
kubectl get pods -n heappe

echo ""
echo "============================================================"
echo "  To access the API (Swagger UI):"
echo "  Run: kubectl port-forward svc/heappe-api 5000:80 -n heappe"
echo "  URL: http://localhost:5000/swagger"
echo "============================================================"
