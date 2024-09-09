#!/bin/bash

# Define colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Default values
VAULT_FILE="/opt/heappe/projects/credentials"
VAULT_PASSWORD=""
INSTANCE_NAME="Develop"

# Function to display help message
function display_help {
    echo -e "${CYAN}Usage:${NC} $0 <vault_password> [--path <path_to_ansible_vault_file>] [-i|--instance-name <instance_name>]"
    echo
    echo "This script checks if the Vault service is running and if the Vault is sealed."
    echo "If it is sealed, it unseals the Vault using three random keys from the decrypted Ansible Vault file."
    echo
    echo -e "  ${CYAN}Arguments:${NC}"
    echo -e "  ${GREEN}vault_password${NC}             ${RED}(required)${NC} Password to decrypt the Ansible Vault file."
    echo -e "  ${CYAN}--path <path_to_file>${NC}      Path to the Ansible Vault file containing unseal keys (default: /opt/heappe/projects/credentials)."
    echo -e "  ${CYAN}-i${NC}, ${CYAN}--instance-name${NC}   Name of the section in the Ansible Vault file (default: Develop)."
    echo
    echo -e "${CYAN}Example:${NC}"
    echo "  $0 myVaultPassword --path /path/to/vault.json -i MyInstance"
    exit 0
}



# Check for minimum arguments and help
if [ "$#" -lt 1 ]; then
    echo -e "${RED}Error:${NC} Insufficient arguments provided."
    display_help
fi

# Parse command-line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --path)
            VAULT_FILE="$2"
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
            if [ -z "$VAULT_PASSWORD" ]; then
                VAULT_PASSWORD="$1"
            else
                echo -e "${RED}Error:${NC} Unexpected argument: $1"
                display_help
            fi
            shift
            ;;
    esac
done

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
    echo -e "${RED}Error:${NC} vault_password is a required argument."
    display_help
fi

# Check if the Ansible Vault file exists
if [ ! -f "$VAULT_FILE" ]; then
    echo -e "${RED}Error:${NC} Ansible Vault file does not exist at the specified path: $VAULT_FILE"
    exit 1
fi
PASSWORD_FILE=$(mktemp)
# Store the password in the temporary password file
echo "$VAULT_PASSWORD" > "$PASSWORD_FILE"


# Decrypt the Ansible Vault file and extract unseal keys
echo -n "Decrypting Ansible Vault file... "
DECRYPTED_CONTENT=$(ansible-vault decrypt --vault-password-file="$PASSWORD_FILE" "$VAULT_FILE" --output=-)
if [ $? -ne 0 ]; then
    echo -e "${RED}Failed${NC}"
    echo -e "${RED}Error:${NC} Failed to decrypt the Ansible Vault file. Exiting."
    exit 1
fi
echo -e "${GREEN}Success${NC}"

UNSEAL_KEYS=($(echo "$DECRYPTED_CONTENT" | jq -r ".HashiCorpVault_$INSTANCE_NAME.Unseal_Keys[]"))
KEY_ID=1
for key in "${UNSEAL_KEYS[@]}"; do
    echo "Unsealsed key ${KEY_ID}"
    KEY_ID=$((KEY_ID+1))
done

# After using the unseal keys
KEY_COUNT=${#UNSEAL_KEYS[@]}
if [ "$KEY_COUNT" -lt 3 ]; then
    echo -e "${RED}Error:${NC} Not enough unseal keys available. At least 3 are required."
    exit 1
fi

DECRYPTED_CONTENT=""
# Select 3 random keys
SELECTED_KEYS=($(shuf -e "${UNSEAL_KEYS[@]}" -n 3))

# Check if Vault service is running using Docker Compose
echo -n "Checking if the Vault service is running... "
if ! docker compose ps | grep -q "vault.*Up"; then
    echo -e "${RED}Failed${NC}"
    echo -e "${RED}Error:${NC} Vault service is not running. Please start the service and try again."
    exit 1
fi
echo -e "${GREEN}Running${NC}"

# Check if the Vault is sealed
echo -n "Checking if Vault is sealed... "
SEALED_STATUS=$(docker compose exec vault vault status -format=json | jq -r '.sealed')

if [ "$SEALED_STATUS" = "true" ]; then
    echo -e "${RED}Sealed${NC}"
    echo "Unsealing Vault..."

    # Unseal the Vault using the selected keys
    docker compose exec vault vault operator unseal "${SELECTED_KEYS[0]}"
    docker compose exec vault vault operator unseal "${SELECTED_KEYS[1]}"
    docker compose exec vault vault operator unseal "${SELECTED_KEYS[2]}"

    echo -n "Re-checking if Vault is sealed... "
    SEALED_STATUS=$(docker compose exec vault vault status -format=json | jq -r '.sealed')
    if [ "$SEALED_STATUS" = "false" ]; then
        echo -e "${GREEN}Unsealed${NC}"
    else
        echo -e "${RED}Still Sealed${NC}"
        echo -e "${RED}Error:${NC} Failed to unseal the Vault. Please check the provided keys and try again."
        exit 1
    fi
else
    echo -e "${GREEN}Unsealed${NC}"
    echo "Vault is already unsealed."
fi

ansible-vault encrypt --vault-password-file="$PASSWORD_FILE" "$VAULT_FILE"
rm -f "$PASSWORD_FILE"

echo "Vault service is running and unsealed."
