```mermaid
erDiagram
    %% Entidades Principais
    RESEARCH_NODE {
        uuid node_id PK
        string node_name
        string institution
        string contact_info
        string node_url
        text certificate
        string status
        datetime created_at
        datetime updated_at
    }
    
    RESEARCH {
        uuid research_id PK
        string title
        text description
        string protocol_number
        date start_date
        date end_date
        string status
        string ethical_approval
        datetime created_at
        datetime updated_at
    }
    
    VOLUNTEER {
        uuid volunteer_id PK
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
    
    RESEARCHER {
        uuid researcher_id PK
        string name
        string email
        string institution
        string role
        string credentials
        string orcid
        datetime created_at
        datetime updated_at
    }
    
    APPLICATION {
        uuid application_id PK
        uuid research_id FK
        string app_name
        string version
        text description
        string type
        json configuration
        datetime created_at
        datetime updated_at
    }
    
    DEVICE {
        uuid device_id PK
        uuid research_id FK
        string device_name
        string manufacturer
        string model
        string serial_number
        string firmware_version
        json specifications
        string calibration_status
        datetime last_calibration
        datetime created_at
        datetime updated_at
    }
    
    SENSOR {
        uuid sensor_id PK
        uuid device_id FK
        string sensor_type
        string sensor_name
        float sampling_rate
        string unit
        float min_range
        float max_range
        float accuracy
        json metadata
        datetime created_at
        datetime updated_at
    }
    
    RECORD {
        uuid record_id PK
        uuid research_id FK
        uuid volunteer_id FK
        datetime collection_date
        string session_id
        text clinical_context
        json environmental_conditions
        string record_type
        integer duration_seconds
        string quality_score
        text notes
        datetime created_at
        datetime updated_at
    }
    
    BIOSIGNAL {
        uuid biosignal_id PK
        uuid record_id FK
        uuid sensor_id FK
        string signal_type
        blob raw_data
        json processed_data
        float sampling_rate
        integer samples_count
        datetime start_timestamp
        datetime end_timestamp
        json annotations
        json quality_metrics
        datetime created_at
    }
    
    %% Tabelas Associativas (Muitos para Muitos)
    NODE_RESEARCH {
        uuid node_id FK
        uuid research_id FK
        string role
        string access_level
        datetime joined_at
    }
    
    RESEARCH_VOLUNTEER {
        uuid research_id FK
        uuid volunteer_id FK
        string enrollment_status
        date consent_date
        string consent_version
        text exclusion_reason
        datetime enrolled_at
        datetime withdrawn_at
    }
    
    RESEARCH_RESEARCHER {
        uuid research_id FK
        uuid researcher_id FK
        string role
        string responsibility
        boolean is_principal
        datetime assigned_at
        datetime removed_at
    }
    
    %% Relacionamentos
    RESEARCH_NODE ||--o{ NODE_RESEARCH : participates
    RESEARCH ||--o{ NODE_RESEARCH : hosted_by
    
    RESEARCH ||--o{ RESEARCH_VOLUNTEER : enrolls
    VOLUNTEER ||--o{ RESEARCH_VOLUNTEER : participates_in
    
    RESEARCH ||--o{ RESEARCH_RESEARCHER : has
    RESEARCHER ||--o{ RESEARCH_RESEARCHER : works_on
    
    RESEARCH ||--o{ APPLICATION : uses
    RESEARCH ||--o{ DEVICE : employs
    
    DEVICE ||--|{ SENSOR : contains
    
    RESEARCH ||--o{ RECORD : generates
    VOLUNTEER ||--o{ RECORD : provides
    
    RECORD ||--|{ BIOSIGNAL : contains
    SENSOR ||--o{ BIOSIGNAL : captures
```