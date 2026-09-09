#!/bin/sh
set -e

echo "Starting QScheduler test service..."
exec python3 /app/server.py
