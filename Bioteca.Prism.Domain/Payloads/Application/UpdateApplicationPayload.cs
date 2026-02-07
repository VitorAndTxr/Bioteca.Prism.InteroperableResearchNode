namespace Bioteca.Prism.Domain.Payloads.Application;

public class UpdateApplicationPayload
{
    public string? AppName { get; set; }
    public string? Url { get; set; }
    public string? Description { get; set; }
    public string? AdditionalInfo { get; set; }
}
