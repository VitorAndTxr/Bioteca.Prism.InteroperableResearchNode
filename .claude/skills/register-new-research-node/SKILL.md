---
name: register-new-research-node
description: |
  Register research nodes into the PRISM federated network. Use when: adding research
  institutions, registering nodes via API, managing node approval workflow, or user says
  "register node", "add node", "registrar nó", "aprovar nó", "create research node".
---

# Register New Research Node

Register a node for approval via the application:

```powershell
.\.claude\skills\register-new-research-node\scripts\register-node.ps1 -NodeName "Hospital Node" -NodeUrl "https://node.hospital.com:5000"
```

## Parameters

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| NodeName | Yes | - | Node display name |
| NodeUrl | Yes | - | Remote node URL |
| InstitutionName | No | "" | Institution name |
| ContactInfo | No | "" | Admin email |
| AccessLevel | No | 1 (ReadWrite) | 0=ReadOnly, 1=ReadWrite, 2=Admin |

The script:
1. Generates a self-signed X.509 certificate
2. Calculates SHA-256 fingerprint
3. Inserts node with **Pending** status for user approval

Requires: Docker with `irn-postgres-node-a` container or psql.
