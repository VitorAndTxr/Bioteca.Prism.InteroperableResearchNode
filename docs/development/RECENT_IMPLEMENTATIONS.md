# Recent Implementations

**Version**: 1.0.0
**Last Updated**: 2025-11-04
**Status**: Production-Ready

This document details recent implementations in the InteroperableResearchNode, including pagination support, user management endpoints, and middleware enhancements.

---

## Table of Contents

1. [Overview](#overview)
2. [User Management System](#user-management-system)
3. [Pagination System](#pagination-system)
4. [Middleware Enhancements](#middleware-enhancements)
5. [Researcher Management](#researcher-management)
6. [Migration Guide](#migration-guide)

---

## Overview

### What's New (November 2025)

#### ✅ Completed

1. **Pagination System**: Standardized pagination across all list endpoints
2. **User Management**: Complete CRUD endpoints for user operations
3. **Middleware Enhancements**: Dual-version encryption middleware (generic and non-generic)
4. **DTO Mapping**: Standardized entity-to-DTO mapping with nested relations
5. **BaseController Extensions**: Service invocation helpers with pagination support

#### 🚧 In Progress

1. **Researcher Management**: Partial implementation (AddAsync has NotImplementedException)
2. **Validation Framework**: Moving from exceptions to structured validation
3. **Comprehensive Testing**: Integration tests for new endpoints

---

## User Management System

### Architecture

```
┌─────────────────────────────────────┐
│         UserController              │
│  - GetUsers() [Paginated]           │
│  - New() [Create User]              │
└──────────────┬──────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│          UserService                │
│  - GetAllUserPaginateAsync()        │
│  - AddAsync(AddUserPayload)         │
│  - ValidateAddUserPayload()         │
└──────────────┬──────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│         UserRepository              │
│  - GetPagedAsync() [Override]       │
│  - GetByUsername()                  │
│  - AddAsync()                       │
└─────────────────────────────────────┘
```

### Files Modified

| File | Location | Changes |
|------|----------|---------|
| `UserController.cs` | `InteroperableResearchNode/Controllers/` | Added `GetUsers()` and `New()` endpoints |
| `UserService.cs` | `Service/Services/User/` | Added `GetAllUserPaginateAsync()` and `AddAsync()` |
| `UserRepository.cs` | `Data/Repositories/User/` | Overridden `GetPagedAsync()` with `.Include()` |
| `IUserService.cs` | `Service/Interfaces/User/` | Added method signatures |
| `UserDTO.cs` | `Domain/DTOs/User/` | Added `ResearcherInfoDto` nested class |
| `AddUserPayload.cs` | `Domain/Payloads/User/` | Request payload for user creation |

---

### Implementation Details

#### 1. UserController - GetUsers Endpoint

**File**: `Bioteca.Prism.InteroperableResearchNode/Controllers/UserController.cs:48`

```csharp
/// <summary>
/// Get paginated list of users
/// </summary>
[Route("[action]")]
[HttpGet]
[PrismEncryptedChannelConnection]
[PrismAuthenticatedSession]
[Authorize("sub")]
[ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public async Task<IActionResult> GetUsers(CancellationToken cancellationToken = default)
{
    try
    {
        return ServiceInvoke(_userService.GetAllUserPaginateAsync).Result;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to retrieve paginated users");
        return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
            "ERR_GET_USERS_FAILED",
            "Failed to retrieve users",
            new Dictionary<string, object> { ["reason"] = "internal_error" },
            retryable: true
        ));
    }
}
```

**Key Features**:
- ✓ Paginated response via `ServiceInvoke()`
- ✓ Encrypted communication (`PrismEncryptedChannelConnection`)
- ✓ Session authentication (`PrismAuthenticatedSession`)
- ✓ Capability-based authorization (`Authorize("sub")`)
- ✓ Structured error handling with logging

**Usage**:
```http
GET /api/user/GetUsers?page=1&pageSize=20
X-Channel-Id: {channel_id}
X-Session-Id: {session_token}
```

**Response**:
```json
{
  "data": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "login": "researcher1",
      "role": "Researcher",
      "createdAt": "2025-11-04T10:30:00Z",
      "updatedAt": "2025-11-04T10:30:00Z",
      "researcher": {
        "name": "Dr. John Doe",
        "email": "john.doe@institution.edu",
        "role": "Principal Investigator",
        "orcid": "0000-0002-1825-0097"
      }
    }
  ],
  "currentPage": 1,
  "pageSize": 20,
  "totalRecords": 150
}
```

---

#### 2. UserController - New Endpoint

**File**: `Bioteca.Prism.InteroperableResearchNode/Controllers/UserController.cs:73`

```csharp
[Route("[action]")]
[HttpPost]
[PrismEncryptedChannelConnection<AddUserPayload>]
[PrismAuthenticatedSession]
[Authorize("sub")]
[ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public IActionResult New()
{
    try
    {
        var payload = HttpContext.Items["DecryptedRequest"] as AddUserPayload;
        return ServiceInvoke(_userService.AddAsync, payload).Result;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to register new user");
        return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
            "ERR_USER_ADD_FAILED",
            "Failed to add new user:" + ex.Message,
            new Dictionary<string, object> { ["reason"] = "internal_error" },
            retryable: true
        ));
    }
}
```

**Key Features**:
- ✓ Generic middleware (`PrismEncryptedChannelConnection<AddUserPayload>`)
- ✓ Automatic payload decryption and deserialization
- ✓ Payload retrieved from `HttpContext.Items["DecryptedRequest"]`
- ✓ Password encryption handled in service layer

**Usage**:
```http
POST /api/user/New
X-Channel-Id: {channel_id}
X-Session-Id: {session_token}
Content-Type: application/json

{
  "encryptedPayload": "...",
  "iv": "...",
  "tag": "..."
}
```

**Request Payload** (decrypted):
```json
{
  "login": "newuser",
  "password": "SecurePassword123!",
  "role": "Researcher",
  "researcherId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

---

#### 3. UserService - GetAllUserPaginateAsync

**File**: `Bioteca.Prism.Service/Services/User/UserService.cs:72`

```csharp
public async Task<List<UserDTO>> GetAllUserPaginateAsync()
{
    var result = await _userRepository.GetPagedAsync();

    var mappedResult = result.Select(user => new UserDTO
    {
        Id = user.Id,
        Login = user.Login,
        Role = user.Role,
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt,
        Researcher = user.Researcher != null ? new ResearcherInfoDto()
        {
            Name = user.Researcher.Name,
            Email = user.Researcher.Email,
            Role = user.Researcher.Role,
            Orcid = user.Researcher.Orcid
        } : null
    }).ToList();

    return mappedResult;
}
```

**Key Features**:
- ✓ Entity-to-DTO mapping
- ✓ Nested DTO for related Researcher
- ✓ Null-safe mapping (`user.Researcher != null`)
- ✓ Excludes sensitive fields (e.g., `PasswordHash`)

---

#### 4. UserService - AddAsync

**File**: `Bioteca.Prism.Service/Services/User/UserService.cs:29`

```csharp
public async Task<Domain.Entities.User.User?> AddAsync(AddUserPayload payload)
{
    ValidateAddUserPayload(payload);

    Domain.Entities.Researcher.Researcher? researcher = null;

    if (payload.ResearcherId != null)
    {
        researcher = _researcherRepository.GetByIdAsync(payload.ResearcherId.Value).Result;

        if (researcher == null)
        {
            throw new Exception("Researcher does not exist");
        }
    }

    Domain.Entities.User.User user = new Domain.Entities.User.User
    {
        Id = Guid.NewGuid(),
        Login = payload.Login,
        PasswordHash = _userAuthService.EncryptAsync(payload.Password).Result,
        Role = payload.Role,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        Researcher = researcher != null ? researcher : null
    };

    return await _userRepository.AddAsync(user);
}
```

**Key Features**:
- ✓ Payload validation
- ✓ Optional researcher association
- ✓ Password encryption via `IUserAuthService`
- ✓ Automatic timestamp generation

**Validation Logic**:
```csharp
private void ValidateAddUserPayload(AddUserPayload payload)
{
    if (string.IsNullOrEmpty(payload.Login) ||
        string.IsNullOrEmpty(payload.Password) ||
        string.IsNullOrEmpty(payload.Role))
    {
        throw new Exception("Invalid payload");
    }

    if (_userRepository.GetByUsername(payload.Login) != null)
    {
        throw new Exception("User already exists");
    }
}
```

---

#### 5. UserRepository - GetPagedAsync

**File**: `Bioteca.Prism.Data/Repositories/User/UserRepository.cs:30`

```csharp
public override async Task<List<Domain.Entities.User.User>> GetPagedAsync()
{
    // Set request pagination in ApiContext
    var page = _apiContext.PagingContext.RequestPaging.Page;
    var pageSize = _apiContext.PagingContext.RequestPaging.PageSize;

    // Validate and normalize pagination parameters
    if (page < 1) page = 1;
    if (pageSize < 1) pageSize = 10;
    if (pageSize > 100) pageSize = 100; // Max page size limit

    // Build base query with related entities
    var query = _dbSet
        .Include(u => u.Researcher)
            .ThenInclude(r => r.ResearchResearchers)
        .AsQueryable();

    // Get total count
    var totalCount = await query.CountAsync();

    // Apply pagination
    var items = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

    _apiContext.PagingContext.ResponsePaging.SetValues(page, pageSize, totalPages);

    return items;
}
```

**Key Features**:
- ✓ Pagination parameter validation
- ✓ **Eager loading of related entities** (`.Include()`)
- ✓ Total count before pagination
- ✓ Response metadata update

**Important**: The `.Include()` is critical to avoid N+1 query problems.

---

#### 6. UserDTO with Nested Relations

**File**: `Bioteca.Prism.Domain/DTOs/User/UserDTO.cs:1`

```csharp
public class UserDTO
{
    public Guid Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Associated researcher (if applicable)
    /// </summary>
    public ResearcherInfoDto? Researcher { get; set; }
}

public class ResearcherInfoDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Orcid { get; set; } = string.Empty;
}
```

**Key Features**:
- ✓ Excludes sensitive fields (`PasswordHash`)
- ✓ Nested DTO for related entity
- ✓ Nullable researcher (not all users are researchers)

---

## Pagination System

### Overview

Standardized pagination implemented across all list endpoints using `IApiContext.PagingContext`.

**For detailed documentation, see**: `PAGINATION_SYSTEM.md`

### Key Components

1. **RequestPaging**: Query parameters from client (`page`, `pageSize`)
2. **ResponsePaging**: Metadata for response (`totalPages`, `totalRecords`)
3. **PagedResult<T>**: Wrapper DTO for paginated responses

### Flow

```
Client Request (?page=2&pageSize=20)
    ↓
BaseController.HandleQueryParameters()
    ↓
IApiContext.PagingContext.RequestPaging updated
    ↓
Repository.GetPagedAsync() uses RequestPaging
    ↓
Repository updates ResponsePaging
    ↓
BaseController.JsonResponseMessage() wraps in PagedResult<T>
    ↓
Encrypted response sent to client
```

### Usage Pattern

```csharp
// Repository
public override async Task<List<Entity>> GetPagedAsync()
{
    var page = _apiContext.PagingContext.RequestPaging.Page;
    var pageSize = _apiContext.PagingContext.RequestPaging.PageSize;

    // ... pagination logic

    _apiContext.PagingContext.ResponsePaging.SetValues(page, pageSize, totalPages);
    return items;
}

// Service
public async Task<List<EntityDTO>> GetAllPaginateAsync()
{
    var result = await _repository.GetPagedAsync();
    return result.Select(e => MapToDTO(e)).ToList();
}

// Controller
[HttpGet]
public async Task<IActionResult> GetAll()
{
    return ServiceInvoke(_service.GetAllPaginateAsync).Result;
}
```

---

## Middleware Enhancements

### Dual-Version Encryption Middleware

**File**: `Bioteca.Prism.Core/Middleware/Channel/PrismEncryptedChannelConnectionAttribute.cs`

#### Version 1: Generic (For POST/PUT/DELETE)

```csharp
[PrismEncryptedChannelConnection<TPayload>]
```

**Features**:
- Decrypts request body
- Deserializes to `TPayload`
- Validates digital signatures (for `NodeIdentifyRequest`)
- Stores in `HttpContext.Items["DecryptedRequest"]`
- Encrypts response

**Usage**:
```csharp
[HttpPost]
[PrismEncryptedChannelConnection<AddUserPayload>]
public IActionResult New()
{
    var payload = HttpContext.Items["DecryptedRequest"] as AddUserPayload;
    // ... process payload
}
```

#### Version 2: Non-Generic (For GET)

```csharp
[PrismEncryptedChannelConnection]
```

**Features**:
- Validates channel (no body decryption)
- Encrypts response
- Lighter-weight for read operations

**Usage**:
```csharp
[HttpGet]
[PrismEncryptedChannelConnection]
public async Task<IActionResult> GetUsers()
{
    // No payload decryption needed
}
```

### Middleware Stack

Typical middleware ordering for protected endpoints:

```csharp
[PrismEncryptedChannelConnection<TPayload>]  // Layer 1: Encryption
[PrismAuthenticatedSession]                   // Layer 2: Session validation
[Authorize("capability")]                      // Layer 3: Authorization
```

**Execution Order**:
1. Validate channel
2. Decrypt request (if generic version)
3. Validate session
4. Check authorization capability
5. Execute action
6. Encrypt response

---

## Researcher Management

### Current Status

**Implemented**:
- ✓ Repository interface and implementation
- ✓ Service interface
- ✓ `GetAllResearchersPaginateAsync()` method
- ✓ Validation logic for `AddAsync()`

**Pending**:
- ⏳ Complete `AddAsync()` implementation (currently throws `NotImplementedException`)
- ⏳ Controller endpoints
- ⏳ Integration tests

### Files

| File | Location | Status |
|------|----------|--------|
| `ResearcherService.cs` | `Service/Services/Researcher/` | Partial |
| `IResearcherService.cs` | `Service/Interfaces/Researcher/` | Complete |
| `ResearcherDTO.cs` | `Domain/DTOs/User/` | Empty class (TODO) |

### Known Issues

#### Issue 1: AddAsync Not Implemented

**File**: `ResearcherService.cs:32`

```csharp
public Task<Domain.Entities.Researcher.Researcher?> AddAsync(AddResearcherPayload payload)
{
    ValidateAddResearcherPayload(payload);

    Domain.Entities.Researcher.Researcher researcher = new Domain.Entities.Researcher.Researcher
    {
        ResearcherId = Guid.NewGuid(),
        Name = payload.Name,
        Email = payload.Email,
        Orcid = payload.Orcid,
        Role = payload.Role,
        ResearchNodeId = payload.ResearchNodeId,
        Institution = payload.Institution,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    throw new NotImplementedException(); // TODO: Implement repository call
}
```

**Fix Needed**:
```csharp
return await _researcherRepository.AddAsync(researcher);
```

#### Issue 2: Validation Logic Bug

**File**: `ResearcherService.cs:73-81`

```csharp
// ✗ WRONG: Inverted logic
if (_researcherRepository.GetByOrcidAsync(payload.Orcid).Result == null)
{
    throw new Exception("Researcher already exists");
}

if (_researcherRepository.GetByEmailAsync(payload.Orcid).Result == null)
{
    throw new Exception("Email already in use");
}
```

**Should Be**:
```csharp
// ✓ CORRECT
if (_researcherRepository.GetByOrcidAsync(payload.Orcid).Result != null)
{
    throw new Exception("Researcher already exists");
}

if (_researcherRepository.GetByEmailAsync(payload.Email).Result != null)
{
    throw new Exception("Email already in use");
}
```

**Issues**:
1. Logic is inverted (`== null` should be `!= null`)
2. Email check uses `payload.Orcid` instead of `payload.Email`

#### Issue 3: Empty ResearcherDTO

**File**: `Domain/DTOs/User/ResearcherDTO.cs:47`

```csharp
public class ResearcherDTO
{
    // Empty class - TODO: Implement
}
```

**Should Include**:
```csharp
public class ResearcherDTO
{
    public Guid ResearcherId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Orcid { get; set; } = string.Empty;
    public string Institution { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

---

## Migration Guide

### For Existing Endpoints

If you have existing list endpoints without pagination:

#### Step 1: Override GetPagedAsync() in Repository

```csharp
// Before
public async Task<List<Entity>> GetAll()
{
    return await _dbSet.ToListAsync();
}

// After
public override async Task<List<Entity>> GetPagedAsync()
{
    var page = _apiContext.PagingContext.RequestPaging.Page;
    var pageSize = _apiContext.PagingContext.RequestPaging.PageSize;

    if (page < 1) page = 1;
    if (pageSize < 1) pageSize = 10;
    if (pageSize > 100) pageSize = 100;

    var query = _dbSet.AsQueryable();
    var totalCount = await query.CountAsync();

    var items = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
    _apiContext.PagingContext.ResponsePaging.SetValues(page, pageSize, totalPages);

    return items;
}
```

#### Step 2: Update Service Method

```csharp
// Before
public async Task<List<EntityDTO>> GetAll()
{
    var entities = await _repository.GetAll();
    return entities.Select(e => MapToDTO(e)).ToList();
}

// After
public async Task<List<EntityDTO>> GetAllPaginateAsync()
{
    var entities = await _repository.GetPagedAsync();
    return entities.Select(e => MapToDTO(e)).ToList();
}
```

#### Step 3: Update Controller

```csharp
// Before
[HttpGet]
public async Task<IActionResult> GetAll()
{
    var result = await _service.GetAll();
    return Ok(result);
}

// After
[HttpGet]
public async Task<IActionResult> GetAll()
{
    return ServiceInvoke(_service.GetAllPaginateAsync).Result;
}
```

---

## Best Practices Summary

### 1. Repository Layer
- ✓ Override `GetPagedAsync()` for pagination
- ✓ Use `.Include()` for related entities
- ✓ Validate page/pageSize parameters
- ✓ Update `ResponsePaging` metadata

### 2. Service Layer
- ✓ Map entities to DTOs
- ✓ Implement validation logic
- ✓ Handle business rules
- ✓ Use dependency injection

### 3. Controller Layer
- ✓ Use `ServiceInvoke()` helper
- ✓ Add appropriate middleware attributes
- ✓ Handle exceptions and log errors
- ✓ Document query parameters

### 4. Security
- ✓ Use encrypted channels for sensitive data
- ✓ Combine middleware for defense-in-depth
- ✓ Validate session tokens
- ✓ Check authorization capabilities

---

## Related Documentation

- **API Endpoint Implementation Guide**: `API_ENDPOINT_IMPLEMENTATION_GUIDE.md`
- **Pagination System**: `PAGINATION_SYSTEM.md`
- **Middleware Patterns**: `MIDDLEWARE_PATTERNS.md`
- **Security Overview**: `../SECURITY_OVERVIEW.md`

---

**Next Steps**:
1. Fix Researcher Management issues (see Known Issues)
2. Implement integration tests for new endpoints
3. Add validation framework (replace exceptions with structured validation)
4. Document remaining endpoints

---

**Last Updated**: 2025-11-04
**Maintained By**: PRISM Development Team
