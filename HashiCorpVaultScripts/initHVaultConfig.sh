#!/bin/bash

# Define colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
CYAN='\033[0;36m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Default base path
BASE_PATH="../../app/confs"

# Function to display help message
function display_help {
    echo -e "${CYAN}Usage:${NC} $0 [--base-path <path>]"
    echo
    echo -e "${CYAN}Description:${NC}"
    echo "This script creates the necessary Vault configuration files and directories under the base path."
    echo -e "Base path: ${YELLOW}$BASE_PATH${NC} (can be overridden with --base-path)"
    echo
    echo -e "${CYAN}Options:${NC}"
    echo "  --base-path <path>      Specify a custom base path for the configuration files."
    echo
    echo -e "${CYAN}Files created:${NC}"
    echo -e "  - ${YELLOW}${BASE_PATH}/vault/vault/vault-config.hcl${NC}"
    echo -e "  - ${YELLOW}${BASE_PATH}/vault/agent/role_id${NC} (empty)"
    echo -e "  - ${YELLOW}${BASE_PATH}/vault/agent/secret_id${NC} (empty)"
    echo -e "  - ${YELLOW}${BASE_PATH}/vault/agent/vault-agent.hcl${NC}"
    exit 0
}

# Parse command-line arguments for --base_path and --help
while [[ $# -gt 0 ]]; do
    case $1 in
        --base-path)
            BASE_PATH="$2"
            shift 2
            ;;
        --help)
            display_help
            ;;
        *)
            echo -e "${RED}Error:${NC} Unexpected argument: $1"
            display_help
            ;;
    esac
done

# Create necessary directories
echo "Creating directories..."
mkdir -p "$BASE_PATH/vault/vault"
mkdir -p "$BASE_PATH/vault/agent"

# Create vault-config.hcl file with specified content if it doesn't exist
CONFIG_FILE="$BASE_PATH/vault/vault/vault-config.hcl"
if [ -f "$CONFIG_FILE" ]; then
    echo -e "${YELLOW}Skipping${NC}: $CONFIG_FILE already exists."
else
    echo "Creating vault-config.hcl..."
    cat <<EOF > "$CONFIG_FILE"
ui            = false
api_addr      = "http://localhost:8200"
disable_mlock = true
tls_skip_verify = true
log_level = "Debug"

storage "file" {
  path = "/vault/data"
}

listener "tcp" {
  address = "0.0.0.0:8200"
  tls_disable = true
}
EOF
    echo -e "${GREEN}Created${NC}: $CONFIG_FILE"
fi

# Create empty role_id and secret_id files if they don't exist
for file in "role_id" "secret_id"; do
    AGENT_FILE="$BASE_PATH/vault/agent/$file"
    if [ -f "$AGENT_FILE" ]; then
        echo -e "${YELLOW}Skipping${NC}: $AGENT_FILE already exists."
    else
        echo "Creating $file..."
        touch "$AGENT_FILE"
        echo -e "${GREEN}Created${NC}: $AGENT_FILE"
    fi
done

# Create vault-agent.hcl file with specified content if it doesn't exist
AGENT_CONFIG_FILE="$BASE_PATH/vault/agent/vault-agent.hcl"
if [ -f "$AGENT_CONFIG_FILE" ]; then
    echo -e "${YELLOW}Skipping${NC}: $AGENT_CONFIG_FILE already exists."
else
    echo "Creating vault-agent.hcl..."
    cat <<EOF > "$AGENT_CONFIG_FILE"
pid_file = "/tmp/pidfile"

auto_auth {
  method {
    type = "approle"

    config = {
      role_id_file_path = "/vault/config/role_id"
      secret_id_file_path = "/vault/config/secret_id"
      remove_secret_id_file_after_reading = false
    }
  }

  sink {
    type = "file"
    config = {
      path = "/tmp/token"
    }
  }
}

cache {
}

api_proxy {
  use_auto_auth_token = "force"
  enforce_consistency = "always"
}

listener "tcp" {
   address = "0.0.0.0:8100"
   tls_disable = true
}

vault {
  tls_skip_verify = 1
  address = "http://vault:8200"
    retry {
    num_retries = 3
  }
}
EOF
    echo -e "${GREEN}Created${NC}: $AGENT_CONFIG_FILE"
fi

echo "All operations completed."
