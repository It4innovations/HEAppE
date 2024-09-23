#!/bin/bash

# Define colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Check if ansible-vault is installed
if ! command -v ansible-vault &> /dev/null; then
    echo "Ansible Vault is not installed. Please install it and try again."
    exit 1
fi

# Check if jq is installed
if ! command -v jq &> /dev/null; then
    echo "jq is not installed. Please install it and try again."
    exit 1
fi

# Check if Docker Compose is installed (as a plugin or standalone)
if ! docker compose version &> /dev/null && ! command -v docker-compose &> /dev/null; then
    echo "Neither Docker Compose plugin nor standalone Docker Compose is installed. Please install it and try again."
    exit 1
fi

# Check if the required vault_password is provided
if [ -z "$VAULT_PASSWORD" ]; then
    echo -e "${RED}Error:${NC} vault_password is a required argument."
    exit 1
fi


# Check if configuration files exist
CONFIG_FILES=("/vault/vault/vault-config.hcl" "/vault/agent/role_id" "/vault/agent/secret_id" "/vault/agent/vault-agent.hcl")

CONFIG_MISSING=false

for file in "${CONFIG_FILES[@]}"; do
    if [ ! -f "/opt/confs/$file" ]; then
        echo -e "${YELLOW}Missing configuration file:${NC} /opt/confs/$file"
        CONFIG_MISSING=true
    fi
done

# Run the first script if any configuration file is missing
if [ "$CONFIG_MISSING" = true ]; then
    echo "Running the first script to generate missing configuration files..."
    /opt/initHVaultConfig.sh
    # Check again if the configuration files are created after running the script
    CONFIG_MISSING=false
    for file in "${CONFIG_FILES[@]}"; do
        if [ ! -f "/opt/confs/$file" ]; then
            CONFIG_MISSING=true
            break
        fi
    done

    if [ "$CONFIG_MISSING" = true ]; then
        echo -e "${RED}Error:${NC} Required configuration files are still missing. Exiting."
        exit 1
    fi
fi

# Check if ./appendToAnsibleVault.sh exists
if [ ! -f "/opt/appendToAnsibleVault.sh" ]; then
  echo "Error: appendToAnsibleVault.sh does not exist."
  exit 1
fi

ANSIBLE_VAULT_FILE=/opt/ansibleVault/${INSTANCE_NAME}_credentials
if [ "$SHARED_VAULT_FILE" = true ]; then
    ANSIBLE_VAULT_FILE="${VAULT_FILE_DIR_PATH}/${SHARED_VAULT_FILE_NAME}"
fi

# Check if the Ansible Vault file exists and is decryptable
if [ -f "$ANSIBLE_VAULT_FILE" ]; then
    echo -n "Checking if the Ansible Vault can be decrypted... "
    TEMP_DECRYPT_FILE=$(mktemp)
    PASSWORD_FILE=$(mktemp)
    # Store the password in the temporary password file
    echo "$VAULT_PASSWORD" > "$PASSWORD_FILE"


    if ! ansible-vault decrypt --vault-password-file="$PASSWORD_FILE" --output="$TEMP_DECRYPT_FILE" "$ANSIBLE_VAULT_FILE" &> /dev/null; then
        echo -e "${RED}Failed${NC}"
        echo -e "${RED}Error:${NC} The Ansible Vault file cannot be decrypted with the provided password. Exiting."
        rm -f "$TEMP_DECRYPT_FILE"
        exit 1
    fi

    rm -f "$TEMP_DECRYPT_FILE" "$PASSWORD_FILE"
    echo -e "${GREEN}Success${NC}"
else
    echo "Ansible Vault file does not exist; skipping decryption check."
fi

# Check if Vault service is already running and initialized
echo -n "Checking if Vault service is running... "
if ! docker ps | grep -q "Up.*_vault\b"; then
    echo -e "${YELLOW}Vault service is not running.${NC} Starting Vault service..."
    docker compose up -d vault
    sleep 3s
else
    echo -e "${GREEN}Vault service is running.${NC}"
fi

echo -n "Checking if Vault is already initialized... "
init_status=$(docker exec "${INSTANCE_NAME}_vault" vault status -format=json | jq -r '.initialized')

if [ "$init_status" == "true" ]; then
    echo -e "${GREEN}Vault is already initialized. Running vault unseal procedure.${NC}"
    /opt/unsealHVault.sh $VAULT_PASSWORD --path "$ANSIBLE_VAULT_FILE"
    exit 0
else
    echo -e "${YELLOW}Vault is not initialized.${NC} Initializing Vault..."
fi

# Initialize the Vault
init_response=$(docker exec "${INSTANCE_NAME}_vault" vault operator init -format=json)

if [ $? -ne 0 ]; then
    echo -e "${RED}Error:${NC} Vault initialization failed. Exiting."
    exit 1
fi

# Extract the Unseal Keys and Initial Root Token from the response
unseal_keys=$(echo "$init_response" | jq -r '.unseal_keys_b64[]')
root_token=$(echo "$init_response" | jq -r '.root_token')

# Convert unseal_keys into a JSON array
unseal_keys_json=$(echo "$unseal_keys" | jq -R . | jq -s .)

# Create JSON formatted data
json_data=$(cat <<EOF
{
  "HashiCorpVault_$INSTANCE_NAME": {
    "Unseal_Keys": $unseal_keys_json,
    "Initial_Root_Token": "$root_token"
  }
}
EOF
)

# Append the data to the Ansible Vault file
sh /opt/appendToAnsibleVault.sh "$VAULT_PASSWORD" --path "$ANSIBLE_VAULT_FILE" --data "$json_data"

# Check if the script failed
if [ $? -ne 0 ]; then
  echo -e "${RED}Error:${NC} Failed to append data to Ansible Vault. Exiting."
  exit 1
fi

# Unsealing Vault
echo "Unsealing Vault..."
sleep 1s

# Take the first, second, and third lines and call vault unseal command
unseal_key_1=$(echo "$unseal_keys" | sed -n '1p')
unseal_key_2=$(echo "$unseal_keys" | sed -n '2p')
unseal_key_3=$(echo "$unseal_keys" | sed -n '3p')

docker exec "${INSTANCE_NAME}_vault" vault operator unseal "$unseal_key_1"
docker exec "${INSTANCE_NAME}_vault" vault operator unseal "$unseal_key_2"
docker exec "${INSTANCE_NAME}_vault" vault operator unseal "$unseal_key_3"

echo "Vault unsealed"

echo "Preparing vault..."

docker exec -e VAULT_TOKEN=$root_token "${INSTANCE_NAME}_vault" vault secrets enable kv-v2 
docker exec -e VAULT_TOKEN=$root_token "${INSTANCE_NAME}_vault" vault secrets enable -version=2 -path=HEAppE kv-v2

docker exec -e VAULT_TOKEN=$root_token "${INSTANCE_NAME}_vault" vault auth enable approle

docker exec -e VAULT_TOKEN=$root_token "${INSTANCE_NAME}_vault" sh -c "vault policy write heappe-policy - << EOF
# List, create, update, and delete key/value secrets
path \"HEAppE/*\"
{
  capabilities = [\"create\", \"read\", \"update\", \"delete\", \"list\", \"sudo\"]
}
EOF"

docker exec -e VAULT_TOKEN=$root_token "${INSTANCE_NAME}_vault" vault write auth/approle/role/heappe-role \
    secret_id_ttl=0 \
    token_policies="heappe-policy" \
    token_num_uses=0 \
    token_ttl=5m \
    token_max_ttl=10m \
    secret_id_num_uses=0

role_id=$(docker exec -e VAULT_TOKEN=$root_token "${INSTANCE_NAME}_vault" vault read auth/approle/role/heappe-role/role-id | awk '/role_id/{print $2}')
sleep 1s
secret_id=$(docker exec -e VAULT_TOKEN=$root_token "${INSTANCE_NAME}_vault" vault write -f auth/approle/role/heappe-role/secret-id | awk '/secret_id/{print $2; exit}')

echo "Writing role_id to vaultagent"
echo $role_id > /opt/confs/vault/agent/role_id 

echo "Writing secret_id to vaultagent"
echo $secret_id > /opt/confs/vault/agent/secret_id

echo "Vault is ready for use"
echo "Restarting VaultAgent"
docker restart "${INSTANCE_NAME}_vaultagent"
echo "Vault setup done"
