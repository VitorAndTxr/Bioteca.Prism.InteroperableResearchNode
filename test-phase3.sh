#!/bin/bash

# Phase 3 Manual Testing Script
# Complete end-to-end test of authentication flow

set -e

echo "=== PHASE 3 MANUAL TESTING SCRIPT ==="
echo ""

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# ============================================================================
# Helper Functions
# ============================================================================

# Extract JSON field
get_json_field() {
    echo "$1" | grep -o "\"$2\"[[:space:]]*:[[:space:]]*\"[^\"]*\"" | sed 's/.*"\(.*\)"/\1/'
}

# ============================================================================
# PHASE 1: Establish Encrypted Channel
# ============================================================================
echo -e "${YELLOW}PHASE 1: Establishing encrypted channel...${NC}"

CHANNEL_RESPONSE=$(curl -s -X POST http://localhost:5000/api/channel/initiate \
    -H "Content-Type: application/json" \
    -d '{"remoteNodeUrl": "http://irn-node-b:8080"}')

CHANNEL_ID=$(echo "$CHANNEL_RESPONSE" | grep -o '"channelId":"[^"]*"' | sed 's/.*":\"\(.*\)\"/\1/')

echo -e "${GREEN}✓ Channel established: $CHANNEL_ID${NC}"
echo ""

# ============================================================================
# PHASE 2: Generate Node Identity and Register
# ============================================================================
echo -e "${YELLOW}PHASE 2: Generating node identity...${NC}"

IDENTITY_RESPONSE=$(curl -s -X POST http://localhost:5000/api/testing/generate-node-identity \
    -H "Content-Type: application/json" \
    -d "{\"nodeId\": \"test-node-001\", \"nodeName\": \"Test Node 001\", \"channelId\": \"$CHANNEL_ID\", \"validityYears\": 2, \"password\": \"test123\"}")

# Save to file for inspection
echo "$IDENTITY_RESPONSE" > identity-response.json

# Extract certificate (public key) and certificateWithPrivateKey (PFX)
CERTIFICATE=$(echo "$IDENTITY_RESPONSE" | grep -o '"certificate":"[^"]*"' | head -1 | sed 's/.*":\"\(.*\)\"/\1/')
CERTIFICATE_WITH_KEY=$(echo "$IDENTITY_RESPONSE" | grep -o '"certificateWithPrivateKey":"[^"]*"' | sed 's/.*":\"\(.*\)\"/\1/')

echo -e "${GREEN}✓ Node identity generated${NC}"
echo ""

# Build registration request from identificationRequest in response
echo -e "${YELLOW}PHASE 2.2: Encrypting and sending registration request...${NC}"

# Extract identification request fields
NODE_ID="test-node-001"
NODE_NAME="Test Node 001"
SIGNATURE=$(echo "$IDENTITY_RESPONSE" | grep -o '"signature":"[^"]*"' | sed 's/.*":\"\(.*\)\"/\1/')
TIMESTAMP=$(echo "$IDENTITY_RESPONSE" | grep -o '"timestamp":"[^"]*"' | sed 's/.*":\"\(.*\)\"/\1/')

# Build registration payload
REGISTRATION_PAYLOAD="{\"channelId\":\"$CHANNEL_ID\",\"nodeId\":\"$NODE_ID\",\"nodeName\":\"$NODE_NAME\",\"nodeUrl\":\"http://test-node-001:8080\",\"certificate\":\"$CERTIFICATE\",\"signature\":\"$SIGNATURE\",\"timestamp\":\"$TIMESTAMP\",\"requestedCapabilities\":[\"query:read\",\"query:aggregate\"]}"

# Encrypt payload
ENCRYPT_REQUEST="{\"channelId\":\"$CHANNEL_ID\",\"payload\":$REGISTRATION_PAYLOAD}"
ENCRYPTED_PAYLOAD=$(curl -s -X POST http://localhost:5000/api/testing/encrypt-payload \
    -H "Content-Type: application/json" \
    -d "$ENCRYPT_REQUEST" | grep -o '"encryptedPayload":{[^}]*}' | sed 's/"encryptedPayload"://')

# Send to Node B
REGISTRATION_RESPONSE=$(curl -s -X POST http://localhost:5001/api/node/register \
    -H "Content-Type: application/json" \
    -H "X-Channel-Id: $CHANNEL_ID" \
    -d "$ENCRYPTED_PAYLOAD")

echo -e "${GREEN}✓ Registration request sent${NC}"
echo ""

# ============================================================================
# PHASE 2.3: Approve Node (on Node B where it was registered)
# ============================================================================
echo -e "${YELLOW}PHASE 2.3: Approving node on Node B...${NC}"

APPROVAL_RESPONSE=$(curl -s -X PUT http://localhost:5001/api/node/$NODE_ID/status \
    -H "Content-Type: application/json" \
    -d '{"status": "Authorized", "grantedCapabilities": ["query:read", "query:aggregate"]}')

echo -e "${GREEN}✓ Node approved on Node B${NC}"
echo ""

# ============================================================================
# PHASE 3.1: Request Challenge (from Node B where node is registered)
# ============================================================================
echo -e "${YELLOW}PHASE 3.1: Requesting authentication challenge from Node B...${NC}"

CHALLENGE_REQUEST="{\"channelId\":\"$CHANNEL_ID\",\"nodeId\":\"$NODE_ID\"}"
CHALLENGE_RESPONSE=$(curl -s -X POST http://localhost:5001/api/testing/request-challenge \
    -H "Content-Type: application/json" \
    -d "$CHALLENGE_REQUEST")

# Save for inspection
echo "$CHALLENGE_RESPONSE" > challenge-response.json

CHALLENGE_DATA=$(echo "$CHALLENGE_RESPONSE" | grep -o '"challengeData":"[^"]*"' | sed 's/.*":\"\(.*\)\"/\1/')
CHALLENGE_TTL=$(echo "$CHALLENGE_RESPONSE" | grep -o '"challengeTtlSeconds":[0-9]*' | sed 's/.*:\([0-9]*\)/\1/')

echo -e "${GREEN}✓ Challenge received${NC}"
echo "  Challenge Data: ${CHALLENGE_DATA:0:40}..."
echo "  TTL: $CHALLENGE_TTL seconds"
echo ""

# ============================================================================
# PHASE 3.2: Sign Challenge (on Node A - client side)
# ============================================================================
echo -e "${YELLOW}PHASE 3.2: Signing challenge with private key on Node A...${NC}"

AUTH_TIMESTAMP=$(date -u +"%Y-%m-%dT%H:%M:%S.%6NZ")

SIGN_CHALLENGE_REQUEST="{\"challengeData\":\"$CHALLENGE_DATA\",\"channelId\":\"$CHANNEL_ID\",\"nodeId\":\"$NODE_ID\",\"certificateWithPrivateKey\":\"$CERTIFICATE_WITH_KEY\",\"password\":\"test123\",\"timestamp\":\"$AUTH_TIMESTAMP\"}"

SIGN_RESPONSE=$(curl -s -X POST http://localhost:5000/api/testing/sign-challenge \
    -H "Content-Type: application/json" \
    -d "$SIGN_CHALLENGE_REQUEST")

# Save for inspection
echo "$SIGN_RESPONSE" > sign-response.json

CHALLENGE_SIGNATURE=$(echo "$SIGN_RESPONSE" | grep -o '"signature":"[^"]*"' | sed 's/.*":\"\(.*\)\"/\1/')

echo -e "${GREEN}✓ Challenge signed on Node A${NC}"
echo "  Signature: ${CHALLENGE_SIGNATURE:0:40}..."
echo ""

# ============================================================================
# PHASE 3.3: Authenticate (send back to Node B)
# ============================================================================
echo -e "${YELLOW}PHASE 3.3: Authenticating with signed challenge on Node B...${NC}"

AUTHENTICATE_REQUEST="{\"channelId\":\"$CHANNEL_ID\",\"nodeId\":\"$NODE_ID\",\"challengeData\":\"$CHALLENGE_DATA\",\"signature\":\"$CHALLENGE_SIGNATURE\",\"timestamp\":\"$AUTH_TIMESTAMP\"}"

AUTH_RESPONSE=$(curl -s -X POST http://localhost:5001/api/testing/authenticate \
    -H "Content-Type: application/json" \
    -d "$AUTHENTICATE_REQUEST")

# Save for inspection
echo "$AUTH_RESPONSE" > auth-response.json

SESSION_TOKEN=$(echo "$AUTH_RESPONSE" | grep -o '"sessionToken":"[^"]*"' | sed 's/.*":\"\(.*\)\"/\1/')
AUTHENTICATED=$(echo "$AUTH_RESPONSE" | grep -o '"authenticated":[^,}]*' | sed 's/.*:\(.*\)/\1/')

if [ "$AUTHENTICATED" = "true" ]; then
    echo -e "${GREEN}✓ AUTHENTICATION SUCCESSFUL!${NC}"
    echo ""
    echo -e "${CYAN}SESSION DETAILS:${NC}"
    echo "  Authenticated: $AUTHENTICATED"
    echo "  Session Token: $SESSION_TOKEN"
    echo ""
    echo -e "${GREEN}=== TEST COMPLETED SUCCESSFULLY ===${NC}"
    echo ""
    echo "SUMMARY:"
    echo "  ✓ Phase 1: Encrypted channel established"
    echo "  ✓ Phase 2: Node registered and approved"
    echo "  ✓ Phase 3: Challenge-response authentication completed"
    echo ""
    echo "Session token is valid for 1 hour."
else
    echo -e "${RED}✗ AUTHENTICATION FAILED${NC}"
    echo "Response:"
    echo "$AUTH_RESPONSE"
    exit 1
fi
