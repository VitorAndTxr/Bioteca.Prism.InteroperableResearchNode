using Bioteca.Prism.Core.Controllers;
using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Core.Middleware.Channel;
using Bioteca.Prism.Core.Security.Authorization;
using Bioteca.Prism.Domain.Payloads.Device;
using Bioteca.Prism.InteroperableResearchNode.Middleware;
using Bioteca.Prism.Service.Interfaces.Device;
using Microsoft.AspNetCore.Mvc;

namespace Bioteca.Prism.InteroperableResearchNode.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DeviceController : BaseController
    {
        private readonly IDeviceService _deviceService;
        private readonly ILogger<DeviceController> _logger;

        public DeviceController(
            IDeviceService deviceService,
            ILogger<DeviceController> logger,
            IConfiguration configuration,
            IApiContext apiContext
        ) : base(logger, configuration, apiContext)
        {
            _deviceService = deviceService;
            _logger = logger;
        }

        [Route("[action]")]
        [HttpGet]
        [PrismEncryptedChannelConnection]
        [PrismAuthenticatedSession]
        [Authorize("sub")]
        [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDevices()
        {
            try
            {
                return ServiceInvoke(_deviceService.GetAllDevicesPaginateAsync).Result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve paginated devices");
                return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                    "ERR_DEVICE_RETRIEVAL_FAILED",
                    "Failed to retrieve devices",
                    new Dictionary<string, object> { ["reason"] = "internal_error" },
                    retryable: true
                ));
            }
        }

        [Route("[action]")]
        [HttpPost]
        [PrismEncryptedChannelConnection<AddDevicePayload>]
        [PrismAuthenticatedSession]
        [Authorize("sub")]
        [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> New()
        {
            try
            {
                var payload = HttpContext.Items["DecryptedRequest"] as AddDevicePayload;
                var result = await _deviceService.AddDeviceAsync(payload);
                return await ServiceInvoke(() => Task.FromResult(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create new device");
                return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                    "ERR_DEVICE_CREATION_FAILED",
                    "Failed to create new device: " + ex.Message,
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
        public async Task<IActionResult> GetDeviceById(Guid id)
        {
            try
            {
                var device = await _deviceService.GetByIdAsync(id);

                if (device == null)
                {
                    return NotFound(CreateError(
                        "ERR_DEVICE_NOT_FOUND",
                        $"Device with ID {id} not found",
                        new Dictionary<string, object> { ["deviceId"] = id },
                        retryable: false
                    ));
                }

                return await ServiceInvoke(() => Task.FromResult(device));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve device by ID {DeviceId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                    "ERR_DEVICE_RETRIEVAL_FAILED",
                    "Failed to retrieve device",
                    new Dictionary<string, object> { ["reason"] = "internal_error" },
                    retryable: true
                ));
            }
        }
    }
}
