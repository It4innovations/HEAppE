#!/bin/bash
# Start vault init


clear
docker compose down vault vaultagent
docker compose up -d vault
echo "Vault is initializing..."

sleep 3s
# Call "vault init" and capture the response
init_response=$(docker compose exec vault vault operator init)

# Extract the Unseal Keys and Initial Root Token from the response
unseal_keys=$(echo "$init_response" | grep "Unseal Key" | awk '{print $NF}')
root_token=$(echo "$init_response" | grep "Initial Root Token" | awk '{print $NF}')

# Save the Unseal Keys and Initial Root Token to a new text file
echo "Unseal Keys:" > vault_keys_REMOVE_AFTER_SAVE.txt
echo "$unseal_keys" >> vault_keys_REMOVE_AFTER_SAVE.txt
echo "" >> vault_keys_REMOVE_AFTER_SAVE.txt
echo "Initial Root Token:" >> vault_keys_REMOVE_AFTER_SAVE.txt
echo "$root_token" >> vault_keys_REMOVE_AFTER_SAVE.txt

echo "Unseal Keys and Initial Root Token saved to vault_keys_REMOVE_AFTER_SAVE.txt"

echo Unsealing Vault..
sleep 1s
# Take the first, second, and third lines and call vault unseal command
unseal_key_1=$(echo "$unseal_keys" | sed -n '1p')
unseal_key_2=$(echo "$unseal_keys" | sed -n '2p')
unseal_key_3=$(echo "$unseal_keys" | sed -n '3p')

docker compose exec vault vault operator unseal "$unseal_key_1"
docker compose exec vault vault operator unseal "$unseal_key_2"
docker compose exec vault vault operator unseal "$unseal_key_3"

echo "Vault unsealed"

echo Preparing vault...

docker compose exec -e VAULT_TOKEN=$root_token vault vault secrets enable kv-v2 
docker compose exec -e VAULT_TOKEN=$root_token vault vault secrets enable -version=2 -path=HEAppE kv-v2

docker compose exec -e VAULT_TOKEN=$root_token vault vault auth enable approle

docker compose exec -e VAULT_TOKEN=$root_token vault sh -c "vault policy write heappe-policy - << EOF
# List, create, update, and delete key/value secrets
path \"HEAppE/*\"
{
  capabilities = [\"create\", \"read\", \"update\", \"delete\", \"list\", \"sudo\"]
}
EOF"


docker compose exec -e VAULT_TOKEN=$root_token vault vault write auth/approle/role/heappe-role \
    secret_id_ttl=0 \
    token_policies="heappe-policy" \
    token_num_uses=0 \
    token_ttl=5m \
    token_max_ttl=10m \
    secret_id_num_uses=0

role_id=$(docker compose exec -e VAULT_TOKEN=$root_token vault vault read auth/approle/role/heappe-role/role-id | awk '/role_id/{print $2}')
sleep 1s
secret_id=$(docker compose exec -e VAULT_TOKEN=$root_token vault vault write -f auth/approle/role/heappe-role/secret-id | awk '/secret_id/{print $2; exit}')

echo "role_id JE: $role_id"
echo "secret JE: $secret_id"
echo $role_id > /opt/heappe/projects/general/app/confs/vault/agent/role_id 
echo $secret_id > /opt/heappe/projects/general/app/confs/vault/agent/secret_id


echo "Vault is ready for use"

# Agent start

sleep 1s
docker compose up vaultagent -d
echo "Vault agent started"
echo "Write role_id to vaultagent"
echo "Write secret_id to vaultagent"



# curl -v -X 'GET' 'http://vaultagent:8100/v1/HEAppE/data/ClusterAuthenticationCredentials' -H 'accept: application/json' 
# curl -v -X 'GET' 'http://vaultagent:8100/v1/HEAppE/data/ClusterAuthenticationCredentials' -H 'accept: application/json' 
# curl -v -X 'GET' 'http://vaultagent:8100/v1/HEAppE/data/ClusterAuthenticationCredentials' -H 'accept: application/json' 


# Delete Volumes fo development
# docker compose down --volumes


docker compose restart vaultagent