## Excepted use for manual init
1. set up .env variables as needed
```bash
VAULT_PASSWORD=test docker compose --profile initVault up
```

## Excepted use for automatic use
Unseal or Init will be automaticaly detected and handled
1. set up .env variables as needed
2. run heappe
```bash
VAULT_PASSWORD=test docker compose up -d
```
or with profiles
```bash
VAULT_PASSWORD=test docker compose --profile db up -d
```