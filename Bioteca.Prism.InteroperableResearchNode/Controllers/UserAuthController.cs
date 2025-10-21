using Bioteca.Prism.Domain.Payloads;
using Bioteca.Prism.Domain.Responses;
using Bioteca.Prism.Service.Interfaces.User;
using Microsoft.AspNetCore.Mvc;

namespace Bioteca.Prism.InteroperableResearchNode.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserAuthController : ControllerBase
    {
        private readonly IUserAuthService _userAuthService;
        public UserAuthController(IUserAuthService userAuthService)
        {
            _userAuthService = userAuthService;
        }

        [Route("[action]")]
        [HttpPost]
        [ProducesDefaultResponseType(typeof(UserLoginResponse))]
        public IActionResult Login(UserLoginPayload userLoginPayload)
        {
            var response =  _userAuthService.LoginAsync(userLoginPayload).Result;
            return Ok(response);
        }

        [Route("[action]")]
        [HttpPost]
        [ProducesDefaultResponseType(typeof(UserLoginResponse))]
        public IActionResult RefreshToken()
        {
            var response = _userAuthService.RefreshTokenAsync().Result;
            return Ok(response);
        }
    }
}
