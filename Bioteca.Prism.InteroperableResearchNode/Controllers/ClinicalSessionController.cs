using Bioteca.Prism.Core.Controllers;
using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Core.Middleware.Channel;
using Bioteca.Prism.Core.Security.Authorization;
using Bioteca.Prism.Domain.Payloads.Record;
using Bioteca.Prism.InteroperableResearchNode.Middleware;
using Bioteca.Prism.Service.Interfaces.Record;
using Microsoft.AspNetCore.Mvc;
using RecordEntity = Bioteca.Prism.Domain.Entities.Record.Record;

namespace Bioteca.Prism.InteroperableResearchNode.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ClinicalSessionController : BaseController
{
    private readonly IClinicalSessionService _clinicalSessionService;
    private readonly ILogger<ClinicalSessionController> _logger;

    public ClinicalSessionController(
        IClinicalSessionService clinicalSessionService,
        ILogger<ClinicalSessionController> logger,
        IConfiguration configuration,
        IApiContext apiContext
    ) : base(logger, configuration, apiContext)
    {
        _clinicalSessionService = clinicalSessionService;
        _logger = logger;
    }

    [Route("[action]")]
    [HttpPost]
    [PrismEncryptedChannelConnection<CreateClinicalSessionPayload>]
    [PrismAuthenticatedSession]
    [Authorize("sub")]
    [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> New()
    {
        try
        {
            var payload = HttpContext.Items["DecryptedRequest"] as CreateClinicalSessionPayload;
            return await ServiceInvoke(() => _clinicalSessionService.CreateAsync(payload!));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(CreateError(
                "ERR_VOLUNTEER_NOT_FOUND",
                ex.Message,
                null,
                retryable: false
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create clinical session");
            return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                "ERR_CLINICAL_SESSION_CREATE_FAILED",
                "Failed to create clinical session: " + ex.Message,
                new Dictionary<string, object> { ["reason"] = "internal_error" },
                retryable: true
            ));
        }
    }

    [Route("[action]")]
    [HttpGet]
    [PrismEncryptedChannelConnection]
    [PrismAuthenticatedSession]
    [Authorize("sub")]
    [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllPaginated()
    {
        try
        {
            Guid? researchId = null;
            Guid? volunteerId = null;
            string? status = null;
            DateTime? dateFrom = null;
            DateTime? dateTo = null;

            if (HttpContext.Request.Query.TryGetValue("researchId", out var researchIdStr) && Guid.TryParse(researchIdStr, out var rid))
                researchId = rid;

            if (HttpContext.Request.Query.TryGetValue("volunteerId", out var volunteerIdStr) && Guid.TryParse(volunteerIdStr, out var vid))
                volunteerId = vid;

            if (HttpContext.Request.Query.TryGetValue("status", out var statusStr))
                status = statusStr.ToString();

            if (HttpContext.Request.Query.TryGetValue("dateFrom", out var dateFromStr) && DateTime.TryParse(dateFromStr, out var df))
                dateFrom = df;

            if (HttpContext.Request.Query.TryGetValue("dateTo", out var dateToStr) && DateTime.TryParse(dateToStr, out var dt))
                dateTo = dt;

            return await ServiceInvoke(() => _clinicalSessionService.GetFilteredPagedAsync(researchId, volunteerId, status, dateFrom, dateTo));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(CreateError(
                "ERR_INVALID_FILTER",
                ex.Message,
                null,
                retryable: false
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve paginated clinical sessions");
            return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                "ERR_CLINICAL_SESSION_RETRIEVAL_FAILED",
                "Failed to retrieve clinical sessions: " + ex.Message,
                new Dictionary<string, object> { ["reason"] = "internal_error" },
                retryable: true
            ));
        }
    }

    [HttpGet("{id:guid}")]
    [PrismEncryptedChannelConnection]
    [PrismAuthenticatedSession]
    [Authorize("sub")]
    [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var session = await _clinicalSessionService.GetByIdDetailAsync(id);

            if (session == null)
            {
                return NotFound(CreateError(
                    "ERR_CLINICAL_SESSION_NOT_FOUND",
                    $"Clinical session with ID {id} not found",
                    new Dictionary<string, object> { ["sessionId"] = id },
                    retryable: false
                ));
            }

            return await ServiceInvoke(() => Task.FromResult(session));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve clinical session {SessionId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                "ERR_CLINICAL_SESSION_RETRIEVAL_FAILED",
                "Failed to retrieve clinical session",
                new Dictionary<string, object> { ["reason"] = "internal_error" },
                retryable: true
            ));
        }
    }

    [HttpPut("Update/{id:guid}")]
    [PrismEncryptedChannelConnection<UpdateClinicalSessionPayload>]
    [PrismAuthenticatedSession]
    [Authorize("sub")]
    [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(Guid id)
    {
        try
        {
            var payload = HttpContext.Items["DecryptedRequest"] as UpdateClinicalSessionPayload;
            var updated = await _clinicalSessionService.UpdateAsync(id, payload!);

            if (updated == null)
            {
                return NotFound(CreateError(
                    "ERR_CLINICAL_SESSION_NOT_FOUND",
                    $"Clinical session with ID {id} not found",
                    new Dictionary<string, object> { ["sessionId"] = id },
                    retryable: false
                ));
            }

            return await ServiceInvoke(() => Task.FromResult(updated));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update clinical session {SessionId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                "ERR_CLINICAL_SESSION_UPDATE_FAILED",
                "Failed to update clinical session: " + ex.Message,
                new Dictionary<string, object> { ["reason"] = "internal_error" },
                retryable: true
            ));
        }
    }

    // ===== Recordings =====

    [HttpPost("{sessionId:guid}/recordings/New")]
    [PrismEncryptedChannelConnection<CreateRecordingPayload>]
    [PrismAuthenticatedSession]
    [Authorize("sub")]
    [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> NewRecording(Guid sessionId)
    {
        try
        {
            var payload = HttpContext.Items["DecryptedRequest"] as CreateRecordingPayload;
            var result = await _clinicalSessionService.CreateRecordingAsync(sessionId, payload!);
            return await ServiceInvoke(() => Task.FromResult(result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(CreateError(
                "ERR_CLINICAL_SESSION_NOT_FOUND",
                ex.Message,
                new Dictionary<string, object> { ["sessionId"] = sessionId },
                retryable: false
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create recording for session {SessionId}", sessionId);
            return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                "ERR_RECORDING_CREATE_FAILED",
                "Failed to create recording: " + ex.Message,
                new Dictionary<string, object> { ["reason"] = "internal_error" },
                retryable: true
            ));
        }
    }

    [HttpGet("{sessionId:guid}/recordings")]
    [PrismEncryptedChannelConnection]
    [PrismAuthenticatedSession]
    [Authorize("sub")]
    [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetRecordings(Guid sessionId)
    {
        try
        {
            return await ServiceInvoke(() => _clinicalSessionService.GetRecordingsBySessionAsync(sessionId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve recordings for session {SessionId}", sessionId);
            return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                "ERR_RECORDING_RETRIEVAL_FAILED",
                "Failed to retrieve recordings",
                new Dictionary<string, object> { ["reason"] = "internal_error" },
                retryable: true
            ));
        }
    }

    // ===== Annotations =====

    [HttpPost("{sessionId:guid}/annotations/New")]
    [PrismEncryptedChannelConnection<CreateAnnotationPayload>]
    [PrismAuthenticatedSession]
    [Authorize("sub")]
    [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> NewAnnotation(Guid sessionId)
    {
        try
        {
            var payload = HttpContext.Items["DecryptedRequest"] as CreateAnnotationPayload;
            var result = await _clinicalSessionService.CreateAnnotationAsync(sessionId, payload!);
            return await ServiceInvoke(() => Task.FromResult(result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(CreateError(
                "ERR_CLINICAL_SESSION_NOT_FOUND",
                ex.Message,
                new Dictionary<string, object> { ["sessionId"] = sessionId },
                retryable: false
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create annotation for session {SessionId}", sessionId);
            return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                "ERR_ANNOTATION_CREATE_FAILED",
                "Failed to create annotation: " + ex.Message,
                new Dictionary<string, object> { ["reason"] = "internal_error" },
                retryable: true
            ));
        }
    }

    [HttpGet("{sessionId:guid}/annotations")]
    [PrismEncryptedChannelConnection]
    [PrismAuthenticatedSession]
    [Authorize("sub")]
    [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAnnotations(Guid sessionId)
    {
        try
        {
            return await ServiceInvoke(() => _clinicalSessionService.GetAnnotationsBySessionAsync(sessionId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve annotations for session {SessionId}", sessionId);
            return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                "ERR_ANNOTATION_RETRIEVAL_FAILED",
                "Failed to retrieve annotations",
                new Dictionary<string, object> { ["reason"] = "internal_error" },
                retryable: true
            ));
        }
    }
}
