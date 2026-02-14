namespace Bioteca.Prism.Domain.Payloads.Record;

public class CreateAnnotationPayload
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
