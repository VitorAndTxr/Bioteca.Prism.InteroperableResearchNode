using Bioteca.Prism.Domain.Errors.Node;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Nodes;

namespace Bioteca.Prism.Core.Middleware.Channel
{
    //[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    //public class PrismChannelConnectionAttribute : Attribute, IAsyncActionFilter
    //{
    //    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    //    {
    //        if (!context.HttpContext.Request.Headers.TryGetValue("X-Channel-Id", out var channelIdHeader))
    //        {
    //            context.Result = new BadRequestObjectResult(new HandshakeError
    //            {
    //                Error = new ErrorDetails
    //                {
    //                    Code = "ERR_MISSING_CHANNEL_ID",
    //                    Message = "X-Channel-Id header is required",
    //                    Retryable = false
    //                }
    //            });
    //            return;
    //        }

    //        var channelId = channelIdHeader.ToString();
    //        var channelStore = context.HttpContext.RequestServices.GetRequiredService<IChannelStore>();
    //        var channelContext = channelStore.GetChannel(channelId);

    //        if (channelContext == null)
    //        {
    //            context.Result = new BadRequestObjectResult(new HandshakeError
    //            {
    //                Error = new ErrorDetails
    //                {
    //                    Code = "ERR_INVALID_CHANNEL",
    //                    Message = "Channel does not exist or has expired",
    //                    Retryable = true
    //                }
    //            });
    //            return;
    //        }

    //        context.HttpContext.Items["ChannelId"] = channelId;
    //        context.HttpContext.Items["ChannelContext"] = channelContext;

    //        await next();
    //    }
    //}
}
