using Bioteca.Prism.Core.Controllers;
using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Core.Middleware.Channel;
using Bioteca.Prism.Core.Security.Authorization;
using Bioteca.Prism.Domain.Payloads.Sensor;
using Bioteca.Prism.InteroperableResearchNode.Middleware;
using Bioteca.Prism.Service.Interfaces.Sensor;
using Microsoft.AspNetCore.Mvc;

namespace Bioteca.Prism.InteroperableResearchNode.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SensorController : BaseController
    {
        private readonly ISensorService _sensorService;
        private readonly ILogger<SensorController> _logger;

        public SensorController(
            ISensorService sensorService,
            ILogger<SensorController> logger,
            IConfiguration configuration,
            IApiContext apiContext
        ) : base(logger, configuration, apiContext)
        {
            _sensorService = sensorService;
            _logger = logger;
        }

        [Route("[action]")]
        [HttpPost]
        [PrismEncryptedChannelConnection<AddSensorPayload>]
        [PrismAuthenticatedSession]
        [Authorize("sub")]
        [ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> New()
        {
            try
            {
                var payload = HttpContext.Items["DecryptedRequest"] as AddSensorPayload;
                var result = await _sensorService.AddSensorAsync(payload);
                return await ServiceInvoke(() => Task.FromResult(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create new sensor");
                return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                    "ERR_SENSOR_CREATION_FAILED",
                    "Failed to create new sensor: " + ex.Message,
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
        public async Task<IActionResult> GetSensorById(Guid id)
        {
            try
            {
                var sensor = await _sensorService.GetByIdAsync(id);

                if (sensor == null)
                {
                    return NotFound(CreateError(
                        "ERR_SENSOR_NOT_FOUND",
                        $"Sensor with ID {id} not found",
                        new Dictionary<string, object> { ["sensorId"] = id },
                        retryable: false
                    ));
                }

                return await ServiceInvoke(() => Task.FromResult(sensor));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve sensor by ID {SensorId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, CreateError(
                    "ERR_SENSOR_RETRIEVAL_FAILED",
                    "Failed to retrieve sensor",
                    new Dictionary<string, object> { ["reason"] = "internal_error" },
                    retryable: true
                ));
            }
        }
    }
}
