using Bioteca.Prism.Core.Controllers;
using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Core.Middleware.Channel;
using Bioteca.Prism.Core.Security.Authorization;
using Bioteca.Prism.Domain.Payloads.Record;
using Bioteca.Prism.InteroperableResearchNode.Middleware;
using Bioteca.Prism.Service.Interfaces.Record;
using Microsoft.AspNetCore.Mvc;

namespace Bioteca.Prism.InteroperableResearchNode.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UploadController : BaseController
{
    private readonly IFileUploadService _fileUploadService;
    private readonly ILogger<UploadController> _logger;

    public UploadController(
        IFileUploadService fileUploadService,
        ILogger<UploadController> logger,
        IConfiguration configuration,
        IApiContext apiContext
    ) : base(logger, configuration, apiContext)
    {
        _fileUploadService = fileUploadService;
        _logger = logger;
    }

    [Route("recording")]
    [HttpPost]
    [PrismEncryptedChannelConnection<UploadRecordingPayload>]
    [PrismAuthenticatedSession]
    [Authorize("sub")]
    [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Recording()
    {
        try
        {
            var payload = HttpContext.Items["DecryptedRequest"] as UploadRecordingPayload;
            var fileUrl = await _fileUploadService.UploadRecordingAsync(payload!);
            return Ok(new { message = "Recording uploaded successfully", fileUrl });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(CreateError(
                "ERR_UPLOAD_VALIDATION_FAILED",
                ex.Message,
                null,
                retryable: false
            ));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(CreateError(
                "ERR_SESSION_NOT_FOUND",
                ex.Message,
                null,
                retryable: false
            ));
        }
        catch (FormatException)
        {
            return BadRequest(CreateError(
                "ERR_INVALID_FILE_DATA",
                "FileData must be a valid base64-encoded string",
                null,
                retryable: false
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload recording");
            return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                "ERR_UPLOAD_FAILED",
                "Failed to upload recording: " + ex.Message,
                new Dictionary<string, object> { ["reason"] = "internal_error" },
                retryable: true
            ));
        }
    }
}
