#!/bin/bash

root_token="$1"
credentialsId="$2"
private_key="$3"
private_key_certificate="$4"

if [[ -z $root_token || -z $credentialsId || -z $private_key || -z $private_key_certificate ]]; then
    echo "Missing parameters. Please provide all required parameters."
    echo "Positional parameters: root_token=$1, credentialsId='${2}', private_key=$3, private_key_certificate=$4"
    exit 1
fi

url="http://vaultagent:8100/v1/HEAppE/data/ClusterAuthenticationCredentials/$credentialsId"
body="{
  \"data\": {\"Id\": ${credentialsId},\"Password\": \"\", \"PrivateKey\": \"${private_key}\",\"PrivateKeyPassword\": \"\",\"PrivateKeyCertificate\": \"${private_key_certificate}\"},
  \"options\": {}
}"

docker compose exec -T -e VAULT_TOKEN=$root_token heappeV4.2 sh -c "curl -v  -H 'Content-Type: application/json' -d '${body}' '${url}'"


