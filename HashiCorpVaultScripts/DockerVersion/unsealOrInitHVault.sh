#!/bin/bash

# Wait for Vault container to respond
while true; do
    docker exec "${INSTANCE_NAME}_vault" vault status > /dev/null 2>&1
    STATUS=$?
    if [ $STATUS -eq 0 ] || [ $STATUS -eq 2 ]; then
        break
    fi
    echo "Waiting for Vault container to respond..."
    sleep 2
done

# Check if Vault is initialized
IS_INITIALIZED=$(docker exec "${INSTANCE_NAME}_vault" vault status | grep -q "Initialized.*true"; echo $?)
IS_SEALED=$(docker exec "${INSTANCE_NAME}_vault" vault status | grep -q "Sealed.*true"; echo $?)

# If Vault is not initialized, run Init.sh
if [ $IS_INITIALIZED -ne 0 ]; then
    echo "Vault is not initialized. Running Init ..."
    /opt/initHVault.sh
else
    echo "Vault is initialized."

    # If Vault is initialized but sealed, run Unseal.sh
    if [ $IS_SEALED -eq 0 ]; then
        echo "Vault is sealed. Running Unseal ..."
        /opt/unsealHVault.sh 
    else
        echo "Vault is not sealed. No action needed."
    fi
fi
