using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Core.Middleware.Channel;
using Bioteca.Prism.Core.Middleware.Session;
using Bioteca.Prism.Core.Security.Authorization;
using Bioteca.Prism.Domain.Payloads;
using Bioteca.Prism.Domain.Requests.Session;
using Bioteca.Prism.Domain.Responses;
using Bioteca.Prism.InteroperableResearchNode.Middleware;
using Bioteca.Prism.Service.Interfaces.User;
using Microsoft.AspNetCore.Mvc;

namespace Bioteca.Prism.InteroperableResearchNode.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserAuthController : ControllerBase
    {
        private readonly IUserAuthService _userAuthService;
        private readonly IChannelEncryptionService _encryptionService;
        public UserAuthController(
            IUserAuthService userAuthService,
            IChannelEncryptionService encryptionService
            )
        {
            _userAuthService = userAuthService;
            _encryptionService = encryptionService;
        }

        [Route("[action]")]
        [HttpPost]
        [PrismEncryptedChannelConnection<UserLoginPayload>]
        [PrismAuthenticatedSession]
        [ProducesDefaultResponseType(typeof(UserLoginResponse))]
        public IActionResult Login()
        {
            var channelContext = HttpContext.Items["ChannelContext"] as ChannelContext;
            var sessionContext = HttpContext.Items["SessionContext"] as SessionContext;
            var request = HttpContext.Items["DecryptedRequest"] as UserLoginPayload;

            var response =  _userAuthService.LoginAsync(request).Result;
            return Ok(response);  
        }

        [Route("[action]")]
        [HttpPost]
        [PrismEncryptedChannelConnection<WhoAmIRequest>]
        [PrismAuthenticatedSession]
        [Authorize("sub")]
        [ProducesDefaultResponseType(typeof(UserLoginResponse))]
        public IActionResult RefreshToken(Guid researchId)
        {
            var response = _userAuthService.RefreshTokenAsync(researchId).Result;
            return Ok(response);
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
