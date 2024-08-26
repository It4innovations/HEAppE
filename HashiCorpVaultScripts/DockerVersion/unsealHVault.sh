#!/bin/bash

# Define colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Check if jq is installed
if ! command -v jq &> /dev/null; then
    echo "jq is not installed. Please install it and try again."
    exit 1
fi

# Check if ansible-vault is installed
if ! command -v ansible-vault &> /dev/null; then
    echo "Ansible Vault is not installed. Please install it and try again."
    exit 1
fi

# Check if the required arguments are provided
if [ -z "$VAULT_PASSWORD" ]; then
    echo $VAULT_PASSWORD
    echo -e "${RED}Error:${NC} vault_password is a required argument."
    exit 1
fi

ANSIBLE_VAULT_FILE=/app/ansibleVault/${INSTANCE_NAME}_credentials
if [ "$SHARED_VAULT_FILE" = true ]; then
    ANSIBLE_VAULT_FILE="${VAULT_FILE_DIR_PATH}/${SHARED_VAULT_FILE_NAME}"
fi

# Check if the Ansible Vault file exists
if [ ! -f "$ANSIBLE_VAULT_FILE" ]; then
    echo $VAULT_PASSWORD
    echo -e "${RED}Error:${NC} Ansible Vault file does not exist at the specified path: $ANSIBLE_VAULT_FILE"
    exit 1
fi
PASSWORD_FILE=$(mktemp)
# Store the password in the temporary password file
echo "$VAULT_PASSWORD" > "$PASSWORD_FILE"


# Decrypt the Ansible Vault file and extract unseal keys
echo -n "Decrypting Ansible Vault file... "
DECRYPTED_CONTENT=$(ansible-vault decrypt --vault-password-file="$PASSWORD_FILE" "$ANSIBLE_VAULT_FILE" --output=-)
if [ $? -ne 0 ]; then
    echo -e "${RED}Failed${NC}"
    echo -e "${RED}Error:${NC} Failed to decrypt the Ansible Vault file."
    rm -f "$PASSWORD_FILE"
    exit 1
fi
echo -e "${GREEN}Success${NC}"

UNSEAL_KEYS=($(echo "$DECRYPTED_CONTENT" | jq -r ".HashiCorpVault_$INSTANCE_NAME.Unseal_Keys[]"))
for key in "${UNSEAL_KEYS[@]}"; do
    echo "$key"
done

# After using the unseal keys
KEY_COUNT=${#UNSEAL_KEYS[@]}
if [ "$KEY_COUNT" -lt 3 ]; then
    echo -e "${RED}Error:${NC} Not enough unseal keys available. At least 3 are required."
    rm -f "$PASSWORD_FILE"
    exit 1
fi

DECRYPTED_CONTENT=""
# Select 3 random keys
SELECTED_KEYS=($(shuf -e "${UNSEAL_KEYS[@]}" -n 3))

# Check if Vault service is running using Docker 
echo -n "Checking if the Vault service is running... "
if ! docker ps | grep -q "vault.*Up"; then
    echo -e "${RED}Failed${NC}"
    echo -e "${RED}Error:${NC} Vault service is not running. Please start the service and try again."
    rm -f "$PASSWORD_FILE"
    exit 1
fi
echo -e "${GREEN}Running${NC}"

# Check if the Vault is sealed
echo -n "Checking if Vault is sealed... "
SEALED_STATUS=$(docker exec "${INSTANCE_NAME}_vault" vault status -format=json | jq -r '.sealed')

if [ "$SEALED_STATUS" = "true" ]; then
    echo -e "${RED}Sealed${NC}"
    echo "Unsealing Vault..."

    # Unseal the Vault using the selected keys
    docker exec "${INSTANCE_NAME}_vault" vault operator unseal "${SELECTED_KEYS[0]}"
    docker exec "${INSTANCE_NAME}_vault" vault operator unseal "${SELECTED_KEYS[1]}"
    docker exec "${INSTANCE_NAME}_vault" vault operator unseal "${SELECTED_KEYS[2]}"

    echo -n "Re-checking if Vault is sealed... "
    SEALED_STATUS=$(docker exec "${INSTANCE_NAME}_vault" vault status -format=json | jq -r '.sealed')
    if [ "$SEALED_STATUS" = "false" ]; then
        echo -e "${GREEN}Unsealed${NC}"
    else
        echo -e "${RED}Still Sealed${NC}"
        echo -e "${RED}Error:${NC} Failed to unseal the Vault. Please check the provided keys and try again."
        rm -f "$PASSWORD_FILE"
        exit 1
    fi
else
    echo -e "${GREEN}Unsealed${NC}"
    echo "Vault is already unsealed."
fi

rm -f "$PASSWORD_FILE"

echo "Vault service is running and unsealed."
