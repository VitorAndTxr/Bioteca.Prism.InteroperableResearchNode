using Bioteca.Prism.Core.Exceptions;
using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Domain.DTOs.Paging;
using Bioteca.Prism.Domain.Errors.Node;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;


namespace Bioteca.Prism.Core.Controllers;


public class BaseController: ControllerBase
{
    private readonly ILogger<BaseController> _logger;
    private readonly IConfiguration _configuration;
    private readonly IApiContext _apiContext;

    public BaseController(
        ILogger<BaseController> logger,
        IConfiguration configuration,
        IApiContext apiContext
        )
    {
        _logger = logger;
        _configuration = configuration;
        _apiContext = apiContext;
    }

    protected IActionResult ServiceInvoke<T, R>(Func<T, R> serviceMethod, T payload)
    {
        R serviceMethodResult;
        IActionResult httpResponseMessage;

        HandleQueryParameters();
        serviceMethodResult = serviceMethod(payload);
        httpResponseMessage = JsonResponseMessage<R>(serviceMethodResult);

        return httpResponseMessage;
    }

    // Versão assíncrona com payload
    protected async Task<IActionResult> ServiceInvoke<T, R>(Func<T, Task<R>> serviceMethod, T payload)
    {
        HandleQueryParameters();
        R serviceMethodResult = await serviceMethod(payload);
        return JsonResponseMessage<R>(serviceMethodResult);
    }

    // Versão assíncrona sem payload (para GetAllUserPaginateAsync)
    protected async Task<IActionResult> ServiceInvoke<R>(Func<Task<R>> serviceMethod)
    {
        HandleQueryParameters();
        R serviceMethodResult = await serviceMethod();
        return JsonResponseMessage<R>(serviceMethodResult);
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

    private void HandleQueryParameters()
    {
        string queryString = Request?.QueryString.ToString();
        if (string.IsNullOrEmpty(queryString))
        {
            return;
        }

        int n = 0;

        var queryDictionary = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(queryString);
        foreach (var item in queryDictionary)
        {
            string queryValue = string.Empty;
            string[] values = null;

            switch (item.Key.ToLowerInvariant())
            {
                case "page":
                    n = 0;
                    if (int.TryParse(item.Value, out n))
                    {
                        _apiContext.PagingContext.RequestPaging.Page = n;
                    }
                    if (n <= 0)
                    {
                        throw new BadRequestException("Invalid value for 'page'.");
                    }
                    break;

                case "pagesize":
                    n = 0;
                    if (int.TryParse(item.Value, out n))
                    {
                        _apiContext.PagingContext.RequestPaging.PageSize = n;
                    }
                    if (n <= 0)
                    {
                        throw new BadRequestException("Invalid value for 'pageSize'.");
                    }
                    break;
                default:
                    break;
            }
        }
    }

    protected IActionResult JsonResponseMessage<R>(R result)
    {
        object resp;

        if (_apiContext.PagingContext.IsPaginated)
        {
            PagedResult<R> apiPaginatedResponse = new PagedResult<R>();
            var contextResponsePaging = _apiContext.PagingContext.ResponsePaging;



            apiPaginatedResponse.Data = result;

            apiPaginatedResponse.PageSize = _apiContext.PagingContext.RequestPaging.PageSize;
            apiPaginatedResponse.CurrentPage = _apiContext.PagingContext.RequestPaging.Page;
            apiPaginatedResponse.TotalRecords = contextResponsePaging.TotalRecords;
            apiPaginatedResponse.TotalPages = contextResponsePaging.TotalRecords;

            resp = apiPaginatedResponse;
        }
        else
        {
            resp = result;
        }

        return Ok(resp);
    }

    protected IActionResult ServiceInvoke<R>(Func<R> serviceMethod)
    {
        R serviceMethodResult;
        IActionResult httpResponseMessage;

        HandleQueryParameters();
        serviceMethodResult = serviceMethod();
        httpResponseMessage = JsonResponseMessage(serviceMethodResult);

        return Ok(httpResponseMessage);
    }
}
