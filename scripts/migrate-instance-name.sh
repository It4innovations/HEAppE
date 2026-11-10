#!/bin/bash
set -euo pipefail

# Define colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

display_help() {
    echo -e "${CYAN}Usage:${NC} $0 <OLD_INSTANCE_NAME> <NEW_INSTANCE_NAME> [DEPLOYMENT_DIR]"
    echo ""
    echo "Migrates an existing HEAppE deployment after renaming the instance:"
    echo "  1. Renames deployment directories and confs"
    echo "  2. Renames Ansible Vault credentials files"
    echo "  3. Updates the JSON root key in the Ansible Vault file from"
    echo "     'HashiCorpVault_<OLD>' to 'HashiCorpVault_<NEW>'"
    echo "  4. Updates INSTANCE_NAME in .env"
    echo ""
    echo "Arguments:"
    echo "  OLD_INSTANCE_NAME   Previous instance name (e.g. develop)"
    echo "  NEW_INSTANCE_NAME   New instance name (e.g. core_dev_it4i_develop)"
    echo "  DEPLOYMENT_DIR      (Optional) Directory containing the instance."
    echo "                      Default auto-detects from /opt/heappe/projects or /opt/heappe-deployment."
    exit 1
}

if [ $# -lt 2 ]; then
    display_help
fi

OLD_INSTANCE="$1"
NEW_INSTANCE="$2"
DEPLOY_BASE="${3:-}"

echo -e "${CYAN}=== HEAppE Instance Renaming Migration ===${NC}"
echo -e "Old instance name: ${YELLOW}${OLD_INSTANCE}${NC}"
echo -e "New instance name: ${GREEN}${NEW_INSTANCE}${NC}"

# Auto-detect deployment directory if not supplied
if [ -z "$DEPLOY_BASE" ]; then
    if [ -d "/opt/heappe-deployment/${OLD_INSTANCE}" ]; then
        DEPLOY_BASE="/opt/heappe-deployment"
    elif [ -d "/opt/heappe/projects/${OLD_INSTANCE}" ]; then
        DEPLOY_BASE="/opt/heappe/projects"
    elif [ -d "/opt/heappe-deployment/${NEW_INSTANCE}" ]; then
        DEPLOY_BASE="/opt/heappe-deployment"
    elif [ -d "/opt/heappe/projects/${NEW_INSTANCE}" ]; then
        DEPLOY_BASE="/opt/heappe/projects"
    else
        DEPLOY_BASE="."
    fi
fi

echo -e "Deployment base directory: ${CYAN}${DEPLOY_BASE}${NC}"

OLD_DIR="${DEPLOY_BASE}/${OLD_INSTANCE}"
NEW_DIR="${DEPLOY_BASE}/${NEW_INSTANCE}"

# 1. Rename directory if needed
if [ -d "$OLD_DIR" ] && [ ! -d "$NEW_DIR" ]; then
    echo -e "Renaming instance directory from ${OLD_DIR} to ${NEW_DIR}..."
    mv "$OLD_DIR" "$NEW_DIR"
    echo -e "${GREEN}Directory renamed.${NC}"
elif [ -d "$NEW_DIR" ]; then
    echo -e "${GREEN}Target directory ${NEW_DIR} already exists.${NC}"
fi

# Locate .env file
ENV_FILE=""
if [ -f "${NEW_DIR}/.env" ]; then
    ENV_FILE="${NEW_DIR}/.env"
elif [ -f "${NEW_DIR}/heappe-core/.env" ]; then
    ENV_FILE="${NEW_DIR}/heappe-core/.env"
fi

# 2. Update INSTANCE_NAME in .env
if [ -n "$ENV_FILE" ]; then
    echo -e "Updating INSTANCE_NAME in ${ENV_FILE}..."
    sed -i.bak "s/^INSTANCE_NAME=.*/INSTANCE_NAME=${NEW_INSTANCE}/" "$ENV_FILE"
    echo -e "${GREEN}Updated INSTANCE_NAME in ${ENV_FILE}.${NC}"
fi

# 3. Locate and rename Ansible Vault credentials file
OLD_CRED_FILE="${NEW_DIR}/${OLD_INSTANCE}_credentials"
NEW_CRED_FILE="${NEW_DIR}/${NEW_INSTANCE}_credentials"

if [ ! -f "$OLD_CRED_FILE" ] && [ -f "${DEPLOY_BASE}/${OLD_INSTANCE}_credentials" ]; then
    OLD_CRED_FILE="${DEPLOY_BASE}/${OLD_INSTANCE}_credentials"
    NEW_CRED_FILE="${DEPLOY_BASE}/${NEW_INSTANCE}_credentials"
fi

if [ -f "$OLD_CRED_FILE" ] && [ ! -f "$NEW_CRED_FILE" ]; then
    echo -e "Renaming credentials file from ${OLD_CRED_FILE} to ${NEW_CRED_FILE}..."
    cp -p "$OLD_CRED_FILE" "$NEW_CRED_FILE"
    echo -e "${GREEN}Credentials file copied to new name.${NC}"
fi

# 4. Update JSON key inside the encrypted Ansible Vault file
TARGET_CRED_FILE=""
if [ -f "$NEW_CRED_FILE" ]; then
    TARGET_CRED_FILE="$NEW_CRED_FILE"
elif [ -f "$OLD_CRED_FILE" ]; then
    TARGET_CRED_FILE="$OLD_CRED_FILE"
fi

if [ -n "$TARGET_CRED_FILE" ]; then
    echo -e "Checking Ansible Vault credentials in ${TARGET_CRED_FILE}..."
    
    # Try reading VAULT_PASSWORD from env or .env file
    VAULT_PASSWORD="${VAULT_PASSWORD:-}"
    if [ -z "$VAULT_PASSWORD" ] && [ -n "$ENV_FILE" ]; then
        VAULT_PASSWORD=$(grep "^VAULT_PASSWORD=" "$ENV_FILE" 2>/dev/null | cut -d'=' -f2- | tr -d '"' | tr -d "'" || true)
    fi

    if [ -n "$VAULT_PASSWORD" ] && command -v ansible-vault &>/dev/null && command -v jq &>/dev/null; then
        echo -e "Decrypting credentials to verify and update root key..."
        PWD_FILE=$(mktemp)
        echo "$VAULT_PASSWORD" > "$PWD_FILE"

        DECRYPTED=$(ansible-vault decrypt --vault-password-file="$PWD_FILE" "$TARGET_CRED_FILE" --output=- 2>/dev/null || true)
        
        if [ -n "$DECRYPTED" ]; then
            if echo "$DECRYPTED" | jq -e ".HashiCorpVault_${OLD_INSTANCE}" &>/dev/null; then
                echo -e "Updating JSON key 'HashiCorpVault_${OLD_INSTANCE}' -> 'HashiCorpVault_${NEW_INSTANCE}'..."
                MODIFIED=$(echo "$DECRYPTED" | jq ".HashiCorpVault_${NEW_INSTANCE} = .HashiCorpVault_${OLD_INSTANCE} | del(.HashiCorpVault_${OLD_INSTANCE})")
                
                TMP_DATA=$(mktemp)
                echo "$MODIFIED" > "$TMP_DATA"
                ansible-vault encrypt --vault-password-file="$PWD_FILE" --output="$TARGET_CRED_FILE" "$TMP_DATA"
                rm -f "$TMP_DATA"
                echo -e "${GREEN}Successfully updated JSON key inside Ansible Vault credentials.${NC}"
            elif echo "$DECRYPTED" | jq -e ".HashiCorpVault_${NEW_INSTANCE}" &>/dev/null; then
                echo -e "${GREEN}JSON key 'HashiCorpVault_${NEW_INSTANCE}' already exists in credentials.${NC}"
            fi
        else
            echo -e "${YELLOW}Warning:${NC} Could not decrypt credentials with the available password. Please check manually."
        fi
        rm -f "$PWD_FILE"
    else
        echo -e "${YELLOW}Notice:${NC} To update the Ansible Vault file key, ensure ansible-vault, jq, and VAULT_PASSWORD are available."
    fi
fi

echo ""
echo -e "${GREEN}=== Migration steps complete ===${NC}"
echo -e "Next steps:"
echo -e "  1. Verify the .env configuration in ${NEW_DIR}"
echo -e "  2. Restart services: docker compose up -d"
echo -e "  3. Synchronize Vault credentials: docker compose --profile initVault up vault_init"
