# Database Entity-Relationship Diagrams (Modular)

This document breaks the complete ER diagram into smaller, domain-focused sub-diagrams for easier comprehension. Each diagram highlights a specific bounded context with explicit boundary markers for entities from other domains.

---

## 1. Research Infrastructure Domain

Core organizational structure: nodes, research projects, and researchers.

```mermaid
erDiagram
    RESEARCH_NODE {
        uuid id PK
        string node_name
        string certificate_fingerprint
        string status
        int nodeAccessLevel
        string institution_details
        string contact_info
        string node_url
        text certificate
        datetime created_at
        datetime updated_at
    }

    RESEARCH {
        uuid id PK
        uuid research_node_id FK
        string title
        text description
        date start_date
        date end_date
        string status
        datetime created_at
        datetime updated_at
    }

    RESEARCHER {
        uuid researcher_id PK
        uuid research_node_id FK
        string name
        string email
        string institution
        string role
        datetime created_at
        datetime updated_at
    }

    RESEARCH_RESEARCHER {
        uuid id PK
        uuid research_id FK
        uuid researcher_id FK
        boolean is_principal
        datetime assigned_at
        datetime removed_at
        boolean is_active
        uuid recorded_by FK
        datetime created_at
        datetime updated_at
    }

    RESEARCH_NODE ||--o{ RESEARCH : "hosts"
    RESEARCH_NODE ||--o{ RESEARCHER : "employs"
    RESEARCH ||--o{ RESEARCH_RESEARCHER : "staffed_by"
    RESEARCHER ||--o{ RESEARCH_RESEARCHER : "works_on"
```

**Key Points:**
- `RESEARCH_NODE` is the root entity representing a federated institution
- Each node hosts multiple `RESEARCH` projects
- `RESEARCH_RESEARCHER` is a many-to-many join with audit trail (`recorded_by`, `assigned_at`)

---

## 2. Volunteer Management Domain

Volunteer enrollment and participation tracking.

```mermaid
erDiagram
    RESEARCH_NODE {
        uuid id PK
        string node_name
    }

    VOLUNTEER {
        uuid volunteer_id PK
        uuid research_node_id FK
        string volunteer_code
        date birth_date
        string gender
        string blood_type
        float height
        float weight
        text medical_history
        string consent_status
        datetime enrolled_at
        datetime updated_at
    }

    RESEARCH {
        uuid id PK
        string title
        string status
    }

    RESEARCH_VOLUNTEER {
        uuid id PK
        uuid research_id FK
        uuid volunteer_id FK
        string enrollment_status
        date consent_date
        string consent_version
        text exclusion_reason
        string doc_signed_file_url
        datetime enrolled_at
        datetime withdrawn_at
        uuid recorded_by FK
        datetime created_at
        datetime updated_at
    }

    RESEARCH_NODE ||--o{ VOLUNTEER : "registers"
    RESEARCH ||--o{ RESEARCH_VOLUNTEER : "enrolls"
    VOLUNTEER ||--o{ RESEARCH_VOLUNTEER : "participates_in"
```

**Key Points:**
- Volunteers belong to a single `RESEARCH_NODE` (data sovereignty)
- `RESEARCH_VOLUNTEER` tracks consent and enrollment with audit fields
- `volunteer_code` provides anonymization (no PII in cross-node queries)

---

## 3. Equipment & Sensors Domain

Devices and sensors used in research data acquisition.

```mermaid
erDiagram
    RESEARCH {
        uuid id PK
        string title
    }

    APPLICATION {
        uuid application_id PK
        uuid research_id FK
        string app_name
        string url
        text description
        text additional_info
        datetime created_at
        datetime updated_at
    }

    DEVICE {
        uuid device_id PK
        uuid research_id FK
        string device_name
        string manufacturer
        string model
        text additional_info
        datetime created_at
        datetime updated_at
    }

    SENSOR {
        uuid sensor_id PK
        uuid device_id FK
        string sensor_name
        float max_sampling_rate
        string unit
        float min_range
        float max_range
        float accuracy
        text additional_info
        datetime created_at
        datetime updated_at
    }

    RESEARCH ||--o{ APPLICATION : "uses"
    RESEARCH ||--o{ DEVICE : "employs"
    DEVICE ||--|{ SENSOR : "contains"
```

**Key Points:**
- `DEVICE` and `APPLICATION` are scoped per research project
- `SENSOR` specifications (sampling rate, range, accuracy) enable data validation
- Metadata stored in `additional_info` JSONB columns

---

## 4. Data Recording Domain

Session-based biosignal acquisition workflow.

```mermaid
erDiagram
    RESEARCH {
        uuid id PK
        string title
    }

    VOLUNTEER {
        uuid volunteer_id PK
        string volunteer_code
    }

    SENSOR {
        uuid sensor_id PK
        string sensor_name
        float max_sampling_rate
    }

    RECORD_SESSION {
        uuid id PK
        uuid volunteer_id FK
        uuid research_id FK
        uuid target_area_id FK
        datetime start_at
        datetime finished_at
        datetime created_at
        datetime updated_at
    }

    SESSION_ANNOTATION {
        uuid id PK
        uuid record_session_id FK
        text text
        datetime created_at
        datetime updated_at
    }

    RECORD {
        uuid id PK
        uuid record_session_id FK
        datetime collection_date
        string session_id
        string record_type
        datetime created_at
        datetime updated_at
    }

    RECORD_CHANNEL {
        uuid id PK
        uuid record_id FK
        uuid sensor_id FK
        string signal_type
        string file_url
        float sampling_rate
        integer samples_count
        datetime start_timestamp
        datetime created_at
        datetime updated_at
    }

    RESEARCH ||--o{ RECORD_SESSION : "generates"
    VOLUNTEER ||--o{ RECORD_SESSION : "participates_in"
    RECORD_SESSION ||--|{ RECORD : "contains"
    RECORD_SESSION ||--o{ SESSION_ANNOTATION : "annotated_by"
    RECORD ||--|{ RECORD_CHANNEL : "contains"
    SENSOR ||--o{ RECORD_CHANNEL : "captures"
```

**Key Points:**
- `RECORD_SESSION` groups all recordings from a single collection event and holds a nullable FK to `TARGET_AREA` (the anatomical target for the entire session — Phase 20)
- `SESSION_ANNOTATION` replaces the removed `RECORD_CHANNEL.annotations` JSONB field; annotations are persisted at session level via `POST /api/ClinicalSession/{sessionId}/annotations/New`
- `RECORD_CHANNEL` stores actual biosignal data (`file_url` points to binary storage)
- `RECORD.notes` and `RECORD_CHANNEL.annotations` were removed in Phase 20

---

## 5. SNOMED CT Anatomical Terminology Domain

Self-referencing hierarchies for body regions and structures, plus the session-level target area.

```mermaid
erDiagram
    RECORD_SESSION {
        uuid id PK
        uuid target_area_id FK
    }

    SNOMED_BODY_REGION {
        string snomed_code PK
        string display_name
        string parent_region_code FK
        text description
        boolean is_active
        datetime created_at
        datetime updated_at
    }

    SNOMED_BODY_STRUCTURE {
        string snomed_code PK
        string body_region_code FK
        string display_name
        string structure_type
        string parent_structure_code FK
        text description
        boolean is_active
        datetime created_at
        datetime updated_at
    }

    SNOMED_LATERALITY {
        string code PK
        string display_name
        text description
        boolean is_active
    }

    SNOMED_TOPOGRAPHICAL_MODIFIER {
        string code PK
        string display_name
        string category
        text description
        boolean is_active
    }

    SNOMED_SEVERITY_CODE {
        string code PK
        string display_name
        text description
        boolean is_active
        datetime created_at
        datetime updated_at
    }

    TARGET_AREA {
        uuid id PK
        uuid record_session_id FK
        string body_structure_code FK
        string laterality_code FK
        datetime created_at
        datetime updated_at
    }

    TARGET_AREA_TOPOGRAPHICAL_MODIFIER {
        uuid target_area_id PK,FK
        string topographical_modifier_code PK,FK
    }

    RECORD_SESSION ||--o| TARGET_AREA : "targets"
    SNOMED_BODY_REGION ||--o{ SNOMED_BODY_REGION : "has_sub_regions"
    SNOMED_BODY_REGION ||--o{ SNOMED_BODY_STRUCTURE : "contains"
    SNOMED_BODY_STRUCTURE ||--o{ SNOMED_BODY_STRUCTURE : "has_sub_structures"
    SNOMED_BODY_STRUCTURE ||--o{ TARGET_AREA : "instantiated_as"
    TARGET_AREA }o--o| SNOMED_LATERALITY : "qualified_by"
    TARGET_AREA ||--o{ TARGET_AREA_TOPOGRAPHICAL_MODIFIER : "qualified_by"
    SNOMED_TOPOGRAPHICAL_MODIFIER ||--o{ TARGET_AREA_TOPOGRAPHICAL_MODIFIER : "used_in"
```

**Key Points:**
- Self-referencing hierarchies enable anatomical taxonomy (e.g., Upper Limb → Arm → Forearm)
- `TARGET_AREA` post-coordinates SNOMED concepts: structure + optional laterality + N topographical modifiers
- **Phase 20**: `TARGET_AREA` is now owned by `RECORD_SESSION` (1:0..1), not `RECORD_CHANNEL`. Topographical modifiers moved from a scalar FK to the explicit N:M join table `TARGET_AREA_TOPOGRAPHICAL_MODIFIER`
- `SNOMED_SEVERITY_CODE` shared across clinical conditions and events

---

## 6. Clinical Data Domain

Volunteer health records: conditions, events, medications, allergies, vitals.

```mermaid
erDiagram
    VOLUNTEER {
        uuid volunteer_id PK
        string volunteer_code
    }

    RESEARCHER {
        uuid researcher_id PK
        string name
    }

    RECORD_SESSION {
        uuid id PK
        datetime start_at
    }

    SNOMED_SEVERITY_CODE {
        string code PK
        string display_name
    }

    TARGET_AREA {
        uuid id PK
        string body_structure_code FK
    }

    CLINICAL_CONDITION {
        string snomed_code PK
        string display_name
        text description
        boolean is_active
        datetime created_at
        datetime updated_at
    }

    CLINICAL_EVENT {
        string snomed_code PK
        string display_name
        text description
        boolean is_active
        datetime created_at
        datetime updated_at
    }

    MEDICATION {
        string snomed_code PK
        string medication_name
        string active_ingredient
        string anvisa_code
        boolean is_active
        datetime created_at
        datetime updated_at
    }

    ALLERGY_INTOLERANCE {
        string snomed_code PK
        string category
        string substance_name
        string type
        boolean is_active
        datetime created_at
        datetime updated_at
    }

    VOLUNTEER_CLINICAL_CONDITION {
        uuid id PK
        uuid volunteer_id FK
        string snomed_code FK
        string clinical_status
        date onset_date
        date abatement_date
        string severity_code FK
        string verification_status
        text clinical_notes
        uuid recorded_by FK
        datetime created_at
        datetime updated_at
    }

    VOLUNTEER_CLINICAL_EVENT {
        uuid id PK
        uuid volunteer_id FK
        string event_type
        string snomed_code FK
        datetime event_datetime
        integer duration_minutes
        string severity_code FK
        float numeric_value
        string value_unit
        json characteristics
        uuid target_area_id FK
        uuid record_session_id FK
        uuid recorded_by FK
        datetime created_at
        datetime updated_at
    }

    VOLUNTEER_MEDICATION {
        uuid id PK
        uuid volunteer_id FK
        string medication_snomed_code FK
        uuid condition_id FK
        string dosage
        string frequency
        string route
        date start_date
        date end_date
        string status
        text notes
        uuid recorded_by FK
        datetime created_at
        datetime updated_at
    }

    VOLUNTEER_ALLERGY_INTOLERANCE {
        uuid id PK
        uuid volunteer_id FK
        string allergy_intolerance_snomed_code FK
        string criticality
        string clinical_status
        json manifestations
        date onset_date
        date last_occurrence
        string verification_status
        uuid recorded_by FK
        datetime created_at
        datetime updated_at
    }

    VITAL_SIGNS {
        uuid id PK
        uuid volunteer_id FK
        uuid record_session_id FK
        float systolic_bp
        float diastolic_bp
        float heart_rate
        float respiratory_rate
        float temperature
        float oxygen_saturation
        float weight
        float height
        float bmi
        datetime measurement_datetime
        string measurement_context
        uuid recorded_by FK
        datetime created_at
        datetime updated_at
    }

    %% Volunteer relationships
    VOLUNTEER ||--o{ VOLUNTEER_CLINICAL_CONDITION : "diagnosed_with"
    VOLUNTEER ||--o{ VOLUNTEER_CLINICAL_EVENT : "experiences"
    VOLUNTEER ||--o{ VOLUNTEER_MEDICATION : "takes"
    VOLUNTEER ||--o{ VOLUNTEER_ALLERGY_INTOLERANCE : "has"
    VOLUNTEER ||--o{ VITAL_SIGNS : "measured"

    %% Reference data relationships
    CLINICAL_CONDITION ||--o{ VOLUNTEER_CLINICAL_CONDITION : "classifies"
    CLINICAL_EVENT ||--o{ VOLUNTEER_CLINICAL_EVENT : "describes"
    MEDICATION ||--o{ VOLUNTEER_MEDICATION : "prescribed_as"
    ALLERGY_INTOLERANCE ||--o{ VOLUNTEER_ALLERGY_INTOLERANCE : "manifests_as"

    %% Clinical interconnections
    VOLUNTEER_CLINICAL_CONDITION ||--o{ VOLUNTEER_CLINICAL_EVENT : "generates"
    VOLUNTEER_CLINICAL_CONDITION ||--o{ VOLUNTEER_MEDICATION : "treated_by"

    %% Session context
    VOLUNTEER_CLINICAL_EVENT }o--|| RECORD_SESSION : "captured_during"
    VITAL_SIGNS }o--|| RECORD_SESSION : "collected_during"

    %% Severity grading
    SNOMED_SEVERITY_CODE ||--o{ VOLUNTEER_CLINICAL_CONDITION : "grades"
    SNOMED_SEVERITY_CODE ||--o{ VOLUNTEER_CLINICAL_EVENT : "rates"

    %% Anatomical localization
    VOLUNTEER_CLINICAL_EVENT }o--o| TARGET_AREA : "located_at"

    %% Audit trail
    RESEARCHER ||--o{ VOLUNTEER_CLINICAL_CONDITION : "records"
    RESEARCHER ||--o{ VOLUNTEER_CLINICAL_EVENT : "observes"
    RESEARCHER ||--o{ VOLUNTEER_MEDICATION : "prescribes"
    RESEARCHER ||--o{ VOLUNTEER_ALLERGY_INTOLERANCE : "documents"
    RESEARCHER ||--o{ VITAL_SIGNS : "measures"
```

**Key Points:**
- Four reference tables (`CLINICAL_CONDITION`, `CLINICAL_EVENT`, `MEDICATION`, `ALLERGY_INTOLERANCE`) use SNOMED CT codes as PKs
- Volunteer-specific tables link reference data with temporal and contextual information
- `recorded_by` FK on all clinical data provides audit trail
- `VITAL_SIGNS` captures standard physiological measurements

---

## Domain Interconnections Overview

High-level view showing how domains connect:

```mermaid
flowchart TB
    subgraph Infrastructure["1. Research Infrastructure"]
        NODE[RESEARCH_NODE]
        RES[RESEARCH]
        RSCH[RESEARCHER]
    end

    subgraph Volunteers["2. Volunteer Management"]
        VOL[VOLUNTEER]
    end

    subgraph Equipment["3. Equipment & Sensors"]
        DEV[DEVICE]
        SEN[SENSOR]
        APP[APPLICATION]
    end

    subgraph Recording["4. Data Recording"]
        SESS[RECORD_SESSION]
        ANN[SESSION_ANNOTATION]
        REC[RECORD]
        CHAN[RECORD_CHANNEL]
    end

    subgraph SNOMED["5. SNOMED CT Terminology"]
        REG[BODY_REGION]
        STR[BODY_STRUCTURE]
        TA[TARGET_AREA]
        TAJOIN[TARGET_AREA_TOPOGRAPHICAL_MODIFIER]
        LAT[LATERALITY]
        MOD[MODIFIER]
        SEV[SEVERITY]
    end

    subgraph Clinical["6. Clinical Data"]
        COND[CONDITIONS]
        EVT[EVENTS]
        MED[MEDICATIONS]
        ALG[ALLERGIES]
        VIT[VITAL_SIGNS]
    end

    NODE --> RES
    NODE --> RSCH
    NODE --> VOL

    RES --> DEV
    RES --> APP
    RES --> SESS

    DEV --> SEN
    SEN --> CHAN

    VOL --> SESS
    SESS --> REC
    SESS --> ANN
    SESS --> TA
    REC --> CHAN

    STR --> TA
    LAT --> TA
    MOD --> TAJOIN
    TAJOIN --> TA
    REG --> STR

    VOL --> COND
    VOL --> EVT
    VOL --> MED
    VOL --> ALG
    VOL --> VIT

    COND --> EVT
    COND --> MED
    SEV --> COND
    SEV --> EVT
    TA --> EVT

    SESS --> EVT
    SESS --> VIT

    RSCH -.->|records| Clinical
```

---

## Cross-Reference Table

| Domain | Tables | Primary Relationships |
|--------|--------|----------------------|
| **1. Research Infrastructure** | 4 | Root of all data ownership |
| **2. Volunteer Management** | 2 | Connects participants to research |
| **3. Equipment & Sensors** | 3 | Defines acquisition capabilities |
| **4. Data Recording** | 4 | Captures biosignal data (`+SESSION_ANNOTATION`) |
| **5. SNOMED CT Terminology** | 7 | Standardized anatomical vocabulary (`+TARGET_AREA_TOPOGRAPHICAL_MODIFIER`) |
| **6. Clinical Data** | 9 | Health records and observations |
| **Total** | **29** | 28 main + 1 explicit join table |

> **Phase 20 delta**: Domain 4 gained `SESSION_ANNOTATION`; Domain 5 gained `TARGET_AREA_TOPOGRAPHICAL_MODIFIER` join table and re-parented `TARGET_AREA` from channel to session. Domains 1-3 and 6 unchanged.

---

## Navigation

- **Complete ER Diagram**: [er-diagram.md](er-diagram.md)
- **Implementation Notes**: See original file for repository specializations
- **Database Commands**: See original file for migration instructions
