# InteroperableResearchsEMGDevice - NeuroEstimulator

**Component Type**: Hardware Device (Embedded Firmware)
**Technology**: ESP32, C++, FreeRTOS, PlatformIO
**Purpose**: Biomedical electrostimulation device combining Functional Electrical Stimulation (FES) with surface electromyography (sEMG)

---

## Overview

The **sEMG Device** (NeuroEstimulator) is an ESP32-based biomedical electrostimulation device for therapeutic neurorehabilitation. It operates as a standalone embedded device in the PRISM ecosystem, representing the **Device** abstraction for biosignal acquisition and therapeutic intervention.

**Core Purpose**: Real-time biosignal acquisition (sEMG), processing, analysis, and therapeutic stimulation delivery with wireless data transmission to research nodes for federated biomedical data management.

---

## Key Features

### Dual-Core FreeRTOS Real-Time Architecture
- **Core 0**: Time-critical biosignal sampling and trigger detection (860 Hz ADC)
- **Core 1**: Bluetooth communication, message processing, streaming data transmission

### Biosignal Acquisition Pipeline (215 Hz output)
- **Hardware ADC**: 860 SPS @ 16-bit (ADS1115 I2C)
- **Digital filtering**: 2nd-order Butterworth bandpass (10-40 Hz)
- **Downsampling**: 860 Hz → 215 Hz (4x decimation)
- **Trigger detection**: RMS-based threshold comparison

### FES Capabilities
Programmable biphasic pulse generation:
- **Amplitude**: 0-15V (digital potentiometer control)
- **Frequency**: 1-100 Hz
- **Pulse width**: 1-100 milliseconds
- **Duration**: 1-60 seconds per stimulus

### Real-Time Streaming Modes
- Raw ADC data (unfiltered)
- Filtered data (Butterworth-processed)
- RMS envelope (amplitude modulation tracking)
- Fixed rate: 215 Hz via binary protocol optimization

### Bluetooth Protocol
- **Dual-mode**: Binary protocol (optimized for bandwidth) + JSON fallback
- **Connection type**: SPP (Serial Port Profile) over HC-05/HC-06
- **Baud rates**: 9600 (default), upgradable to 115200

### Multi-Layer Safety Mechanisms
- Emergency stop (multi-level override capability)
- Voltage verification and error detection
- Automatic 10-minute streaming timeout
- Circular buffer overflow protection
- Mutual exclusion (FES session XOR streaming mode)

---

## Hardware Components

| Component | Model | Description |
|-----------|-------|-------------|
| **Microcontroller** | ESP32 DevKit V1 | Dual-core Tensilica Xtensa @ 240 MHz, 520 KB SRAM, WiFi/BLE capable |
| **Bluetooth Module** | HC-05/HC-06 | UART Serial Port Profile, 9600-115200 baud |
| **ADC** | ADS1115 | 16-bit, I2C @ 400 kHz, 860 SPS in continuous mode |
| **sEMG Front-End** | AD8232 | 1000x gain instrumentation amplifier for bioelectrical signals |
| **IMU** | MPU6050 | 6-axis accelerometer/gyroscope, I2C, position/motion tracking |
| **Motor Control** | H-Bridge circuit | Biphasic FES stimulus output |
| **Digital Potentiometer** | MCP4131 | SPI, 8-bit, for precise amplitude control 0-15V |
| **Battery Management** | Dual ADC channels | Stimulation and main battery voltage monitoring (low-battery: 3.0V) |
| **Status Indicators** | 3x LEDs | Power (connection), Trigger (detection), FES (stimulation active) |

---

## Signal Processing Architecture

```
Physical sEMG Signal
        │
        ▼
┌──────────────────────────┐
│ AD8232 Sensor (1000x)    │ ◄─── Preprocessing
└──────────────────────────┘
        │
        ▼
┌──────────────────────────┐
│ ADS1115 ADC (860 Hz)     │ ◄─── Hardware Sampling (16-bit)
└──────────────────────────┘
        │
        ▼
┌──────────────────────────┐
│ Butterworth Filter       │ ◄─── Digital Filter (10-40 Hz bandpass)
│ 2nd-order (FIR/IIR)      │
└──────────────────────────┘
        │
        ▼
┌──────────────────────────┐
│ Downsampler (4x)         │ ◄─── 860 Hz → 215 Hz
└──────────────────────────┘
        │
        ▼
    ┌───┴───┐
    │       │
    ▼       ▼
┌─────────┐ ┌──────────────────┐
│ Trigger │ │ Real-Time Stream │
│Detection│ │ Binary Protocol  │
└─────────┘ └──────────────────┘
    │              │
    ▼              ▼
┌─────────┐ ┌──────────────────┐
│ FES     │ │ Mobile App       │
│ Control │ │ Data Collection  │
└─────────┘ └──────────────────┘
    │
    ▼
┌──────────────────────────┐
│ H-Bridge (Biphasic)      │ ◄─── Therapeutic Output
│ Amplitude: 0-15V         │
└──────────────────────────┘
```

---

## Bluetooth Communication Protocol (v2.0)

### Dual-Protocol System

**1. JSON Command/Control Protocol** (Commands & Control)
- Used for FES configuration, session management, status queries
- All messages use standardized JSON structure with 14 message codes
- Human-readable format for debugging and development

**2. Binary Streaming Protocol** (Real-Time Data)
- Used for continuous 215 Hz sEMG data acquisition
- Optimized for bandwidth (464 bytes/s @ 9600 baud = 48% utilization)
- 108-byte fixed packet structure with magic byte synchronization

### Dual Protocol Message Structure

```json
{
  "cd": <integer 1-14>,     // Message code (identifies command type)
  "mt": "<method>",         // Method: 'r' (read), 'w' (write), 'x' (execute), 'a' (acknowledge)
  "bd": <optional object>   // Body: command-specific data
}
```

### All 14 Message Codes

| Code | Name | Purpose | Direction | v Min |
|------|------|---------|-----------|-------|
| 1 | Gyroscope Reading | Read MPU6050 6-axis IMU data | ↔ | 1.0 |
| 2 | Session Start | Begin closed-loop FES therapeutic session | → | 1.0 |
| 3 | Session Stop | Terminate active FES session | → | 1.0 |
| 4 | Session Pause | Temporarily suspend session | → | 2.0 |
| 5 | Session Resume | Resume previously paused session | → | 2.0 |
| 6 | Single Stimulus | Manual FES trigger (one stimulus) | → | 2.0 |
| 7 | Set Parameters | Configure FES parameters (amp, freq, width, difficulty, duration) | → | 1.0 |
| 8 | Session Status | Real-time session metrics (stimulus count, duration, etc.) | ← | 1.0 |
| 9 | Trigger Detected | Notify muscle activation detected (sEMG threshold crossed) | ← | 1.0 |
| 10 | Battery Status | Request battery voltage levels (main + stimulation) | ↔ | 2.5 |
| 11 | Start Streaming | Begin continuous 215 Hz sEMG data transmission (binary protocol) | → | 3.0 |
| 12 | Stop Streaming | Terminate streaming and return to JSON mode | → | 3.0 |
| 13 | Streaming Data | sEMG data packets (binary: 50 samples/packet, ~4.3 pkts/sec) | ← | 1.0 |
| 14 | Stream Config | Configure streaming rate/type (DEPRECATED v3.1+, uses fixed 215 Hz) | → | 2.5 |

### FES Parameter Specification (Code 7)

```json
{
  "a": 3.0,         // Amplitude (0-15V)
  "f": 38.0,        // Frequency (1-100 Hz)
  "pw": 12.0,       // Pulse width (1-100 ms)
  "df": 50,         // Difficulty/Threshold (1-100%)
  "pd": 5           // Pulse duration (1-60 sec)
}
```

### Session Status Response (Code 8)

```json
{
  "parameters": { "a": 3.0, "f": 38, "pw": 12, "df": 50, "pd": 5 },
  "status": {
    "csa": 45,              // Complete stimuli count
    "isa": 3,               // Interrupted stimuli count
    "tlt": 156000,          // Time of last trigger (ms)
    "sd": 300000,           // Session duration (ms)
    "battery_main": 4.2,    // Main battery voltage (V)
    "battery_stim": 4.1,    // Stimulation battery voltage (V)
    "state": "active"       // Session state
  }
}
```

### Binary Streaming Packet (Code 13, v3.0+)

```c
struct BinaryPacket {
    uint8_t  magic;         // 0xAA (sync marker)
    uint8_t  code;          // 0x0D (message code 13)
    uint32_t timestamp;     // millis() since session start
    uint16_t count;         // 50 samples
    int16_t  samples[50];   // sEMG data (-4096 to +4096 mV)
} __attribute__((packed));  // 108 bytes total
```

### Message Methods

- `r` (read): Request data without executing
- `w` (write): Send data or notify event
- `x` (execute): Trigger action/command
- `a` (acknowledge): Confirm receipt

---

## Device Operating Modes

The NeuroEstimulator operates in two primary **mutually-exclusive** modes:

### Mode 1: FES Session (Closed-Loop Therapeutic Stimulus)

- **Trigger Detection**: Continuous sEMG monitoring for threshold-based activation
- **Automated Stimulation**: Biphasic pulse delivery upon muscle activation detection
- **Session Duration**: Configurable time window (1-60 seconds per stimulus)
- **Real-time Feedback**: Session status updates to mobile application
- **State Tracking**: Complete stimulus count, interrupted stimulus count, trigger timestamps

### Mode 2: Real-Time sEMG Streaming (Open-Loop Data Acquisition)

- **Data Types**: Raw (unfiltered), Filtered (Butterworth), or RMS (envelope)
- **Fixed Rate**: 215 Hz continuous streaming to mobile application
- **Circular Buffering**: Prevents data loss with automatic oldest-sample dropping on buffer overflow
- **Configuration**: Dynamic rate/type adjustment via Bluetooth command
- **Automatic TTL**: 10-minute inactivity timeout with reconnection allowed

---

## Bluetooth Protocol Flow

```
1. Connection Establishment
   App → Bluetooth pairing with "NeuroEstimulator"
   App → Opens SPP connection (RFCOMM)
   ESP32 → STATUS_PIN goes HIGH
   ESP32 → LED_POWER turns solid (connected)

2. Session Configuration
   App → {"cd":7,"mt":"w","bd":{"a":3.0,"f":38,"pw":12,"df":5,"pd":5}}
   ESP32 → Stores parameters (amplitude, frequency, pulse width, difficulty, duration)

3. Session Execution
   App → {"cd":2,"mt":"x"}  // Start session
   ESP32 → Begins sEMG sampling loop (~860 Hz)
   ESP32 → Monitors for trigger (threshold-based detection)

   [When trigger detected:]
   ESP32 → {"cd":9,"mt":"w"}  // Notify trigger
   ESP32 → Executes FES stimulation (biphasic pulses)
   ESP32 → {"cd":8,"mt":"w","bd":{...}}  // Send session status

   App → {"cd":3,"mt":"x"}  // Stop session

4. Real-Time sEMG Streaming
   App → {"cd":14,"mt":"w","bd":{"rate":100,"type":"filtered"}}  // Configure
   ESP32 → {"cd":14,"mt":"a"}  // Acknowledge

   App → {"cd":11,"mt":"x"}  // Start streaming
   ESP32 → {"cd":11,"mt":"a"}  // Acknowledge
   ESP32 → {"cd":13,"mt":"w","bd":{"t":1234,"v":[23.4,25.1,...]}}  // Data packets
   ESP32 → {"cd":13,"mt":"w","bd":{"t":1345,"v":[22.8,24.5,...]}}  // Continuous stream

   App → {"cd":12,"mt":"x"}  // Stop streaming
   ESP32 → {"cd":12,"mt":"a"}  // Acknowledge
```

---

## Protocol Constraints

- **Serial parameters**: 9600 baud (default), 8N1, null-terminated strings (`\0`)
- **JSON buffer size**: 512 bytes maximum
- **Streaming bandwidth**: Recommended ≤30 Hz at 9600 baud (or upgrade to 115200 baud for higher rates)
- **Mutex protection**: All Bluetooth operations use FreeRTOS semaphores for thread safety
- **Automatic timeout**: Streaming stops after 10 minutes or on disconnect
- **Mutual exclusion**: Cannot run FES session and streaming simultaneously (shared timer)

---

## Development Tools

- **PlatformIO**: Build system and dependency manager
- **ESP-IDF v5.x**: ESP32 framework with FreeRTOS
- **ArduinoJson v6.21**: JSON serialization/deserialization
- **libFilter**: Digital signal processing (Butterworth filters, open-source)

---

## Integration with PRISM Ecosystem

The NeuroEstimulator integrates with the PRISM federated research framework:

1. **Data Source**: Provides high-fidelity biosignals (215 Hz sEMG) to mobile applications
2. **Device Registry**: Registers with research nodes for federated data management
3. **Session Recording**: Captures therapeutic session metadata for research databases
4. **Data Federation**: Session data (timestamps, stimulus counts, device parameters) can be submitted to InteroperableResearchNode via mobile interface
5. **Standards Compliance**: Aligns with HL7 FHIR for research data exchange

---

## Development Commands

```bash
# Navigate to sEMG device directory
cd InteroperableResearchsEMGDevice

# Build firmware
pio run

# Upload to ESP32 (auto-detect port)
pio run --target upload

# Upload to specific port (Windows)
pio run --target upload --upload-port COM3

# Upload to specific port (Linux/Mac)
pio run --target upload --upload-port /dev/ttyUSB0

# Open serial monitor (115200 baud)
pio device monitor

# Build, upload, and monitor in one command
pio run --target upload && pio device monitor

# List available serial ports
pio device list

# Clean build
pio run --target clean
```

---

## Testing Bluetooth Protocol

Use any Bluetooth serial terminal app (e.g., Serial Bluetooth Terminal for Android):

```json
// Configure FES parameters
{"cd":7,"mt":"w","bd":{"a":3.0,"f":38,"pw":12,"df":5,"pd":5}}

// Start session
{"cd":2,"mt":"x"}

// Stop session
{"cd":3,"mt":"x"}

// Configure streaming (100 Hz, filtered data)
{"cd":14,"mt":"w","bd":{"rate":100,"type":"filtered"}}

// Start streaming
{"cd":11,"mt":"x"}

// Stop streaming
{"cd":12,"mt":"x"}

// Read gyroscope
{"cd":1,"mt":"x"}
```

---

## Documentation References

For detailed implementation guidance, see:

- **Main Guide**: `InteroperableResearchsEMGDevice/CLAUDE.md`
- **Complete Protocol**: `InteroperableResearchsEMGDevice/docs/api/COMPLETE_BLUETOOTH_PROTOCOL.md` (v2.0, 80 KB master reference)
- **Protocol Guide**: `InteroperableResearchsEMGDevice/docs/api/PROTOCOL_DOCUMENTATION_GUIDE.md` (navigation and learning paths)
- **Binary Streaming**: `InteroperableResearchsEMGDevice/docs/api/bluetooth-protocol.md` (v1.1)
- **Quick Reference**: `InteroperableResearchsEMGDevice/docs/api/streaming-protocol.md` (v1.0, 5-minute overview)
- **Integration**: `InteroperableResearchsEMGDevice/docs/INTEGRATION_GUIDE.md` (PRISM ecosystem integration)
- **Device-to-Node**: `InteroperableResearchsEMGDevice/docs/DEVICE_NODE_PROTOCOL_REFERENCE.md`
- **Update Summary**: `InteroperableResearchsEMGDevice/docs/api/BLUETOOTH_PROTOCOL_UPDATE_SUMMARY.md` (October 2025)
