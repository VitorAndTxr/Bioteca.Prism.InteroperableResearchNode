using Bioteca.Prism.Core.Controllers;
using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Core.Middleware.Channel;
using Bioteca.Prism.Core.Security.Authorization;
using Bioteca.Prism.Domain.DTOs.User;
using Bioteca.Prism.Domain.Payloads.User;
using Bioteca.Prism.InteroperableResearchNode.Middleware;
using Bioteca.Prism.Service.Interfaces.User;
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
                    "ERR_GET_USERR_FAILED",
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
                    "ERR_USER_ADD_FAILED",
                    "Failed to add new user:" + ex.Message,
                    new Dictionary<string, object> { ["reason"] = "internal_error" },
                    retryable: true
                ));
            }
        }

        /// <summary>
        /// Get user by ID
        /// </summary>
        /// <param name="id">User ID (GUID)</param>
        /// <returns>User entity with Researcher navigation property if exists</returns>
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
                var user = await _userService.GetUserByIdAsync(id);

                if (user == null)
                {
                    return NotFound(CreateError(
                        "ERR_USER_NOT_FOUND",
                        $"User with ID {id} not found",
                        new Dictionary<string, object> { ["userId"] = id },
                        retryable: false
                    ));
                }

                return await ServiceInvoke(() => Task.FromResult(user));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve user {UserId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                    "ERR_USER_RETRIEVAL_FAILED",
                    "Failed to retrieve user",
                    new Dictionary<string, object> { ["reason"] = "internal_error" },
                    retryable: true
                ));
            }
        }

        /// <summary>
        /// Update existing user
        /// </summary>
        /// <param name="id">User ID (GUID)</param>
        /// <returns>Updated User entity</returns>
        [HttpPut("Update/{id:guid}")]
        [PrismEncryptedChannelConnection<UpdateUserPayload>]
        [PrismAuthenticatedSession]
        [Authorize("sub")]
        [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Update(Guid id)
        {
            try
            {
                var payload = HttpContext.Items["DecryptedRequest"] as UpdateUserPayload;

                var updatedUser = await _userService.UpdateUserAsync(id, payload);

                if (updatedUser == null)
                {
                    return NotFound(CreateError(
                        "ERR_USER_NOT_FOUND",
                        $"User with ID {id} not found",
                        new Dictionary<string, object> { ["userId"] = id },
                        retryable: false
                    ));
                }

                return await ServiceInvoke(() => Task.FromResult(updatedUser));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update user {UserId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                    "ERR_USER_UPDATE_FAILED",
                    "Failed to update user: " + ex.Message,
                    new Dictionary<string, object> { ["reason"] = "internal_error" },
                    retryable: true
                ));
            }
        }
    }
}
