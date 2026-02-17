using Azure.Storage.Blobs;
using Bioteca.Prism.Data.Interfaces.Record;
using Bioteca.Prism.Domain.Payloads.Record;
using Bioteca.Prism.Service.Interfaces.Record;
using Microsoft.Extensions.Configuration;

namespace Bioteca.Prism.Service.Services.Record;

/// <summary>
/// Service implementation for file upload operations using Azure Blob Storage
/// </summary>
public class FileUploadService : IFileUploadService
{
    private readonly IRecordChannelRepository _recordChannelRepository;
    private readonly IRecordSessionRepository _recordSessionRepository;
    private readonly BlobContainerClient _containerClient;
    private bool _containerInitialized;
    private const long MaxFileSizeBytes = 50 * 1024 * 1024; // 50MB

    public FileUploadService(
        IRecordChannelRepository recordChannelRepository,
        IRecordSessionRepository recordSessionRepository,
        IConfiguration configuration)
    {
        _recordChannelRepository = recordChannelRepository;
        _recordSessionRepository = recordSessionRepository;

        var connectionString = configuration["AzureBlobStorage:ConnectionString"] ?? "UseDevelopmentStorage=true";
        var containerName = configuration["AzureBlobStorage:ContainerName"] ?? "node-a";
        var blobServiceClient = new BlobServiceClient(connectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient(containerName);
    }

    public async Task<string> UploadRecordingAsync(UploadRecordingPayload payload)
    {
        // M5: Lazy async container initialization instead of blocking constructor
        if (!_containerInitialized)
        {
            await _containerClient.CreateIfNotExistsAsync();
            _containerInitialized = true;
        }

        // M4: Validate actual base64 decoded size, not client-provided FileSizeBytes
        var estimatedSize = (long)(payload.FileData.Length * 3.0 / 4.0);
        if (estimatedSize > MaxFileSizeBytes)
            throw new ArgumentException($"File data exceeds maximum allowed size of {MaxFileSizeBytes} bytes (50MB).");

        if (payload.FileSizeBytes > MaxFileSizeBytes)
            throw new ArgumentException($"File size {payload.FileSizeBytes} bytes exceeds maximum allowed size of {MaxFileSizeBytes} bytes (50MB).");

        if (!string.Equals(payload.ContentType, "text/csv", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"Invalid content type '{payload.ContentType}'. Only 'text/csv' is accepted.");

        var session = await _recordSessionRepository.GetByIdAsync(payload.SessionId);
        if (session == null)
            throw new KeyNotFoundException($"Session with ID {payload.SessionId} not found.");

        // M1: Use nullable ResearchId — null means unlinked session
        var researchFolder = session.ResearchId.HasValue
            ? session.ResearchId.Value.ToString()
            : "unlinked";

        var blobPath = $"{researchFolder}/{payload.SessionId}/{payload.RecordingId}.csv";

        var fileBytes = Convert.FromBase64String(payload.FileData);
        using var stream = new MemoryStream(fileBytes);

        var blobClient = _containerClient.GetBlobClient(blobPath);
        await blobClient.UploadAsync(stream, overwrite: true);

        var fileUrl = blobClient.Uri.ToString();

        // Update RecordChannel with the file URL
        var channels = await _recordChannelRepository.GetByRecordIdAsync(payload.RecordingId);
        foreach (var channel in channels)
        {
            channel.FileUrl = fileUrl;
            await _recordChannelRepository.UpdateAsync(channel);
        }

        return fileUrl;
    }
}
