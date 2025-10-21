# neurax_react_native_app - Mobile Application

**Component Type**: Mobile Application
**Technology**: React Native, Expo, TypeScript
**Purpose**: User interface for research data collection and device control

---

## Overview

The **neurax React Native mobile application** is the primary user interface for the PRISM ecosystem. It enables researchers and clinicians to control sEMG devices, visualize biosignals in real-time, manage research sessions, and submit data to research nodes.

**Core Purpose**: Provides the **Application** abstraction in the PRISM model - general-purpose software that adds context (volunteer info, session metadata), controls hardware, and submits data to research nodes.

---

## Key Features

### Bluetooth Communication with sEMG Device
- Auto-discovery and pairing with NeuroEstimulator devices
- JSON command protocol for FES session control
- Binary protocol for real-time sEMG streaming (215 Hz)
- Connection status monitoring and auto-reconnect

### Real-Time Biosignal Visualization
- Live sEMG waveform display (215 Hz sampling rate)
- Filtered, raw, and RMS envelope modes
- Trigger detection indicators
- Multi-channel support (planned)

### Session Management
- Create and configure therapeutic FES sessions
- Set stimulation parameters (amplitude, frequency, pulse width, difficulty, duration)
- Monitor session status (stimulus count, duration, triggers)
- Pause/resume/stop session controls

### FES Parameter Configuration
- Amplitude: 0-15V slider control
- Frequency: 1-100 Hz selector
- Pulse width: 1-100 ms selector
- Difficulty threshold: 1-100% (sEMG sensitivity)
- Pulse duration: 1-60 seconds per stimulus

### User Interface for Researchers and Volunteers
- Researcher dashboard (session management, volunteer selection)
- Volunteer profile management
- Research project selection
- Session notes and annotations
- Battery status monitoring

### Data Submission to Research Nodes (Planned)
- Package session data with metadata
- Authenticate with research node (4-phase handshake)
- Submit encrypted biosignal files
- Receive confirmation and storage ID

---

## Technology Stack

| Component | Technology | Purpose |
|-----------|-----------|---------|
| **Framework** | React Native | Cross-platform mobile development (iOS/Android) |
| **Build Tool** | Expo | Rapid development and deployment |
| **Language** | TypeScript | Type-safe JavaScript |
| **State Management** | Redux / Context API | Application state management |
| **Bluetooth** | react-native-ble-plx | Bluetooth Low Energy communication |
| **Charting** | react-native-chart-kit | Real-time waveform visualization |
| **Navigation** | React Navigation | Screen routing and navigation |
| **Storage** | AsyncStorage / SQLite | Local data persistence |

---

## Role in PRISM Ecosystem

```
┌────────────────────────────────────────────┐
│         Mobile Application                  │
│  ┌──────────────────────────────────────┐  │
│  │  User Interface                      │  │
│  │  - Researcher Dashboard              │  │
│  │  - Session Configuration             │  │
│  │  - Real-Time Visualization           │  │
│  └──────────────────────────────────────┘  │
│              │                              │
│              ▼                              │
│  ┌──────────────────────────────────────┐  │
│  │  Business Logic                      │  │
│  │  - Session Management                │  │
│  │  - Data Packaging                    │  │
│  │  - Authentication                    │  │
│  └──────────────────────────────────────┘  │
│              │                              │
│              ▼                              │
│  ┌──────────────────────────────────────┐  │
│  │  Communication Layer                 │  │
│  │  - Bluetooth Manager                 │  │
│  │  - HTTPS Client                      │  │
│  │  - Protocol Handlers                 │  │
│  └──────────────────────────────────────┘  │
└─────────┬────────────────────────┬─────────┘
          │ Bluetooth              │ HTTPS
          │ (SPP/JSON)             │ (Encrypted)
          ▼                        ▼
┌──────────────────┐      ┌─────────────────────┐
│  sEMG Device     │      │  Research Node      │
│  (ESP32)         │      │  (Backend Server)   │
└──────────────────┘      └─────────────────────┘
```

### Integration Points

1. **Mobile App ↔ sEMG Device (Bluetooth)**
   - Device discovery and pairing
   - JSON commands for FES control (Message Codes 1-14)
   - Binary streaming for real-time sEMG data (Code 13)
   - Status monitoring (battery, connection)

2. **Mobile App ↔ Research Node (HTTPS)**
   - Node discovery and registration
   - 4-phase handshake authentication
   - Encrypted data submission
   - Session token management

3. **Mobile App ↔ User**
   - Touch-based UI for configuration
   - Visual feedback for stimulation events
   - Real-time waveform display
   - Session history and notes

---

## Key Screens

### 1. Device Connection Screen
- Scan for available NeuroEstimulator devices
- Display device name, signal strength, battery level
- Connect/disconnect button
- Connection status indicator

### 2. Session Configuration Screen
- Research project selector
- Volunteer selector
- FES parameter sliders:
  - Amplitude (V)
  - Frequency (Hz)
  - Pulse Width (ms)
  - Difficulty (%)
  - Duration (seconds)
- Start session button

### 3. Live Session Screen
- Real-time sEMG waveform chart
- Trigger detection indicator (LED simulation)
- Stimulus counter (complete/interrupted)
- Session timer
- Pause/Resume/Stop controls
- Battery level indicator

### 4. Data Visualization Screen
- Historical session data
- Waveform playback
- Statistical analysis (mean, std dev, peak detection)
- Export options (CSV, JSON)

### 5. Research Node Connection Screen (Planned)
- Node URL configuration
- Authentication status
- Data submission queue
- Upload progress

---

## Bluetooth Protocol Implementation

The mobile app implements the complete 14-message Bluetooth protocol:

### Command Messages (App → Device)
| Code | Message | Usage |
|------|---------|-------|
| 1 | Gyroscope Reading | Read IMU data for position tracking |
| 2 | Session Start | Begin FES therapeutic session |
| 3 | Session Stop | Terminate active session |
| 4 | Session Pause | Temporarily suspend session |
| 5 | Session Resume | Resume paused session |
| 6 | Single Stimulus | Manual trigger (testing) |
| 7 | Set Parameters | Configure FES settings |
| 10 | Battery Status | Request battery levels |
| 11 | Start Streaming | Begin real-time sEMG data |
| 12 | Stop Streaming | End streaming mode |
| 14 | Stream Config | Configure rate/type (deprecated) |

### Response Messages (Device → App)
| Code | Message | Usage |
|------|---------|-------|
| 8 | Session Status | Periodic status updates (stimulus counts, duration) |
| 9 | Trigger Detected | Muscle activation threshold crossed |
| 13 | Streaming Data | Binary sEMG packets (50 samples @ 215 Hz) |

### Example Protocol Flow

```typescript
// 1. Configure FES parameters
const fesParams = {
  cd: 7,  // Message Code 7: Set Parameters
  mt: 'w',  // Method: write
  bd: {
    a: 3.0,    // Amplitude (V)
    f: 38.0,   // Frequency (Hz)
    pw: 12.0,  // Pulse width (ms)
    df: 50,    // Difficulty (%)
    pd: 5      // Duration (seconds)
  }
};
await bluetoothManager.sendCommand(JSON.stringify(fesParams));

// 2. Start FES session
const startSession = { cd: 2, mt: 'x' };
await bluetoothManager.sendCommand(JSON.stringify(startSession));

// 3. Listen for trigger events
bluetoothManager.on('message', (data) => {
  const message = JSON.parse(data);
  if (message.cd === 9) {
    console.log('Trigger detected!');
    updateUI({ triggerDetected: true });
  }
});

// 4. Start streaming real-time sEMG
const startStream = { cd: 11, mt: 'x' };
await bluetoothManager.sendCommand(JSON.stringify(startStream));

// 5. Handle binary streaming data (Code 13)
bluetoothManager.on('binaryData', (packet) => {
  // Decode 108-byte packet: magic + code + timestamp + count + samples[50]
  const samples = decodeBinaryPacket(packet);
  updateWaveform(samples);
});
```

---

## Data Model

### Session Entity
```typescript
interface Session {
  id: string;
  researchProjectId: string;
  volunteerId: string;
  deviceId: string;
  startTimestamp: Date;
  endTimestamp?: Date;
  fesParameters: FESParameters;
  stimulusCount: {
    complete: number;
    interrupted: number;
  };
  triggerTimestamps: number[];
  biosignalFiles: string[];  // Local file paths
  notes: string;
  uploadStatus: 'pending' | 'uploading' | 'uploaded' | 'failed';
}

interface FESParameters {
  amplitude: number;      // 0-15V
  frequency: number;      // 1-100 Hz
  pulseWidth: number;     // 1-100 ms
  difficulty: number;     // 1-100%
  duration: number;       // 1-60 seconds
}
```

### Volunteer Entity
```typescript
interface Volunteer {
  id: string;
  volunteerCode: string;  // Anonymized identifier
  birthDate: Date;
  gender: string;
  bloodType: string;
  height?: number;
  weight?: number;
  medicalHistory: string;
  consentStatus: 'pending' | 'approved' | 'revoked';
}
```

---

## Development Commands

```bash
# Navigate to mobile app directory
cd neurax_react_native_app

# Install dependencies
npm install

# Start Expo development server
npm start

# Run on iOS simulator
npm run ios

# Run on Android emulator
npm run android

# Run tests
npm test

# Build for production (iOS)
expo build:ios

# Build for production (Android)
expo build:android
```

---

## Development Status

**Current State**: Prototype with basic functionality
**Completed**:
- ✅ Bluetooth device discovery
- ✅ Basic FES parameter configuration
- ✅ Session start/stop controls
- ✅ Real-time waveform display (partial)

**In Progress**:
- 🚧 Binary streaming protocol implementation
- 🚧 Complete UI/UX design
- 🚧 State management refactoring

**Planned**:
- 📋 Research node authentication
- 📋 Data submission workflow
- 📋 Offline mode with sync queue
- 📋 Multi-volunteer management
- 📋 Session history and analytics
- 📋 Export to CSV/PDF reports

---

## Configuration

**Environment Variables** (`.env`):
```bash
# API Configuration
REACT_APP_RESEARCH_NODE_URL=https://research-node.example.com
REACT_APP_API_TIMEOUT=30000

# Bluetooth Configuration
REACT_APP_DEVICE_NAME_PREFIX=NeuroEstimulator
REACT_APP_AUTO_RECONNECT=true
REACT_APP_CONNECTION_TIMEOUT=15000

# Feature Flags
REACT_APP_ENABLE_STREAMING=true
REACT_APP_ENABLE_DATA_SUBMISSION=false
REACT_APP_DEBUG_MODE=false
```

---

## Documentation References

For detailed information, see:

- **Main Documentation**: `neurax_react_native_app/README.md` (if exists)
- **Bluetooth Protocol**: `InteroperableResearchsEMGDevice/docs/api/COMPLETE_BLUETOOTH_PROTOCOL.md`
- **Binary Streaming**: `InteroperableResearchsEMGDevice/docs/api/bluetooth-protocol.md`
- **Research Node API**: `InteroperableResearchNode/docs/architecture/handshake-protocol.md`
- **Data Model**: `InteroperableResearchNode/docs/architecture/PROJECT_STRUCTURE.md`

---

## Known Limitations

- iOS/Android platform differences in Bluetooth handling
- Limited error recovery for connection drops
- No offline data persistence (planned)
- Single device connection only
- No background mode support for long sessions

---

## Future Enhancements

1. **Multi-Device Support**: Connect to multiple devices simultaneously
2. **Cloud Sync**: Automatic data backup and synchronization
3. **Advanced Analytics**: Statistical analysis and ML-based insights
4. **Push Notifications**: Session reminders and completion notifications
5. **Multi-Language Support**: Internationalization (i18n)
6. **Accessibility**: Screen reader support and high contrast mode
7. **Wearable Integration**: Sync with Apple Watch / Wear OS for vital signs
