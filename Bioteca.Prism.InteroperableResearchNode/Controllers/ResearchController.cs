using Bioteca.Prism.Core.Controllers;
using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Core.Middleware.Channel;
using Bioteca.Prism.Core.Security.Authorization;
using Bioteca.Prism.Domain.DTOs.Research;
using Bioteca.Prism.Domain.Payloads.Application;
using Bioteca.Prism.Domain.Payloads.Research;
using Bioteca.Prism.InteroperableResearchNode.Middleware;
using Bioteca.Prism.Service.Interfaces.Research;
using Microsoft.AspNetCore.Mvc;

namespace Bioteca.Prism.InteroperableResearchNode.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ResearchController : BaseController
{
    private readonly IResearchService _researchService;
    private readonly IResearchExportService _researchExportService;
    private readonly ILogger<ResearchController> _logger;

    public ResearchController(
            IResearchService researchService,
            IResearchExportService researchExportService,
            ILogger<ResearchController> logger,
            IConfiguration configuration,
            IApiContext apiContext
        ) : base(logger, configuration, apiContext)
    {
        _researchService = researchService;
        _researchExportService = researchExportService;
        _logger = logger;
    }

    // ===== Existing Endpoints (unchanged) =====

    [Route("[action]")]
    [HttpPost]
    [PrismEncryptedChannelConnection<AddResearchDTO>]
    [PrismAuthenticatedSession]
    [Authorize("sub")]
    [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> New()
    {
        try
        {
            var payload = HttpContext.Items["DecryptedRequest"] as AddResearchDTO;
            return ServiceInvoke(_researchService.AddAsync, payload).Result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register new research");
            return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                "ERR_RESEARCH_REGISTRATION_FAILED",
                "Failed to register new research: " + ex.Message,
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
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllPaginatedAsync()
    {
        try
        {
            return ServiceInvoke(_researchService.GetAllPaginateAsync).Result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve paginated research");
            return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                "ERR_RESEARCH_PAGINATED_RETRIEVAL_FAILED",
                "Failed to retrieve paginated research: " + ex.Message,
                new Dictionary<string, object> { ["reason"] = "internal_error" },
                retryable: true
            ));
        }
    }

    // ===== Group 1: Research Core CRUD =====

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
            var research = await _researchService.GetByIdDetailAsync(id);

            if (research == null)
            {
                return NotFound(CreateError(
                    "ERR_RESEARCH_NOT_FOUND",
                    $"Research with ID {id} not found",
                    new Dictionary<string, object> { ["researchId"] = id },
                    retryable: false
                ));
            }

            return await ServiceInvoke(() => Task.FromResult(research));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve research {ResearchId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                "ERR_RESEARCH_RETRIEVAL_FAILED",
                "Failed to retrieve research",
                new Dictionary<string, object> { ["reason"] = "internal_error" },
                retryable: true
            ));
        }
    }

    [HttpPut("{id:guid}")]
    [PrismEncryptedChannelConnection<UpdateResearchPayload>]
    [PrismAuthenticatedSession]
    [Authorize("sub")]
    [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateResearch(Guid id)
    {
        try
        {
            var payload = HttpContext.Items["DecryptedRequest"] as UpdateResearchPayload;

            var updated = await _researchService.UpdateResearchAsync(id, payload);

            if (updated == null)
            {
                return NotFound(CreateError(
                    "ERR_RESEARCH_NOT_FOUND",
                    $"Research with ID {id} not found",
                    new Dictionary<string, object> { ["researchId"] = id },
                    retryable: false
                ));
            }

            return await ServiceInvoke(() => Task.FromResult(updated));
        }
        catch (Exception ex) when (ex.Message.Contains("must be") || ex.Message.Contains("must not"))
        {
            return BadRequest(CreateError(
                "ERR_RESEARCH_VALIDATION_FAILED",
                ex.Message,
                new Dictionary<string, object> { ["researchId"] = id },
                retryable: false
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update research {ResearchId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                "ERR_RESEARCH_UPDATE_FAILED",
                "Failed to update research: " + ex.Message,
                new Dictionary<string, object> { ["reason"] = "internal_error" },
                retryable: true
            ));
        }
    }

    [HttpDelete("{id:guid}")]
    [PrismEncryptedChannelConnection]
    [PrismAuthenticatedSession]
    [Authorize("sub")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteResearch(Guid id)
    {
        try
        {
            var deleted = await _researchService.DeleteResearchAsync(id);

            if (!deleted)
            {
                return NotFound(CreateError(
                    "ERR_RESEARCH_NOT_FOUND",
                    $"Research with ID {id} not found",
                    new Dictionary<string, object> { ["researchId"] = id },
                    retryable: false
                ));
            }

            return Ok(new { message = "Research deleted successfully", researchId = id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete research {ResearchId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                "ERR_RESEARCH_DELETE_FAILED",
                "Failed to delete research: " + ex.Message,
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
    public async Task<IActionResult> GetByStatus([FromQuery] string status)
    {
        try
        {
            return await ServiceInvoke(() => _researchService.GetByStatusPagedAsync(status));
        }
        catch (Exception ex) when (ex.Message.Contains("must be one of"))
        {
            return BadRequest(CreateError(
                "ERR_INVALID_STATUS",
                ex.Message,
                new Dictionary<string, object> { ["status"] = status },
                retryable: false
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve research by status");
            return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                "ERR_RESEARCH_RETRIEVAL_FAILED",
                "Failed to retrieve research by status",
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
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetActive()
    {
        try
        {
            return await ServiceInvoke(() => _researchService.GetActiveResearchPagedAsync());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve active research");
            return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                "ERR_RESEARCH_RETRIEVAL_FAILED",
                "Failed to retrieve active research",
                new Dictionary<string, object> { ["reason"] = "internal_error" },
                retryable: true
            ));
        }
    }

    // ===== Group 2: Research Researchers =====

    [HttpGet("{researchId:guid}/researchers")]
    [PrismEncryptedChannelConnection]
    [PrismAuthenticatedSession]
    [Authorize("sub")]
    [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetResearchers(Guid researchId)
    {
        try
        {
            var research = await _researchService.GetByIdAsync(researchId);
            if (research == null)
            {
                return NotFound(CreateError(
                    "ERR_RESEARCH_NOT_FOUND",
                    $"Research with ID {researchId} not found",
                    new Dictionary<string, object> { ["researchId"] = researchId },
                    retryable: false
                ));
            }

            return await ServiceInvoke(() => _researchService.GetResearchResearchersAsync(researchId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve researchers for research {ResearchId}", researchId);
            return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                "ERR_RESEARCH_RESEARCHER_RETRIEVAL_FAILED",
                "Failed to retrieve researchers",
                new Dictionary<string, object> { ["reason"] = "internal_error" },
                retryable: true
            ));
        }
    }

    [HttpPost("{researchId:guid}/researchers")]
    [PrismEncryptedChannelConnection<AddResearchResearcherPayload>]
    [PrismAuthenticatedSession]
    [Authorize("sub")]
    [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AddResearcher(Guid researchId)
    {
        try
        {
            var research = await _researchService.GetByIdAsync(researchId);
            if (research == null)
            {
                return NotFound(CreateError(
                    "ERR_RESEARCH_NOT_FOUND",
                    $"Research with ID {researchId} not found",
                    new Dictionary<string, object> { ["researchId"] = researchId },
                    retryable: false
                ));
            }

            var payload = HttpContext.Items["DecryptedRequest"] as AddResearchResearcherPayload;
            var result = await _researchService.AddResearchResearcherAsync(researchId, payload);
            return await ServiceInvoke(() => Task.FromResult(result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(CreateError(
                ex.Message,
                "Referenced entity not found",
                new Dictionary<string, object> { ["researchId"] = researchId },
                retryable: false
            ));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(CreateError(
                ex.Message,
                "Researcher is already assigned to this research",
                new Dictionary<string, object> { ["researchId"] = researchId },
                retryable: false
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add researcher to research {ResearchId}", researchId);
            return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                "ERR_RESEARCH_RESEARCHER_ADD_FAILED",
                "Failed to add researcher: " + ex.Message,
                new Dictionary<string, object> { ["reason"] = "internal_error" },
                retryable: true
            ));
        }
    }

    [HttpPut("{researchId:guid}/researchers/{researcherId:guid}")]
    [PrismEncryptedChannelConnection<UpdateResearchResearcherPayload>]
    [PrismAuthenticatedSession]
    [Authorize("sub")]
    [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateResearcher(Guid researchId, Guid researcherId)
    {
        try
        {
            var payload = HttpContext.Items["DecryptedRequest"] as UpdateResearchResearcherPayload;
            var result = await _researchService.UpdateResearchResearcherAsync(researchId, researcherId, payload);

            if (result == null)
            {
                return NotFound(CreateError(
                    "ERR_RESEARCH_RESEARCHER_NOT_FOUND",
                    "Researcher assignment not found",
                    new Dictionary<string, object> { ["researchId"] = researchId, ["researcherId"] = researcherId },
                    retryable: false
                ));
            }

            return await ServiceInvoke(() => Task.FromResult(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update researcher assignment {ResearchId}/{ResearcherId}", researchId, researcherId);
            return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                "ERR_RESEARCH_RESEARCHER_UPDATE_FAILED",
                "Failed to update researcher assignment: " + ex.Message,
                new Dictionary<string, object> { ["reason"] = "internal_error" },
                retryable: true
            ));
        }
    }

    [HttpDelete("{researchId:guid}/researchers/{researcherId:guid}")]
    [PrismEncryptedChannelConnection]
    [PrismAuthenticatedSession]
    [Authorize("sub")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RemoveResearcher(Guid researchId, Guid researcherId)
    {
        try
        {
            var removed = await _researchService.RemoveResearchResearcherAsync(researchId, researcherId);

            if (!removed)
            {
                return NotFound(CreateError(
                    "ERR_RESEARCH_RESEARCHER_NOT_FOUND",
                    "Researcher assignment not found or already removed",
                    new Dictionary<string, object> { ["researchId"] = researchId, ["researcherId"] = researcherId },
                    retryable: false
                ));
            }

            return Ok(new { message = "Researcher removed from research successfully", researchId, researcherId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove researcher {ResearcherId} from research {ResearchId}", researcherId, researchId);
            return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                "ERR_RESEARCH_RESEARCHER_REMOVE_FAILED",
                "Failed to remove researcher: " + ex.Message,
                new Dictionary<string, object> { ["reason"] = "internal_error" },
                retryable: true
            ));
        }
    }

    // ===== Group 3: Research Volunteers =====

    [HttpGet("{researchId:guid}/volunteers")]
    [PrismEncryptedChannelConnection]
    [PrismAuthenticatedSession]
    [Authorize("sub")]
    [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetVolunteers(Guid researchId)
    {
        try
        {
            var research = await _researchService.GetByIdAsync(researchId);
            if (research == null)
            {
                return NotFound(CreateError(
                    "ERR_RESEARCH_NOT_FOUND",
                    $"Research with ID {researchId} not found",
                    new Dictionary<string, object> { ["researchId"] = researchId },
                    retryable: false
                ));
            }

            return await ServiceInvoke(() => _researchService.GetResearchVolunteersAsync(researchId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve volunteers for research {ResearchId}", researchId);
            return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                "ERR_RESEARCH_VOLUNTEER_RETRIEVAL_FAILED",
                "Failed to retrieve volunteers",
                new Dictionary<string, object> { ["reason"] = "internal_error" },
                retryable: true
            ));
        }
    }

    [HttpPost("{researchId:guid}/volunteers")]
    [PrismEncryptedChannelConnection<AddResearchVolunteerPayload>]
    [PrismAuthenticatedSession]
    [Authorize("sub")]
    [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AddVolunteer(Guid researchId)
    {
        try
        {
            var research = await _researchService.GetByIdAsync(researchId);
            if (research == null)
            {
                return NotFound(CreateError(
                    "ERR_RESEARCH_NOT_FOUND",
                    $"Research with ID {researchId} not found",
                    new Dictionary<string, object> { ["researchId"] = researchId },
                    retryable: false
                ));
            }

            var payload = HttpContext.Items["DecryptedRequest"] as AddResearchVolunteerPayload;
            var result = await _researchService.AddResearchVolunteerAsync(researchId, payload);
            return await ServiceInvoke(() => Task.FromResult(result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(CreateError(
                ex.Message,
                "Referenced entity not found",
                new Dictionary<string, object> { ["researchId"] = researchId },
                retryable: false
            ));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(CreateError(
                ex.Message,
                "Volunteer is already enrolled in this research",
                new Dictionary<string, object> { ["researchId"] = researchId },
                retryable: false
            ));
        }
        catch (Exception ex) when (ex.Message.Contains("must not") || ex.Message.Contains("is required"))
        {
            return BadRequest(CreateError(
                "ERR_VOLUNTEER_ENROLLMENT_VALIDATION_FAILED",
                ex.Message,
                new Dictionary<string, object> { ["researchId"] = researchId },
                retryable: false
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add volunteer to research {ResearchId}", researchId);
            return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                "ERR_RESEARCH_VOLUNTEER_ADD_FAILED",
                "Failed to add volunteer: " + ex.Message,
                new Dictionary<string, object> { ["reason"] = "internal_error" },
                retryable: true
            ));
        }
    }

    [HttpPut("{researchId:guid}/volunteers/{volunteerId:guid}")]
    [PrismEncryptedChannelConnection<UpdateResearchVolunteerPayload>]
    [PrismAuthenticatedSession]
    [Authorize("sub")]
    [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateVolunteer(Guid researchId, Guid volunteerId)
    {
        try
        {
            var payload = HttpContext.Items["DecryptedRequest"] as UpdateResearchVolunteerPayload;
            var result = await _researchService.UpdateResearchVolunteerAsync(researchId, volunteerId, payload);

            if (result == null)
            {
                return NotFound(CreateError(
                    "ERR_RESEARCH_VOLUNTEER_NOT_FOUND",
                    "Volunteer enrollment not found",
                    new Dictionary<string, object> { ["researchId"] = researchId, ["volunteerId"] = volunteerId },
                    retryable: false
                ));
            }

            return await ServiceInvoke(() => Task.FromResult(result));
        }
        catch (Exception ex) when (ex.Message.Contains("must be") || ex.Message.Contains("must not"))
        {
            return BadRequest(CreateError(
                "ERR_VOLUNTEER_ENROLLMENT_VALIDATION_FAILED",
                ex.Message,
                new Dictionary<string, object> { ["researchId"] = researchId, ["volunteerId"] = volunteerId },
                retryable: false
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update volunteer enrollment {ResearchId}/{VolunteerId}", researchId, volunteerId);
            return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                "ERR_RESEARCH_VOLUNTEER_UPDATE_FAILED",
                "Failed to update volunteer enrollment: " + ex.Message,
                new Dictionary<string, object> { ["reason"] = "internal_error" },
                retryable: true
            ));
        }
    }

    [HttpDelete("{researchId:guid}/volunteers/{volunteerId:guid}")]
    [PrismEncryptedChannelConnection]
    [PrismAuthenticatedSession]
    [Authorize("sub")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RemoveVolunteer(Guid researchId, Guid volunteerId)
    {
        try
        {
            var removed = await _researchService.RemoveResearchVolunteerAsync(researchId, volunteerId);

            if (!removed)
            {
                return NotFound(CreateError(
                    "ERR_RESEARCH_VOLUNTEER_NOT_FOUND",
                    "Volunteer enrollment not found or already withdrawn",
                    new Dictionary<string, object> { ["researchId"] = researchId, ["volunteerId"] = volunteerId },
                    retryable: false
                ));
            }

            return Ok(new { message = "Volunteer withdrawn from research successfully", researchId, volunteerId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove volunteer {VolunteerId} from research {ResearchId}", volunteerId, researchId);
            return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                "ERR_RESEARCH_VOLUNTEER_REMOVE_FAILED",
                "Failed to remove volunteer: " + ex.Message,
                new Dictionary<string, object> { ["reason"] = "internal_error" },
                retryable: true
            ));
        }
    }

    // ===== Group 4: Research Applications =====

    [HttpGet("{researchId:guid}/applications")]
    [PrismEncryptedChannelConnection]
    [PrismAuthenticatedSession]
    [Authorize("sub")]
    [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetApplications(Guid researchId)
    {
        try
        {
            var research = await _researchService.GetByIdAsync(researchId);
            if (research == null)
            {
                return NotFound(CreateError(
                    "ERR_RESEARCH_NOT_FOUND",
                    $"Research with ID {researchId} not found",
                    new Dictionary<string, object> { ["researchId"] = researchId },
                    retryable: false
                ));
            }

            return await ServiceInvoke(() => _researchService.GetResearchApplicationsAsync(researchId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve applications for research {ResearchId}", researchId);
            return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                "ERR_RESEARCH_APPLICATION_RETRIEVAL_FAILED",
                "Failed to retrieve applications",
                new Dictionary<string, object> { ["reason"] = "internal_error" },
                retryable: true
            ));
        }
    }

    [HttpPost("{researchId:guid}/applications")]
    [PrismEncryptedChannelConnection<AddApplicationPayload>]
    [PrismAuthenticatedSession]
    [Authorize("sub")]
    [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AddApplication(Guid researchId)
    {
        try
        {
            var research = await _researchService.GetByIdAsync(researchId);
            if (research == null)
            {
                return NotFound(CreateError(
                    "ERR_RESEARCH_NOT_FOUND",
                    $"Research with ID {researchId} not found",
                    new Dictionary<string, object> { ["researchId"] = researchId },
                    retryable: false
                ));
            }

            var payload = HttpContext.Items["DecryptedRequest"] as AddApplicationPayload;
            var result = await _researchService.AddApplicationAsync(researchId, payload);
            return await ServiceInvoke(() => Task.FromResult(result));
        }
        catch (Exception ex) when (ex.Message.Contains("required") || ex.Message.Contains("must be") || ex.Message.Contains("must not"))
        {
            return BadRequest(CreateError(
                "ERR_APPLICATION_VALIDATION_FAILED",
                ex.Message,
                new Dictionary<string, object> { ["researchId"] = researchId },
                retryable: false
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add application to research {ResearchId}", researchId);
            return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                "ERR_APPLICATION_ADD_FAILED",
                "Failed to add application: " + ex.Message,
                new Dictionary<string, object> { ["reason"] = "internal_error" },
                retryable: true
            ));
        }
    }

    [HttpPut("{researchId:guid}/applications/{applicationId:guid}")]
    [PrismEncryptedChannelConnection<UpdateApplicationPayload>]
    [PrismAuthenticatedSession]
    [Authorize("sub")]
    [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateApplication(Guid researchId, Guid applicationId)
    {
        try
        {
            var payload = HttpContext.Items["DecryptedRequest"] as UpdateApplicationPayload;
            var result = await _researchService.UpdateApplicationAsync(researchId, applicationId, payload);

            if (result == null)
            {
                return NotFound(CreateError(
                    "ERR_APPLICATION_NOT_FOUND",
                    "Application not found",
                    new Dictionary<string, object> { ["researchId"] = researchId, ["applicationId"] = applicationId },
                    retryable: false
                ));
            }

            return await ServiceInvoke(() => Task.FromResult(result));
        }
        catch (Exception ex) when (ex.Message.Contains("must be") || ex.Message.Contains("must not"))
        {
            return BadRequest(CreateError(
                "ERR_APPLICATION_VALIDATION_FAILED",
                ex.Message,
                new Dictionary<string, object> { ["researchId"] = researchId, ["applicationId"] = applicationId },
                retryable: false
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update application {ApplicationId}", applicationId);
            return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                "ERR_APPLICATION_UPDATE_FAILED",
                "Failed to update application: " + ex.Message,
                new Dictionary<string, object> { ["reason"] = "internal_error" },
                retryable: true
            ));
        }
    }

    [HttpDelete("{researchId:guid}/applications/{applicationId:guid}")]
    [PrismEncryptedChannelConnection]
    [PrismAuthenticatedSession]
    [Authorize("sub")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteApplication(Guid researchId, Guid applicationId)
    {
        try
        {
            var deleted = await _researchService.DeleteApplicationAsync(researchId, applicationId);

            if (!deleted)
            {
                return NotFound(CreateError(
                    "ERR_APPLICATION_NOT_FOUND",
                    "Application not found",
                    new Dictionary<string, object> { ["researchId"] = researchId, ["applicationId"] = applicationId },
                    retryable: false
                ));
            }

            return Ok(new { message = "Application deleted successfully", applicationId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete application {ApplicationId}", applicationId);
            return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                "ERR_APPLICATION_DELETE_FAILED",
                "Failed to delete application: " + ex.Message,
                new Dictionary<string, object> { ["reason"] = "internal_error" },
                retryable: true
            ));
        }
    }

    // ===== Group 5: Research Devices =====

    [HttpGet("{researchId:guid}/devices")]
    [PrismEncryptedChannelConnection]
    [PrismAuthenticatedSession]
    [Authorize("sub")]
    [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDevices(Guid researchId)
    {
        try
        {
            var research = await _researchService.GetByIdAsync(researchId);
            if (research == null)
            {
                return NotFound(CreateError(
                    "ERR_RESEARCH_NOT_FOUND",
                    $"Research with ID {researchId} not found",
                    new Dictionary<string, object> { ["researchId"] = researchId },
                    retryable: false
                ));
            }

            return await ServiceInvoke(() => _researchService.GetResearchDevicesAsync(researchId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve devices for research {ResearchId}", researchId);
            return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                "ERR_RESEARCH_DEVICE_RETRIEVAL_FAILED",
                "Failed to retrieve devices",
                new Dictionary<string, object> { ["reason"] = "internal_error" },
                retryable: true
            ));
        }
    }

    [HttpPost("{researchId:guid}/devices")]
    [PrismEncryptedChannelConnection<AddResearchDevicePayload>]
    [PrismAuthenticatedSession]
    [Authorize("sub")]
    [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AddDevice(Guid researchId)
    {
        try
        {
            var research = await _researchService.GetByIdAsync(researchId);
            if (research == null)
            {
                return NotFound(CreateError(
                    "ERR_RESEARCH_NOT_FOUND",
                    $"Research with ID {researchId} not found",
                    new Dictionary<string, object> { ["researchId"] = researchId },
                    retryable: false
                ));
            }

            var payload = HttpContext.Items["DecryptedRequest"] as AddResearchDevicePayload;
            var result = await _researchService.AddResearchDeviceAsync(researchId, payload);
            return await ServiceInvoke(() => Task.FromResult(result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(CreateError(
                ex.Message,
                "Referenced entity not found",
                new Dictionary<string, object> { ["researchId"] = researchId },
                retryable: false
            ));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(CreateError(
                ex.Message,
                "Device is already assigned to this research",
                new Dictionary<string, object> { ["researchId"] = researchId },
                retryable: false
            ));
        }
        catch (Exception ex) when (ex.Message.Contains("required") || ex.Message.Contains("must be") || ex.Message.Contains("must not"))
        {
            return BadRequest(CreateError(
                "ERR_DEVICE_ASSIGNMENT_VALIDATION_FAILED",
                ex.Message,
                new Dictionary<string, object> { ["researchId"] = researchId },
                retryable: false
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add device to research {ResearchId}", researchId);
            return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                "ERR_RESEARCH_DEVICE_ADD_FAILED",
                "Failed to add device: " + ex.Message,
                new Dictionary<string, object> { ["reason"] = "internal_error" },
                retryable: true
            ));
        }
    }

    [HttpPut("{researchId:guid}/devices/{deviceId:guid}")]
    [PrismEncryptedChannelConnection<UpdateResearchDevicePayload>]
    [PrismAuthenticatedSession]
    [Authorize("sub")]
    [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateDevice(Guid researchId, Guid deviceId)
    {
        try
        {
            var payload = HttpContext.Items["DecryptedRequest"] as UpdateResearchDevicePayload;
            var result = await _researchService.UpdateResearchDeviceAsync(researchId, deviceId, payload);

            if (result == null)
            {
                return NotFound(CreateError(
                    "ERR_RESEARCH_DEVICE_NOT_FOUND",
                    "Device assignment not found",
                    new Dictionary<string, object> { ["researchId"] = researchId, ["deviceId"] = deviceId },
                    retryable: false
                ));
            }

            return await ServiceInvoke(() => Task.FromResult(result));
        }
        catch (Exception ex) when (ex.Message.Contains("must be") || ex.Message.Contains("must not"))
        {
            return BadRequest(CreateError(
                "ERR_DEVICE_ASSIGNMENT_VALIDATION_FAILED",
                ex.Message,
                new Dictionary<string, object> { ["researchId"] = researchId, ["deviceId"] = deviceId },
                retryable: false
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update device assignment {ResearchId}/{DeviceId}", researchId, deviceId);
            return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                "ERR_RESEARCH_DEVICE_UPDATE_FAILED",
                "Failed to update device assignment: " + ex.Message,
                new Dictionary<string, object> { ["reason"] = "internal_error" },
                retryable: true
            ));
        }
    }

    [HttpDelete("{researchId:guid}/devices/{deviceId:guid}")]
    [PrismEncryptedChannelConnection]
    [PrismAuthenticatedSession]
    [Authorize("sub")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RemoveDevice(Guid researchId, Guid deviceId)
    {
        try
        {
            var removed = await _researchService.RemoveResearchDeviceAsync(researchId, deviceId);

            if (!removed)
            {
                return NotFound(CreateError(
                    "ERR_RESEARCH_DEVICE_NOT_FOUND",
                    "Device assignment not found or already removed",
                    new Dictionary<string, object> { ["researchId"] = researchId, ["deviceId"] = deviceId },
                    retryable: false
                ));
            }

            return Ok(new { message = "Device removed from research successfully", researchId, deviceId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove device {DeviceId} from research {ResearchId}", deviceId, researchId);
            return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                "ERR_RESEARCH_DEVICE_REMOVE_FAILED",
                "Failed to remove device: " + ex.Message,
                new Dictionary<string, object> { ["reason"] = "internal_error" },
                retryable: true
            ));
        }
    }

    // ===== Group 6: Device Sensors =====

    [HttpGet("{researchId:guid}/sensors")]
    [PrismEncryptedChannelConnection]
    [PrismAuthenticatedSession]
    [Authorize("sub")]
    [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllResearchSensors(Guid researchId)
    {
        try
        {
            var research = await _researchService.GetByIdAsync(researchId);
            if (research == null)
            {
                return NotFound(CreateError(
                    "ERR_RESEARCH_NOT_FOUND",
                    $"Research with ID {researchId} not found",
                    new Dictionary<string, object> { ["researchId"] = researchId },
                    retryable: false
                ));
            }

            var sensors = await _researchService.GetAllSensorsForResearchAsync(researchId);
            return await ServiceInvoke(() => Task.FromResult(sensors));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve all sensors for research {ResearchId}", researchId);
            return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                "ERR_SENSOR_RETRIEVAL_FAILED",
                "Failed to retrieve sensors",
                new Dictionary<string, object> { ["reason"] = "internal_error" },
                retryable: true
            ));
        }
    }

    [HttpGet("{researchId:guid}/devices/{deviceId:guid}/sensors")]
    [PrismEncryptedChannelConnection]
    [PrismAuthenticatedSession]
    [Authorize("sub")]
    [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDeviceSensors(Guid researchId, Guid deviceId)
    {
        try
        {
            var research = await _researchService.GetByIdAsync(researchId);
            if (research == null)
            {
                return NotFound(CreateError(
                    "ERR_RESEARCH_NOT_FOUND",
                    $"Research with ID {researchId} not found",
                    new Dictionary<string, object> { ["researchId"] = researchId },
                    retryable: false
                ));
            }

            var sensors = await _researchService.GetDeviceSensorsForResearchAsync(researchId, deviceId);

            if (sensors == null)
            {
                return NotFound(CreateError(
                    "ERR_RESEARCH_DEVICE_NOT_FOUND",
                    "Device is not assigned to this research",
                    new Dictionary<string, object> { ["researchId"] = researchId, ["deviceId"] = deviceId },
                    retryable: false
                ));
            }

            return await ServiceInvoke(() => Task.FromResult(sensors));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve sensors for device {DeviceId} in research {ResearchId}", deviceId, researchId);
            return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                "ERR_SENSOR_RETRIEVAL_FAILED",
                "Failed to retrieve sensors",
                new Dictionary<string, object> { ["reason"] = "internal_error" },
                retryable: true
            ));
        }
    }

    // ===== Export =====

    [HttpGet("{id:guid}/export")]
    [Authorize("sub")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Export(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _researchExportService.ExportAsync(id, cancellationToken);
            return File(result.ZipStream, "application/zip", result.FileName);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(CreateError(
                "ERR_RESEARCH_NOT_FOUND",
                $"Research with ID {id} not found",
                new Dictionary<string, object> { ["researchId"] = id },
                retryable: false
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export research {ResearchId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                "ERR_EXPORT_FAILED",
                "Failed to export research data",
                new Dictionary<string, object> { ["reason"] = "internal_error" },
                retryable: false
            ));
        }
    }
}
