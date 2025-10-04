using Bioteca.Prism.Core.Middleware.Node;
using Bioteca.Prism.Core.Security.Certificate;
using Bioteca.Prism.Domain.Enumerators.Node;
using Bioteca.Prism.Domain.Requests.Node;
using FluentAssertions;

namespace Bioteca.Prism.InteroperableResearchNode.Test;

/// <summary>
/// Tests for NodeChannelClient - simulates the client-side operations
/// Equivalent to the PowerShell scripts that use the client to connect to nodes
/// </summary>
public class NodeChannelClientTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly INodeChannelClient _channelClient;

    public NodeChannelClientTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _channelClient = factory.Services.GetRequiredService<INodeChannelClient>();
    }

    #region Channel Initiation Tests

    [Fact]
    public async Task InitiateChannel_WithValidRemoteUrl_EstablishesChannel()
    {
        // Arrange
        using var remoteFactory = TestWebApplicationFactory.Create("RemoteNode");
        var remoteClient = remoteFactory.CreateClient();
        var remoteUrl = remoteClient.BaseAddress!.ToString().TrimEnd('/');

        // Register the remote factory so HttpClient can route to it
        TestWebApplicationFactory.RegisterRemoteFactory(remoteUrl, remoteFactory);

        try
        {
            // Act
            var result = await _channelClient.OpenChannelAsync(remoteUrl);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.ChannelId.Should().NotBeNullOrEmpty();
            result.SelectedCipher.Should().NotBeNullOrEmpty();
            result.SymmetricKey.Should().NotBeNullOrEmpty();
        }
        finally
        {
            TestWebApplicationFactory.ClearRemoteFactories();
        }
    }

    [Fact]
    public async Task InitiateChannel_WithInvalidUrl_ReturnsFailure()
    {
        // Arrange
        var invalidUrl = "http://non-existent-server:9999";

        // Act
        var result = await _channelClient.OpenChannelAsync(invalidUrl);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNull();
    }

    #endregion

    #region Node Registration Tests

    [Fact]
    public async Task RegisterNode_AfterChannelEstablished_SuccessfullyRegisters()
    {
        // Arrange - Establish channel first
        using var remoteFactory = TestWebApplicationFactory.Create("RemoteNode");
        var remoteClient = remoteFactory.CreateClient();
        var remoteUrl = remoteClient.BaseAddress!.ToString().TrimEnd('/');

        // Register the remote factory
        TestWebApplicationFactory.RegisterRemoteFactory(remoteUrl, remoteFactory);

        try
        {
            var channelResult = await _channelClient.OpenChannelAsync(remoteUrl);

            // Generate certificate
            var certificate = CertificateHelper.GenerateSelfSignedCertificate(
                "client-test-node-001",
                1);

            var certBase64 = CertificateHelper.ExportCertificateToBase64(certificate);

            var registrationRequest = new NodeRegistrationRequest
            {
                NodeId = "client-test-node-001",
                NodeName = "Client Test Node",
                Certificate = certBase64,
                ContactInfo = "admin@clienttest.test",
                InstitutionDetails = "Client Test Institution",
                NodeUrl = "http://clienttest:8080",
                RequestedNodeAccessLevel = NodeAccessTypeEnum.ReadWrite
            };

            // Act
            var result = await _channelClient.RegisterNodeAsync(
                channelResult.ChannelId,
                registrationRequest);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.RegistrationId.Should().NotBeNullOrEmpty();
        }
        finally
        {
            TestWebApplicationFactory.ClearRemoteFactories();
        }
    }

    #endregion

    #region Node Identification Tests

    [Fact]
    public async Task IdentifyNode_UnknownNode_ReturnsNotKnown()
    {
        // Arrange - Establish channel
        using var remoteFactory = TestWebApplicationFactory.Create("RemoteNode");
        var remoteClient = remoteFactory.CreateClient();
        var remoteUrl = remoteClient.BaseAddress!.ToString().TrimEnd('/');

        // Register the remote factory
        TestWebApplicationFactory.RegisterRemoteFactory(remoteUrl, remoteFactory);

        try
        {
            var channelResult = await _channelClient.OpenChannelAsync(remoteUrl);

            // Generate certificate and sign data
            var certificate = CertificateHelper.GenerateSelfSignedCertificate(
                "unknown-client-node",
                1);

            var certBase64 = CertificateHelper.ExportCertificateToBase64(certificate);

            var timestamp = DateTime.UtcNow;
            var signedData = $"{channelResult.ChannelId}unknown-client-node{timestamp:O}";
            var signature = CertificateHelper.SignData(signedData, certificate);

            var identifyRequest = new NodeIdentifyRequest
            {
                NodeId = "unknown-client-node",
                Certificate = certBase64,
                Signature = signature,
                Timestamp = timestamp
            };

            // Act
            var result = await _channelClient.IdentifyNodeAsync(
                channelResult.ChannelId,
                identifyRequest);

            // Assert
            result.Should().NotBeNull();
            result.IsKnown.Should().BeFalse();
            result.Status.Should().Be(Domain.Responses.Node.AuthorizationStatus.Unknown);
        }
        finally
        {
            TestWebApplicationFactory.ClearRemoteFactories();
        }
    }

    [Fact]
    public async Task IdentifyNode_AfterRegistration_ReturnsPending()
    {
        // Arrange - Establish channel and register
        using var remoteFactory = TestWebApplicationFactory.Create("RemoteNode");
        var remoteClient = remoteFactory.CreateClient();
        var remoteUrl = remoteClient.BaseAddress!.ToString().TrimEnd('/');

        // Register the remote factory
        TestWebApplicationFactory.RegisterRemoteFactory(remoteUrl, remoteFactory);

        try
        {
            var channelResult = await _channelClient.OpenChannelAsync(remoteUrl);

            var certificate = CertificateHelper.GenerateSelfSignedCertificate(
                "pending-client-node",
                1);

            var certBase64 = CertificateHelper.ExportCertificateToBase64(certificate);

            // Register first
            var registrationRequest = new NodeRegistrationRequest
            {
                NodeId = "pending-client-node",
                NodeName = "Pending Client Node",
                Certificate = certBase64,
                ContactInfo = "admin@pending.test",
                InstitutionDetails = "Pending Test Institution",
                NodeUrl = "http://pending:8080",
                RequestedNodeAccessLevel = NodeAccessTypeEnum.ReadWrite
            };

            await _channelClient.RegisterNodeAsync(
                channelResult.ChannelId,
                registrationRequest);

            // Generate signature for identification
            var timestamp = DateTime.UtcNow;
            var signedData = $"{channelResult.ChannelId}pending-client-node{timestamp:O}";
            var signature = CertificateHelper.SignData(signedData, certificate);

            var identifyRequest = new NodeIdentifyRequest
            {
                NodeId = "pending-client-node",
                Certificate = certBase64,
                Signature = signature,
                Timestamp = timestamp
            };

            // Act
            var result = await _channelClient.IdentifyNodeAsync(
                channelResult.ChannelId,
                identifyRequest);

            // Assert
            result.Should().NotBeNull();
            result.IsKnown.Should().BeTrue();
            result.Status.Should().Be(Domain.Responses.Node.AuthorizationStatus.Pending);
        }
        finally
        {
            TestWebApplicationFactory.ClearRemoteFactories();
        }
    }

    #endregion

    #region Full Workflow Tests

    [Fact]
    public async Task FullWorkflow_InitiateRegisterIdentify_WorksEndToEnd()
    {
        // This test simulates the complete PowerShell script workflow:
        // 1. Node A initiates channel with Node B
        // 2. Node A registers with Node B
        // 3. Node A identifies itself (should be Pending)
        // 4. Admin approves Node A (simulated via direct API call)
        // 5. Node A identifies again (should be Authorized)

        // Arrange
        using var nodeB = TestWebApplicationFactory.Create("NodeB");
        var nodeBClient = nodeB.CreateClient();
        var nodeBUrl = nodeBClient.BaseAddress!.ToString().TrimEnd('/');

        // Register the remote factory
        TestWebApplicationFactory.RegisterRemoteFactory(nodeBUrl, nodeB);

        try
        {
            var nodeId = "full-workflow-node";

        // Step 1: Initiate channel
        var channelResult = await _channelClient.OpenChannelAsync(nodeBUrl);
        channelResult.Success.Should().BeTrue();

        // Step 2: Generate certificate
        var certificate = CertificateHelper.GenerateSelfSignedCertificate(
            nodeId,
            1);

        var certBase64 = CertificateHelper.ExportCertificateToBase64(certificate);

        // Step 3: Register
        var registrationRequest = new NodeRegistrationRequest
        {
            NodeId = nodeId,
            NodeName = "Full Workflow Test Node",
            Certificate = certBase64,
            ContactInfo = "admin@fullworkflow.test",
            InstitutionDetails = "Full Workflow Institution",
            NodeUrl = "http://fullworkflow:8080",
            RequestedNodeAccessLevel = NodeAccessTypeEnum.ReadWrite
        };

        var regResult = await _channelClient.RegisterNodeAsync(
            channelResult.ChannelId,
            registrationRequest);

        regResult.Success.Should().BeTrue();
        regResult.Status.Should().Be(Domain.Responses.Node.AuthorizationStatus.Pending);

        // Step 4: Identify (should be Pending)
        var timestamp1 = DateTime.UtcNow;
        var signedData1 = $"{channelResult.ChannelId}{nodeId}{timestamp1:O}";
        var signature1 = CertificateHelper.SignData(signedData1, certificate);

        var identifyRequest1 = new NodeIdentifyRequest
        {
            NodeId = nodeId,
            Certificate = certBase64,
            Signature = signature1,
            Timestamp = timestamp1
        };

        var identifyResult1 = await _channelClient.IdentifyNodeAsync(
            channelResult.ChannelId,
            identifyRequest1);

        identifyResult1.IsKnown.Should().BeTrue();
        identifyResult1.Status.Should().Be(Domain.Responses.Node.AuthorizationStatus.Pending);

        // Step 5: Approve node (admin operation)
        var updateRequest = new
        {
            status = 1 // Authorized
        };

        var approveResponse = await nodeBClient.PutAsJsonAsync($"/api/node/{nodeId}/status", updateRequest);
        approveResponse.Should().NotBeNull();
        approveResponse.IsSuccessStatusCode.Should().BeTrue();

        // Step 6: Identify again (should be Authorized)
        var timestamp2 = DateTime.UtcNow;
        var signedData2 = $"{channelResult.ChannelId}{nodeId}{timestamp2:O}";
        var signature2 = CertificateHelper.SignData(signedData2, certificate);

        var identifyRequest2 = new NodeIdentifyRequest
        {
            NodeId = nodeId,
            Certificate = certBase64,
            Signature = signature2,
            Timestamp = timestamp2
        };

        var identifyResult2 = await _channelClient.IdentifyNodeAsync(
            channelResult.ChannelId,
            identifyRequest2);

            // Assert final state
            identifyResult2.IsKnown.Should().BeTrue();
            identifyResult2.Status.Should().Be(Domain.Responses.Node.AuthorizationStatus.Authorized);
            identifyResult2.NextPhase.Should().Be("phase3_authenticate");
        }
        finally
        {
            TestWebApplicationFactory.ClearRemoteFactories();
        }
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task RegisterNode_WithInvalidChannelId_ThrowsException()
    {
        // Arrange
        using var remoteFactory = TestWebApplicationFactory.Create("RemoteNode");
        var remoteClient = remoteFactory.CreateClient();
        var remoteUrl = remoteClient.BaseAddress!.ToString().TrimEnd('/');

        var fakeKey = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);

        var registrationRequest = new NodeRegistrationRequest
        {
            NodeId = "test-node",
            NodeName = "Test Node",
            Certificate = "test-cert",
            ContactInfo = "admin@test.test",
            InstitutionDetails = "Test Institution",
            NodeUrl = "http://test:8080",
            RequestedNodeAccessLevel = NodeAccessTypeEnum.ReadOnly
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            await _channelClient.RegisterNodeAsync(
                "invalid-channel-id",
                registrationRequest);
        });

        Array.Clear(fakeKey, 0, fakeKey.Length);
    }

    [Fact]
    public async Task IdentifyNode_WithInvalidSignature_ReturnsError()
    {
        // Arrange - Establish channel
        using var remoteFactory = TestWebApplicationFactory.Create("RemoteNode");
        var remoteClient = remoteFactory.CreateClient();
        var remoteUrl = remoteClient.BaseAddress!.ToString().TrimEnd('/');

        // Register the remote factory
        TestWebApplicationFactory.RegisterRemoteFactory(remoteUrl, remoteFactory);

        try
        {
            var channelResult = await _channelClient.OpenChannelAsync(remoteUrl);

            var identifyRequest = new NodeIdentifyRequest
            {
                NodeId = "invalid-sig-node",
                Certificate = "dummy-cert",
                Signature = "invalid-signature"
            };

            // Act & Assert
            // The identify should throw an exception due to invalid signature
            await Assert.ThrowsAsync<Exception>(async () =>
            {
                await _channelClient.IdentifyNodeAsync(
                    channelResult.ChannelId,
                    identifyRequest);
            });
        }
        finally
        {
            TestWebApplicationFactory.ClearRemoteFactories();
        }
    }

    #endregion
}

