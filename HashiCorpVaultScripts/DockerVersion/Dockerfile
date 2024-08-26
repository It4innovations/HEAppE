# Použijeme lehký Alpine Linux jako základní image
FROM alpine:3.20.2

# Instalace nezbytných nástrojů: bash, docker-cli, ansible, jq
RUN apk add --no-cache bash docker-cli ansible jq

# Nastavení pracovního adresáře
WORKDIR /app

# Zkopírování vašeho skriptu do image
COPY initHVault.sh /app/initHVault.sh
COPY initHVaultConfig.sh /app/initHVaultConfig.sh
COPY appendToAnsibleVault.sh /app/appendToAnsibleVault.sh
COPY unsealHVault.sh /app/unsealHVault.sh

# Nastavení práv pro spustitelnost skriptu
RUN chmod +x /app/*

# Nastavení defaultních environmentálních proměnných
ENV VAULT_FILE=/opt/heappe/projects/credentials
ENV VAULT_PASSWORD=""
ENV BASE_PATH=../../app/confs
ENV INSTANCE_NAME=Develop

# Entry point nastaví, že kontejner bude přijímat parametry
ENTRYPOINT ["/bin/bash", "-c"]

# CMD nastaví defaultní příkaz jako `init`
CMD ["/app/initHVault.sh"]