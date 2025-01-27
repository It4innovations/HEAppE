#!/bin/bash

DEFAULT_PATH="../../.."

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[0;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to display help
function display_help() {
    echo -e "${BLUE}Usage: $0 [root_token] [--path pathToKeys]${NC}"
    echo
    echo "Parameters:"
    echo -e "  ${BLUE}root_token${NC}            The root token required for the script."
    echo -e "  ${BLUE}--path pathToKeys${NC}     Optional parameter to specify the path to the keys."
    echo -e "                        If not provided, the default path is set to $DEFAULT_PATH."
    echo -e "  ${BLUE}--help${NC}                Display this help message and exit."
    exit 0
}

# Check for --help option
if [[ "$1" == "--help" ]]; then
    display_help
fi

root_token="$1"
pathToProject="$DEFAULT_PATH"

# Check for --path option
if [[ "$2" == "--path" && -n "$3" ]]; then
    pathToProject="$3"
elif [[ -n "$2" && "$2" != "--path" ]]; then
    echo -e "${RED}Invalid parameter. Use --help for usage information.${NC}"
    exit 1
fi

if [[ -z $root_token ]]; then
    echo -e "${RED}Missing root_token parameter. Please provide the required parameter.${NC}"
    echo -e "${BLUE}Use --help for usage information.${NC}"
    exit 1
fi

# Kontrola, zda existuje soubor sendFullKeyToVault.sh
if [ ! -f "./sendFullKeyToVault.sh" ]; then
  echo -e "${RED}Error: Required script './sendFullKeyToVault.sh' not found.${NC}"
  exit 1
fi

rm -rf .temp
mkdir -p .temp
# Načtěte CSV soubor a přeskočte první řádek
tail -n +2 ./data.csv | while IFS=',' read -r id password private_key private_key_password private_key_certificate
do
    # pokud je private_key prazdny nebo obsahuje pouze mezery, tak preskoc na dalsi
    if [[ -z "$private_key" || "$private_key" =~ ^[[:space:]]*$ ]]; then
        echo -e "${YELLOW}Skipping entry with empty or whitespace-only private_key for id $id${NC}"
        continue
    fi
    ppath=${private_key#/opt/heappe/}
    if [ -z "$ppath" ]; then
        break
    fi
    pathToKey="$pathToProject/app/$ppath"
    filename=$(echo $pathToKey | awk -F/ '{print $NF}')
    pathToTempKey="./.temp/$filename"

    cp $pathToKey $pathToTempKey

    # Převedení zamknutého privátního klíče do base64
    lockedPrivatekey=$(base64 "${pathToKey}" | tr -d '\n')

    # pokud je private_key_certificate prazdny, tak inicializuj na mezeru
    if [ -z "$private_key_certificate" ]; then
        private_key_certificate=" "
    fi

    if [ -z "$password" ]; then
        password=" "
    fi

    # Volání skriptu sendFullKeyToVault.sh s potřebnými parametry
    # cd ../..
    status_code=$(./sendFullKeyToVault.sh "$root_token" "$id" "$lockedPrivatekey" "$private_key_certificate" "$password" "$private_key_password" &)
    sleep 0.2s
    # cd Scripts/databaseDataMigrateScritps

    echo -e "${GREEN}Result: sendFullKeyToVault.sh for id $id returns status code: $status_code${NC}"

done
# uklid temp souboru
rm -rf .temp
# Reminder to delete data.csv
echo -e "${RED}Reminder: Don't forget to delete the data.csv file as it contains sensitive information.${NC}"

exit 1