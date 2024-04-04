#!/bin/bash

root_token='hvs.wlw0VzaCkEqYofFCJJ6wndbw'



docker compose exec -e VAULT_TOKEN=$root_token vault vault kv get HEAppE/ClusterAuthenticationCredentials/1

echo "-------------"

docker compose exec -e VAULT_TOKEN=$root_token vault vault kv get HEAppE/ClusterAuthenticationCredentials/2
