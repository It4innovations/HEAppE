# Použijeme lehký Alpine Linux jako základní image
FROM alpine:3.20.2

# Instalace nezbytných nástrojů: bash, docker-cli, ansible, jq
RUN apk update && apk upgrade && apk add --no-cache bash docker-cli ansible jq

# Nastavení pracovního adresáře
WORKDIR /opt

# Zkopírování vašeho skriptu do image
COPY HashiCorpVaultScripts/DockerVersion/initHVault.sh /opt/initHVault.sh
COPY HashiCorpVaultScripts/DockerVersion/initHVaultConfig.sh /opt/initHVaultConfig.sh
COPY HashiCorpVaultScripts/DockerVersion/appendToAnsibleVault.sh /opt/appendToAnsibleVault.sh
COPY HashiCorpVaultScripts/DockerVersion/unsealHVault.sh /opt/unsealHVault.sh
COPY HashiCorpVaultScripts/DockerVersion/unsealOrInitHVault.sh /opt/unsealOrInitHVault.sh

# Nastavení práv pro spustitelnost skriptu
RUN chmod +x /opt/*

# Entry point nastaví, že kontejner bude přijímat parametry
ENTRYPOINT ["/bin/bash", "-c"]

# CMD nastaví defaultní příkaz jako `init`
CMD ["/opt/initHVault.sh"]