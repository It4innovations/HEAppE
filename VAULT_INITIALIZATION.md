# Guide for Initializing HashiCorp Vault

## Step 1: Preparing Configuration Files

1. Create a folder `app/confs/vault/vault` in your project directory.
2. Create a file `vault-config.hcl` with the following content:

    ```hcl
    ui            = true
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
    ```

3. Create a folder `app/confs/vault/agent`.
4. In this folder, create empty files named `role_id` and `secret_id`.
5. Create a file `vault-config.hcl` in `app/confs/vault/agent` with the following content:

    ```hcl
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
    ```

## Step 2: Initializing Vault

1. Navigate to the folder with the HEAppE source code (where `docker-compose.yml` and `vaultInitToAnsibleFile.sh` are located).
2. Run the script `vaultInitToAnsibleFile.sh`, which will:
    - Initialize Vault.
    - Restart the SSH agent.
    - Save secret keys to the Ansible Vault at the end of the file.

## Step 3: Running Vault and HEAppE

1. After successful initialization, Vault will be running.
2. You can now start the HEAppE instance.
3. If you are using version 4.2.2, you can run the data migration.

By following these steps, you should have HashiCorp Vault set up and initialized for your HEAppE project.
