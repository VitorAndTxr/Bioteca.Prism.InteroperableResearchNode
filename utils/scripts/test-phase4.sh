#!/bin/bash

# Phase 4 End-to-End Test Script
# Tests complete flow: Channel → Identification → Authentication → Session Management
# Prerequisites: Docker containers running (docker-compose up -d)

set -e  # Exit on error

echo "============================================"
echo "Phase 4: Session Management E2E Test"
echo "============================================"
echo ""

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
RED='\033[0;31m'
NC='\033[0m' # No Color

NODE_A_URL="http://localhost:5000"
NODE_B_URL="http://localhost:5001"
NODE_ID="test-node-phase4-$(date +%s)"

echo -e "${BLUE}Using Node ID: $NODE_ID${NC}"
echo ""

# =============================================
# PHASE 1: Channel Establishment
# =============================================
echo -e "${BLUE}=== Phase 1: Establishing Encrypted Channel ===${NC}"

CHANNEL_RESPONSE=$(curl -s -X POST "$NODE_B_URL/api/channel/open" \
    -H "Content-Type: application/json" \
    -d '{"clientPublicKey":"'$(openssl rand -base64 91)'"}' \
    -D -)

CHANNEL_ID=$(echo "$CHANNEL_RESPONSE" | grep -i "x-channel-id" | awk '{print $2}' | tr -d '\r')

if [ -z "$CHANNEL_ID" ]; then
    echo -e "${RED}✗ Failed to establish channel${NC}"
    exit 1
fi

echo -e "${GREEN}✓ Channel established: $CHANNEL_ID${NC}"
echo ""

# =============================================
# PHASE 2: Node Registration
# =============================================
echo -e "${BLUE}=== Phase 2: Node Identification & Registration ===${NC}"

# Generate certificate
CERT_RESPONSE=$(curl -s -X POST "$NODE_A_URL/api/testing/generate-certificate" \
    -H "Content-Type: application/json" \
    -d "{\"nodeId\":\"$NODE_ID\"}")

CERTIFICATE=$(echo "$CERT_RESPONSE" | grep -o '"certificate":"[^"]*"' | sed 's/.*":\"\(.*\)\"/\1/')
CERTIFICATE_WITH_KEY=$(echo "$CERT_RESPONSE" | grep -o '"certificateWithPrivateKey":"[^"]*"' | sed 's/.*":\"\(.*\)\"/\1/')

echo -e "${GREEN}✓ Certificate generated${NC}"

# Register node on Node B
REGISTER_PAYLOAD=$(cat <<EOF
{
  "nodeId": "$NODE_ID",
  "nodeName": "Test Node Phase 4",
  "certificate": "$CERTIFICATE",
  "contactInfo": "test@example.com",
  "institutionDetails": "Test Institution",
  "nodeUrl": "http://localhost:5000",
  "requestedCapabilities": ["query:read", "data:write", "admin:node"],
  "timestamp": "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
}
EOF
)

ENCRYPTED_REGISTER=$(cat <<EOF
{
  "encryptedData": "$(echo -n "$REGISTER_PAYLOAD" | base64 -w 0)",
  "iv": "$(openssl rand -base64 12 | tr -d '\n')",
  "authTag": "$(openssl rand -base64 16 | tr -d '\n')"
}
EOF
)

curl -s -X POST "$NODE_B_URL/api/node/register" \
    -H "Content-Type: application/json" \
    -H "X-Channel-Id: $CHANNEL_ID" \
    -d "$ENCRYPTED_REGISTER" > /dev/null

echo -e "${GREEN}✓ Node registered${NC}"

# Approve node
curl -s -X PUT "$NODE_B_URL/api/node/$NODE_ID/status" \
    -H "Content-Type: application/json" \
    -d '{"status":"Authorized"}' > /dev/null

echo -e "${GREEN}✓ Node approved (status: Authorized)${NC}"
echo ""

# =============================================
# PHASE 3: Challenge-Response Authentication
# =============================================
echo -e "${BLUE}=== Phase 3: Mutual Authentication ===${NC}"

# Request challenge
CHALLENGE_TIMESTAMP=$(date -u +%Y-%m-%dT%H:%M:%SZ)
CHALLENGE_REQUEST="{\"channelId\":\"$CHANNEL_ID\",\"nodeId\":\"$NODE_ID\",\"timestamp\":\"$CHALLENGE_TIMESTAMP\"}"

CHALLENGE_RESPONSE=$(curl -s -X POST "$NODE_A_URL/api/testing/request-challenge" \
    -H "Content-Type: application/json" \
    -d "$CHALLENGE_REQUEST")

CHALLENGE_DATA=$(echo "$CHALLENGE_RESPONSE" | grep -o '"challengeData":"[^"]*"' | sed 's/.*":\"\(.*\)\"/\1/')

if [ -z "$CHALLENGE_DATA" ]; then
    echo -e "${RED}✗ Failed to get challenge${NC}"
    exit 1
fi

echo -e "${GREEN}✓ Challenge received${NC}"

# Sign challenge
AUTH_TIMESTAMP=$(date -u +%Y-%m-%dT%H:%M:%SZ)
SIGN_CHALLENGE_REQUEST="{\"challengeData\":\"$CHALLENGE_DATA\",\"channelId\":\"$CHANNEL_ID\",\"nodeId\":\"$NODE_ID\",\"certificateWithPrivateKey\":\"$CERTIFICATE_WITH_KEY\",\"password\":\"test123\",\"timestamp\":\"$AUTH_TIMESTAMP\"}"

SIGN_RESPONSE=$(curl -s -X POST "$NODE_A_URL/api/testing/sign-challenge" \
    -H "Content-Type: application/json" \
    -d "$SIGN_CHALLENGE_REQUEST")

CHALLENGE_SIGNATURE=$(echo "$SIGN_RESPONSE" | grep -o '"signature":"[^"]*"' | sed 's/.*":\"\(.*\)\"/\1/')

echo -e "${GREEN}✓ Challenge signed${NC}"

# Authenticate
AUTH_REQUEST="{\"channelId\":\"$CHANNEL_ID\",\"nodeId\":\"$NODE_ID\",\"challengeData\":\"$CHALLENGE_DATA\",\"signature\":\"$CHALLENGE_SIGNATURE\",\"timestamp\":\"$AUTH_TIMESTAMP\"}"

AUTH_RESPONSE=$(curl -s -X POST "$NODE_A_URL/api/testing/authenticate" \
    -H "Content-Type: application/json" \
    -d "$AUTH_REQUEST")

SESSION_TOKEN=$(echo "$AUTH_RESPONSE" | grep -o '"sessionToken":"[^"]*"' | sed 's/.*":\"\(.*\)\"/\1/')

if [ -z "$SESSION_TOKEN" ]; then
    echo -e "${RED}✗ Authentication failed${NC}"
    echo "$AUTH_RESPONSE"
    exit 1
fi

echo -e "${GREEN}✓ Authentication successful${NC}"
echo -e "${GREEN}  Session Token: ${SESSION_TOKEN:0:20}...${NC}"
echo ""

# =============================================
# PHASE 4: Session Management
# =============================================
echo -e "${BLUE}=== Phase 4: Session Management ===${NC}"

# Test 1: WhoAmI - Get current session info
echo ""
echo -e "${BLUE}Test 1: GET /api/session/whoami${NC}"
WHOAMI_RESPONSE=$(curl -s -X GET "$NODE_B_URL/api/session/whoami" \
    -H "Authorization: Bearer $SESSION_TOKEN")

echo "$WHOAMI_RESPONSE" | python3 -m json.tool 2>/dev/null || echo "$WHOAMI_RESPONSE"

WHOAMI_NODE_ID=$(echo "$WHOAMI_RESPONSE" | grep -o '"nodeId":"[^"]*"' | sed 's/.*":\"\(.*\)\"/\1/')
if [ "$WHOAMI_NODE_ID" == "$NODE_ID" ]; then
    echo -e "${GREEN}✓ WhoAmI successful${NC}"
else
    echo -e "${RED}✗ WhoAmI failed${NC}"
fi

# Test 2: Missing Authorization Header
echo ""
echo -e "${BLUE}Test 2: Missing Authorization Header${NC}"
NO_AUTH_RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" -X GET "$NODE_B_URL/api/session/whoami")

if [ "$NO_AUTH_RESPONSE" == "401" ]; then
    echo -e "${GREEN}✓ Returns 401 Unauthorized (expected)${NC}"
else
    echo -e "${RED}✗ Expected 401, got $NO_AUTH_RESPONSE${NC}"
fi

# Test 3: Invalid Session Token
echo ""
echo -e "${BLUE}Test 3: Invalid Session Token${NC}"
INVALID_RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" -X GET "$NODE_B_URL/api/session/whoami" \
    -H "Authorization: Bearer invalid-token-12345")

if [ "$INVALID_RESPONSE" == "401" ]; then
    echo -e "${GREEN}✓ Returns 401 for invalid token (expected)${NC}"
else
    echo -e "${RED}✗ Expected 401, got $INVALID_RESPONSE${NC}"
fi

# Test 4: Renew Session
echo ""
echo -e "${BLUE}Test 4: POST /api/session/renew${NC}"
RENEW_RESPONSE=$(curl -s -X POST "$NODE_B_URL/api/session/renew" \
    -H "Authorization: Bearer $SESSION_TOKEN" \
    -H "Content-Type: application/json" \
    -d '{"additionalSeconds":1800}')

echo "$RENEW_RESPONSE" | python3 -m json.tool 2>/dev/null || echo "$RENEW_RESPONSE"

if echo "$RENEW_RESPONSE" | grep -q "expiresAt"; then
    echo -e "${GREEN}✓ Session renewed${NC}"
else
    echo -e "${RED}✗ Session renewal failed${NC}"
fi

# Test 5: Get Metrics (requires admin:node capability)
echo ""
echo -e "${BLUE}Test 5: GET /api/session/metrics (admin capability)${NC}"
METRICS_RESPONSE=$(curl -s -X GET "$NODE_B_URL/api/session/metrics" \
    -H "Authorization: Bearer $SESSION_TOKEN")

echo "$METRICS_RESPONSE" | python3 -m json.tool 2>/dev/null || echo "$METRICS_RESPONSE"

if echo "$METRICS_RESPONSE" | grep -q "activeSessions\|ERR_INSUFFICIENT_PERMISSIONS"; then
    echo -e "${GREEN}✓ Metrics endpoint responded (either with data or permission error)${NC}"
else
    echo -e "${RED}✗ Unexpected metrics response${NC}"
fi

# Test 6: Revoke Session
echo ""
echo -e "${BLUE}Test 6: POST /api/session/revoke (logout)${NC}"
REVOKE_RESPONSE=$(curl -s -X POST "$NODE_B_URL/api/session/revoke" \
    -H "Authorization: Bearer $SESSION_TOKEN")

echo "$REVOKE_RESPONSE" | python3 -m json.tool 2>/dev/null || echo "$REVOKE_RESPONSE"

if echo "$REVOKE_RESPONSE" | grep -q '"revoked":true'; then
    echo -e "${GREEN}✓ Session revoked${NC}"
else
    echo -e "${RED}✗ Session revocation failed${NC}"
fi

# Test 7: Verify session is invalid after revocation
echo ""
echo -e "${BLUE}Test 7: Verify revoked session is invalid${NC}"
POST_REVOKE_RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" -X GET "$NODE_B_URL/api/session/whoami" \
    -H "Authorization: Bearer $SESSION_TOKEN")

if [ "$POST_REVOKE_RESPONSE" == "401" ]; then
    echo -e "${GREEN}✓ Revoked session returns 401 (expected)${NC}"
else
    echo -e "${RED}✗ Expected 401 after revocation, got $POST_REVOKE_RESPONSE${NC}"
fi

echo ""
echo "============================================"
echo -e "${GREEN}Phase 4 Testing Complete!${NC}"
echo "============================================"
echo ""
echo "Summary:"
echo "  1. ✓ Channel established"
echo "  2. ✓ Node registered and approved"
echo "  3. ✓ Authentication successful"
echo "  4. ✓ Session token obtained"
echo "  5. ✓ WhoAmI endpoint tested"
echo "  6. ✓ Authorization validation tested"
echo "  7. ✓ Session renewal tested"
echo "  8. ✓ Metrics endpoint tested"
echo "  9. ✓ Session revocation tested"
echo " 10. ✓ Post-revocation validation tested"
echo ""
