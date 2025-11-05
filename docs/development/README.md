# Development Guides

**Version**: 1.0.0
**Last Updated**: 2025-11-04

This directory contains comprehensive development guides for implementing features in the InteroperableResearchNode.

---

## 📘 Essential Guides

### [API Endpoint Implementation Guide](API_ENDPOINT_IMPLEMENTATION_GUIDE.md) ✅

Complete step-by-step guide for implementing new API endpoints following PRISM architecture patterns.

**Topics Covered**:
- Clean Architecture layers (Domain → Data → Service → API)
- Entity, DTO, and Payload definitions
- Repository and Service implementation
- Controller creation with middleware
- Pagination support
- Security and encryption
- Testing guidelines
- Real-world examples

**When to Use**: Implementing any new API endpoint

---

### [Pagination System](PAGINATION_SYSTEM.md) ✅

Detailed documentation of PRISM's standardized pagination system.

**Topics Covered**:
- Architecture and data flow
- IApiContext and PagingContext components
- Repository pagination implementation
- Service and Controller integration
- Request/Response format
- Best practices and troubleshooting

**When to Use**: Implementing list endpoints with pagination

---

### [Recent Implementations](RECENT_IMPLEMENTATIONS.md) ✅

Documentation of recent implementations with real-world examples.

**Topics Covered**:
- User Management System (complete implementation)
- Pagination System (architecture)
- Middleware Enhancements (dual-version encryption)
- Researcher Management (partial, with known issues)
- Migration guide for existing endpoints

**When to Use**: Looking for real examples of implemented features

---

## 📋 Additional Guides

### [Middleware Patterns](MIDDLEWARE_PATTERNS.md)

Documentation of PRISM middleware patterns and usage.

**Topics Covered**:
- `PrismEncryptedChannelConnection` (generic and non-generic)
- `PrismAuthenticatedSession`
- `Authorize` capability-based authorization
- Middleware stacking and execution order

**When to Use**: Implementing secure endpoints

---

### [Implementation Roadmap](implementation-roadmap.md)

High-level implementation roadmap for Phase 5 and beyond.

**Topics Covered**:
- Phase 5: Federated Queries
- Data Exchange Protocol
- Query Language Design
- Security and Privacy
- Testing Strategy

**When to Use**: Planning major features

---

### [Docker Setup](DOCKER-SETUP.md)

Docker multi-node architecture and setup guide.

**Topics Covered**:
- Docker Compose file structure
- Network configuration
- Container orchestration
- Environment variables
- Troubleshooting

**When to Use**: Setting up development environment

---

### [Debugging Docker](debugging-docker.md)

Troubleshooting guide for Docker-related issues.

**Topics Covered**:
- Network issues
- Container logs
- Database connections
- Redis connectivity

**When to Use**: Troubleshooting Docker issues

---

## 🚀 Quick Start

### New to PRISM Backend Development?

1. **Start Here**: Read `API_ENDPOINT_IMPLEMENTATION_GUIDE.md`
2. **Set Up Environment**: Follow `DOCKER-SETUP.md`
3. **Study Examples**: Review `RECENT_IMPLEMENTATIONS.md`
4. **Implement Feature**: Follow step-by-step guide
5. **Test**: Use `../testing/manual-testing-guide.md`

### Implementing a New Endpoint?

**Step-by-Step Process**:

1. **Read the Guide**: `API_ENDPOINT_IMPLEMENTATION_GUIDE.md`
2. **Check Examples**: `RECENT_IMPLEMENTATIONS.md` (UserController, UserService)
3. **Follow Clean Architecture**:
   - Domain: Entity, DTO, Payload
   - Data: Repository (interface + implementation)
   - Service: Service (interface + implementation)
   - API: Controller
4. **Add Pagination**: `PAGINATION_SYSTEM.md`
5. **Add Security**: `MIDDLEWARE_PATTERNS.md`
6. **Register Services**: `Program.cs` DI container
7. **Test**: Write unit and integration tests

### Need Pagination?

**Quick Reference**:

```csharp
// Repository
public override async Task<List<Entity>> GetPagedAsync()
{
    var page = _apiContext.PagingContext.RequestPaging.Page;
    var pageSize = _apiContext.PagingContext.RequestPaging.PageSize;

    // Validate, query, update ResponsePaging
    // See PAGINATION_SYSTEM.md for details
}

// Service
public async Task<List<EntityDTO>> GetAllPaginateAsync()
{
    var entities = await _repository.GetPagedAsync();
    return entities.Select(e => MapToDTO(e)).ToList();
}

// Controller
[HttpGet]
[PrismEncryptedChannelConnection]
public async Task<IActionResult> GetAll()
{
    return ServiceInvoke(_service.GetAllPaginateAsync).Result;
}
```

**Full Details**: `PAGINATION_SYSTEM.md`

---

## 📚 Related Documentation

### Architecture
- `../ARCHITECTURE_PHILOSOPHY.md` - PRISM architecture and design principles
- `../SECURITY_OVERVIEW.md` - Complete security architecture
- `../architecture/handshake-protocol.md` - 4-phase protocol
- `../architecture/GENERIC_BASE_PATTERN.md` - Repository/service pattern (TODO)

### Workflows
- `../workflows/CHANNEL_FLOW.md` - Phase 1 implementation
- `../workflows/PHASE2_IDENTIFICATION_FLOW.md` - Phase 2 implementation
- `../workflows/PHASE3_AUTHENTICATION_FLOW.md` - Phase 3 implementation
- `../workflows/PHASE4_SESSION_FLOW.md` - Phase 4 implementation

### Components
- `../components/INTEROPERABLE_RESEARCH_NODE.md` - Complete backend reference
- `../PROJECT_STATUS.md` - Implementation status and roadmap

### Testing
- `../testing/manual-testing-guide.md` - Manual testing procedures
- `../testing/redis-testing-guide.md` - Redis persistence testing
- `../testing/docker-compose-quick-start.md` - Docker setup

---

## 🔧 Development Tools

### Required Tools
- .NET 8.0 SDK
- Docker Desktop
- PostgreSQL client (for debugging)
- Redis CLI (for debugging)
- Postman or similar (for API testing)

### Recommended IDE
- Visual Studio 2022 (Windows)
- Visual Studio Code with C# extension (cross-platform)
- JetBrains Rider (cross-platform)

### Helpful Extensions
- C# Dev Kit (VS Code)
- Docker (VS Code)
- REST Client (VS Code)
- GitLens (VS Code)

---

## 📖 Documentation Standards

### Writing New Guides

When creating new development guides:

1. **Use English**: All documentation must be in English
2. **Follow Template**: Use existing guides as templates
3. **Include Examples**: Provide real code examples
4. **Add Navigation**: Update this README and `NAVIGATION_INDEX.md`
5. **Cross-Reference**: Link to related documentation
6. **Version**: Include version and last updated date

### Documentation Format

```markdown
# Guide Title

**Version**: X.Y.Z
**Last Updated**: YYYY-MM-DD
**Status**: Draft | In Review | Production-Ready

Brief description of what this guide covers.

---

## Table of Contents
...

## Overview
...

## Examples
...

## Related Documentation
...
```

---

## 🆘 Getting Help

### Documentation Issues
- Missing guide? Check `../PROJECT_STATUS.md` for planned documentation
- Unclear content? File an issue or submit a pull request
- Broken examples? Report in project issues

### Technical Support
- Architecture questions: See `../ARCHITECTURE_PHILOSOPHY.md`
- Security questions: See `../SECURITY_OVERVIEW.md`
- Testing questions: See `../testing/manual-testing-guide.md`

---

## 🗺️ Navigation

**Parent Directory**: `../` (docs/)
**Project Root**: `../../` (InteroperableResearchNode/)
**Main Index**: `../NAVIGATION_INDEX.md`

---

## 📊 Guide Status

| Guide | Status | Last Updated | Priority |
|-------|--------|--------------|----------|
| API_ENDPOINT_IMPLEMENTATION_GUIDE.md | ✅ Complete | 2025-11-04 | High |
| PAGINATION_SYSTEM.md | ✅ Complete | 2025-11-04 | High |
| RECENT_IMPLEMENTATIONS.md | ✅ Complete | 2025-11-04 | High |
| MIDDLEWARE_PATTERNS.md | ✅ Complete | 2025-10-XX | High |
| DOCKER-SETUP.md | ✅ Complete | 2025-10-XX | Medium |
| debugging-docker.md | ✅ Complete | 2025-10-XX | Medium |
| implementation-roadmap.md | ✅ Complete | 2025-10-XX | Medium |
| COMMON_COMMANDS.md | 📋 Planned | - | Medium |
| PERSISTENCE_LAYER.md | 📋 Planned | - | Medium |
| SERVICE_REGISTRATION.md | 📋 Planned | - | Low |
| CERTIFICATE_MANAGEMENT.md | 📋 Planned | - | Low |

---

**Last Updated**: 2025-11-04
**Maintained By**: PRISM Development Team
