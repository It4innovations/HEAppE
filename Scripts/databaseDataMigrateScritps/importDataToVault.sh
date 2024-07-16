#!/bin/bash

root_token="$1"
pathToProject="$2"

if [[ -z $root_token || -z $pathToProject ]]; then
    echo "Missing parameters. Please provide all required parameters."
    echo "Positional parameters: root_token=$1, pathToKeys=$2"
    exit 1
fi

# Kontrola, zda existuje soubor sendKeyToVault.sh
if [ ! -f "../sendKeyToVault.sh" ]; then
  echo "Error: Required script '../sendKeyToVault.sh' not found."
  exit 1
fi

rm -rf .temp
mkdir -p .temp
# Načtěte CSV soubor a přeskočte první řádek
tail -n +2 data.csv | while IFS=',' read -r id password private_key private_key_password private_key_certificate
do
    # pokud je private_key prazdny nebo obsahuje pouze mezery, tak preskoc na dalsi
    if [[ -z "$private_key" || "$private_key" =~ ^[[:space:]]*$ ]]; then
        echo "Skipping entry with empty or whitespace-only private_key for id $id"
        continue
    fi
    # echo $private_key
    ppath=${private_key#/opt/heappe/}
    if [ -z "$ppath" ]; then
        break
    fi
    pathToKey="$pathToProject/app/$ppath"
    filename=$(echo $pathToKey | awk -F/ '{print $NF}')
    pathToTempKey="./.temp/$filename"

    cp $pathToKey $pathToTempKey
    ssh-keygen -P $private_key_password -N '' -f ${pathToTempKey} <<<y

    # Převedení privátního klíče do base64
    openPrivatekey=$(base64 "${pathToTempKey}" | tr -d '\n')

    # pokud je private_key_certificate prazdny, tak inicializuj na mezeru
    if [ -z "$private_key_certificate" ]; then
        private_key_certificate=" "
    fi
    # Volání skriptu sendKeyToVault.sh s potřebnými parametry
    cd ../..
    sh Scripts/sendKeyToVault.sh "$root_token" "$id" "$openPrivatekey" "$private_key_certificate"
    result=$?
    if [ $result -eq 1 ]; then
        echo "Error: sendKeyToVault.sh failed for id $id"
        exit 1
    fi
    cd Scripts/databaseDataMigrateScritps
done
# uklid temp souboru
rm -rf .temp
exit 1
