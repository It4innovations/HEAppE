#!/bin/bash
root_token="$1"

# Načtěte soubor data.json
json_array=$(jq -c '.[]' data.json)

# Pro každý objekt v poli
for json_object in ${json_array[@]}; do
    # Extrahujte hodnoty
    credentialsId=$(echo $json_object | jq -r '.Id')
    private_key=$(echo $json_object | jq -r '.PrivateKey')
    private_key_certificate=$(echo $json_object | jq -r '.PrivateKeyCertificate')
    private_key_certificate=$(echo $json_object | jq -r '.PrivateKeyPassword')
    private_key_certificate=$(echo $json_object | jq -r '.Password')

    # Spusťte skript sendFullKeyToVault.sh s extrahovanými hodnotami jako argumenty
    ./../sendFullKeyToVault.sh $root_token $credentialsId $private_key $private_key_certificate
done