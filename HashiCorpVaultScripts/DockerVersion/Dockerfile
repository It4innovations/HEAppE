# Použijeme lehký Alpine Linux jako základní image
FROM alpine:3.20.2

# Instalace nezbytných nástrojů: bash, docker-cli, ansible, jq
RUN apk add --no-cache bash docker-cli ansible jq

# Nastavení pracovního adresáře
WORKDIR /app

# Zkopírování vašeho skriptu do image
COPY HashiCorpVaultScripts/DockerVersion/initHVault.sh /app/initHVault.sh
COPY HashiCorpVaultScripts/DockerVersion/initHVaultConfig.sh /app/initHVaultConfig.sh
COPY HashiCorpVaultScripts/DockerVersion/appendToAnsibleVault.sh /app/appendToAnsibleVault.sh
COPY HashiCorpVaultScripts/DockerVersion/unsealHVault.sh /app/unsealHVault.sh
COPY HashiCorpVaultScripts/DockerVersion/unsealOrInitHVault.sh /app/unsealOrInitHVault.sh

# Nastavení práv pro spustitelnost skriptu
RUN chmod +x /app/*

# Entry point nastaví, že kontejner bude přijímat parametry
ENTRYPOINT ["/bin/bash", "-c"]

# CMD nastaví defaultní příkaz jako `init`
CMD ["/app/initHVault.sh"]