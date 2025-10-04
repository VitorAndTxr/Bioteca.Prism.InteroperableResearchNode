using Bioteca.Prism.Core.Middleware.Session;
using Bioteca.Prism.Domain.Enumerators.Node;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Reflection;

namespace Bioteca.Prism.InteroperableResearchNode.Middleware;

/// <summary>
/// Action filter that validates session tokens and enforces capability-based authorization
/// Expects the session token to be in the decrypted request (HttpContext.Items["DecryptedRequest"])
/// Must be used AFTER PrismEncryptedChannelConnection attribute
/// Usage: [PrismAuthenticatedSession(RequiredCapability = "query:read")]
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class PrismAuthenticatedSessionAttribute : Attribute, IAsyncActionFilter
{
    /// <summary>
    /// Optional capability required to access this endpoint
    /// Examples: NodeAccessTypeEnum.ReadOnly, NodeAccessTypeEnum.ReadWrite, NodeAccessTypeEnum.Admin
    /// If null, only validates session existence
    /// </summary>
    public NodeAccessTypeEnum RequiredCapability { get; set; } = NodeAccessTypeEnum.ReadOnly;

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILogger<PrismAuthenticatedSessionAttribute>>();
        var sessionService = context.HttpContext.RequestServices
            .GetRequiredService<ISessionService>();

        // 1. Extract session token from decrypted request
        var decryptedRequest = context.HttpContext.Items["DecryptedRequest"];

        if (decryptedRequest == null)
        {
            logger.LogWarning("No decrypted request found - PrismEncryptedChannelConnection must run first");
            context.Result = new UnauthorizedObjectResult(new
            {
                error = "ERR_NO_DECRYPTED_REQUEST",
                message = "Request must be encrypted via channel"
            });
            return;
        }

        // Use reflection to get SessionToken property from any request type
        var sessionTokenProperty = decryptedRequest.GetType().GetProperty("SessionToken");
        if (sessionTokenProperty == null)
        {
            logger.LogWarning("Decrypted request does not contain SessionToken property");
            context.Result = new UnauthorizedObjectResult(new
            {
                error = "ERR_NO_SESSION_TOKEN_IN_REQUEST",
                message = "Request must include sessionToken field"
            });
            return;
        }

        var sessionToken = sessionTokenProperty.GetValue(decryptedRequest) as string;

        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            logger.LogWarning("Empty session token in decrypted request");
            context.Result = new UnauthorizedObjectResult(new
            {
                error = "ERR_EMPTY_TOKEN",
                message = "Session token cannot be empty"
            });
            return;
        }

        // 2. Validate session
        var sessionContext = await sessionService.ValidateSessionAsync(sessionToken);

        if (sessionContext == null)
        {
            logger.LogWarning("Invalid or expired session token: {SessionToken}", sessionToken);
            context.Result = new UnauthorizedObjectResult(new
            {
                error = "ERR_INVALID_SESSION",
                message = "Session token is invalid or expired",
                hint = "Re-authenticate using POST /api/node/authenticate"
            });
            return;
        }

        logger.LogDebug(
            "Session validated: {SessionToken}, node {NodeId}, {RemainingSeconds}s remaining",
            sessionToken,
            sessionContext.NodeId,
            sessionContext.GetRemainingSeconds());

        // 3. Check capability if required
        if (RequiredCapability!= null)
        {
            if (sessionContext.NodeAccessLevel < RequiredCapability)
            {
                logger.LogWarning(
                    "Node {NodeId} lacks required capability: {Capability} (has {CurrentLevel})",
                    sessionContext.NodeId,
                    RequiredCapability.ToString(),
                    sessionContext.NodeAccessLevel.ToString());

                context.Result = new ObjectResult(new
                {
                    error = "ERR_INSUFFICIENT_PERMISSIONS",
                    message = $"This endpoint requires capability: {RequiredCapability.ToString()}",
                    grantedCapabilities = sessionContext.NodeAccessLevel
                })
                {
                    StatusCode = 403 // Forbidden
                };
                return;
            }
        }

        // 4. Rate limiting
        var allowed = await sessionService.RecordRequestAsync(sessionToken);
        if (!allowed)
        {
            logger.LogWarning("Rate limit exceeded for session {SessionToken}", sessionToken);
            context.Result = new ObjectResult(new
            {
                error = "ERR_RATE_LIMIT_EXCEEDED",
                message = "Too many requests. Maximum 60 requests per minute.",
                retryAfter = 60
            })
            {
                StatusCode = 429 // Too Many Requests
            };
            return;
        }

        // 5. Store SessionContext in HttpContext for controller access
        context.HttpContext.Items["SessionContext"] = sessionContext;

        // 6. Continue to controller action
        await next();
    }
}
