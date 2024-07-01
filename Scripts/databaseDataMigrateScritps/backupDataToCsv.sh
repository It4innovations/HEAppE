#!/bin/bash
# Funkce pro zobrazení nápovědy
usage() {
  echo "Usage: $0 -d DB_NAME -u DB_USER -p DB_PASSWORD [-c CONTAINER_NAME]"
  exit 1
}

# Defaultní hodnota pro CONTAINER_NAME
CONTAINER_NAME="MssqlDb"
OUTPUT_FILE="data.csv"
# Zpracování parametrů
while getopts ":c:d:u:p:" opt; do
  case $opt in
    c) CONTAINER_NAME="$OPTARG"
    ;;
    d) DB_NAME="$OPTARG"
    ;;
    u) DB_USER="$OPTARG"
    ;;
    p) DB_PASSWORD="$OPTARG"
    ;;
    \?) echo "Invalid option -$OPTARG" >&2
        usage
    ;;
    :) echo "Option -$OPTARG requires an argument." >&2
       usage
    ;;
  esac
done

# Kontrola, zda byly zadány povinné parametry
if [ -z "$DB_NAME" ] || [ -z "$DB_USER" ] || [ -z "$DB_PASSWORD" ]; then
  echo "Error: Missing required parameters."
  usage
fi

# SQL dotaz pro výběr dat
SQL_QUERY="SELECT Id AS id, Password AS password, PrivateKeyFile AS private_key, PrivateKeyPassword AS private_key_password, ' ' AS private_key_certificate FROM ClusterAuthenticationCredentials WHERE IsDeleted = 0"

# Výběr dat a vytvoření CSV souboru uvnitř kontejneru
docker exec -i $CONTAINER_NAME /opt/mssql-tools/bin/sqlcmd -d $DB_NAME -U $DB_USER -P $DB_PASSWORD -Q "$SQL_QUERY" -s"," -W > $OUTPUT_FILE

# Odstranění nadbytečných řádků a náhrada NULL za mezeru
sed -i '/^id,password,private_key,private_key_password$/d' $OUTPUT_FILE
sed -i '/^--/d' $OUTPUT_FILE
sed -i '/^$/d' $OUTPUT_FILE
sed -i 's/NULL/ /g' $OUTPUT_FILE
sed -i '/rows affected/d' $OUTPUT_FILE

echo "CSV file has been created successfully: $OUTPUT_FILE"
