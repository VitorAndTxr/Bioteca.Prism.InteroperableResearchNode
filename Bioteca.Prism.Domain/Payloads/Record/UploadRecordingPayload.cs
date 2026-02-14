namespace Bioteca.Prism.Domain.Payloads.Record;

public class UploadRecordingPayload
{
    public Guid RecordingId { get; set; }
    public Guid SessionId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string FileData { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
}
