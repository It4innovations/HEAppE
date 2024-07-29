#!/bin/bash

# Usage: ./your_script.sh <root_token> <record_id>

root_token=$1
record_id=$2

if [ -z "$root_token" ] || [ -z "$record_id" ]; then
  echo "Usage: $0 <root_token> <record_id>"
  exit 1
fi

docker compose exec -e VAULT_TOKEN=$root_token vault vault kv get HEAppE/ClusterAuthenticationCredentials/$record_id
