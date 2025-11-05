# API Endpoint Implementation Guide

**Version**: 1.0.0
**Last Updated**: 2025-11-04
**Status**: Production-Ready

This guide provides step-by-step instructions for implementing new API endpoints in the InteroperableResearchNode, following PRISM architecture patterns and best practices.

---

## Table of Contents

1. [Overview](#overview)
2. [Architecture Layers](#architecture-layers)
3. [Step-by-Step Implementation](#step-by-step-implementation)
4. [Pagination Implementation](#pagination-implementation)
5. [Encryption & Security](#encryption--security)
6. [Testing Guidelines](#testing-guidelines)
7. [Common Patterns](#common-patterns)
8. [Examples](#examples)

---

## Overview

PRISM follows **Clean Architecture** principles with clear separation of concerns:

```
Domain (Entities, DTOs, Payloads)
    ↓
Core (Interfaces, Base Classes, Middleware)
    ↓
Data (Repositories, EF Core)
    ↓
Service (Business Logic)
    ↓
API (Controllers)
```

### Key Principles

1. **Dependency Injection**: All services and repositories use DI
2. **Generic Base Pattern**: Leverage `BaseRepository<TEntity, TKey>` and `BaseService<TEntity, TKey>`
3. **DTO Mapping**: Never expose entities directly - use DTOs
4. **Encrypted Communication**: Use `PrismEncryptedChannelConnection` for sensitive endpoints
5. **Pagination Support**: Built-in pagination via `IApiContext`

---

## Architecture Layers

### 1. Domain Layer (`Bioteca.Prism.Domain`)

**Purpose**: Define data structures without business logic

**Contains**:
- **Entities**: Database models (e.g., `User`, `Researcher`)
- **DTOs**: Data Transfer Objects for API responses
- **Payloads**: Request bodies for API calls
- **Enums**: Enumeration types

**Example Structure**:
```
Domain/
├── Entities/
│   └── User/
│       └── User.cs
├── DTOs/
│   └── User/
│       └── UserDTO.cs
└── Payloads/
    └── User/
        └── AddUserPayload.cs
```

### 2. Data Layer (`Bioteca.Prism.Data`)

**Purpose**: Database access and persistence

**Contains**:
- **Repositories**: Implement data access logic
- **Interfaces**: Repository contracts
- **DbContext**: EF Core configuration

**Example Structure**:
```
Data/
├── Repositories/
│   └── User/
│       └── UserRepository.cs
├── Interfaces/
│   └── User/
│       └── IUserRepository.cs
└── Persistence/
    └── Contexts/
        └── PrismDbContext.cs
```

### 3. Service Layer (`Bioteca.Prism.Service`)

**Purpose**: Business logic and orchestration

**Contains**:
- **Services**: Implement business rules
- **Interfaces**: Service contracts

**Example Structure**:
```
Service/
├── Services/
│   └── User/
│       └── UserService.cs
└── Interfaces/
    └── User/
        └── IUserService.cs
```

### 4. API Layer (`Bioteca.Prism.InteroperableResearchNode`)

**Purpose**: HTTP endpoints and request handling

**Contains**:
- **Controllers**: API endpoints
- **Middleware**: Request/response processing
- **Filters**: Authorization, validation

---

## Step-by-Step Implementation

### Step 1: Define Entity (Domain Layer)

**File**: `Bioteca.Prism.Domain/Entities/{EntityType}/{EntityName}.cs`

```csharp
namespace Bioteca.Prism.Domain.Entities.User
{
    public class User
    {
        public Guid Id { get; set; }
        public string Login { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public Researcher? Researcher { get; set; }
    }
}
```

**Key Points**:
- Use appropriate data types (Guid for IDs, DateTime for timestamps)
- Include navigation properties for relationships
- Don't include business logic

---

### Step 2: Define DTOs (Domain Layer)

**File**: `Bioteca.Prism.Domain/DTOs/{EntityType}/{EntityName}DTO.cs`

```csharp
namespace Bioteca.Prism.Domain.DTOs.User
{
    /// <summary>
    /// Data Transfer Object for User entity - excludes sensitive fields
    /// </summary>
    public class UserDTO
    {
        public Guid Id { get; set; }
        public string Login { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Related entity info (simplified)
        public ResearcherInfoDto? Researcher { get; set; }
    }

    /// <summary>
    /// Simplified researcher information for nested display
    /// </summary>
    public class ResearcherInfoDto
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Orcid { get; set; } = string.Empty;
    }
}
```

**Key Points**:
- **Exclude sensitive fields** (e.g., `PasswordHash`)
- Use nested DTOs for related entities
- Add XML documentation comments

---

### Step 3: Define Payloads (Domain Layer)

**File**: `Bioteca.Prism.Domain/Payloads/{EntityType}/Add{EntityName}Payload.cs`

```csharp
namespace Bioteca.Prism.Domain.Payloads.User
{
    /// <summary>
    /// Payload for creating a new user
    /// </summary>
    public class AddUserPayload
    {
        public string Login { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public Guid? ResearcherId { get; set; }
    }
}
```

**Key Points**:
- Only include fields needed for the operation
- Use nullable types for optional fields
- Plain password (will be encrypted in service layer)

---

### Step 4: Create Repository Interface (Data Layer)

**File**: `Bioteca.Prism.Data/Interfaces/{EntityType}/I{EntityName}Repository.cs`

```csharp
using Bioteca.Prism.Core.Interfaces;

namespace Bioteca.Prism.Data.Interfaces.User
{
    /// <summary>
    /// Repository interface for User entity
    /// </summary>
    public interface IUserRepository : IBaseRepository<Domain.Entities.User.User, Guid>
    {
        /// <summary>
        /// Get user by username
        /// </summary>
        Domain.Entities.User.User? GetByUsername(string username);
    }
}
```

**Key Points**:
- Extend `IBaseRepository<TEntity, TKey>` for CRUD operations
- Add custom methods specific to this entity
- Use proper return types (nullable if may not exist)

---

### Step 5: Implement Repository (Data Layer)

**File**: `Bioteca.Prism.Data/Repositories/{EntityType}/{EntityName}Repository.cs`

```csharp
using Bioteca.Prism.Core.Database;
using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Data.Interfaces.User;
using Bioteca.Prism.Data.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Bioteca.Prism.Data.Repositories.User
{
    public class UserRepository : BaseRepository<Domain.Entities.User.User, Guid>, IUserRepository
    {
        public UserRepository(PrismDbContext context, IApiContext apiContext)
            : base(context, apiContext)
        {
        }

        public Domain.Entities.User.User? GetByUsername(string username)
        {
            try
            {
                return _dbSet
                    .Include(u => u.Researcher)
                        .ThenInclude(r => r.ResearchResearchers)
                    .FirstOrDefault(u => u.Login == username);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public override async Task<List<Domain.Entities.User.User>> GetPagedAsync()
        {
            // Get pagination parameters from ApiContext
            var page = _apiContext.PagingContext.RequestPaging.Page;
            var pageSize = _apiContext.PagingContext.RequestPaging.PageSize;

            // Validate and normalize
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100; // Max limit

            // Build query with related entities
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

            // Update response paging context
            _apiContext.PagingContext.ResponsePaging.SetValues(page, pageSize, totalPages);

            return items;
        }
    }
}
```

**Key Points**:
- **Always use `.Include()` for related entities** to avoid N+1 queries
- Override `GetPagedAsync()` to implement pagination
- Use `_apiContext.PagingContext` for pagination metadata
- Validate pagination parameters (min/max page size)

---

### Step 6: Create Service Interface (Service Layer)

**File**: `Bioteca.Prism.Service/Interfaces/{EntityType}/I{EntityName}Service.cs`

```csharp
using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Domain.DTOs.User;
using Bioteca.Prism.Domain.Payloads.User;

namespace Bioteca.Prism.Service.Interfaces.User
{
    /// <summary>
    /// Service interface for User operations
    /// </summary>
    public interface IUserService : IServiceBase<Domain.Entities.User.User, Guid>
    {
        /// <summary>
        /// Add a new user with encrypted password
        /// </summary>
        Task<Domain.Entities.User.User?> AddAsync(AddUserPayload payload);

        /// <summary>
        /// Get all users paginated
        /// </summary>
        Task<List<UserDTO>> GetAllUserPaginateAsync();
    }
}
```

**Key Points**:
- Extend `IServiceBase<TEntity, TKey>` for base operations
- Define methods that accept **Payloads** and return **DTOs**
- Use async methods for database operations

---

### Step 7: Implement Service (Service Layer)

**File**: `Bioteca.Prism.Service/Services/{EntityType}/{EntityName}Service.cs`

```csharp
using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Core.Service;
using Bioteca.Prism.Data.Interfaces.User;
using Bioteca.Prism.Domain.DTOs.User;
using Bioteca.Prism.Domain.Payloads.User;
using Bioteca.Prism.Service.Interfaces.User;

namespace Bioteca.Prism.Service.Services.User
{
    public class UserService : BaseService<Domain.Entities.User.User, Guid>, IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserAuthService _userAuthService;

        public UserService(
            IUserRepository repository,
            IUserAuthService userAuthService,
            IApiContext apiContext
        ) : base(repository, apiContext)
        {
            _userRepository = repository;
            _userAuthService = userAuthService;
        }

        public async Task<Domain.Entities.User.User?> AddAsync(AddUserPayload payload)
        {
            // Validate payload
            ValidateAddUserPayload(payload);

            // Create entity
            var user = new Domain.Entities.User.User
            {
                Id = Guid.NewGuid(),
                Login = payload.Login,
                PasswordHash = await _userAuthService.EncryptAsync(payload.Password),
                Role = payload.Role,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Save to database
            return await _userRepository.AddAsync(user);
        }

        public async Task<List<UserDTO>> GetAllUserPaginateAsync()
        {
            // Get paginated entities
            var result = await _userRepository.GetPagedAsync();

            // Map to DTOs
            var mappedResult = result.Select(user => new UserDTO
            {
                Id = user.Id,
                Login = user.Login,
                Role = user.Role,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                Researcher = user.Researcher != null ? new ResearcherInfoDto
                {
                    Name = user.Researcher.Name,
                    Email = user.Researcher.Email,
                    Role = user.Researcher.Role,
                    Orcid = user.Researcher.Orcid
                } : null
            }).ToList();

            return mappedResult;
        }

        private void ValidateAddUserPayload(AddUserPayload payload)
        {
            if (string.IsNullOrEmpty(payload.Login) ||
                string.IsNullOrEmpty(payload.Password) ||
                string.IsNullOrEmpty(payload.Role))
            {
                throw new Exception("Invalid payload: missing required fields");
            }

            if (_userRepository.GetByUsername(payload.Login) != null)
            {
                throw new Exception("User already exists");
            }
        }
    }
}
```

**Key Points**:
- **Validate payloads** before processing
- **Map entities to DTOs** - never return entities directly
- Use dependency injection for other services
- Handle business logic here (e.g., password encryption)

---

### Step 8: Create Controller (API Layer)

**File**: `Bioteca.Prism.InteroperableResearchNode/Controllers/{EntityName}Controller.cs`

```csharp
using Bioteca.Prism.Core.Controllers;
using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Core.Middleware.Channel;
using Bioteca.Prism.Core.Security.Authorization;
using Bioteca.Prism.Domain.Payloads.User;
using Bioteca.Prism.InteroperableResearchNode.Middleware;
using Bioteca.Prism.Service.Interfaces.User;
using Microsoft.AspNetCore.Mvc;

namespace Bioteca.Prism.InteroperableResearchNode.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : BaseController
    {
        private readonly IUserService _userService;
        private readonly ILogger<UserController> _logger;

        public UserController(
            IUserService userService,
            ILogger<UserController> logger,
            IConfiguration configuration,
            IApiContext apiContext
        ) : base(logger, configuration, apiContext)
        {
            _logger = logger;
            _userService = userService;
        }

        /// <summary>
        /// Get paginated list of users
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Paginated list of users</returns>
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

        /// <summary>
        /// Create a new user
        /// </summary>
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
                _logger.LogError(ex, "Failed to create new user");
                return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                    "ERR_USER_ADD_FAILED",
                    "Failed to add new user: " + ex.Message,
                    new Dictionary<string, object> { ["reason"] = "internal_error" },
                    retryable: true
                ));
            }
        }
    }
}
```

**Key Points**:
- **Extend `BaseController`** for common functionality
- Use `ServiceInvoke()` helper for calling services
- Add appropriate middleware attributes (see below)
- Handle exceptions and log errors
- Use XML documentation for Swagger

---

### Step 9: Register Services (Startup)

**File**: `Program.cs` or `Startup.cs`

```csharp
// Register repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Register services
builder.Services.AddScoped<IUserService, UserService>();
```

**Key Points**:
- Repositories: Use **Scoped** lifetime (per request)
- Services: Use **Scoped** lifetime
- Singletons: Only for stateless utilities

---

## Pagination Implementation

### How Pagination Works

1. **Query Parameters**: Client sends `?page=1&pageSize=10`
2. **BaseController**: Parses query parameters into `IApiContext.PagingContext`
3. **Repository**: Uses pagination context to query database
4. **Response**: Returns data + pagination metadata

### Client Request Example

```http
GET /api/user/GetUsers?page=2&pageSize=20
X-Channel-Id: {channel_id}
```

### Server Response Structure

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
  "currentPage": 2,
  "pageSize": 20,
  "totalRecords": 150
}
```

### Implementation Checklist

**Repository**:
- ✓ Override `GetPagedAsync()` method
- ✓ Read pagination from `_apiContext.PagingContext.RequestPaging`
- ✓ Validate page/pageSize parameters
- ✓ Apply `.Skip()` and `.Take()` to query
- ✓ Count total records
- ✓ Update `_apiContext.PagingContext.ResponsePaging`

**Controller**:
- ✓ Use `ServiceInvoke()` helper (handles pagination automatically)
- ✓ Document query parameters in XML comments

**Service**:
- ✓ Call `repository.GetPagedAsync()`
- ✓ Map entities to DTOs

---

## Encryption & Security

### Middleware Attributes

#### 1. `[PrismEncryptedChannelConnection]`

**Purpose**: Decrypt incoming requests, encrypt outgoing responses

**Usage**: Read-only operations (GET)

```csharp
[HttpGet]
[PrismEncryptedChannelConnection]
public async Task<IActionResult> GetUsers() { }
```

**Behavior**:
- Validates `X-Channel-Id` header
- Verifies channel exists and is not expired
- **Does NOT decrypt body** (GET requests have no body)
- Encrypts response automatically

#### 2. `[PrismEncryptedChannelConnection<TPayload>]`

**Purpose**: Decrypt request body, validate, encrypt response

**Usage**: Write operations (POST, PUT, DELETE)

```csharp
[HttpPost]
[PrismEncryptedChannelConnection<AddUserPayload>]
public IActionResult New()
{
    var payload = HttpContext.Items["DecryptedRequest"] as AddUserPayload;
    // ... use payload
}
```

**Behavior**:
- All features of non-generic version
- **Decrypts request body** to `TPayload`
- Stores decrypted payload in `HttpContext.Items["DecryptedRequest"]`
- Validates digital signatures (for `NodeIdentifyRequest`)

#### 3. `[PrismAuthenticatedSession]`

**Purpose**: Verify active session token

**Usage**: Endpoints requiring Phase 4 authentication

```csharp
[PrismEncryptedChannelConnection]
[PrismAuthenticatedSession]
public async Task<IActionResult> GetUsers() { }
```

**Behavior**:
- Validates `X-Session-Id` header
- Verifies session exists and is not expired
- Checks rate limiting (60 req/min)

#### 4. `[Authorize("sub")]`

**Purpose**: Capability-based authorization

**Usage**: Restrict access by session capability

```csharp
[PrismAuthenticatedSession]
[Authorize("sub")]
public async Task<IActionResult> GetUsers() { }
```

**Capabilities**:
- `ReadOnly`: Can view data
- `ReadWrite`: Can modify data
- `Admin`: Full access

### Security Best Practices

1. **Always use encrypted channels** for sensitive data
2. **Combine middleware attributes** for defense-in-depth
3. **Log security events** (authentication failures, invalid signatures)
4. **Never expose entities directly** - use DTOs
5. **Validate all inputs** in service layer

---

## Testing Guidelines

### Unit Tests

```csharp
[Fact]
public async Task AddAsync_ValidPayload_ReturnsUser()
{
    // Arrange
    var payload = new AddUserPayload
    {
        Login = "testuser",
        Password = "SecurePassword123!",
        Role = "Researcher"
    };

    // Act
    var result = await _userService.AddAsync(payload);

    // Assert
    Assert.NotNull(result);
    Assert.Equal("testuser", result.Login);
}
```

### Integration Tests

```csharp
[Fact]
public async Task GetUsers_WithPagination_ReturnsPagedResults()
{
    // Arrange
    var client = _factory.CreateClient();

    // Act
    var response = await client.GetAsync("/api/user/GetUsers?page=1&pageSize=10");

    // Assert
    response.EnsureSuccessStatusCode();
    var content = await response.Content.ReadAsStringAsync();
    var result = JsonSerializer.Deserialize<PagedResult<List<UserDTO>>>(content);

    Assert.Equal(1, result.CurrentPage);
    Assert.Equal(10, result.PageSize);
}
```

---

## Common Patterns

### Pattern 1: Simple CRUD Endpoint

**Use Case**: Basic read/write operations

**Implementation**:
```csharp
// Repository: Use BaseRepository methods directly
public class EntityRepository : BaseRepository<Entity, Guid>, IEntityRepository { }

// Service: Call repository methods
public async Task<Entity> GetByIdAsync(Guid id)
{
    return await _repository.GetByIdAsync(id);
}

// Controller: Standard CRUD endpoints
[HttpGet("{id}")]
public async Task<IActionResult> Get(Guid id)
{
    return Ok(await _service.GetByIdAsync(id));
}
```

### Pattern 2: Paginated List Endpoint

**Use Case**: List endpoints with pagination

**Implementation**: See [Pagination Implementation](#pagination-implementation)

### Pattern 3: Complex Query Endpoint

**Use Case**: Filtering, searching, sorting

**Implementation**:
```csharp
// Repository
public async Task<List<User>> SearchAsync(string query)
{
    return await _dbSet
        .Where(u => u.Login.Contains(query) || u.Email.Contains(query))
        .Include(u => u.Researcher)
        .ToListAsync();
}

// Service
public async Task<List<UserDTO>> SearchUsersAsync(string query)
{
    var users = await _repository.SearchAsync(query);
    return users.Select(u => MapToDTO(u)).ToList();
}
```

### Pattern 4: Nested Resource Endpoint

**Use Case**: Accessing related entities

**Implementation**:
```csharp
// Controller
[Route("api/researcher/{researcherId}/users")]
public async Task<IActionResult> GetUsersByResearcher(Guid researcherId)
{
    return Ok(await _userService.GetByResearcherIdAsync(researcherId));
}
```

---

## Examples

### Example 1: Simple GET Endpoint

```csharp
/// <summary>
/// Get user by ID
/// </summary>
[Route("{id}")]
[HttpGet]
[PrismEncryptedChannelConnection]
[PrismAuthenticatedSession]
public async Task<IActionResult> GetById(Guid id)
{
    try
    {
        var user = await _userService.GetByIdAsync(id);

        if (user == null)
        {
            return NotFound(CreateError(
                "ERR_USER_NOT_FOUND",
                "User not found",
                new Dictionary<string, object> { ["userId"] = id }
            ));
        }

        return Ok(user);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to retrieve user {UserId}", id);
        return StatusCode(500, CreateError(
            "ERR_GET_USER_FAILED",
            "Failed to retrieve user",
            retryable: true
        ));
    }
}
```

### Example 2: POST Endpoint with Validation

```csharp
/// <summary>
/// Create a new researcher
/// </summary>
[Route("[action]")]
[HttpPost]
[PrismEncryptedChannelConnection<AddResearcherPayload>]
[PrismAuthenticatedSession]
[Authorize("sub")]
public async Task<IActionResult> NewResearcher()
{
    try
    {
        var payload = HttpContext.Items["DecryptedRequest"] as AddResearcherPayload;

        // Service handles validation
        var researcher = await _researcherService.AddAsync(payload);

        return CreatedAtAction(
            nameof(GetById),
            new { id = researcher.ResearcherId },
            researcher
        );
    }
    catch (ArgumentException ex)
    {
        return BadRequest(CreateError(
            "ERR_INVALID_PAYLOAD",
            ex.Message,
            retryable: false
        ));
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to create researcher");
        return StatusCode(500, CreateError(
            "ERR_CREATE_RESEARCHER_FAILED",
            "Failed to create researcher",
            retryable: true
        ));
    }
}
```

### Example 3: Paginated GET Endpoint

```csharp
/// <summary>
/// Get paginated list of researchers
/// </summary>
/// <param name="page">Page number (default: 1)</param>
/// <param name="pageSize">Items per page (default: 10, max: 100)</param>
[Route("[action]")]
[HttpGet]
[PrismEncryptedChannelConnection]
[PrismAuthenticatedSession]
public async Task<IActionResult> GetResearchers()
{
    try
    {
        // BaseController.ServiceInvoke handles pagination automatically
        return ServiceInvoke(_researcherService.GetAllResearchersPaginateAsync).Result;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to retrieve paginated researchers");
        return StatusCode(500, CreateError(
            "ERR_GET_RESEARCHERS_FAILED",
            "Failed to retrieve researchers",
            retryable: true
        ));
    }
}
```

---

## Quick Reference Checklist

### New Entity Implementation

- [ ] **Domain**: Create Entity, DTO, Payload classes
- [ ] **Data**: Create Repository interface and implementation
- [ ] **Data**: Override `GetPagedAsync()` if pagination needed
- [ ] **Data**: Add `.Include()` for related entities
- [ ] **Service**: Create Service interface and implementation
- [ ] **Service**: Implement validation logic
- [ ] **Service**: Map entities to DTOs
- [ ] **API**: Create Controller with endpoints
- [ ] **API**: Add appropriate middleware attributes
- [ ] **API**: Handle exceptions and logging
- [ ] **Startup**: Register repository and service in DI
- [ ] **Testing**: Write unit tests for service
- [ ] **Testing**: Write integration tests for endpoints
- [ ] **Documentation**: Update API documentation

### Middleware Checklist

- [ ] `[PrismEncryptedChannelConnection]` for GET endpoints
- [ ] `[PrismEncryptedChannelConnection<TPayload>]` for POST/PUT/DELETE
- [ ] `[PrismAuthenticatedSession]` for protected endpoints
- [ ] `[Authorize("capability")]` for capability-based access

### Pagination Checklist

- [ ] Repository overrides `GetPagedAsync()`
- [ ] Repository uses `_apiContext.PagingContext`
- [ ] Repository validates page/pageSize
- [ ] Repository updates `ResponsePaging`
- [ ] Service maps entities to DTOs
- [ ] Controller uses `ServiceInvoke()` helper
- [ ] Controller documents query parameters

---

## Related Documentation

- **Pagination System**: `PAGINATION_SYSTEM.md`
- **Middleware Patterns**: `MIDDLEWARE_PATTERNS.md`
- **Security Overview**: `../SECURITY_OVERVIEW.md`
- **Generic Base Pattern**: `../architecture/GENERIC_BASE_PATTERN.md`
- **Recent Implementations**: `RECENT_IMPLEMENTATIONS.md`

---

**Next Steps**: See `RECENT_IMPLEMENTATIONS.md` for real-world examples of recent endpoint implementations.
