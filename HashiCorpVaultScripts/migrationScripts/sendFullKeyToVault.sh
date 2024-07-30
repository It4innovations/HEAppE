#!/bin/bash

root_token="$1"
credentialsId="$2"
private_key="$3"
private_key_certificate="$4"
password="$5"
private_key_password="$6"

# Check if all required parameters are provided
if [[ -z $root_token || -z $credentialsId || -z $private_key || -z $private_key_certificate || -z $password || -z $private_key_password ]]; then
    echo "Missing parameters. Please provide all required parameters."
    echo "Positional parameters: root_token=$1, credentialsId=${2}, private_key=${3}, private_key_certificate=${4}, password=${5}, private_key_password=${6}"
    exit 1
fi

url="http://vaultagent:8100/v1/HEAppE/data/ClusterAuthenticationCredentials/$credentialsId"

password="${password// /}"
private_key_password="${private_key_password// /}"

body="{
  \"data\": {
    \"Id\": ${credentialsId},
    \"Password\": \"${password}\",
    \"PrivateKey\": \"${private_key}\",
    \"PrivateKeyPassword\": \"${private_key_password}\",
    \"PrivateKeyCertificate\": \"${private_key_certificate}\"
  },
  \"options\": {}
}"

# Execute the curl command and capture the status code
status_code=$(docker compose exec -T -e VAULT_TOKEN=$root_token heappe sh -c "curl -o /dev/null -s -w '%{http_code}' -H 'Content-Type: application/json' -d '${body}' '${url}'" &)

# Output the status code
echo "Status code: $status_code"
exit $status_code
