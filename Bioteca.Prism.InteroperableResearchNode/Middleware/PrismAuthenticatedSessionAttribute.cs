using Bioteca.Prism.Core.Middleware.Session;
using Bioteca.Prism.Domain.Enumerators.Node;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Reflection;

namespace Bioteca.Prism.InteroperableResearchNode.Middleware;

/// <summary>
/// Action filter that validates session tokens and enforces capability-based authorization
///
/// Session Token Sources (in order of precedence):
/// 1. X-Session-Id header (RECOMMENDED) - Fast, standard HTTP header pattern
/// 2. SessionToken in decrypted request body (DEPRECATED) - Legacy pattern, will be removed in v0.11.0
///
/// Automatic Behaviors:
/// - Validates session token and enforces capability requirements
/// - Applies rate limiting (60 requests/minute)
/// - Automatically adds X-Session-Id header to successful responses
///
/// Must be used AFTER PrismEncryptedChannelConnection attribute
///
/// Usage: [PrismAuthenticatedSession(RequiredCapability = NodeAccessTypeEnum.ReadOnly)]
///
/// Example Request:
/// POST /api/session/whoami
/// X-Channel-Id: {channelId}
/// X-Session-Id: {sessionToken}
/// Content-Type: application/json
///
/// Example Response:
/// HTTP/1.1 200 OK
/// X-Session-Id: {sessionToken}
/// Content-Type: application/json
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

        // 1. Extract session token from X-Session-Id header (new pattern)
        var sessionToken = context.HttpContext.Request.Headers["X-Session-Id"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            // 2. Fallback to body extraction (deprecated pattern) for backward compatibility
            logger.LogDebug("X-Session-Id header not found, attempting to extract from request body");

            var decryptedRequest = context.HttpContext.Items["DecryptedRequest"];

            if (decryptedRequest == null)
            {
                logger.LogWarning("No session token in header and no decrypted request found");
                context.Result = new UnauthorizedObjectResult(new
                {
                    error = "ERR_NO_SESSION_TOKEN",
                    message = "Session token must be provided via X-Session-Id header",
                    hint = "Add X-Session-Id header with your session token"
                });
                return;
            }

            // Use reflection to get SessionToken property from any request type
            var sessionTokenProperty = decryptedRequest.GetType().GetProperty("SessionToken");
            if (sessionTokenProperty == null)
            {
                logger.LogWarning("No session token in header and decrypted request does not contain SessionToken property");
                context.Result = new UnauthorizedObjectResult(new
                {
                    error = "ERR_NO_SESSION_TOKEN",
                    message = "Session token must be provided via X-Session-Id header",
                    hint = "Add X-Session-Id header with your session token"
                });
                return;
            }

            sessionToken = sessionTokenProperty.GetValue(decryptedRequest) as string;

            if (string.IsNullOrWhiteSpace(sessionToken))
            {
                logger.LogWarning("Session token is empty in both header and request body");
                context.Result = new UnauthorizedObjectResult(new
                {
                    error = "ERR_EMPTY_TOKEN",
                    message = "Session token cannot be empty",
                    hint = "Add X-Session-Id header with your session token"
                });
                return;
            }

            // Log deprecation warning
            logger.LogWarning(
                "Session token extracted from request body. This is deprecated. Use X-Session-Id header instead. " +
                "Body token support will be removed in v0.11.0");
        }
        else
        {
            logger.LogDebug("Session token extracted from X-Session-Id header: {SessionToken}", sessionToken);
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
        // Sync endpoints use 600 req/min (elevated) to allow paginated sync operations.
        // All other endpoints use the standard 60 req/min enforced by SessionService.
        var isSyncEndpoint = context.ActionDescriptor.EndpointMetadata
            .Any(m => m is PrismSyncEndpointAttribute);
        var allowed = await sessionService.RecordRequestAsync(sessionToken, isSyncEndpoint ? 600 : 0);
        if (!allowed)
        {
            var limit = isSyncEndpoint ? 600 : 60;
            logger.LogWarning("Rate limit exceeded for session {SessionToken} (limit: {Limit}/min)", sessionToken, limit);
            context.Result = new ObjectResult(new
            {
                error = "ERR_RATE_LIMIT_EXCEEDED",
                message = $"Too many requests. Maximum {limit} requests per minute.",
                retryAfter = 60
            })
            {
                StatusCode = 429 // Too Many Requests
            };
            return;
        }

        // 5. Store SessionContext in HttpContext for controller access
        context.HttpContext.Items["SessionContext"] = sessionContext;

        // 6. Execute controller action
        var executedContext = await next();

        // 7. Add session token to response header automatically
        // Only add if the action executed successfully (no exception and not an error status)
        if (executedContext.Exception == null)
        {
            var shouldAddHeader = executedContext.Result switch
            {
                UnauthorizedObjectResult => false,
                ObjectResult objResult when objResult.StatusCode >= 400 => false,
                _ => true
            };

            if (shouldAddHeader)
            {
                executedContext.HttpContext.Response.Headers.Append(
                    "X-Session-Id",
                    sessionContext.SessionToken);

                logger.LogDebug("Added X-Session-Id header to response: {SessionToken}", sessionContext.SessionToken);
            }
        }
    }
}
