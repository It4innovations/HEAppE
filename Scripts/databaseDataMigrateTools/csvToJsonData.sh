#!/bin/bash

# Zkontrolujte, zda je nástroj jq nainstalován
if ! command -v jq &> /dev/null
then
    echo "jq could not be found"
    echo "Installing jq..."
    sudo apt-get install jq
fi

# Načtěte CSV soubor a přeskočte první řádek
tail -n +2 data.csv | while IFS=';' read -r id password private_key private_key_password private_key_certificate
do
    # Vytvořte JSON objekt s těmito hodnotami
    json_object=$(jq -n \
                    --arg id "$id" \
                    --arg pw "$password" \
                    --arg pk "$private_key" \
                    --arg pkpw "$private_key_password" \
                    --arg pkc "$private_key_certificate" \
                    '{data: {Id: $id, Password: $pw, PrivateKey: $pk, PrivateKeyPassword: $pkpw, PrivateKeyCertificate: $pkc}, options: {}}')

    # Přidejte JSON objekt do pole
    json_array+=$json_object
done

# Uložte JSON pole do souboru data.json
echo [$json_array] | jq . > data.json