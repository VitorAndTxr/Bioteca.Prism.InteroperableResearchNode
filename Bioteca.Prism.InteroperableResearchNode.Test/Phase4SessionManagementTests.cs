using Bioteca.Prism.Core.Middleware.Session;
using Bioteca.Prism.Domain.Entities.Session;
using Bioteca.Prism.Domain.Enumerators.Node;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bioteca.Prism.InteroperableResearchNode.Test;

/// <summary>
/// Unit tests for Phase 4: Session Management
/// Tests SessionService and session lifecycle operations
///
/// Note: Integration tests for encrypted endpoints would require:
/// 1. Full channel establishment (Phase 1)
/// 2. Node registration and approval (Phase 2)
/// 3. Challenge-response authentication (Phase 3)
/// 4. Proper AES-256-GCM encryption of all requests/responses
///
/// These complex integration tests are better validated via test-phase4.sh script
/// </summary>
public class Phase4SessionManagementTests
{
    private readonly ISessionService _sessionService;

    public Phase4SessionManagementTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ISessionService, Bioteca.Prism.Service.Services.Session.SessionService>();

        var serviceProvider = services.BuildServiceProvider();
        _sessionService = serviceProvider.GetRequiredService<ISessionService>();
    }

    [Fact]
    public async Task CreateSession_WithValidParameters_ReturnsSessionData()
    {
        // Arrange
        var nodeId = "test-node-001";
        var channelId = "test-channel-001";
        var accessLevel = NodeAccessTypeEnum.ReadWrite;

        // Act
        var session = await _sessionService.CreateSessionAsync(nodeId, channelId, accessLevel);

        // Assert
        session.Should().NotBeNull();
        session.SessionToken.Should().NotBeNullOrEmpty();
        session.NodeId.Should().Be(nodeId);
        session.ChannelId.Should().Be(channelId);
        session.AccessLevel.Should().Be(accessLevel);
        session.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        session.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        session.RequestCount.Should().Be(0);
        session.IsValid().Should().BeTrue();
    }

    [Fact]
    public async Task ValidateSession_WithValidToken_ReturnsSessionContext()
    {
        // Arrange
        var session = await _sessionService.CreateSessionAsync(
            "test-node-002",
            "test-channel-002",
            NodeAccessTypeEnum.Admin);

        // Act
        var context = await _sessionService.ValidateSessionAsync(session.SessionToken);

        // Assert
        context.Should().NotBeNull();
        context!.SessionToken.Should().Be(session.SessionToken);
        context.NodeId.Should().Be("test-node-002");
        context.ChannelId.Should().Be("test-channel-002");
        context.NodeAccessLevel.Should().Be(NodeAccessTypeEnum.Admin);
        context.GetRemainingSeconds().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ValidateSession_WithInvalidToken_ReturnsNull()
    {
        // Arrange
        var invalidToken = "invalid-token-12345";

        // Act
        var context = await _sessionService.ValidateSessionAsync(invalidToken);

        // Assert
        context.Should().BeNull();
    }

    [Fact]
    public async Task RenewSession_WithValidToken_ExtendsExpiration()
    {
        // Arrange
        var session = await _sessionService.CreateSessionAsync(
            "test-node-003",
            "test-channel-003",
            NodeAccessTypeEnum.ReadOnly);

        var originalExpiration = session.ExpiresAt;
        await Task.Delay(100); // Small delay to ensure time difference

        // Act
        var renewedSession = await _sessionService.RenewSessionAsync(session.SessionToken, 1800);

        // Assert
        renewedSession.Should().NotBeNull();
        renewedSession!.ExpiresAt.Should().BeAfter(originalExpiration);
        renewedSession.GetRemainingSeconds().Should().BeGreaterThan(1700);
    }

    [Fact]
    public async Task RenewSession_WithInvalidToken_ReturnsNull()
    {
        // Arrange
        var invalidToken = "invalid-token-12345";

        // Act
        var result = await _sessionService.RenewSessionAsync(invalidToken);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RevokeSession_WithValidToken_InvalidatesSession()
    {
        // Arrange
        var session = await _sessionService.CreateSessionAsync(
            "test-node-004",
            "test-channel-004",
            NodeAccessTypeEnum.ReadWrite);

        // Act
        var revoked = await _sessionService.RevokeSessionAsync(session.SessionToken);

        // Assert
        revoked.Should().BeTrue();

        // Verify session is no longer valid
        var context = await _sessionService.ValidateSessionAsync(session.SessionToken);
        context.Should().BeNull();
    }

    [Fact]
    public async Task RevokeSession_WithInvalidToken_ReturnsFalse()
    {
        // Arrange
        var invalidToken = "invalid-token-12345";

        // Act
        var revoked = await _sessionService.RevokeSessionAsync(invalidToken);

        // Assert
        revoked.Should().BeFalse();
    }

    [Fact]
    public async Task GetNodeSessions_ReturnsAllActiveSessionsForNode()
    {
        // Arrange
        var nodeId = "test-node-005";
        await _sessionService.CreateSessionAsync(nodeId, "channel-1", NodeAccessTypeEnum.ReadOnly);
        await _sessionService.CreateSessionAsync(nodeId, "channel-2", NodeAccessTypeEnum.ReadWrite);
        await _sessionService.CreateSessionAsync("other-node", "channel-3", NodeAccessTypeEnum.Admin);

        // Act
        var sessions = await _sessionService.GetNodeSessionsAsync(nodeId);

        // Assert
        sessions.Should().HaveCount(2);
        sessions.Should().AllSatisfy(s => s.NodeId.Should().Be(nodeId));
    }

    [Fact]
    public async Task GetSessionMetrics_ReturnsCorrectMetrics()
    {
        // Arrange
        var nodeId = "test-node-006";
        var session1 = await _sessionService.CreateSessionAsync(nodeId, "channel-1", NodeAccessTypeEnum.ReadWrite);
        var session2 = await _sessionService.CreateSessionAsync(nodeId, "channel-2", NodeAccessTypeEnum.ReadWrite);

        // Record some requests
        await _sessionService.RecordRequestAsync(session1.SessionToken);
        await _sessionService.RecordRequestAsync(session1.SessionToken);
        await _sessionService.RecordRequestAsync(session2.SessionToken);

        // Act
        var metrics = await _sessionService.GetSessionMetricsAsync(nodeId);

        // Assert
        metrics.Should().NotBeNull();
        metrics.NodeId.Should().Be(nodeId);
        metrics.ActiveSessions.Should().Be(2);
        metrics.TotalRequests.Should().Be(3);
        metrics.LastAccessedAt.Should().NotBeNull();
        metrics.NodeAccessLevel.Should().Be(NodeAccessTypeEnum.ReadWrite);
    }

    [Fact]
    public async Task CleanupExpiredSessions_RemovesExpiredSessions()
    {
        // Arrange
        var nodeId = "test-node-007";

        // Create session with 1 second TTL
        var shortSession = await _sessionService.CreateSessionAsync(
            nodeId,
            "channel-expired",
            NodeAccessTypeEnum.ReadOnly,
            ttlSeconds: 1);

        // Create normal session
        var normalSession = await _sessionService.CreateSessionAsync(
            nodeId,
            "channel-normal",
            NodeAccessTypeEnum.ReadOnly);

        // Wait for short session to expire
        await Task.Delay(1100);

        // Act
        var cleanedCount = await _sessionService.CleanupExpiredSessionsAsync();

        // Assert
        cleanedCount.Should().BeGreaterThan(0);

        // Verify expired session is gone
        var expiredContext = await _sessionService.ValidateSessionAsync(shortSession.SessionToken);
        expiredContext.Should().BeNull();

        // Verify normal session still exists
        var normalContext = await _sessionService.ValidateSessionAsync(normalSession.SessionToken);
        normalContext.Should().NotBeNull();
    }

    [Fact]
    public async Task RecordRequest_EnforcesRateLimiting()
    {
        // Arrange
        var session = await _sessionService.CreateSessionAsync(
            "test-node-008",
            "test-channel-008",
            NodeAccessTypeEnum.ReadWrite);

        // Act - Make 61 requests rapidly (limit is 60/minute)
        var results = new List<bool>();
        for (int i = 0; i < 61; i++)
        {
            var allowed = await _sessionService.RecordRequestAsync(session.SessionToken);
            results.Add(allowed);
        }

        // Assert
        var allowedCount = results.Count(r => r);
        var deniedCount = results.Count(r => !r);

        allowedCount.Should().Be(60); // First 60 should be allowed
        deniedCount.Should().Be(1);   // 61st should be denied
    }

    [Fact]
    public async Task SessionContext_HasCapability_WorksCorrectly()
    {
        // Arrange
        var session = await _sessionService.CreateSessionAsync(
            "test-node-009",
            "test-channel-009",
            NodeAccessTypeEnum.ReadWrite);

        var context = await _sessionService.ValidateSessionAsync(session.SessionToken);

        // Assert
        context.Should().NotBeNull();
        context!.HasCapability(NodeAccessTypeEnum.ReadWrite).Should().BeTrue();
        context.HasCapability(NodeAccessTypeEnum.ReadOnly).Should().BeFalse();
        context.HasCapability(NodeAccessTypeEnum.Admin).Should().BeFalse();
    }

    [Fact]
    public void SessionData_IsValid_ReturnsCorrectStatus()
    {
        // Arrange
        var validSession = new SessionData
        {
            SessionToken = "token-1",
            NodeId = "node-1",
            ChannelId = "channel-1",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            AccessLevel = NodeAccessTypeEnum.ReadOnly,
            RequestCount = 0
        };

        var expiredSession = new SessionData
        {
            SessionToken = "token-2",
            NodeId = "node-2",
            ChannelId = "channel-2",
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            ExpiresAt = DateTime.UtcNow.AddHours(-1),
            AccessLevel = NodeAccessTypeEnum.ReadOnly,
            RequestCount = 0
        };

        // Assert
        validSession.IsValid().Should().BeTrue();
        expiredSession.IsValid().Should().BeFalse();
    }

    [Fact]
    public void SessionData_GetRemainingSeconds_ReturnsCorrectValue()
    {
        // Arrange
        var session = new SessionData
        {
            SessionToken = "token-1",
            NodeId = "node-1",
            ChannelId = "channel-1",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            AccessLevel = NodeAccessTypeEnum.ReadOnly,
            RequestCount = 0
        };

        // Act
        var remaining = session.GetRemainingSeconds();

        // Assert
        remaining.Should().BeGreaterThan(590); // ~10 minutes
        remaining.Should().BeLessThan(610);
    }
}
