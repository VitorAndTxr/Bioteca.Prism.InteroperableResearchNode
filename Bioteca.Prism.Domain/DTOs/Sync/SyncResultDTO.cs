namespace Bioteca.Prism.Domain.DTOs.Sync;

/// <summary>
/// Response from POST /api/sync/import.
/// </summary>
public class SyncResultDTO
{
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public Dictionary<string, int> EntitiesReceived { get; set; } = new();
    public string? ErrorMessage { get; set; }
}
