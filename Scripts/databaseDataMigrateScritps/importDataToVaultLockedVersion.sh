#!/bin/bash

root_token="$1"
pathToProject="$2"

if [[ -z $root_token || -z $pathToProject ]]; then
    echo "Missing parameters. Please provide all required parameters."
    echo "Positional parameters: root_token=$1, pathToKeys=$2"
    exit 1
fi

# Kontrola, zda existuje soubor sendKeyWithPassphraseToVault.sh
if [ ! -f "../sendKeyWithPassphraseToVault.sh" ]; then
  echo "Error: Required script '../sendKeyWithPassphraseToVault.sh' not found."
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

    # Volání skriptu sendKeyWithPassphraseToVault.sh s potřebnými parametry
    cd ../..
    status_code=$(./Scripts/sendKeyWithPassphraseToVault.sh "$root_token" "$id" "$lockedPrivatekey" "$private_key_password")
    sleep 0.2s
    cd Scripts/databaseDataMigrateScritps

    echo "Result: sendKeyWithPassphraseToVault.sh for id $id returns status code: $status_code"

done
# uklid temp souboru
rm -rf .temp
exit 1
