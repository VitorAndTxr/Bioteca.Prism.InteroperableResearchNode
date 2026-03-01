namespace Bioteca.Prism.Service.Interfaces.Research;

public interface IResearchExportService
{
    Task<ResearchExportResult> ExportAsync(Guid researchId, CancellationToken cancellationToken = default);
}

public class ResearchExportResult
{
    public MemoryStream ZipStream { get; set; } = null!;
    public string FileName { get; set; } = string.Empty;
}
