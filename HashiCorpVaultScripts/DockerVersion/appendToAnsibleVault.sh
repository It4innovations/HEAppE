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

# Check if vaultKey is provided as the first positional argument
if [ -z "$1" ]; then
    echo -e "${RED}Error:${NC} vaultKey is a required positional argument."
    display_help
else
    VAULT_PASSWORD="$1"
    shift # Remove vaultKey from arguments
fi

# Parse remaining command-line options
while [[ $# -gt 0 ]]
do
    key="$1"
    case $key in
        --path)
        VAULT_FILE="$2"
        shift # past argument
        shift # past value
        ;;
        --data)
        JSON_DATA="$2"
        shift # past argument
        shift # past value
        ;;
    esac
done


if [ -z "$VAULT_PASSWORD" ]; then
    echo -e "${RED}Error:${NC} VAULT_PASSWORD is a required."
    exit 1
fi


# Validate that --data contains a valid JSON string
if ! echo "$JSON_DATA" | jq . &> /dev/null; then
    echo -e "${RED}Error:${NC} --data must be a valid JSON string."
    exit 1
fi

# Temporary files
DECRYPTED_FILE=$(mktemp)
ENCRYPTED_FILE=$(mktemp)
PASSWORD_FILE=$(mktemp)

# Store the password in the temporary password file
echo "$VAULT_PASSWORD" > "$PASSWORD_FILE"

# Check if the file exists, if not, create a new Ansible Vault file with an empty JSON object
if [ ! -f "$VAULT_FILE" ] || [ "$(cat "$VAULT_FILE")" == "{}" ]; then
    echo "File $VAULT_FILE does not exist or is empty. Creating a new Ansible Vault file."
    echo "{}" > "$DECRYPTED_FILE"
    echo  "CREDENTIALS FILE PATH: $VAULT_FILE"
    ansible-vault encrypt --vault-password-file="$PASSWORD_FILE" --output="$VAULT_FILE" "$DECRYPTED_FILE"
    if [ $? -ne 0 ]; then
        echo "Failed to create the new Ansible Vault file $VAULT_FILE"
        rm -f "$DECRYPTED_FILE" "$PASSWORD_FILE"
        exit 1
    fi
fi

# Decrypt the Ansible Vault file
ansible-vault decrypt --vault-password-file="$PASSWORD_FILE" "$VAULT_FILE" --output="$DECRYPTED_FILE"
if [ $? -ne 0 ]; then
    echo "Failed to decrypt the file $VAULT_FILE"
    rm -f "$DECRYPTED_FILE" "$ENCRYPTED_FILE" "$PASSWORD_FILE"
    exit 1
fi

# Read and update the JSON content using jq
if jq -e . "$DECRYPTED_FILE" &> /dev/null; then
    updated_content=$(jq ". + $JSON_DATA" "$DECRYPTED_FILE")
else
    updated_content=$JSON_DATA
fi

# Check if the update was successful
if [ $? -ne 0 ]; then
    echo "Failed to update the JSON content in $DECRYPTED_FILE"
    rm -f "$DECRYPTED_FILE" "$ENCRYPTED_FILE" "$PASSWORD_FILE"
    exit 1
fi

# Write updated JSON content back to the decrypted file
echo "$updated_content" > "$DECRYPTED_FILE"

# Encrypt the file back
ansible-vault encrypt --vault-password-file="$PASSWORD_FILE" "$DECRYPTED_FILE" --output="$ENCRYPTED_FILE"
if [ $? -ne 0 ]; then
    echo "Failed to encrypt the file $VAULT_FILE"
    rm -f "$DECRYPTED_FILE" "$ENCRYPTED_FILE" "$PASSWORD_FILE"
    exit 1
fi

# Replace the original file with the newly encrypted file
mv "$ENCRYPTED_FILE" "$VAULT_FILE"

# Clean up
rm -f "$DECRYPTED_FILE" "$PASSWORD_FILE"

echo "File $VAULT_FILE has been updated and re-encrypted."
