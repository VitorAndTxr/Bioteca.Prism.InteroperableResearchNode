using Bioteca.Prism.Core.Controllers;
using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Core.Middleware.Channel;
using Bioteca.Prism.Core.Security.Authorization;
using Bioteca.Prism.Domain.Errors.Node;
using Bioteca.Prism.Domain.Payloads.User;
using Bioteca.Prism.Domain.Requests.Session;
using Bioteca.Prism.InteroperableResearchNode.Middleware;
using Bioteca.Prism.Service.Interfaces.User;
using Microsoft.AspNetCore.Mvc;

namespace Bioteca.Prism.InteroperableResearchNode.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserAuthController : BaseController
    {
        private readonly IUserAuthService _userAuthService;
        private readonly ILogger<UserAuthController> _logger;
        private readonly IConfiguration _configuration;

        public UserAuthController(
            IUserAuthService userAuthService,
            ILogger<UserAuthController> logger,
            IConfiguration configuration,
            IApiContext apiContext
            ) : base( logger, configuration, apiContext)
        {
            _userAuthService = userAuthService;
            _logger = logger;
            _configuration = configuration;
        }

        [Route("[action]")]
        [HttpPost]
        [PrismEncryptedChannelConnection<UserLoginPayload>]
        [PrismAuthenticatedSession]
        [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
        public IActionResult Login()
        {
            try
            {
                var request = HttpContext.Items["DecryptedRequest"] as UserLoginPayload;

                return ServiceInvoke(_userAuthService.LoginAsync, request).Result;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open encrypted channel");

                return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                    "ERR_AUTH_FAILED",
                    "Failed to establish user auth:" + ex.Message,
                    new Dictionary<string, object> { ["reason"] = "internal_error" },
                    retryable: true
                ));
            }
        }

        [Route("[action]")]
        [HttpPost]
        [PrismEncryptedChannelConnection<WhoAmIRequest>]
        [PrismAuthenticatedSession]
        [Authorize("sub")]
        [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
        public IActionResult RefreshToken(Guid researchId)
        {
            try
            {
                return ServiceInvoke(_userAuthService.RefreshTokenAsync, researchId).Result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open encrypted channel");

                return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                    "ERR_REFRESH_TOKEN_FAILED",
                    "Failed to refresh user auth:" + ex.Message,
                    new Dictionary<string, object> { ["reason"] = "internal_error" },
                    retryable: true
                ));
            }
        }

        [Route("[action]")]
        [HttpPost]
        [ProducesDefaultResponseType(typeof(string))]
        public IActionResult Encrypt(string text)
        {
            var response = _userAuthService.EncryptAsync(text).Result;
            return Ok(response);
        }
    }
}
