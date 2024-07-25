#!/bin/bash

root_token="$1"
credentialsId="$2"
private_key="$3"
private_key_password="$4"

if [[ -z $root_token || -z $credentialsId || -z $private_key || -z $private_key_password ]]; then
    echo "Missing parameters. Please provide all required parameters."
    echo "Positional parameters: root_token=$1, credentialsId='${2}', private_key=$3, private_key_password=$4"
    exit 1
fi

url="http://vaultagent:8100/v1/HEAppE/data/ClusterAuthenticationCredentials/$credentialsId"
body="{
  \"data\": {\"Id\": ${credentialsId},\"Password\": \"\", \"PrivateKey\": \"${private_key}\",\"PrivateKeyPassword\": \"${private_key_password}\",\"PrivateKeyCertificate\": \"\"},
  \"options\": {}
}"

status_code=$(docker compose exec -T -e VAULT_TOKEN=$root_token heappe sh -c "curl -o /dev/null -s -w '%{http_code}' -H 'Content-Type: application/json' -d '${body}' '${url}'" &)
# Výpis statusového kódu před jeho vrácením
echo "Status code: $status_code"
exit $status_code