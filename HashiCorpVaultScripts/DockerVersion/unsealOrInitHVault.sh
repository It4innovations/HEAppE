#!/bin/bash

sleep 4s;
# Check if Vault is initialized
IS_INITIALIZED=$(docker exec "${INSTANCE_NAME}_vault" vault status | grep -q "Initialized.*true"; echo $?)
IS_SEALED=$(docker exec "${INSTANCE_NAME}_vault" vault status | grep -q "Sealed.*true"; echo $?)

# If Vault is not initialized, run Init.sh
if [ $IS_INITIALIZED -ne 0 ]; then
    echo "Vault is not initialized. Running Init ..."
    ./initHVault.sh
else
    echo "Vault is initialized."

    # If Vault is initialized but sealed, run Unseal.sh
    if [ $IS_SEALED -eq 0 ]; then
        echo "Vault is sealed. Running Unseal ..."
        ./unsealHVault.sh 
    else
        echo "Vault is not sealed. No action needed."
    fi
fi
