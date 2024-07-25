
## Prerequisites
For the proper functioning of the script, the following packages must be installed on the machine:
- ansible-vault
- jq

## Initialization

To initialize the HashiCorp Vault, follow these steps:
1. Navigate to the directory:
   ```
   heappe-core/HashiCorpVaultScripts
   ```
2. Run the initialization script:
   ```
   Usage: ./initHVault.sh <vault_password> [--path <path to vault file>] [--base-path <base path>]

   This script initializes and configures HashiCorp Vault and appends generated credentials to an Ansible Vault file.

   Options:
     --path          Path to the existing or new Ansible Vault file (default: /opt/heappe/projects/credentials).
     --base-path     Base path for output files (default: ../../app/confs).
     vault_password  Password to encrypt/decrypt the vault file (required).

   Example:
     ./initHVault.sh myVaultPassword --path /path/to/vault.json --base-path /path/to/base

   Note:
     If configuration files do not exist in the specified base path, default configuration files will be created.
   ```

## Unlocking an Already Initialized Vault

If the Vault container is restarted (after it has been initialized), you can unlock the Vault using:
   ```
   heappe-core/HashiCorpVaultScripts/unsealHVault.sh

   Usage: ./unsealHVault.sh <vault_password> [--path <path_to_ansible_vault_file>]

   This script checks if the Vault service is running and if the Vault is sealed.
   If it is sealed, it unseals the Vault using three random keys from the decrypted Ansible Vault file.

   Arguments:
     vault_password             Password to decrypt the Ansible Vault file.
     --path <path_to_file>      Path to the Ansible Vault file containing unseal keys (default: /opt/heappe/projects/credentials).

   Example:
     ./unsealHVault.sh myVaultPassword --path /path/to/vault.json
   ```

## Database Migration to HashiCorp Vault

To migrate data from the database to the Vault, follow these steps:

1. Navigate to the directory:
   ```
   heappe-core/HashiCorpVaultScripts/migrationScripts
   ```
2. Export data from the database to a CSV file:
   ```
   ./backupDataToCsv.sh

   Usage: ./backupDataToCsv.sh -d DB_NAME -u DB_USER -p DB_PASSWORD [-c CONTAINER_NAME]
   ```
3. Perform the database migration according to HEAppE requirements.
4. Start the migrated instance with the Vault.
5. Import the exported data into the Vault:
   ```
   ./importDataToVault.sh

   Usage: ./importDataToVault.sh [root_token] [--path pathToKeys]

   Parameters:
     root_token            The root token required for the script.
     --path pathToKeys     Optional parameter to specify the path to the keys.
                           If not provided, the default path is set to ../../...
     --help                Display this help message and exit.
   ```
7. To validate data imported to vault you can use getClusterAuthCredFromVault.sh
    ```
    ./getClusterAuthCredFromVault.sh

    Usage: ./getClusterAuthCredFromVault.sh <root_token> <record_id>
    ```
6. Remember to remove data.csv file 