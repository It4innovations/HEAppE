#!/bin/bash

# Define colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Default values
VAULT_FILE="/opt/heappe/projects/credentials"
VAULT_PASSWORD=""
BASE_PATH="../../app/confs" # Default base path
INSTANCE_NAME="Develop"

# Function to display help message
function display_help {
    echo -e "${CYAN}Usage:${NC} $0 <${GREEN}vault_password${NC}> [${CYAN}-p${NC} <path to vault file>] [${CYAN}-b${NC} <base path>] [${CYAN}-i${NC} <instance name>]"
    echo
    echo -e "This script initializes and configures HashiCorp Vault and appends generated credentials to an Ansible Vault file."
    echo
    echo -e "${CYAN}Options:${NC}"
    echo -e "  ${CYAN}-p${NC}, ${CYAN}--path${NC}            Path to the existing or new Ansible Vault file (default: /opt/heappe/projects/credentials)."
    echo -e "  ${CYAN}-b${NC}, ${CYAN}--base-path${NC}       Base path for output files (default: ../../app/confs)."
    echo -e "  ${CYAN}-i${NC}, ${CYAN}--instance-name${NC}   Name of the section in the Ansible Vault file (default: Develop)."
    echo -e "  ${GREEN}vault_password${NC}    Password to encrypt/decrypt the vault file (${RED}required${NC})."
    echo
    echo -e "${CYAN}Example:${NC}"
    echo -e "  $0 ${GREEN}myVaultPassword${NC} ${CYAN}-p${NC} /path/to/vault.json ${CYAN}-b${NC} /path/to/base ${CYAN}-i${NC} MyInstance"
    echo
    echo -e "${CYAN}Note:${NC}"
    echo -e "  If configuration files do not exist in the specified base path, default configuration files will be created."
    echo
    echo -e "Note: The script requires '${YELLOW}ansible-vault${NC}' and '${YELLOW}jq${NC}' to be installed."
    exit 0
}


# Parse command-line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -p|--path)
            VAULT_FILE="$2"
            shift 2
            ;;
        -b|--base-path)
            BASE_PATH="$2"
            shift 2
            ;;
        -i|--instance-name)
            INSTANCE_NAME="$2"
            shift 2
            ;;
        --help)
            display_help
            ;;
        *)
            VAULT_PASSWORD="$1"
            shift
            ;;
    esac
done


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

# Check if the required vault_password is provided
if [ -z "$VAULT_PASSWORD" ]; then
    echo -e "${RED}Error:${NC} vault_password is a required argument."
    display_help
fi

# Check if configuration files exist
CONFIG_FILES=("vault/vault/vault-config.hcl" "vault/agent/role_id" "vault/agent/secret_id" "vault/agent/vault-agent.hcl")

CONFIG_MISSING=false

for file in "${CONFIG_FILES[@]}"; do
    if [ ! -f "$BASE_PATH/$file" ]; then
        echo -e "${YELLOW}Missing configuration file:${NC} $BASE_PATH/$file"
        CONFIG_MISSING=true
    fi
done

# Run the first script if any configuration file is missing
if [ "$CONFIG_MISSING" = true ]; then
    echo "Running the first script to generate missing configuration files..."
    ./initHVaultConfig.sh --base-path "$BASE_PATH"
    # Check again if the configuration files are created after running the script
    CONFIG_MISSING=false
    for file in "${CONFIG_FILES[@]}"; do
        if [ ! -f "$BASE_PATH/$file" ]; then
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
if [ ! -f "./appendToAnsibleVault.sh" ]; then
  echo "Error: appendToAnsibleVault.sh does not exist."
  exit 1
fi

# Check if the Ansible Vault file exists and is decryptable
if [ -f "$VAULT_FILE" ]; then
    echo -n "Checking if the Ansible Vault can be decrypted... "
    TEMP_DECRYPT_FILE=$(mktemp)
    PASSWORD_FILE=$(mktemp)
    # Store the password in the temporary password file
    echo "$VAULT_PASSWORD" > "$PASSWORD_FILE"


    if ! ansible-vault decrypt --vault-password-file="$PASSWORD_FILE" --output="$TEMP_DECRYPT_FILE" "$VAULT_FILE" &> /dev/null; then
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
if ! docker compose ps | grep -q "vault.*Up"; then
    echo -e "${YELLOW}Vault service is not running.${NC} Starting Vault service..."
    docker compose up -d vault
    sleep 3s
else
    echo -e "${GREEN}Vault service is running.${NC}"
fi

echo -n "Checking if Vault is already initialized... "
init_status=$(docker compose exec vault vault status -format=json | jq -r '.initialized')

if [ "$init_status" == "true" ]; then
    echo -e "${GREEN}Vault is already initialized. Running vault unseal procedure.${NC}"
    ./unsealHVault.sh $VAULT_PASSWORD --path $VAULT_FILE
    exit 0
else
    echo -e "${YELLOW}Vault is not initialized.${NC} Initializing Vault..."
fi

# Initialize the Vault
init_response=$(docker compose exec vault vault operator init -format=json)

if [ $? -ne 0 ]; then
    echo -e "${RED}Error:${NC} Vault initialization failed. Exiting."
    exit 1
fi

# Extract the Unseal Keys and Initial Root Token from the response
unseal_keys=$(echo "$init_response" | jq -r '.unseal_keys_b64[]')
root_token=$(echo "$init_response" | jq -r '.root_token')

# Convert unseal_keys into a JSON array
unseal_keys_json=$(jq -n --argjson keys "$(echo "$unseal_keys" | jq -R . | jq -s .)" '{"Unseal_Keys": $keys}')

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
sh ./appendToAnsibleVault.sh "$VAULT_PASSWORD" --path "$VAULT_FILE" --data "$json_data"

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

docker compose exec vault vault operator unseal "$unseal_key_1"
docker compose exec vault vault operator unseal "$unseal_key_2"
docker compose exec vault vault operator unseal "$unseal_key_3"

echo "Vault unsealed"

echo "Preparing vault..."

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

echo $role_id > $BASE_PATH/vault/agent/role_id 
echo $secret_id > $BASE_PATH/vault/agent/secret_id

echo "Vault is ready for use"

# Agent start

sleep 1s
docker compose up -d
echo "HEAppE started"
echo "Write role_id to vaultagent"
echo "Write secret_id to vaultagent"

docker compose restart vaultagent
