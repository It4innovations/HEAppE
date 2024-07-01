#!/bin/bash

# Check arguments
if [ "$#" -ne 3 ]; then
    echo "Usage: $0 <path to vault file> <password> <text to append>"
    exit 1
fi

VAULT_FILE=$1
VAULT_PASSWORD=$2
TEXT_TO_APPEND=$3

# Create a temporary file to store the decrypted content and password
# Temporary files
DECRYPTED_FILE=$(mktemp)
ENCRYPTED_FILE=$(mktemp)
PASSWORD_FILE=$(mktemp)

# Store the password in the temporary password file
echo "$VAULT_PASSWORD" > "$PASSWORD_FILE"

# Decrypt the Ansible Vault file
ansible-vault decrypt --vault-password-file="$PASSWORD_FILE" "$VAULT_FILE" --output="$DECRYPTED_FILE"
if [ $? -ne 0 ]; then
    echo "Failed to decrypt the file $VAULT_FILE"
    rm -f "$DECRYPTED_FILE" "$ENCRYPTED_FILE" "$PASSWORD_FILE"
    exit 1
fi

# Append two blank lines and the new text content
echo -e "\n\n$TEXT_TO_APPEND" >> "$DECRYPTED_FILE"

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
