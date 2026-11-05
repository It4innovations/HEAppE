#!/bin/bash
set -e

echo "============================================================"
echo "  Stopping HEAppE Kubernetes Stack"
echo "============================================================"

# 1. Uninstall Helm release
echo "--> Uninstalling Helm release 'heappe'..."
helm uninstall heappe -n heappe 2>/dev/null || true

# 2. Delete Persistent Volume Claims and Namespace
echo "--> Cleaning up namespace and PVCs..."
kubectl delete pvc --all -n heappe 2>/dev/null || true
kubectl delete namespace heappe 2>/dev/null || true

echo ""
echo "HEAppE deployment has been removed from Kubernetes."
echo "Note: Minikube is still running. To stop Minikube completely, run: minikube stop"
