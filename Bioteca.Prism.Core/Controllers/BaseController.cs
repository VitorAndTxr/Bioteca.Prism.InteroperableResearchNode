using Bioteca.Prism.Domain.Errors.Node;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Bioteca.Prism.Core.Controllers;

public class BaseController: ControllerBase
{
    private readonly ILogger<BaseController> _logger;
    private readonly IConfiguration _configuration;

    public BaseController(
        ILogger<BaseController> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }
    protected Error CreateError(
        string code,
        string message,
        Dictionary<string, object>? details = null,
        bool retryable = false,
        string? retryAfter = null)
    {
        return new Error
        {
            ErrorDetail = new ErrorDetails
            {
                Code = code,
                Message = message,
                Details = details,
                Retryable = retryable,
                RetryAfter = retryAfter
            }
        };
    }

}
