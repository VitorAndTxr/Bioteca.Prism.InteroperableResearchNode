namespace Bioteca.Prism.Domain.Payloads.Record;

public class CreateRecordingPayload
{
    public Guid Id { get; set; }
    public string SignalType { get; set; } = string.Empty;
    public float SamplingRate { get; set; }
    public int SamplesCount { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public DateTime CollectionDate { get; set; }
    public Guid? SensorId { get; set; }
}
