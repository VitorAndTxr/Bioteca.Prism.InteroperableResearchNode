using Bioteca.Prism.Core.Controllers;
using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Core.Middleware.Channel;
using Bioteca.Prism.Core.Security.Authorization;
using Bioteca.Prism.Domain.DTOs.Research;
using Bioteca.Prism.InteroperableResearchNode.Middleware;
using Bioteca.Prism.Service.Interfaces.Research;
using Microsoft.AspNetCore.Mvc;

namespace Bioteca.Prism.InteroperableResearchNode.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ResearchController : BaseController
{
    private readonly IResearchService _researchService;
    private readonly ILogger<ResearchController> _logger;

    public ResearchController(
            IResearchService researchService,
            ILogger<ResearchController> logger,
            IConfiguration configuration,
            IApiContext apiContext
        ) : base(logger, configuration, apiContext)
    {
        _researchService = researchService;
        _logger = logger;
    }

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
}