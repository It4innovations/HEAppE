#!/bin/bash

# Define colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
CYAN='\033[0;36m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Check if the required vault_password is provided
if [ -z "/opt/confs" ]; then
    echo -e "${RED}Error:${NC} BASE_PATH is a required."
    exit 1
fi

# Create necessary directories
echo "Creating directories..."
mkdir -p "/opt/confs/vault/vault"
mkdir -p "/opt/confs/vault/agent"

# Create vault-config.hcl file with specified content if it doesn't exist
CONFIG_FILE="/opt/confs/vault/vault/vault-config.hcl"
if [ -f "$CONFIG_FILE" ]; then
    echo -e "${YELLOW}Skipping${NC}: $CONFIG_FILE already exists."
else
    echo "Creating vault-config.hcl..."
    cat <<EOF > "$CONFIG_FILE"
ui            = true
api_addr      = "http://localhost:8200"
disable_mlock = true
tls_skip_verify = true
log_level = "info"

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
    AGENT_FILE="/opt/confs/vault/agent/$file"
    if [ -f "$AGENT_FILE" ]; then
        echo -e "${YELLOW}Skipping${NC}: $AGENT_FILE already exists."
    else
        echo "Creating $file..."
        touch "$AGENT_FILE"
        echo -e "${GREEN}Created${NC}: $AGENT_FILE"
    fi
done

# Create vault-agent.hcl file with specified content if it doesn't exist
AGENT_CONFIG_FILE="/opt/confs/vault/agent/vault-agent.hcl"
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
