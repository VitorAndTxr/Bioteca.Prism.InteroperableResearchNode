using Bioteca.Prism.Domain.Payloads.Record;

namespace Bioteca.Prism.Service.Interfaces.Record;

/// <summary>
/// Service interface for file upload operations
/// </summary>
public interface IFileUploadService
{
    Task<string> UploadRecordingAsync(UploadRecordingPayload payload);
}
