# InteroperableResearchInterfaceSystem

**Component Type**: Interface Layer / Middleware
**Technology**: TypeScript, Node.js, MCP Tunnel
**Purpose**: Communication orchestration between PRISM components

---

## Overview

The **InteroperableResearchInterfaceSystem** serves as the middleware layer in the PRISM ecosystem, facilitating communication between the mobile application, hardware devices, and research nodes.

**Core Purpose**: Protocol translation and message routing to enable seamless interaction between heterogeneous system components.

---

## Key Features

### Protocol Translation
- Translates between different communication protocols
- Handles message format conversion
- Ensures compatibility between components

### WebSocket/HTTP Communication Management
- Real-time bidirectional communication
- HTTP/REST API gateway
- Connection pooling and management

### API Gateway and Routing
- Centralized entry point for requests
- Intelligent routing based on message type
- Load balancing (planned)

---

## Technology Stack

| Component | Technology | Purpose |
|-----------|-----------|---------|
| **Runtime** | Node.js | JavaScript runtime for server-side execution |
| **Language** | TypeScript | Type-safe JavaScript superset |
| **Communication** | MCP Tunnel | Secure tunneling protocol |
| **Protocols** | WebSocket, HTTP/HTTPS | Real-time and request-response communication |

---

## Role in PRISM Ecosystem

The Interface System bridges three main components:

```
┌──────────────────┐
│  Mobile App      │
│ (React Native)   │
└────────┬─────────┘
         │ HTTPS/WebSocket
         ▼
┌──────────────────────────┐
│  Interface System        │
│  - Protocol Translation  │
│  - Message Routing       │
│  - API Gateway           │
└───┬──────────────────┬───┘
    │                  │
    │ Bluetooth (SPP)  │ HTTPS (Encrypted)
    ▼                  ▼
┌─────────────┐   ┌──────────────────┐
│  sEMG       │   │  Research Node   │
│  Device     │   │  (Backend)       │
└─────────────┘   └──────────────────┘
```

### Integration Points

1. **Mobile App ↔ Interface System**
   - WebSocket for real-time streaming
   - HTTP/REST for configuration and commands
   - Message queuing for reliability

2. **Interface System ↔ sEMG Device**
   - Bluetooth Serial Port Profile (SPP)
   - JSON message protocol translation
   - Binary streaming data handling

3. **Interface System ↔ Research Node**
   - HTTPS with encrypted channels
   - 4-phase handshake protocol support
   - Session management and token handling

---

## Use Cases

### Real-Time sEMG Streaming
1. Mobile app requests streaming via WebSocket
2. Interface system forwards command to device via Bluetooth
3. Device sends binary sEMG packets (215 Hz)
4. Interface system translates and forwards to mobile app
5. Mobile app visualizes real-time biosignals

### FES Session Management
1. Mobile app configures FES parameters
2. Interface system translates to device protocol
3. Device executes therapeutic stimulation
4. Interface system relays status updates to mobile app
5. Session data prepared for research node submission

### Data Submission to Research Node
1. Mobile app packages session data
2. Interface system initiates handshake with research node
3. Encrypted channel established (Phase 1)
4. Session data submitted with metadata
5. Interface system returns confirmation to mobile app

---

## Communication Protocols

### Inbound (from Mobile App)
- **Protocol**: WebSocket (real-time), HTTP/HTTPS (commands)
- **Format**: JSON
- **Message Types**:
  - Device commands (start session, configure parameters)
  - Streaming requests (start/stop)
  - Data submission requests

### Outbound to Device (Bluetooth)
- **Protocol**: Bluetooth SPP (Serial Port Profile)
- **Format**: JSON (control) + Binary (streaming)
- **Message Codes**: 1-14 (see sEMG device protocol)

### Outbound to Research Node (HTTPS)
- **Protocol**: HTTPS with TLS 1.3
- **Format**: JSON (encrypted payloads)
- **Authentication**: 4-phase handshake protocol
- **Session**: Bearer token management

---

## Architecture Patterns

### Message Queue
- Asynchronous message processing
- Reliability and fault tolerance
- Backpressure handling

### Event-Driven
- Event emitters for component communication
- Loose coupling between modules
- Scalability

### Middleware Pattern
- Request/response transformation
- Logging and monitoring
- Error handling

---

## Development Status

**Current State**: Functional prototype
**In Progress**:
- Protocol translation layer
- WebSocket server implementation
- Bluetooth connection management

**Planned**:
- Load balancing for multiple devices
- Message queue persistence
- Advanced error recovery
- Metrics and monitoring

---

## Development Commands

```bash
# Navigate to interface system directory
cd InteroperableResearchInterfaceSystem

# Install dependencies
npm install

# Run in development mode
npm run dev

# Build for production
npm run build

# Run tests
npm test

# Start production server
npm start
```

---

## Configuration

**Environment Variables**:
```bash
# Server configuration
PORT=3000
NODE_ENV=production

# Research node connection
RESEARCH_NODE_URL=https://localhost:5000
RESEARCH_NODE_CERT_PATH=/path/to/cert.pem

# Bluetooth configuration
BLUETOOTH_DEVICE_NAME=NeuroEstimulator
BLUETOOTH_AUTO_RECONNECT=true

# WebSocket configuration
WS_HEARTBEAT_INTERVAL=30000
WS_MAX_CONNECTIONS=100
```

---

## Documentation References

For detailed information, see:

- **Main Documentation**: `InteroperableResearchInterfaceSystem/CLAUDE.md` (if exists)
- **API Documentation**: `InteroperableResearchInterfaceSystem/docs/API.md` (if exists)
- **Bluetooth Protocol**: `InteroperableResearchsEMGDevice/docs/api/COMPLETE_BLUETOOTH_PROTOCOL.md`
- **Research Node Integration**: `InteroperableResearchNode/docs/architecture/handshake-protocol.md`
- **Mobile App Integration**: `neurax_react_native_app/` (if documented)

---

## Known Limitations

- Single-device connection (multi-device support planned)
- No message persistence (planned for future)
- Limited error recovery mechanisms
- Not yet production-ready (prototype phase)

---

## Future Enhancements

1. **Multi-Device Support**: Connect to multiple sEMG devices simultaneously
2. **Message Persistence**: Redis-backed message queue
3. **Advanced Routing**: Intelligent routing based on device capabilities
4. **Monitoring**: Prometheus metrics and OpenTelemetry tracing
5. **High Availability**: Redis Sentinel for session persistence
6. **Load Balancing**: Distribute connections across multiple interface instances
