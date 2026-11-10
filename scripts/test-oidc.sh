#!/usr/bin/env bash
set -eo pipefail

# Konfigurace
KEYCLOAK_URL="https://earth.terrabyte.lrz.de:8443"
REALM="terrabyte"
CLIENT_ID="de.lrz.terrabyte.heappe"
CLIENT_SECRET="ISE4hB0bokSBmASo15DAjd7eCyD57VRvCKoAPJESqsQ6KMLykIsQzLoREs0Am3HOvHBGUU1JvQoM6nmL3BUZzH"
HEAPPE_URL="${HEAPPE_URL:-http://localhost:5001}"

echo "=================================================="
echo " HEAppE OIDC Integration Test"
echo " Keycloak: ${KEYCLOAK_URL}/realms/${REALM}"
echo " HEAppE:   ${HEAPPE_URL}"
echo "=================================================="

# 1. Ziskani tokenu
TOKEN="${1:-$TOKEN}"

if [ -z "$TOKEN" ]; then
    echo ""
    echo "Nebyl predan zadny token. Pokus o ziskani tokenu pres password grant..."
    read -p "Zadejte Keycloak username: " KC_USER
    read -s -p "Zadejte Keycloak heslo: " KC_PASS
    echo ""

    TOKEN_RESPONSE=$(curl -k -s -X POST "${KEYCLOAK_URL}/realms/${REALM}/protocol/openid-connect/token" \
        -H "Content-Type: application/x-www-form-urlencoded" \
        -d "client_id=${CLIENT_ID}" \
        -d "client_secret=${CLIENT_SECRET}" \
        -d "grant_type=password" \
        -d "username=${KC_USER}" \
        -d "password=${KC_PASS}" \
        -d "scope=openid profile email")

    TOKEN=$(echo "$TOKEN_RESPONSE" | jq -r '.access_token // empty')

    if [ -z "$TOKEN" ] || [ "$TOKEN" = "null" ]; then
        echo "Chyba pri ziskavani tokenu z Keycloaku:"
        echo "$TOKEN_RESPONSE" | jq . 2>/dev/null || echo "$TOKEN_RESPONSE"
        echo ""
        echo "Tip: Pokud neni povolen password grant (Direct Access Grants), predejte ziskany token jako parametr:"
        echo "  ./scripts/test-oidc.sh <VAS_ACCESS_TOKEN>"
        exit 1
    fi
    echo "Token uspesne ziskan z Keycloaku."
fi

# 2. Inspekce token claims
echo ""
echo "--- Token Payload (Decoded JWT) ---"
echo "$TOKEN" | awk -F. '{print $2}' | base64 -d 2>/dev/null | jq '{preferred_username, email, groups}' 2>/dev/null || echo "(Nelze dekodovat base64 payload)"

# 3. Test: CurrentUserInfo
echo ""
echo "--- 1. Test: GET /heappe/UserAndLimitationManagement/CurrentUserInfo ---"
USER_INFO_STATUS=$(curl -k -s -o /tmp/heappe_user_info.json -w "%{http_code}" \
    -X GET "${HEAPPE_URL}/heappe/UserAndLimitationManagement/CurrentUserInfo" \
    -H "Authorization: Bearer ${TOKEN}" \
    -H "Accept: application/json")

echo "HTTP Status: ${USER_INFO_STATUS}"
if [ -f /tmp/heappe_user_info.json ]; then
    jq . /tmp/heappe_user_info.json 2>/dev/null || cat /tmp/heappe_user_info.json
fi

# 4. Test: ProjectsForCurrentUser
echo ""
echo "--- 2. Test: GET /heappe/UserAndLimitationManagement/ProjectsForCurrentUser ---"
PROJECTS_STATUS=$(curl -k -s -o /tmp/heappe_projects.json -w "%{http_code}" \
    -X GET "${HEAPPE_URL}/heappe/UserAndLimitationManagement/ProjectsForCurrentUser" \
    -H "Authorization: Bearer ${TOKEN}" \
    -H "Accept: application/json")

echo "HTTP Status: ${PROJECTS_STATUS}"
if [ -f /tmp/heappe_projects.json ]; then
    jq . /tmp/heappe_projects.json 2>/dev/null || cat /tmp/heappe_projects.json
fi

# 5. Test: GetOrCreatePersonalCredential
echo ""
echo "--- 3. Test: POST /heappe/Credentials/GetOrCreatePersonalCredential (ClusterId: 1) ---"
CRED_STATUS=$(curl -k -s -o /tmp/heappe_cred.json -w "%{http_code}" \
    -X POST "${HEAPPE_URL}/heappe/Credentials/GetOrCreatePersonalCredential" \
    -H "Authorization: Bearer ${TOKEN}" \
    -H "Content-Type: application/json" \
    -d '{"ClusterId": 1}')

echo "HTTP Status: ${CRED_STATUS}"
if [ -f /tmp/heappe_cred.json ]; then
    jq . /tmp/heappe_cred.json 2>/dev/null || cat /tmp/heappe_cred.json
fi

echo ""
echo "=================================================="
echo " Testy dokonceny."
echo "=================================================="
