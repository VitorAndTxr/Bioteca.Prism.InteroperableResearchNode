# Technical Risk Analysis: Inter-Node Research Data Synchronization

**Document Version**: 1.0
**Date**: February 18, 2026
**Scope**: Phase 5 (Federated Queries) - Inter-node research data synchronization
**Audience**: Tech Lead, Backend Dev, System Architect

---

## Executive Summary

Inter-node research data synchronization introduces seven critical technical risks due to data model complexity (28 tables), distributed transaction semantics, network resilience requirements, and security implications. This document identifies risks, rates severity, and recommends mitigation strategies.

**Key Finding**: Without proper delta-sync design and conflict resolution strategy, the system risks data inconsistency, performance degradation, and security gaps.

---

## Risk Assessment Overview

| # | Risk Category | Severity | Likelihood | Impact |
|---|---|---|---|---|
| 1 | Data Model Complexity & Sync Scope | HIGH | HIGH | Lost/Inconsistent Data |
| 2 | Delta vs. Full Sync Strategy | CRITICAL | HIGH | Performance Degradation |
| 3 | Conflict Resolution (Concurrent Changes) | CRITICAL | MEDIUM | Data Corruption |
| 4 | Network Resilience & Partial Failures | HIGH | HIGH | Incomplete Sync State |
| 5 | Handshake Overhead in Batch Operations | MEDIUM | MEDIUM | Throughput Bottleneck |
| 6 | Security: Data Boundary Leakage | HIGH | MEDIUM | Privacy Violation |
| 7 | Performance with Large Datasets | MEDIUM | LOW | Timeouts/OOM |

---

## 1. Data Model Complexity & Sync Scope

### Current State
PRISM manages **28 tables** across six layers:
- **Core Entities** (10): Research, Volunteer, Researcher, Device, Sensor, Application, Recording sessions, Records, Record channels, Target areas
- **SNOMED CT** (4): Body structures, Body regions, Lateralities, Topographical modifiers, Severity codes
- **Clinical Data** (14): Conditions, Events, Medications, Allergies, Vital signs, Volunteer-specific clinical records, M2M relationships

### Risk: Unclear Sync Scope

**Problem**: It's unclear which tables participate in federated sync and under what conditions:
- Should *all* 28 tables sync, or only a subset?
- Are core entities (Research, Volunteer) immutable once created?
- Who owns SNOMED CT terminologies—single authority or per-node?
- Should clinical events (Volunteer-specific) sync at all?

**Data Dependencies**:
```
Research
  ├─> ResearchVolunteer (many-to-many)
  │    └─> Volunteer
  │         ├─> VitalSigns
  │         ├─> VolunteerClinicalCondition
  │         ├─> VolunteerClinicalEvent
  │         ├─> VolunteerMedication
  │         └─> VolunteerAllergyIntolerance
  ├─> ResearchResearcher (many-to-many)
  │    └─> Researcher
  ├─> ResearchDevice (many-to-many)
  │    └─> Device
  │         └─> Sensor (one-to-many)
  └─> RecordingSession (one-to-many)
       └─> Record (one-to-many)
            ├─> RecordChannel
            └─> TargetArea
```

**Impact**: Syncing Research without dependent entities creates orphaned records and foreign key constraint violations.

### Risk Rating: **HIGH**

### Mitigation Strategies

1. **Define Sync Boundary** (Phase 5 START):
   - Create sync specification document detailing:
     - Which entities are "syncable" (queryable across federated nodes)
     - Which are "read-only" (SNOMED CT, research metadata)
     - Which are "local-only" (volunteer medical history, sensitive clinical data)

2. **Sync Group Clustering**:
   - Group 28 tables into 3-4 sync domains:
     - **Domain A - Research Metadata** (Research, Device, Sensor, Application, Researcher) — Read-mostly, versioned
     - **Domain B - Participant Data** (Volunteer, VitalSigns) — High-frequency updates, strict consistency
     - **Domain C - Clinical Records** (Clinical conditions/events/medications) — Audit-trail critical
     - **Domain D - Biosignals** (RecordingSession, Record, RecordChannel) — Time-series, append-only

3. **Implement Sync Manifest**:
   - Each sync request includes explicit entity list and versions
   - Server validates scope before processing
   - Rejects cross-domain partial syncs

---

## 2. Delta vs. Full Sync Strategy

### Current State
No sync mechanism exists. Phase 5 must choose between:
- **Full sync**: Every sync operation transfers entire dataset
- **Delta sync**: Only changed records transferred since last sync point
- **Hybrid**: Full periodic, delta incremental

### Risk: Performance Degradation

**Problem**: Full sync of 28 tables × 1000+ records = 10MB+ payloads per sync.

**Scenarios**:
- **Scenario A (Full Sync)**: Node A queries Node B for all Volunteers + Clinical data
  - Cold start (first sync): 10-50MB, 30-60 seconds
  - Repeated syncs: Wasteful, transfers unchanged data repeatedly
  - Per-minute polling: Unbounded CPU/network cost

- **Scenario B (Delta Sync)**: Track "last_sync_at" per node per entity type
  - Requires timestamp-based tracking across all 28 tables
  - Requires durable sync checkpoints (what if connection fails mid-transfer?)
  - Risk: Synchronization clock skew between nodes (UTC vs local time)
  - Risk: Missed updates if node clocks drift > 1 second

- **Scenario C (Hybrid)**: Full daily, delta hourly
  - Complexity: Manage two sync pathways
  - Risk: Version mismatch between full/delta snapshots

### Risk Rating: **CRITICAL**

### Mitigation Strategies

1. **Implement Event Sourcing / Write-Ahead Log**:
   - Track all entity mutations in `EntityChangeLog` table:
     ```sql
     CREATE TABLE entity_change_log (
       id UUID PRIMARY KEY,
       source_node_id UUID,
       entity_type VARCHAR(50),
       entity_id UUID,
       operation VARCHAR(10), -- CREATE, UPDATE, DELETE
       payload JSONB,
       changed_at TIMESTAMP,
       version INT
     );
     ```
   - Enables true delta sync: "Give me all changes since version N"
   - Cost: ~15% write amplification (worth it for 1000+ node federation)

2. **Implement Vector Clock Versioning**:
   - Replace timestamps with vector clocks `(node_a_version, node_b_version, ...)`
   - Eliminates clock skew issues
   - Enables causal consistency (if event A causally precedes event B, vector(A) < vector(B))
   - Example:
     ```csharp
     public class Entity {
         public Dictionary<string, int> VectorClock { get; set; } // {"node-a": 42, "node-b": 15}
     }
     ```

3. **Implement Checkpoint Mechanism**:
   - Store sync checkpoints in Redis:
     ```
     sync:node-a:to:node-b:entity-type:volunteer = {version: 42, timestamp: "2026-02-18T10:30:00Z"}
     ```
   - Enables resumable syncs (restart from checkpoint if network fails)
   - Prevents re-syncing unchanged data

4. **Compression + Batch Window**:
   - Buffer changes for 1-5 minutes, batch into single sync request
   - Compress payload (GZIP): 10MB → 2-3MB typical
   - Reduces handshake overhead (see Risk #5)

---

## 3. Conflict Resolution (Concurrent Changes)

### Current State
No conflict resolution mechanism. Two nodes can independently modify the same entity.

**Scenario**:
```
Time   Node A                          Node B
────   ──────────────────────────────  ──────────────────────────
T1     Fetch Volunteer[id=123]
T2                                     Fetch Volunteer[id=123]
T3     Update: age=30
T4                                     Update: age=31
T5     Sync to Node B (age=30)
T6     Sync to Node A (age=31)
T7     Result: age conflicts between nodes
```

### Risk: Data Corruption

**Problem**: Which value wins? If left unresolved:
- Both values coexist (fork history)
- Data integrity violations (medical records with conflicting vital signs)
- Federated queries return inconsistent results across nodes
- GDPR audit failure (cannot prove data consistency)

### Risk Rating: **CRITICAL**

### Mitigation Strategies

1. **Last-Write-Wins (LWW) with Timestamp Causality**:
   - Always accept timestamp from entity.UpdatedAt
   - Problem: Clock skew breaks this
   - Recommendation: Use vector clocks + logical timestamp (hybrid)

   ```csharp
   public class ConflictResolution {
       public static Entity Merge(Entity local, Entity remote) {
           var localVC = local.VectorClock;
           var remoteVC = remote.VectorClock;

           // Check causal ordering
           if (localVC.IsCausallyBefore(remoteVC)) return remote;
           if (remoteVC.IsCausallyBefore(localVC)) return local;

           // Concurrent: apply deterministic merge rule
           return localVC.TieBreak(remoteVC) > 0 ? local : remote; // Node ID as tiebreaker
       }
   }
   ```

2. **Opt-in Immutability for Core Entities**:
   - Research, Volunteer, Researcher: Immutable after creation
   - Deletions: Soft-delete with expiration (not hard-delete)
   - Modifications: Create new version (Research v1 → v2)
   - Sync: Always sync highest version number
   - Benefit: Eliminates concurrent modification conflicts on stable entities

3. **Three-Way Merge for Clinical Data**:
   - Track "common ancestor" baseline for concurrent edits
   - Merge changes if they don't conflict:
     ```
     Local:    age=30, weight=70, height=180
     Remote:   age=30, weight=75, height=180
     Base:     age=28, weight=70, height=180
     Result:   age=30 (local), weight=75 (remote), height=180 (both same)
     ```
   - Automatic conflict resolution for non-overlapping fields
   - Manual review required if same field modified by both nodes

4. **Capability-Based Access Control**:
   - Restrict sync permissions by entity type:
     - Node A (Hospital): Can write Volunteer clinical data, read-only for Device configuration
     - Node B (Lab): Can write Device calibration, read-only for Volunteer demographics
   - Prevents conflicting writes on same entity
   - Enforced at sync authorization layer (Phase 4 extensions)

---

## 4. Network Resilience & Partial Failures

### Current State
4-phase handshake assumes single, synchronous connection. Sync ops may timeout mid-transfer.

**Scenario**:
```
Node A → Node B: Sync 1000 records, 10MB payload
Connection at 95% (950 records transferred)...
[TIMEOUT - connection drops]

Node A: 950 records applied
Node B: Waiting for remaining 50 records (in inconsistent state)
```

### Risk: Incomplete Sync State

**Problem**:
- Node A believes sync succeeded (950/1000)
- Node B believes sync incomplete (950/1000 received, 50 missing)
- Next sync attempts to re-transfer → Duplicates or conflicts
- Clients querying during partial sync get stale/inconsistent results

### Risk Rating: **HIGH**

### Mitigation Strategies

1. **Implement Idempotent Sync Operations**:
   - Each sync message includes unique `SyncBatchId` (UUID)
   - Server deduplicates: if `SyncBatchId` already processed, skip
   - Store processed batch IDs in Redis (24-hour TTL):
     ```
     sync-batch:node-a:12345-uuid = {status: "complete", processed_at: "2026-02-18T10:30:00Z"}
     ```
   - Retries don't cause duplicates

2. **Implement Two-Phase Commit (2PC) for Sync**:
   - Phase 1: Prepare (server validates, locks rows, generates temporary changeset)
   - Phase 2: Commit (server applies changeset atomically)
   - Rollback if network fails between phases:
     ```
     POST /api/sync/prepare → Returns SyncSessionId
     POST /api/sync/commit → Applies changes atomically or rejects
     ```
   - Cost: Additional round-trip, but ensures all-or-nothing semantics

3. **Implement Resumable Sync with Checksums**:
   - Break 10MB payload into 1MB chunks
   - Each chunk includes checksum (SHA256)
   - Server confirms receipt chunk-by-chunk
   - Client resumes from last confirmed chunk on retry
   - Example:
     ```json
     {
       "syncBatchId": "batch-123",
       "chunks": [
         {"chunkIndex": 0, "data": "...", "checksum": "abc..."},
         {"chunkIndex": 1, "data": "...", "checksum": "def..."}
       ]
     }
     ```

4. **Implement Dead Letter Queue (DLQ) for Failed Syncs**:
   - If sync fails after 3 retries, queue in Redis DLQ
   - Background job retries DLQ every hour
   - Admin alert if DLQ grows (indicates systemic issue)
   - Prevents silent sync failures

5. **Implement Monotonic Sync Sequence Numbers**:
   - Each sync request assigned sequence number (e.g., sync #1, sync #2, sync #3)
   - Server enforces strict ordering: cannot skip sequence numbers
   - Detects mid-transfer failures (gap in sequence)
   - Ensures causality

---

## 5. 4-Phase Handshake Overhead in Batch Operations

### Current State
Each inter-node request requires full 4-phase handshake (Phase 1 → 4).

**Problem**: Batch sync of 1000 records over 10MB payload
- Handshake latency: ~2-5 seconds (ECDH key exchange, RSA signatures)
- Payload transfer: ~10-20 seconds (at 500KB/s network speed)
- **Total**: 12-25 seconds per sync operation

**If syncing 100 nodes**: 100 × 12s = 20 minutes minimum, sequentially

### Risk: Throughput Bottleneck

**Problem**:
- Cannot sync all 100 federated nodes in reasonable time
- Clients waiting for federated query results see 20+ minute latency
- Rate limiting (60 req/min per node) becomes insufficient for multi-node queries

### Risk Rating: **MEDIUM**

### Mitigation Strategies

1. **Session Persistence & Multiplexing**:
   - Reuse Phase 4 session for multiple sequential requests (instead of repeated handshake)
   - One handshake → multiple sync payloads over same encrypted channel
   - Reduces handshake amortization (12s handshake + 10 × 10s transfers ≈ 112s vs. 10 × 12s + 10s transfers ≈ 130s)
   - Already supported by current architecture (channel reuse in Phase 1)

2. **Asynchronous Sync Pipeline**:
   - Don't wait for response before queueing next sync
   - Example:
     ```csharp
     var tasks = nodes.Select(node => SyncAsync(node)).ToList();
     await Task.WhenAll(tasks); // Parallel, not sequential
     ```
   - Sync 100 nodes in parallel: 12s handshake + 10s transfer ≈ 22s total (vs. 1200s sequential)

3. **Reduced Handshake Frequency**:
   - Phase 4 session tokens: Extend TTL from 1 hour to 4 hours for batch operations
   - Or use session pooling: Maintain persistent "batch sync session" per node pair
   - Trade-off: Longer token TTL = larger revocation window (mitigate with continuous revocation list)

4. **Bulk Request Optimization**:
   - Pack multiple sync operations into single Phase 4 request
   - Server-side batching reduces round-trips
   - Example: Single request: `[SyncResearch, SyncVolunteer, SyncVitalSigns]` instead of 3 separate requests

---

## 6. Security: Data Boundary Leakage

### Current State
Phase 4 capabilities are broad: ReadOnly, ReadWrite, Admin on entire node.

**Problem**: No fine-grained access control per entity type or research project.

**Scenario**:
```
Node A (Public Health Institute) requests:
  "Give me all Volunteers from Node B"

Node B grants: ReadOnly access
Result: Node A receives 10,000 Volunteer records + all clinical data
        (including sensitive psychiatric diagnoses, undisclosed to Node A)
```

### Risk: Privacy Violation

**Problem**:
- GDPR Article 32: "Data minimization principle"
- LGPD Article 6: "Legitimate interest requires explicit consent per use"
- Current design: Broad ReadOnly access violates data minimization
- No per-research-project access control
- No query filtering at source node (cannot say "give me only Volunteers consented for respiratory research")

### Risk Rating: **HIGH**

### Mitigation Strategies

1. **Implement Attribute-Based Access Control (ABAC) for Sync**:
   - Extend Phase 4 capabilities to include query constraints:
     ```json
     {
       "capability": "ReadOnly",
       "constraints": {
         "entityTypes": ["Research", "ResearchVolunteer"],
         "researchProjects": ["rp-123", "rp-456"],
         "fields": ["id", "name", "enrollmentDate"], // Exclude clinical data
         "volunteerIds": null // If specified, only these volunteers
       }
     }
     ```
   - Server enforces constraints at query layer before returning results

2. **Implement Consent-Based Data Access**:
   - Track consent per Volunteer per Research per Node:
     ```sql
     CREATE TABLE volunteer_research_consent (
       id UUID PRIMARY KEY,
       volunteer_id UUID,
       research_id UUID,
       requesting_node_id UUID,
       consent_status ENUM ('granted', 'denied', 'pending'),
       consent_at TIMESTAMP,
       scope JSONB -- Which data fields are consented
     );
     ```
   - Sync only returns data matching consent scope
   - Audit trail: All consents logged

3. **Implement Query Signing & Auditing**:
   - All federated queries signed with requesting node's certificate
   - Server logs query + results + requesting node (immutable audit log)
   - Enable forensic analysis if breach suspected

4. **Implement Data Anonymization for Queries**:
   - Remote nodes don't see raw Volunteer IDs
   - Instead, use encrypted identifiers (deterministic encryption):
     ```
     Volunteer[id=123] → Encrypted ID: "enc_aB3cD...xYz"
     Node B cannot reverse-encrypt to find id=123
     Only Node A knows mapping
     ```
   - Enables federated queries without exposing volunteer identities

---

## 7. Performance with Large Datasets

### Current State
PRISM designed for single-node operation. Sync performance not yet validated.

**Problem**: As federation scales:
- 100 hospitals × 10,000 volunteers × 50 clinical records = 50 million records
- Federated query: "All patients with spasticity + sEMG >5mV" → Hits multiple nodes
- Each node processes its slice, returns results
- Total response time = max(all node query times) + aggregation latency

### Risk: Timeouts/OOM

**Problem**:
- Query timeouts if remote node slow (>30 seconds typical HTTP timeout)
- Memory exhaustion if aggregating 1M+ records on requesting node
- No pagination across federated results (all-or-nothing)

### Risk Rating: **MEDIUM**

### Mitigation Strategies

1. **Implement Streaming Results**:
   - Instead of buffering all results in memory:
     ```csharp
     // Before: Load all, then return
     var results = new List<Volunteer>();
     foreach (var node in nodes) {
         results.AddRange(await node.QueryAsync()); // OOM risk
     }
     return results;

     // After: Stream results
     var stream = new AsyncEnumerable<Volunteer>();
     foreach (var node in nodes) {
         await foreach (var volunteer in node.QueryStreamAsync()) {
             yield return volunteer;
         }
     }
     ```
   - Client receives results as they arrive (no OOM)

2. **Implement Distributed Pagination**:
   - Federated query includes `limit=1000, offset=0`
   - Each node returns min(1000, its_matching_count) results
   - Client can fetch next page
   - Example:
     ```
     Query: "spasticity + sEMG > 5mV, limit=1000"
     Node A: Returns 300 results (all it has)
     Node B: Returns 400 results
     Node C: Returns 300 results
     Total: 1000 results (page 1)
     Client fetches page 2 (offset=1000)
     ```

3. **Implement Query Timeouts with Partial Results**:
   - Set per-node timeout: 10 seconds
   - If node slower, mark as "timed out, partial results"
   - Return results from fast nodes, skip slow nodes
   - Trade-off: Completeness for latency
   - Clear indication to client: "Results from 98/100 nodes"

4. **Implement Read Replicas for Popular Queries**:
   - Cache federated query results in Redis (1-hour TTL)
   - First requester pays latency cost, subsequent requesters hit cache
   - Example:
     ```
     Key: "federated-query:spasticity+emg>5mV:hash-abc123"
     Value: [1000 results cached]
     TTL: 3600 seconds
     ```

---

## Summary: Risk Mitigation Roadmap

| Phase | Risk | Mitigation | Effort | Timeline |
|-------|------|-----------|--------|----------|
| **Phase 5a** | 1 | Define sync scope, entity grouping | Low | Week 1 |
| **Phase 5a** | 2 | Implement delta-sync with event log | High | Week 2-3 |
| **Phase 5b** | 3 | Implement vector clocks + conflict resolution | High | Week 3-4 |
| **Phase 5b** | 4 | Implement idempotent sync, checkpoints | Medium | Week 2 |
| **Phase 5c** | 5 | Session reuse, async pipelines | Low | Week 1 |
| **Phase 5c** | 6 | Implement ABAC, consent tracking | High | Week 4+ |
| **Phase 5d** | 7 | Streaming results, distributed pagination | Medium | Week 3-4 |

---

## Critical Success Factors

1. **Never sync without delta tracking** — Full sync scales only to ~10 nodes
2. **Conflict resolution must be deterministic** — Flipping between "A wins" and "B wins" corrupts data
3. **Idempotency is non-negotiable** — Network failures will happen; retries must not break state
4. **Start with immutable core entities** — Eliminates 50% of conflict scenarios
5. **Encrypt personally identifiable data** — Use deterministic encryption for federated queries

---

## Recommended Next Steps

1. ✅ **This document** — Identifies technical risks and mitigations
2. 📋 **Phase 5 Architecture Document** — Detail sync protocol, data structures, algorithms
3. 🛠️ **Spike: Event Log Implementation** — Proof-of-concept delta-sync tracking
4. 🛠️ **Spike: Vector Clock Merge** — Test conflict resolution with sample data
5. 📊 **Load Testing** — Validate performance with 100+ node simulations

---

**Prepared by**: Tech Lead Assistance
**Status**: Ready for Phase 5 architecture planning
**Review Frequency**: Weekly (until Phase 5 complete)
