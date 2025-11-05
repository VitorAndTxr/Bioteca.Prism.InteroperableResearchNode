using Bioteca.Prism.Core.Controllers;
using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Core.Middleware.Channel;
using Bioteca.Prism.Core.Security.Authorization;
using Bioteca.Prism.Domain.DTOs.Snomed;
using Bioteca.Prism.InteroperableResearchNode.Middleware;
using Bioteca.Prism.Service.Interfaces.Snomed;
using Microsoft.AspNetCore.Mvc;

namespace Bioteca.Prism.InteroperableResearchNode.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SNOMEDController : BaseController
    {
        private readonly ISnomedBodyRegionService _snomedBodyRegionServiceice;
        private readonly ISnomedBodyStructureService _snomedBodyStructure;
        private readonly ISnomedLateralityService _snomedLateralityService;
        private readonly ISnomedTopographicalModifierService _snomedTopographicalModifierService;

        private readonly ILogger<ResearcherController> _logger;

        public SNOMEDController(
                ISnomedBodyRegionService snomedBodyRegionService,
                ISnomedBodyStructureService snomedBodyStructure,
                ISnomedLateralityService snomedLateralityService,
                ISnomedTopographicalModifierService snomedTopographicalModifierService,
                ILogger<ResearcherController> logger,
                IConfiguration configuration,
                IApiContext apiContext
            ) : base(logger, configuration, apiContext)
        {
            _snomedBodyRegionServiceice = snomedBodyRegionService;
            _snomedBodyStructure = snomedBodyStructure;
            _snomedLateralityService = snomedLateralityService;
            _snomedTopographicalModifierService = snomedTopographicalModifierService;
            _logger = logger;
        }

        [Route("BodyRegion/[action]")]
        [HttpGet]
        [PrismEncryptedChannelConnection]
        [PrismAuthenticatedSession]
        [Authorize("sub")]
        [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetActiveBodyRegions(CancellationToken cancellationToken = default)
        {
            try
            {
                return ServiceInvoke(_snomedBodyRegionServiceice.GetActiveAsync).Result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve active body regions");
                return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                    "ERR_SNOMED_BODY_REGION_RETRIEVAL_FAILED",
                    "Failed to retrieve active body regions",
                    new Dictionary<string, object> { ["reason"] = "internal_error" },
                    retryable: true
                ));
            }

        }

        [Route("BodyRegion/[action]")]
        [HttpGet]
        [PrismEncryptedChannelConnection]
        [PrismAuthenticatedSession]
        [Authorize("sub")]
        [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllBodyRegionsPaginateAsync()
        {
            try
            {
                return ServiceInvoke(_snomedBodyRegionServiceice.GetAllBodyRegionsPaginateAsync).Result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve paginated body regions");
                return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                    "ERR_SNOMED_BODY_REGION_PAGINATED_RETRIEVAL_FAILED",
                    "Failed to retrieve paginated body regions",
                    new Dictionary<string, object> { ["reason"] = "internal_error" },
                    retryable: true
                ));
            }
        }

        [Route("BodyRegion/[action]")]
        [HttpPost]
        [PrismEncryptedChannelConnection<AddSnomedBodyRegionDTO>]
        [PrismAuthenticatedSession]
        [Authorize("sub")]
        [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult New()
        {
            try
            {
                var payload = HttpContext.Items["DecryptedRequest"] as AddSnomedBodyRegionDTO;
                return ServiceInvoke(_snomedBodyRegionServiceice.AddAsync, payload).Result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register new body region");
                return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                    "ERR_SNOMED_BODY_REGION_REGISTRATION_FAILED",
                    "Failed to register new body region:" + ex.Message,
                    new Dictionary<string, object> { ["reason"] = "internal_error" },
                    retryable: true
                ));
            }
        }
    }
}
