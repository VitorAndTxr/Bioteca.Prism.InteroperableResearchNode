using Bioteca.Prism.Core.Controllers;
using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Core.Middleware.Channel;
using Bioteca.Prism.Core.Security.Authorization;
using Bioteca.Prism.Domain.Payloads.Volunteer;
using Bioteca.Prism.InteroperableResearchNode.Middleware;
using Bioteca.Prism.Service.Interfaces.Volunteer;
using Microsoft.AspNetCore.Mvc;

namespace Bioteca.Prism.InteroperableResearchNode.Controllers;

[Route("api/[controller]")]
[ApiController]
public class VolunteerController : BaseController
{
    private readonly IVolunteerService _volunteerService;
    private readonly ILogger<VolunteerController> _logger;

    public VolunteerController(
        IVolunteerService volunteerService,
        ILogger<VolunteerController> logger,
        IConfiguration configuration,
        IApiContext apiContext
    ) : base(logger, configuration, apiContext)
    {
        _volunteerService = volunteerService;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated list of volunteers
    /// </summary>
    [Route("[action]")]
    [HttpGet]
    [PrismEncryptedChannelConnection]
    [PrismAuthenticatedSession]
    [Authorize("sub")]
    [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllPaginated()
    {
        try
        {
            return ServiceInvoke(_volunteerService.GetAllPaginateAsync).Result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve paginated volunteers");
            return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                "ERR_VOLUNTEER_PAGINATED_RETRIEVAL_FAILED",
                "Failed to retrieve paginated volunteers",
                new Dictionary<string, object> { ["reason"] = "internal_error" },
                retryable: true
            ));
        }
    }

    /// <summary>
    /// Get volunteer by ID
    /// </summary>
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
            var volunteer = await _volunteerService.GetVolunteerByIdAsync(id);

            if (volunteer == null)
            {
                return NotFound(CreateError(
                    "ERR_VOLUNTEER_NOT_FOUND",
                    $"Volunteer with ID {id} not found",
                    new Dictionary<string, object> { ["volunteerId"] = id },
                    retryable: false
                ));
            }

            return await ServiceInvoke(() => Task.FromResult(volunteer));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve volunteer {VolunteerId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                "ERR_VOLUNTEER_RETRIEVAL_FAILED",
                "Failed to retrieve volunteer",
                new Dictionary<string, object> { ["reason"] = "internal_error" },
                retryable: true
            ));
        }
    }

    /// <summary>
    /// Create a new volunteer
    /// </summary>
    [Route("[action]")]
    [HttpPost]
    [PrismEncryptedChannelConnection<AddVolunteerPayload>]
    [PrismAuthenticatedSession]
    [Authorize("sub")]
    [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult New()
    {
        try
        {
            var payload = HttpContext.Items["DecryptedRequest"] as AddVolunteerPayload;
            return ServiceInvoke(_volunteerService.AddAsync, payload).Result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create new volunteer");
            return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                "ERR_VOLUNTEER_ADD_FAILED",
                "Failed to add new volunteer: " + ex.Message,
                new Dictionary<string, object> { ["reason"] = "internal_error" },
                retryable: true
            ));
        }
    }

    /// <summary>
    /// Update an existing volunteer
    /// </summary>
    [HttpPut("Update/{id:guid}")]
    [PrismEncryptedChannelConnection<UpdateVolunteerPayload>]
    [PrismAuthenticatedSession]
    [Authorize("sub")]
    [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(Guid id)
    {
        try
        {
            var payload = HttpContext.Items["DecryptedRequest"] as UpdateVolunteerPayload;

            var updatedVolunteer = await _volunteerService.UpdateVolunteerAsync(id, payload);

            if (updatedVolunteer == null)
            {
                return NotFound(CreateError(
                    "ERR_VOLUNTEER_NOT_FOUND",
                    $"Volunteer with ID {id} not found",
                    new Dictionary<string, object> { ["volunteerId"] = id },
                    retryable: false
                ));
            }

            return await ServiceInvoke(() => Task.FromResult(updatedVolunteer));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update volunteer {VolunteerId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                "ERR_VOLUNTEER_UPDATE_FAILED",
                "Failed to update volunteer: " + ex.Message,
                new Dictionary<string, object> { ["reason"] = "internal_error" },
                retryable: true
            ));
        }
    }

    /// <summary>
    /// Delete a volunteer by ID
    /// </summary>
    [HttpDelete("{id:guid}")]
    [PrismEncryptedChannelConnection]
    [PrismAuthenticatedSession]
    [Authorize("sub")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var deleted = await _volunteerService.DeleteVolunteerAsync(id);

            if (!deleted)
            {
                return NotFound(CreateError(
                    "ERR_VOLUNTEER_NOT_FOUND",
                    $"Volunteer with ID {id} not found",
                    new Dictionary<string, object> { ["volunteerId"] = id },
                    retryable: false
                ));
            }

            return Ok(new { message = "Volunteer deleted successfully", volunteerId = id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete volunteer {VolunteerId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                "ERR_VOLUNTEER_DELETE_FAILED",
                "Failed to delete volunteer: " + ex.Message,
                new Dictionary<string, object> { ["reason"] = "internal_error" },
                retryable: true
            ));
        }
    }
}
