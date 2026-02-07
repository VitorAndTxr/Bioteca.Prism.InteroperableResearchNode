namespace Bioteca.Prism.Domain.Payloads.Application;

public class AddApplicationPayload
{
    public string AppName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AdditionalInfo { get; set; } = string.Empty;
}
