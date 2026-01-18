using Bioteca.Prism.Core.Controllers;
using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Core.Middleware.Channel;
using Bioteca.Prism.Core.Security.Authorization;
using Bioteca.Prism.Domain.Payloads.User;
using Bioteca.Prism.InteroperableResearchNode.Middleware;
using Bioteca.Prism.Service.Interfaces.Researcher;
using Bioteca.Prism.Service.Services.Researcher;
using Microsoft.AspNetCore.Mvc;

namespace Bioteca.Prism.InteroperableResearchNode.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ResearcherController : BaseController
    {
        private readonly IResearcherService _researcherService;

        private readonly ILogger<ResearcherController> _logger;

        public ResearcherController(
                IResearcherService researcherService,
                ILogger<ResearcherController> logger,
                IConfiguration configuration,
                IApiContext apiContext
            ) : base(logger, configuration, apiContext)
        {
            _researcherService = researcherService;

            _logger = logger;
        }

        [Route("[action]")]
        [HttpGet]
        [PrismEncryptedChannelConnection]
        [PrismAuthenticatedSession]
        [Authorize("sub")]
        [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetResearchers(CancellationToken cancellationToken = default)
        {
            try
            {
                return ServiceInvoke(_researcherService.GetAllResearchersPaginateAsync).Result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve paginated researchers");
                return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                    "ERR_RESEARCHER_RETRIEVAL_FAILED",
                    "Failed to retrieve researchers",
                    new Dictionary<string, object> { ["reason"] = "internal_error" },
                    retryable: true
                ));
            }
        }

        [Route("[action]")]
        [HttpPost]
        [PrismEncryptedChannelConnection<AddResearcherPayload>]
        [PrismAuthenticatedSession]
        [Authorize("sub")]
        [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult New()
        {
            try
            {
                var payload = HttpContext.Items["DecryptedRequest"] as AddResearcherPayload;
                return ServiceInvoke(_researcherService.AddAsync, payload).Result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register new researchers");
                return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                    "ERR_RESEARCHER_REGISTRATION_FAILED",
                    "Failed to register new researchers:" + ex.Message,
                    new Dictionary<string, object> { ["reason"] = "internal_error" },
                    retryable: true
                ));
            }
        }

        [Route("[action]/{nodeId}")]
        [HttpGet]
        [PrismEncryptedChannelConnection]
        [PrismAuthenticatedSession]
        [Authorize("sub")]
        [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetResearcherByNodeId(Guid nodeId)
        {
            try
            {
                return ServiceInvoke(_researcherService.GetByNodeIdAsync, nodeId).Result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve researchers");
                return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                    "ERR_RESEARCHER_RETRIEVAL_FAILED",
                    "Failed to retrieve researchers:" + ex.Message,
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
        public async Task<IActionResult> GetResearcherById(Guid id)
        {
            try
            {
                var researcher = await _researcherService.GetByResearcherIdAsync(id);

                if (researcher == null)
                {
                    return NotFound(new { error = "Researcher not found" });
                }

                return await ServiceInvoke(() => Task.FromResult(researcher));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve researcher by ID {ResearcherId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                    "ERR_RESEARCHER_RETRIEVAL_FAILED",
                    "Failed to retrieve researcher",
                    new Dictionary<string, object> { ["reason"] = "internal_error" },
                    retryable: true
                ));
            }
        }
    }
}
