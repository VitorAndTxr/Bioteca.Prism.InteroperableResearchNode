using Bioteca.Prism.Core.Controllers;
using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Core.Middleware.Channel;
using Bioteca.Prism.Core.Security.Authorization;
using Bioteca.Prism.Domain.DTOs.Snomed;
using Bioteca.Prism.InteroperableResearchNode.Middleware;
using Bioteca.Prism.Service.Interfaces.Clinical;
using Bioteca.Prism.Service.Interfaces.Snomed;
using Microsoft.AspNetCore.Mvc;

namespace Bioteca.Prism.InteroperableResearchNode.Controllers;

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
    public async Task<IActionResult> GetAllBodyRegionsPaginatedAsync()
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

    [Route("BodyRegion/{snomedCode}")]
    [HttpGet]
    [PrismEncryptedChannelConnection]
    [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetBodyRegionBySnomedCode(string snomedCode, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _snomedBodyRegionServiceice.GetBySnomedCodeAsync(snomedCode);

            if (result == null)
            {
                return NotFound(CreateError(
                    "ERR_SNOMED_BODY_REGION_NOT_FOUND",
                    $"Body region with SNOMED code '{snomedCode}' not found",
                    new Dictionary<string, object> { ["snomedCode"] = snomedCode },
                    retryable: false
                ));
            }

            return await ServiceInvoke(() => Task.FromResult(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve body region by SNOMED code: {SnomedCode}", snomedCode);
            return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                "ERR_SNOMED_BODY_REGION_RETRIEVAL_FAILED",
                "Failed to retrieve body region",
                new Dictionary<string, object> { ["reason"] = "internal_error" },
                retryable: true
            ));
        }
    }

    [Route("BodyRegion/Update/{snomedCode}")]
    [HttpPut]
    [PrismEncryptedChannelConnection<UpdateSnomedBodyRegionDTO>]
    [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateBodyRegion(string snomedCode, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = HttpContext.Items["DecryptedRequest"] as UpdateSnomedBodyRegionDTO;

            if (payload == null)
            {
                return BadRequest(CreateError(
                    "ERR_INVALID_PAYLOAD",
                    "Invalid or missing request payload",
                    new Dictionary<string, object> { ["reason"] = "payload_missing" },
                    retryable: false
                ));
            }

            var result = await _snomedBodyRegionServiceice.UpdateBySnomedCodeAsync(snomedCode, payload);

            if (result == null)
            {
                return NotFound(CreateError(
                    "ERR_SNOMED_BODY_REGION_NOT_FOUND",
                    $"Body region with SNOMED code '{snomedCode}' not found",
                    new Dictionary<string, object> { ["snomedCode"] = snomedCode },
                    retryable: false
                ));
            }

            return await ServiceInvoke(() => Task.FromResult(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update body region: {SnomedCode}", snomedCode);
            return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                "ERR_SNOMED_BODY_REGION_UPDATE_FAILED",
                "Failed to update body region: " + ex.Message,
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
    public async Task<IActionResult> GetAllBodyStructuresPaginatedAsync()
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

    [Route("BodyStructure/{snomedCode}")]
    [HttpGet]
    [PrismEncryptedChannelConnection]
    [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetBodyStructureBySnomedCode(string snomedCode, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _snomedBodyStructure.GetBySnomedCodeAsync(snomedCode);

            if (result == null)
            {
                return NotFound(CreateError(
                    "ERR_SNOMED_BODY_STRUCTURE_NOT_FOUND",
                    $"Body structure with SNOMED code '{snomedCode}' not found",
                    new Dictionary<string, object> { ["snomedCode"] = snomedCode },
                    retryable: false
                ));
            }

            return await ServiceInvoke(() => Task.FromResult(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve body structure by SNOMED code: {SnomedCode}", snomedCode);
            return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                "ERR_SNOMED_BODY_STRUCTURE_RETRIEVAL_FAILED",
                "Failed to retrieve body structure",
                new Dictionary<string, object> { ["reason"] = "internal_error" },
                retryable: true
            ));
        }
    }

    [Route("BodyStructure/Update/{snomedCode}")]
    [HttpPut]
    [PrismEncryptedChannelConnection<UpdateSnomedBodyStructureDTO>]
    [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateBodyStructure(string snomedCode, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = HttpContext.Items["DecryptedRequest"] as UpdateSnomedBodyStructureDTO;

            if (payload == null)
            {
                return BadRequest(CreateError(
                    "ERR_INVALID_PAYLOAD",
                    "Invalid or missing request payload",
                    new Dictionary<string, object> { ["reason"] = "payload_missing" },
                    retryable: false
                ));
            }

            var result = await _snomedBodyStructure.UpdateBySnomedCodeAsync(snomedCode, payload);

            if (result == null)
            {
                return NotFound(CreateError(
                    "ERR_SNOMED_BODY_STRUCTURE_NOT_FOUND",
                    $"Body structure with SNOMED code '{snomedCode}' not found",
                    new Dictionary<string, object> { ["snomedCode"] = snomedCode },
                    retryable: false
                ));
            }

            return await ServiceInvoke(() => Task.FromResult(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update body structure: {SnomedCode}", snomedCode);
            return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                "ERR_SNOMED_BODY_STRUCTURE_UPDATE_FAILED",
                "Failed to update body structure: " + ex.Message,
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
    public async Task<IActionResult> GetAllClinicalConditionsPaginatedAsync()
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

    [Route("ClinicalCondition/{snomedCode}")]
    [HttpGet]
    [PrismEncryptedChannelConnection]
    [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetClinicalConditionBySnomedCode(string snomedCode, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _clinicalConditionService.GetBySnomedCodeAsync(snomedCode, cancellationToken);

            if (result == null)
            {
                return NotFound(CreateError(
                    "ERR_CLINICAL_CONDITION_NOT_FOUND",
                    $"Clinical condition with SNOMED code '{snomedCode}' not found",
                    new Dictionary<string, object> { ["snomedCode"] = snomedCode },
                    retryable: false
                ));
            }

            return await ServiceInvoke(() => Task.FromResult(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve clinical condition by SNOMED code: {SnomedCode}", snomedCode);
            return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                "ERR_CLINICAL_CONDITION_RETRIEVAL_FAILED",
                "Failed to retrieve clinical condition",
                new Dictionary<string, object> { ["reason"] = "internal_error" },
                retryable: true
            ));
        }
    }

    [Route("ClinicalCondition/Update/{snomedCode}")]
    [HttpPut]
    [PrismEncryptedChannelConnection<UpdateSnomedClinicalConditionDTO>]
    [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateClinicalCondition(string snomedCode, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = HttpContext.Items["DecryptedRequest"] as UpdateSnomedClinicalConditionDTO;

            if (payload == null)
            {
                return BadRequest(CreateError(
                    "ERR_INVALID_PAYLOAD",
                    "Invalid or missing request payload",
                    new Dictionary<string, object> { ["reason"] = "payload_missing" },
                    retryable: false
                ));
            }

            var result = await _clinicalConditionService.UpdateBySnomedCodeAsync(snomedCode, payload, cancellationToken);

            if (result == null)
            {
                return NotFound(CreateError(
                    "ERR_CLINICAL_CONDITION_NOT_FOUND",
                    $"Clinical condition with SNOMED code '{snomedCode}' not found",
                    new Dictionary<string, object> { ["snomedCode"] = snomedCode },
                    retryable: false
                ));
            }

            return await ServiceInvoke(() => Task.FromResult(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update clinical condition: {SnomedCode}", snomedCode);
            return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                "ERR_CLINICAL_CONDITION_UPDATE_FAILED",
                "Failed to update clinical condition: " + ex.Message,
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
    public async Task<IActionResult> GetAllTopographicalModifiersPaginatedAsync()
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

    [Route("TopographicalModifier/{snomedCode}")]
    [HttpGet]
    [PrismEncryptedChannelConnection]
    [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetTopographicalModifierBySnomedCode(string snomedCode, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _snomedTopographicalModifierService.GetBySnomedCodeAsync(snomedCode, cancellationToken);

            if (result == null)
            {
                return NotFound(CreateError(
                    "ERR_TOPOGRAPHICAL_MODIFIER_NOT_FOUND",
                    $"Topographical modifier with SNOMED code '{snomedCode}' not found",
                    new Dictionary<string, object> { ["snomedCode"] = snomedCode },
                    retryable: false
                ));
            }

            return await ServiceInvoke(() => Task.FromResult(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve topographical modifier by SNOMED code: {SnomedCode}", snomedCode);
            return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                "ERR_TOPOGRAPHICAL_MODIFIER_RETRIEVAL_FAILED",
                "Failed to retrieve topographical modifier",
                new Dictionary<string, object> { ["reason"] = "internal_error" },
                retryable: true
            ));
        }
    }

    [Route("TopographicalModifier/Update/{snomedCode}")]
    [HttpPut]
    [PrismEncryptedChannelConnection<UpdateSnomedTopographicalModifierDTO>]
    [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateTopographicalModifier(string snomedCode, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = HttpContext.Items["DecryptedRequest"] as UpdateSnomedTopographicalModifierDTO;

            if (payload == null)
            {
                return BadRequest(CreateError(
                    "ERR_INVALID_PAYLOAD",
                    "Invalid or missing request payload",
                    new Dictionary<string, object> { ["reason"] = "payload_missing" },
                    retryable: false
                ));
            }

            var result = await _snomedTopographicalModifierService.UpdateBySnomedCodeAsync(snomedCode, payload, cancellationToken);

            if (result == null)
            {
                return NotFound(CreateError(
                    "ERR_TOPOGRAPHICAL_MODIFIER_NOT_FOUND",
                    $"Topographical modifier with SNOMED code '{snomedCode}' not found",
                    new Dictionary<string, object> { ["snomedCode"] = snomedCode },
                    retryable: false
                ));
            }

            return await ServiceInvoke(() => Task.FromResult(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update topographical modifier: {SnomedCode}", snomedCode);
            return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                "ERR_TOPOGRAPHICAL_MODIFIER_UPDATE_FAILED",
                "Failed to update topographical modifier: " + ex.Message,
                new Dictionary<string, object> { ["reason"] = "internal_error" },
                retryable: true
            ));
        }
    }

    #endregion
}
