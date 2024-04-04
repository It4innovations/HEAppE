#!/bin/bash

root_token='REMOVED'



docker compose exec -e VAULT_TOKEN=$root_token vault vault kv get HEAppE/ClusterAuthenticationCredentials/1

echo "-------------"

docker compose exec -e VAULT_TOKEN=$root_token vault vault kv get HEAppE/ClusterAuthenticationCredentials/2
