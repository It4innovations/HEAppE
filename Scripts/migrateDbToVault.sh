#!/bin/bash

# Check if 'sqlcmd' is installed
if ! command -v sqlcmd &> /dev/null
then
    echo "sqlcmd is not installed. Please install it using:"
    echo "sudo yum install -y sqlcmd"
    exit 1
fi

# Assign input parameters to variables
USERNAME="SA"
#$1
PASSWORD="tvP2qLNDH87mYVg5TwFLA@e4e8"
#$2
QUERY="SELECT * FROM dbo.ClusterAuthenticationCredentials"
DATABASENAME="heappe_general"

# Execute SQL query and store the result in 'data' variable
data=$(docker compose exec mssql /opt/mssql-tools/bin/sqlcmd -d $DATABASENAME -U "$USERNAME" -P "$PASSWORD" -Q "$QUERY" -h -1)


# Print the stored data
echo "$data"



./opt/mssql-tools/bin/sqlcmd -U heappe_lexisgeneral -P XowCV9ee$JVhGzC6KCYbYcxJ -d heappe_lexisgeneral

 "MiddlewareContext": "Server=MssqlDb;TrustServerCertificate=True;Encrypt=False;Database=heappe_lexisgeneral;User=heappe_lexisgeneral;Password=XowCV9ee$JVhGzC6KCYbYcxJ;",
