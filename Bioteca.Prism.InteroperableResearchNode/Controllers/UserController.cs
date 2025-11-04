using Bioteca.Prism.Core.Controllers;
using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Core.Middleware.Channel;
using Bioteca.Prism.Core.Security.Authorization;
using Bioteca.Prism.Domain.Payloads.User;
using Bioteca.Prism.Domain.Requests.Session;
using Bioteca.Prism.InteroperableResearchNode.Middleware;
using Bioteca.Prism.Service.Interfaces.Researcher;
using Bioteca.Prism.Service.Interfaces.User;
using Bioteca.Prism.Service.Services.User;
using Microsoft.AspNetCore.Mvc;

namespace Bioteca.Prism.InteroperableResearchNode.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : BaseController
    {

        private readonly IUserService _userService;

        private readonly ILogger<UserController> _logger;

        public UserController(

            IUserService userService,
            ILogger<UserController> logger,
            IConfiguration configuration,
            IApiContext apiContext
            ) : base(logger, configuration, apiContext)
        {
            _logger = logger;
            _userService = userService;
        }

        /// <summary>
        /// Get paginated list of users
        /// </summary>
        /// <param name="page">Page number (1-indexed, default: 1)</param>
        /// <param name="pageSize">Number of items per page (default: 10, max: 100)</param>
        /// <param name="apiContext">API context injected by framework</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Paginated list of users</returns>
        [Route("[action]")]
        [HttpGet]
        [PrismEncryptedChannelConnection]
        [PrismAuthenticatedSession]
        [Authorize("sub")]
        [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUsers(CancellationToken cancellationToken = default)
        {
            try
            {
                return ServiceInvoke(_userService.GetAllUserPaginateAsync).Result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve paginated users");
                return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                    "ERR_USER_RETRIEVAL_FAILED",
                    "Failed to retrieve users",
                    new Dictionary<string, object> { ["reason"] = "internal_error" },
                    retryable: true
                ));
            }
        }

        [Route("[action]")]
        [HttpPost]
        [PrismEncryptedChannelConnection<AddUserPayload>]
        [PrismAuthenticatedSession]
        [Authorize("sub")]
        [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult New()
        {
            try
            {
                var payload = HttpContext.Items["DecryptedRequest"] as AddUserPayload;
                return ServiceInvoke(_userService.AddAsync, payload).Result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register new user");
                return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                    "ERR_USER_REGISTRATION_FAILED",
                    "Failed to register new user:" + ex.Message,
                    new Dictionary<string, object> { ["reason"] = "internal_error" },
                    retryable: true
                ));
            }
        }
    }

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
            ):base(logger, configuration, apiContext)
        {
            
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
                _logger.LogError(ex, "Failed to retrieve paginated users");
                return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                    "ERR_USER_RETRIEVAL_FAILED",
                    "Failed to retrieve users",
                    new Dictionary<string, object> { ["reason"] = "internal_error" },
                    retryable: true
                ));
            }
        }

        [Route("[action]")]
        [HttpPost]
        [PrismEncryptedChannelConnection<AddUserPayload>]
        [PrismAuthenticatedSession]
        [Authorize("sub")]
        [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult New()
        {
            try
            {
                var payload = HttpContext.Items["DecryptedRequest"] as AddUserPayload;
                return ServiceInvoke(_researcherService.AddAsync, payload).Result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register new user");
                return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                    "ERR_USER_REGISTRATION_FAILED",
                    "Failed to register new user:" + ex.Message,
                    new Dictionary<string, object> { ["reason"] = "internal_error" },
                    retryable: true
                ));
            }
        }
    }
}
