#!/usr/bin/env bash
set -eo pipefail

# Configuration (can be overridden via environment variables)
KEYCLOAK_URL="${KEYCLOAK_URL:-https://keycloak.example.com}"
REALM="${REALM:-example-realm}"
CLIENT_ID="${CLIENT_ID:-example-client}"
CLIENT_SECRET="${CLIENT_SECRET:-}"
HEAPPE_URL="${HEAPPE_URL:-http://localhost:5001}"

echo "=================================================="
echo " HEAppE OIDC Integration Test"
echo " Keycloak: ${KEYCLOAK_URL}/realms/${REALM}"
echo " HEAppE:   ${HEAPPE_URL}"
echo "=================================================="

# 1. Token acquisition
TOKEN="${1:-$TOKEN}"

if [ -z "$TOKEN" ]; then
    echo ""
    echo "No token provided. Attempting to acquire token via password grant..."
    read -p "Enter Keycloak username: " KC_USER
    read -s -p "Enter Keycloak password: " KC_PASS
    echo ""
    if [ -z "$CLIENT_SECRET" ]; then
        read -s -p "Enter Keycloak client secret: " CLIENT_SECRET
        echo ""
    fi

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
        echo "Error acquiring token from Keycloak:"
        echo "$TOKEN_RESPONSE" | jq . 2>/dev/null || echo "$TOKEN_RESPONSE"
        echo ""
        echo "Tip: If password grant (Direct Access Grants) is not enabled, pass an acquired token as argument:"
        echo "  ./scripts/test-oidc.sh <YOUR_ACCESS_TOKEN>"
        exit 1
    fi
    echo "Token successfully acquired from Keycloak."
fi

# 2. Token claims inspection
echo ""
echo "--- Token Payload (Decoded JWT) ---"
echo "$TOKEN" | awk -F. '{print $2}' | base64 -d 2>/dev/null | jq '{preferred_username, email, groups}' 2>/dev/null || echo "(Unable to decode base64 payload)"

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
echo " Tests completed."
echo "=================================================="

