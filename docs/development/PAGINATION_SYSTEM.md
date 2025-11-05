# Pagination System

**Version**: 1.0.0
**Last Updated**: 2025-11-04
**Status**: Production-Ready

Comprehensive documentation of PRISM's pagination system architecture and implementation.

---

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Components](#components)
4. [Implementation Guide](#implementation-guide)
5. [Request/Response Flow](#requestresponse-flow)
6. [Examples](#examples)
7. [Best Practices](#best-practices)

---

## Overview

PRISM implements a **standardized pagination system** across all list endpoints using a context-based approach. Pagination metadata flows through `IApiContext`, ensuring consistency and reducing boilerplate code.

### Key Features

- ✓ **Automatic Query Parameter Parsing**: `BaseController` extracts `page` and `pageSize`
- ✓ **Context-Based Metadata**: Pagination state stored in `IApiContext.PagingContext`
- ✓ **Consistent Response Format**: All paginated endpoints return same structure
- ✓ **Validation & Limits**: Built-in validation (min: 1, max: 100 items per page)
- ✓ **Total Count Tracking**: Automatic calculation of total pages

---

## Architecture

```
┌─────────────────┐
│  HTTP Request   │  ?page=2&pageSize=20
└────────┬────────┘
         │
         ▼
┌─────────────────────────────────┐
│     BaseController              │
│  - HandleQueryParameters()      │
│  - Sets IApiContext.PagingContext│
└────────┬────────────────────────┘
         │
         ▼
┌─────────────────────────────────┐
│       Service Layer             │
│  - Calls repository methods     │
└────────┬────────────────────────┘
         │
         ▼
┌─────────────────────────────────┐
│      Repository Layer           │
│  - GetPagedAsync()              │
│  - Reads RequestPaging          │
│  - Applies SKIP/TAKE            │
│  - Updates ResponsePaging       │
└────────┬────────────────────────┘
         │
         ▼
┌─────────────────────────────────┐
│     BaseController              │
│  - JsonResponseMessage()        │
│  - Wraps data in PagedResult    │
└────────┬────────────────────────┘
         │
         ▼
┌─────────────────┐
│  HTTP Response  │  { "data": [...], "currentPage": 2, ... }
└─────────────────┘
```

---

## Components

### 1. IApiContext Interface

**Location**: `Bioteca.Prism.Core/Interfaces/IApiContext.cs`

```csharp
public interface IApiContext
{
    /// <summary>
    /// Pagination context for request/response
    /// </summary>
    PagingContext PagingContext { get; }

    // ... other properties
}
```

### 2. PagingContext Class

**Location**: `Bioteca.Prism.Core/Paging/PagingContext.cs`

```csharp
public class PagingContext
{
    /// <summary>
    /// Pagination parameters from client request
    /// </summary>
    public RequestPaging RequestPaging { get; set; } = new RequestPaging();

    /// <summary>
    /// Pagination metadata for response
    /// </summary>
    public ResponsePaging ResponsePaging { get; set; } = new ResponsePaging();

    /// <summary>
    /// Indicates if current request is paginated
    /// </summary>
    public bool IsPaginated => RequestPaging.Page > 0 && RequestPaging.PageSize > 0;
}
```

### 3. RequestPaging Class

```csharp
public class RequestPaging
{
    /// <summary>
    /// Page number (1-indexed)
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; } = 10;
}
```

### 4. ResponsePaging Class

```csharp
public class ResponsePaging
{
    /// <summary>
    /// Current page number
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Items per page
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Total number of records
    /// </summary>
    public int TotalRecords { get; set; }

    /// <summary>
    /// Set pagination values (helper method)
    /// </summary>
    public void SetValues(int page, int pageSize, int totalPages)
    {
        Page = page;
        PageSize = pageSize;
        TotalPages = totalPages;
        TotalRecords = totalPages * pageSize; // Approximation
    }
}
```

### 5. PagedResult<T> DTO

**Location**: `Bioteca.Prism.Domain/DTOs/Paging/PagedResult.cs`

```csharp
public class PagedResult<T>
{
    /// <summary>
    /// Paginated data items
    /// </summary>
    public T Data { get; set; }

    /// <summary>
    /// Current page number
    /// </summary>
    public int CurrentPage { get; set; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of records across all pages
    /// </summary>
    public int TotalRecords { get; set; }
}
```

---

## Implementation Guide

### Step 1: Repository - Override GetPagedAsync()

**File**: `{EntityName}Repository.cs`

```csharp
public override async Task<List<Domain.Entities.User.User>> GetPagedAsync()
{
    // 1. Get pagination parameters from ApiContext
    var page = _apiContext.PagingContext.RequestPaging.Page;
    var pageSize = _apiContext.PagingContext.RequestPaging.PageSize;

    // 2. Validate and normalize pagination parameters
    if (page < 1) page = 1;
    if (pageSize < 1) pageSize = 10;
    if (pageSize > 100) pageSize = 100; // Max page size limit

    // 3. Build query with related entities
    var query = _dbSet
        .Include(u => u.Researcher)
            .ThenInclude(r => r.ResearchResearchers)
        .AsQueryable();

    // 4. Get total count BEFORE pagination
    var totalCount = await query.CountAsync();

    // 5. Apply pagination
    var items = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    // 6. Calculate total pages
    var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

    // 7. Update response paging context
    _apiContext.PagingContext.ResponsePaging.SetValues(page, pageSize, totalPages);

    return items;
}
```

**Key Points**:
- Read from `_apiContext.PagingContext.RequestPaging`
- **Always validate** page and pageSize
- **Count total records BEFORE** applying Skip/Take
- Use `.Include()` to load related entities (avoid N+1 queries)
- Update `ResponsePaging` with metadata

### Step 2: Service - Map to DTOs

**File**: `{EntityName}Service.cs`

```csharp
public async Task<List<UserDTO>> GetAllUserPaginateAsync()
{
    // 1. Call repository (pagination happens here)
    var result = await _userRepository.GetPagedAsync();

    // 2. Map entities to DTOs
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
```

**Key Points**:
- Repository handles pagination logic
- Service handles entity-to-DTO mapping
- Preserve pagination context (no extra work needed)

### Step 3: Controller - Use ServiceInvoke()

**File**: `{EntityName}Controller.cs`

```csharp
/// <summary>
/// Get paginated list of users
/// </summary>
/// <remarks>
/// Query Parameters:
/// - page: Page number (1-indexed, default: 1)
/// - pageSize: Items per page (default: 10, max: 100)
/// </remarks>
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
        // ServiceInvoke handles pagination automatically:
        // 1. Calls HandleQueryParameters() to parse ?page=X&pageSize=Y
        // 2. Invokes service method
        // 3. Wraps result in PagedResult<T> if paginated
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

**Key Points**:
- `ServiceInvoke()` automatically handles pagination
- No manual query parameter parsing needed
- XML documentation describes query parameters
- `Result` property unwraps the `Task<IActionResult>`

---

## Request/Response Flow

### 1. Client Request

```http
GET /api/user/GetUsers?page=2&pageSize=20 HTTP/1.1
Host: localhost:5000
X-Channel-Id: abc123
X-Session-Id: def456
```

### 2. BaseController.HandleQueryParameters()

**Location**: `BaseController.cs:78`

```csharp
private void HandleQueryParameters()
{
    string queryString = Request?.QueryString.ToString();
    if (string.IsNullOrEmpty(queryString))
        return;

    var queryDictionary = QueryHelpers.ParseQuery(queryString);

    foreach (var item in queryDictionary)
    {
        switch (item.Key.ToLowerInvariant())
        {
            case "page":
                if (int.TryParse(item.Value, out int page))
                {
                    if (page <= 0)
                        throw new BadRequestException("Invalid value for 'page'.");
                    _apiContext.PagingContext.RequestPaging.Page = page;
                }
                break;

            case "pagesize":
                if (int.TryParse(item.Value, out int pageSize))
                {
                    if (pageSize <= 0)
                        throw new BadRequestException("Invalid value for 'pageSize'.");
                    _apiContext.PagingContext.RequestPaging.PageSize = pageSize;
                }
                break;
        }
    }
}
```

**Result**: `IApiContext.PagingContext.RequestPaging` now contains:
```csharp
{
    Page = 2,
    PageSize = 20
}
```

### 3. Repository.GetPagedAsync()

**Executed Query** (PostgreSQL):
```sql
-- Count total records
SELECT COUNT(*) FROM "Users";

-- Get paginated results
SELECT u."Id", u."Login", u."Role", u."CreatedAt", u."UpdatedAt",
       r."ResearcherId", r."Name", r."Email", r."Role", r."Orcid"
FROM "Users" u
LEFT JOIN "Researchers" r ON u."ResearcherId" = r."ResearcherId"
ORDER BY u."CreatedAt" DESC
OFFSET 20 ROWS  -- (page - 1) * pageSize = (2 - 1) * 20
FETCH NEXT 20 ROWS ONLY;
```

**Result**: `IApiContext.PagingContext.ResponsePaging` updated:
```csharp
{
    Page = 2,
    PageSize = 20,
    TotalPages = 8,
    TotalRecords = 150
}
```

### 4. BaseController.JsonResponseMessage()

**Location**: `BaseController.cs:125`

```csharp
protected IActionResult JsonResponseMessage<R>(R result)
{
    object resp;

    if (_apiContext.PagingContext.IsPaginated)
    {
        PagedResult<R> apiPaginatedResponse = new PagedResult<R>();
        var contextResponsePaging = _apiContext.PagingContext.ResponsePaging;

        apiPaginatedResponse.Data = result;
        apiPaginatedResponse.PageSize = _apiContext.PagingContext.RequestPaging.PageSize;
        apiPaginatedResponse.CurrentPage = _apiContext.PagingContext.RequestPaging.Page;
        apiPaginatedResponse.TotalRecords = contextResponsePaging.TotalRecords;

        resp = apiPaginatedResponse;
    }
    else
    {
        resp = result;
    }

    return Ok(resp);
}
```

### 5. Server Response

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
    },
    // ... 19 more items
  ],
  "currentPage": 2,
  "pageSize": 20,
  "totalRecords": 150
}
```

---

## Examples

### Example 1: Basic Pagination

**Request**:
```http
GET /api/user/GetUsers?page=1&pageSize=10
```

**Response**:
```json
{
  "data": [ /* 10 users */ ],
  "currentPage": 1,
  "pageSize": 10,
  "totalRecords": 150
}
```

### Example 2: Large Page Size (Clamped to Max)

**Request**:
```http
GET /api/user/GetUsers?page=1&pageSize=500
```

**Repository Validation**:
```csharp
if (pageSize > 100) pageSize = 100; // Clamped to max
```

**Response**:
```json
{
  "data": [ /* 100 users */ ],
  "currentPage": 1,
  "pageSize": 100,  // Clamped from 500
  "totalRecords": 150
}
```

### Example 3: Invalid Parameters

**Request**:
```http
GET /api/user/GetUsers?page=0&pageSize=-10
```

**Controller Response** (400 Bad Request):
```json
{
  "errorDetail": {
    "code": "ERR_INVALID_PARAMETER",
    "message": "Invalid value for 'page'.",
    "retryable": false
  }
}
```

### Example 4: Default Pagination

**Request** (no query parameters):
```http
GET /api/user/GetUsers
```

**Repository Default Values**:
```csharp
var page = _apiContext.PagingContext.RequestPaging.Page;      // Default: 1
var pageSize = _apiContext.PagingContext.RequestPaging.PageSize;  // Default: 10
```

**Response**:
```json
{
  "data": [ /* 10 users */ ],
  "currentPage": 1,
  "pageSize": 10,
  "totalRecords": 150
}
```

---

## Best Practices

### 1. Always Validate Page Size

```csharp
// ✓ CORRECT: Enforce min/max limits
if (pageSize < 1) pageSize = 10;
if (pageSize > 100) pageSize = 100;

// ✗ WRONG: No validation
var pageSize = _apiContext.PagingContext.RequestPaging.PageSize;
```

### 2. Count Total BEFORE Pagination

```csharp
// ✓ CORRECT: Count total records before Skip/Take
var totalCount = await query.CountAsync();
var items = await query.Skip(...).Take(...).ToListAsync();

// ✗ WRONG: Counting after pagination
var items = await query.Skip(...).Take(...).ToListAsync();
var totalCount = items.Count; // Wrong! This is count of current page only
```

### 3. Use Include() for Related Entities

```csharp
// ✓ CORRECT: Load related data upfront (1 query)
var query = _dbSet
    .Include(u => u.Researcher)
        .ThenInclude(r => r.ResearchResearchers)
    .AsQueryable();

// ✗ WRONG: Lazy loading (N+1 queries)
var query = _dbSet.AsQueryable(); // Related entities not loaded
```

### 4. Update ResponsePaging Metadata

```csharp
// ✓ CORRECT: Update pagination metadata
_apiContext.PagingContext.ResponsePaging.SetValues(page, pageSize, totalPages);

// ✗ WRONG: Forget to update metadata
// Client won't know total pages/records
```

### 5. Use ServiceInvoke() in Controllers

```csharp
// ✓ CORRECT: Let BaseController handle pagination
return ServiceInvoke(_userService.GetAllUserPaginateAsync).Result;

// ✗ WRONG: Manual pagination handling
HandleQueryParameters();
var result = await _userService.GetAllUserPaginateAsync();
return Ok(new PagedResult<List<UserDTO>> { Data = result, ... });
```

### 6. Document Query Parameters

```csharp
/// <summary>
/// Get paginated list of users
/// </summary>
/// <remarks>
/// Query Parameters:
/// - page: Page number (1-indexed, default: 1)
/// - pageSize: Items per page (default: 10, max: 100)
/// </remarks>
[HttpGet]
public async Task<IActionResult> GetUsers() { }
```

### 7. Performance Considerations

```csharp
// ✓ CORRECT: Efficient query
var query = _dbSet
    .Include(u => u.Researcher)  // Only load what's needed
    .AsNoTracking()              // Read-only query (faster)
    .AsQueryable();

// ✗ WRONG: Load unnecessary data
var query = _dbSet
    .Include(u => u.Researcher)
        .ThenInclude(r => r.ResearchResearchers)
            .ThenInclude(rr => rr.Research)
                .ThenInclude(r => r.Volunteers)  // Too much data!
```

---

## Troubleshooting

### Issue 1: Pagination Metadata Not Returned

**Symptom**: Response contains data but no pagination metadata

**Cause**: `IsPaginated` is `false`

**Solution**:
```csharp
// Check RequestPaging is set
var page = _apiContext.PagingContext.RequestPaging.Page;
var pageSize = _apiContext.PagingContext.RequestPaging.PageSize;

// Ensure both are > 0
if (page < 1) page = 1;
if (pageSize < 1) pageSize = 10;
```

### Issue 2: Wrong Total Count

**Symptom**: `totalRecords` is always equal to `pageSize`

**Cause**: Counting AFTER pagination

**Solution**:
```csharp
// Count BEFORE Skip/Take
var totalCount = await query.CountAsync();
var items = await query.Skip(...).Take(...).ToListAsync();
```

### Issue 3: N+1 Query Problem

**Symptom**: Slow performance, many database queries

**Cause**: Missing `.Include()` for related entities

**Solution**:
```csharp
// Add Include() for all related entities
var query = _dbSet
    .Include(u => u.Researcher)
        .ThenInclude(r => r.ResearchResearchers)
    .AsQueryable();
```

### Issue 4: Pagination Ignored

**Symptom**: Returns all records regardless of page/pageSize

**Cause**: Not overriding `GetPagedAsync()` in repository

**Solution**:
```csharp
public override async Task<List<Entity>> GetPagedAsync()
{
    // Implement pagination logic
}
```

---

## Related Documentation

- **API Endpoint Implementation Guide**: `API_ENDPOINT_IMPLEMENTATION_GUIDE.md`
- **BaseController Reference**: `../architecture/BASE_CONTROLLER.md`
- **Generic Base Pattern**: `../architecture/GENERIC_BASE_PATTERN.md`
- **Recent Implementations**: `RECENT_IMPLEMENTATIONS.md`

---

**Last Updated**: 2025-11-04
**Maintained By**: PRISM Development Team
