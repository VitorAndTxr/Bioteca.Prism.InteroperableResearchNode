# PRISM InteroperableResearchNode API Documentation

## Overview

The PRISM InteroperableResearchNode implements a secure 4-phase handshake protocol for federated communication between research nodes, plus a separate JWT-based authentication system for human users (researchers). This documentation covers all API endpoints, request/response formats, and migration guides.

**Base URLs**:
- Node A: `http://localhost:5000`
- Node B: `http://localhost:5001`

**Protocol Version**: 1.0
**API Version**: v0.10.0

## Authentication Systems

The application implements **two distinct authentication systems**:

### System A: User Authentication (Researchers)
- **Pattern**: Standard Bearer JWT in `Authorization` header
- **Format**: `Authorization: Bearer {JWT_TOKEN}`
- **Usage**: Human users (researchers) accessing the system
- **Documentation**: [user-authentication.md](user-authentication.md)

### System B: Node-to-Node Authentication (4-Phase Handshake)
- **Pattern**: Custom headers + encrypted payloads
- **Headers**: `X-Channel-Id`, `X-Session-Id` (v0.10.0+)
- **Usage**: Federated node communication
- **Documentation**: Phase 1-4 documents below

## API Endpoint Categories

### Phase 1: Encrypted Channel Establishment
[📄 Full Documentation](phase1-channel.md)

| Endpoint | Method | Description | Auth Required |
|----------|--------|-------------|---------------|
| `/api/channel/open` | POST | Open encrypted channel (server) | No |
| `/api/channel/initiate` | POST | Initiate handshake (client) | No |
| `/api/channel/{channelId}` | GET | Get channel info (debug) | No |
| `/api/channel/health` | GET | Health check | No |

### Phase 2: Node Identification
[📄 Full Documentation](phase2-identification.md)

| Endpoint | Method | Description | Auth Required |
|----------|--------|-------------|---------------|
| `/api/node/identify` | POST | Identify node with certificate | Channel |
| `/api/node/register` | POST | Register new node | Channel |
| `/api/node/nodes` | GET | List all nodes | No |
| `/api/node/{id}/status` | PUT | Update node status | Admin |

### Phase 3: Mutual Authentication
[📄 Full Documentation](phase3-authentication.md)

| Endpoint | Method | Description | Auth Required |
|----------|--------|-------------|---------------|
| `/api/node/challenge` | POST | Request authentication challenge | Channel + Identified |
| `/api/node/authenticate` | POST | Submit challenge response | Channel + Identified |

### Phase 4: Session Management
[📄 Full Documentation](phase4-session.md)

| Endpoint | Method | Description | Auth Required |
|----------|--------|-------------|---------------|
| `/api/session/whoami` | POST | Get session info | Channel + Session |
| `/api/session/renew` | POST | Extend session TTL | Channel + Session |
| `/api/session/revoke` | POST | Revoke session | Channel + Session |
| `/api/session/metrics` | POST | Get metrics (admin) | Channel + Session + Admin |

### User Authentication (JWT)
[📄 Full Documentation](user-authentication.md)

| Endpoint | Method | Description | Auth Required |
|----------|--------|-------------|---------------|
| `/api/userauth/login` | POST | User login | No |
| `/api/userauth/refreshtoken` | POST | Refresh JWT token | Bearer JWT |
| `/api/userauth/encrypt` | POST | Encrypt text (utility) | No |

## Security Requirements by Phase

### Phase 1: No Authentication
- Open endpoints for initial channel establishment
- ECDH P-384 key exchange
- Results in encrypted channel (`X-Channel-Id`)

### Phase 2: Channel Encryption Required
- Header: `X-Channel-Id: {channelId}`
- All payloads encrypted with AES-256-GCM
- Node identification via X.509 certificates

### Phase 3: Channel + Identification Required
- Header: `X-Channel-Id: {channelId}`
- Node must be identified from Phase 2
- Challenge-response with RSA-2048 signatures

### Phase 4: Full Authentication Required

#### v0.10.0+ (New Pattern)
```http
X-Channel-Id: {channelId}
X-Session-Id: {sessionId}
Content-Type: application/json
```

#### v0.9.x (Deprecated)
```http
X-Channel-Id: {channelId}
Content-Type: application/json

{
  "sessionToken": "{sessionId}",  // In encrypted body
  ...
}
```

## Migration Guide

### Migrating to v0.10.0

**Important**: Version 0.10.0 introduces the `X-Session-Id` header for Phase 4 endpoints.

[📄 Full Migration Guide](migration-guide.md)

**Key Changes**:
1. Session tokens moved from request body to `X-Session-Id` header
2. Dual support period (both patterns work)
3. Body pattern deprecated, will be removed in v0.11.0

**Quick Migration**:
```diff
// Old (v0.9.x)
POST /api/session/whoami
X-Channel-Id: abc-123
{
  "sessionToken": "def-456",
  "timestamp": "2025-10-23T10:00:00Z"
}

// New (v0.10.0+)
POST /api/session/whoami
X-Channel-Id: abc-123
+ X-Session-Id: def-456
{
-  "sessionToken": "def-456",
  "timestamp": "2025-10-23T10:00:00Z"
}
```

## Common Headers

| Header | Required For | Format | Example |
|--------|-------------|--------|---------|
| `X-Channel-Id` | Phase 2-4 | GUID | `a1b2c3d4-e5f6-4789-a1b2-c3d4e5f67890` |
| `X-Session-Id` | Phase 4 (v0.10.0+) | GUID | `f7e6d5c4-b3a2-1098-7654-321098765432` |
| `Authorization` | User endpoints | Bearer JWT | `Bearer eyJhbGciOiJSUzI1NiIs...` |
| `Content-Type` | All POST/PUT | MIME type | `application/json` |

## Error Response Format

All errors follow this structure:

```json
{
  "error": {
    "code": "ERR_CODE",
    "message": "Human-readable error message",
    "details": {
      "field": "Additional context"
    },
    "retryable": true,
    "retryAfter": "2025-10-23T10:05:00Z"
  }
}
```

### Common Error Codes

| Code | Description | HTTP Status | Retryable |
|------|-------------|------------|-----------|
| `ERR_CHANNEL_FAILED` | Channel establishment failed | 400/500 | Yes |
| `ERR_INVALID_EPHEMERAL_KEY` | Invalid ECDH public key | 400 | Yes |
| `ERR_INCOMPATIBLE_VERSION` | Protocol version mismatch | 400 | No |
| `ERR_NODE_NOT_IDENTIFIED` | Phase 2 not completed | 400 | No |
| `ERR_NODE_NOT_AUTHORIZED` | Node status not authorized | 400 | No |
| `ERR_INVALID_CHALLENGE_RESPONSE` | Challenge verification failed | 400 | No |
| `ERR_NO_SESSION_CONTEXT` | Session not found/expired | 401 | No |
| `ERR_RATE_LIMIT_EXCEEDED` | Too many requests | 429 | Yes |

## Testing Tools

### curl Examples
Each endpoint documentation includes curl examples for testing.

### Postman Collection
Import the [postman-collection.json](postman-collection.json) for a complete testing suite with:
- Environment variables
- Pre-request scripts for encryption
- Automatic token management
- Test assertions

### Test Scripts
- `test-phase1.sh`: Channel establishment
- `test-phase2.sh`: Node identification
- `test-phase3.sh`: Authentication
- `test-phase4.sh`: Complete flow (Phases 1-4)
- `test-session-header-migration.sh`: v0.10.0 migration test

## Rate Limiting

Phase 4 endpoints implement rate limiting:
- **Limit**: 60 requests per minute per session
- **Implementation**: Redis Sorted Sets
- **Reset**: Sliding window
- **Error**: HTTP 429 with `Retry-After` header

## Encryption Details

### Phase 1: Key Exchange
- **Algorithm**: ECDH P-384
- **Key Derivation**: HKDF-SHA256
- **Info String**: `IRN-Channel-v1.0`
- **Salt**: Combined client + server nonces

### Channel Encryption
- **Algorithm**: AES-256-GCM
- **Key Size**: 256 bits
- **IV Size**: 96 bits (12 bytes)
- **Tag Size**: 128 bits (16 bytes)

### Encrypted Payload Format
```json
{
  "encryptedData": "base64-encoded-ciphertext",
  "iv": "base64-encoded-iv",
  "tag": "base64-encoded-auth-tag"
}
```

## Development Resources

### Quick Start
```bash
# Start infrastructure
docker-compose -f docker-compose.persistence.yml up -d
docker-compose -f docker-compose.application.yml up -d

# Test complete handshake
bash test-phase4.sh

# Access Swagger UI
open http://localhost:5000/swagger
```

### Related Documentation
- [Architecture Philosophy](../ARCHITECTURE_PHILOSOPHY.md)
- [Security Overview](../SECURITY_OVERVIEW.md)
- [Handshake Protocol](../architecture/handshake-protocol.md)
- [Manual Testing Guide](../testing/manual-testing-guide.md)
- [Project Status](../PROJECT_STATUS.md)

## Support

For issues or questions:
- GitHub Issues: [PRISM Repository](https://github.com/prism/interoperable-research-node)
- Documentation: Check the [docs/](../) directory
- Swagger UI: Available at `/swagger` on each node

---

**Version**: 0.10.0
**Last Updated**: October 2025
**Status**: Production Ready (Phase 1-4 Complete)