#!/bin/bash
set -e

TIMEOUT=${1:-120}
INTERVAL=3
COMPOSE_FILE="docker-compose.ci.yml"

echo "Waiting for all services in $COMPOSE_FILE to become healthy (timeout: ${TIMEOUT}s)..."

start_time=$(date +%s)
while true; do
    current_time=$(date +%s)
    elapsed=$((current_time - start_time))
    
    if [ $elapsed -ge $TIMEOUT ]; then
        echo "ERROR: Timed out waiting for services to become healthy after ${TIMEOUT} seconds."
        docker compose -f "$COMPOSE_FILE" ps
        exit 1
    fi

    # Check services health status
    unhealthy=$(docker compose -f "$COMPOSE_FILE" ps --format json | grep -E '"(unhealthy|starting)"' || true)
    
    if [ -z "$unhealthy" ]; then
        running_count=$(docker compose -f "$COMPOSE_FILE" ps --status running -q | wc -l)
        if [ "$running_count" -gt 0 ]; then
            echo "All services are up and healthy!"
            docker compose -f "$COMPOSE_FILE" ps
            break
        fi
    fi

    echo "Still waiting for services... (${elapsed}s elapsed)"
    sleep $INTERVAL
done
