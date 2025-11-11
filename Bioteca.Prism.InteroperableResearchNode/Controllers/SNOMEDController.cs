using Bioteca.Prism.Core.Controllers;
using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Core.Middleware.Channel;
using Bioteca.Prism.Core.Security.Authorization;
using Bioteca.Prism.Domain.DTOs.Snomed;
using Bioteca.Prism.InteroperableResearchNode.Middleware;
using Bioteca.Prism.Service.Interfaces.Clinical;
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
        private readonly IClinicalConditionService _clinicalConditionService;

        private readonly ILogger<ResearcherController> _logger;

        public SNOMEDController(
                ISnomedBodyRegionService snomedBodyRegionService,
                ISnomedBodyStructureService snomedBodyStructure,
                ISnomedLateralityService snomedLateralityService,
                ISnomedTopographicalModifierService snomedTopographicalModifierService,
                IClinicalConditionService clinicalConditionService,
                ILogger<ResearcherController> logger,
                IConfiguration configuration,
                IApiContext apiContext
            ) : base(logger, configuration, apiContext)
        {
            _snomedBodyRegionServiceice = snomedBodyRegionService;
            _snomedBodyStructure = snomedBodyStructure;
            _snomedLateralityService = snomedLateralityService;
            _snomedTopographicalModifierService = snomedTopographicalModifierService;
            _clinicalConditionService = clinicalConditionService;
            _logger = logger;
        }


        #region BodyRegion Endpoints
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

        [Route("BodyRegion/New")]
        [HttpPost]
        [PrismEncryptedChannelConnection<AddSnomedBodyRegionDTO>]
        [PrismAuthenticatedSession]
        [Authorize("sub")]
        [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult NewBodyRegion()
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

        #endregion

        #region BodyStructure Endpoints

        [Route("BodyStructure/[action]")]
        [HttpGet]
        [PrismEncryptedChannelConnection]
        [PrismAuthenticatedSession]
        [Authorize("sub")]
        [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetActiveBodyStructures(CancellationToken cancellationToken = default)
        {
            try
            {
                return ServiceInvoke(_snomedBodyStructure.GetActiveAsync).Result;
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

        [Route("BodyStructure/[action]")]
        [HttpGet]
        [PrismEncryptedChannelConnection]
        [PrismAuthenticatedSession]
        [Authorize("sub")]
        [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllBodyStructuresPaginateAsync()
        {
            try
            {
                return ServiceInvoke(_snomedBodyStructure.GetAllBodyStructuresPaginateAsync).Result;
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

        [Route("BodyStructure/New")]
        [HttpPost]
        [PrismEncryptedChannelConnection<AddSnomedBodyStructureDTO>]
        [PrismAuthenticatedSession]
        [Authorize("sub")]
        [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult NewBodyStructure()
        {
            try
            {
                var payload = HttpContext.Items["DecryptedRequest"] as AddSnomedBodyStructureDTO;
                return ServiceInvoke(_snomedBodyStructure.AddAsync, payload).Result;
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

        #endregion

        #region ClinicalCondition Endpoints

        [Route("ClinicalCondition/[action]")]
        [HttpGet]
        [PrismEncryptedChannelConnection]
        [PrismAuthenticatedSession]
        [Authorize("sub")]
        [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetActiveClinicalConditions(CancellationToken cancellationToken = default)
        {
            try
            {
                return ServiceInvoke(_clinicalConditionService.GetActiveConditionsAsync).Result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve active clinical conditions");
                return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                    "ERR_CLINICAL_CONDITION_RETRIEVAL_FAILED",
                    "Failed to retrieve active clinical conditions",
                    new Dictionary<string, object> { ["reason"] = "internal_error" },
                    retryable: true
                ));
            }
        }

        [Route("ClinicalCondition/[action]")]
        [HttpGet]
        [PrismEncryptedChannelConnection]
        [PrismAuthenticatedSession]
        [Authorize("sub")]
        [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllClinicalConditionsPaginateAsync()
        {
            try
            {
                return ServiceInvoke(_clinicalConditionService.GetAllClinicalConditionsPaginateAsync).Result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve paginated clinical conditions");
                return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                    "ERR_CLINICAL_CONDITION_PAGINATED_RETRIEVAL_FAILED",
                    "Failed to retrieve paginated clinical conditions",
                    new Dictionary<string, object> { ["reason"] = "internal_error" },
                    retryable: true
                ));
            }
        }

        [Route("ClinicalCondition/New")]
        [HttpPost]
        [PrismEncryptedChannelConnection<SnomedClinicalConditionDTO>]
        [PrismAuthenticatedSession]
        [Authorize("sub")]
        [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult NewClinicalCondition()
        {
            try
            {
                var payload = HttpContext.Items["DecryptedRequest"] as SnomedClinicalConditionDTO;
                return ServiceInvoke(_clinicalConditionService.AddAsync, payload).Result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register new clinical condition");
                return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                    "ERR_CLINICAL_CONDITION_REGISTRATION_FAILED",
                    "Failed to register new clinical condition: " + ex.Message,
                    new Dictionary<string, object> { ["reason"] = "internal_error" },
                    retryable: true
                ));
            }
        }

        #endregion

        #region TopographicalModifier Endpoints

        [Route("TopographicalModifier/[action]")]
        [HttpGet]
        [PrismEncryptedChannelConnection]
        [PrismAuthenticatedSession]
        [Authorize("sub")]
        [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetActiveTopographicalModifiers(CancellationToken cancellationToken = default)
        {
            try
            {
                return (await ServiceInvoke(() => _snomedTopographicalModifierService.GetActiveAsync(cancellationToken)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve active topographical modifiers");
                return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                    "ERR_TOPOGRAPHICAL_MODIFIER_RETRIEVAL_FAILED",
                    "Failed to retrieve active topographical modifiers",
                    new Dictionary<string, object> { ["reason"] = "internal_error" },
                    retryable: true
                ));
            }
        }

        [Route("TopographicalModifier/[action]")]
        [HttpGet]
        [PrismEncryptedChannelConnection]
        [PrismAuthenticatedSession]
        [Authorize("sub")]
        [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllTopographicalModifiersPaginateAsync()
        {
            try
            {
                return ServiceInvoke(_snomedTopographicalModifierService.GetAllTopographicalModifiersPaginateAsync).Result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve paginated topographical modifiers");
                return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                    "ERR_TOPOGRAPHICAL_MODIFIER_PAGINATED_RETRIEVAL_FAILED",
                    "Failed to retrieve paginated topographical modifiers",
                    new Dictionary<string, object> { ["reason"] = "internal_error" },
                    retryable: true
                ));
            }
        }

        [Route("TopographicalModifier/New")]
        [HttpPost]
        [PrismEncryptedChannelConnection<SnomedTopographicalModifierDTO>]
        [PrismAuthenticatedSession]
        [Authorize("sub")]
        [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult NewTopographicalModifier()
        {
            try
            {
                var payload = HttpContext.Items["DecryptedRequest"] as SnomedTopographicalModifierDTO;
                return ServiceInvoke(_snomedTopographicalModifierService.AddAsync, payload).Result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register new topographical modifier");
                return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                    "ERR_TOPOGRAPHICAL_MODIFIER_REGISTRATION_FAILED",
                    "Failed to register new topographical modifier: " + ex.Message,
                    new Dictionary<string, object> { ["reason"] = "internal_error" },
                    retryable: true
                ));
            }
        }

        #endregion
    }
}
