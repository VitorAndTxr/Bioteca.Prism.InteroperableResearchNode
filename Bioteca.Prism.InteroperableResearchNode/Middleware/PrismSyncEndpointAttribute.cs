namespace Bioteca.Prism.InteroperableResearchNode.Middleware;

/// <summary>
/// Marker attribute for sync endpoints.
/// When present on a controller or action, PrismAuthenticatedSessionAttribute
/// elevates the rate limit from 60 req/min to 600 req/min, enabling paginated
/// sync operations to complete without exceeding the standard node session rate limit.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class PrismSyncEndpointAttribute : Attribute
{
}
