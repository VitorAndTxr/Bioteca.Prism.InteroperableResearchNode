using Bioteca.Prism.Core.Middleware.Node;
using Bioteca.Prism.Core.Security.Cryptography.Interfaces;
using Bioteca.Prism.Domain.Errors.Node;
using Bioteca.Prism.Domain.Requests.Node;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Text;

namespace Bioteca.Prism.Core.Middleware.Channel
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class PrismEncryptedChannelConnectionAttribute<T> : Attribute, IAsyncResourceFilter
    {
        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            if (!context.HttpContext.Request.Headers.TryGetValue("X-Channel-Id", out var channelIdHeader))
            {
                context.Result = new BadRequestObjectResult(new HandshakeError
                {
                    Error = new ErrorDetails
                    {
                        Code = "ERR_MISSING_CHANNEL_ID",
                        Message = "X-Channel-Id header is required",
                        Retryable = false
                    }
                });
                return;
            }

            var channelId = channelIdHeader.ToString();
            var channelStore = context.HttpContext.RequestServices.GetRequiredService<IChannelStore>();
            var channelContext = channelStore.GetChannel(channelId);

            if (channelContext == null)
            {
                context.Result = new BadRequestObjectResult(new HandshakeError
                {
                    Error = new ErrorDetails
                    {
                        Code = "ERR_INVALID_CHANNEL",
                        Message = "Channel does not exist or has expired",
                        Retryable = true
                    }
                });
                return;
            }



            var encryptionService = context.HttpContext.RequestServices.GetRequiredService<IChannelEncryptionService>();
            var nodeRegistryService = context.HttpContext.RequestServices.GetRequiredService<INodeRegistryService>();

            // Enable buffering to allow re-reading the request body
            context.HttpContext.Request.EnableBuffering();

            // Read the body
            string body;
            using (var reader = new StreamReader(
                context.HttpContext.Request.Body,
                encoding: Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                leaveOpen: true))
            {
                body = await reader.ReadToEndAsync();
                context.HttpContext.Request.Body.Position = 0;
            }

            if (string.IsNullOrWhiteSpace(body))
            {
                context.Result = new BadRequestObjectResult(new HandshakeError
                {
                    Error = new ErrorDetails
                    {
                        Code = "ERR_EMPTY_BODY",
                        Message = "Request body is empty",
                        Retryable = false
                    }
                });
                return;
            }

            EncryptedPayload encryptedRequest = JsonConvert.DeserializeObject<EncryptedPayload>(body);

            T request;

            try
            {
                request = encryptionService.DecryptPayload<T>(encryptedRequest, channelContext.SymmetricKey);

            }
            catch (Exception ex)
            {
                context.Result = new BadRequestObjectResult(new HandshakeError
                {
                    Error = new ErrorDetails
                    {
                        Code = "ERR_DECRYPTION_FAILED",
                        Message = "Failed to decrypt request payload",
                        Retryable =  false
                    }
                });
                return;
            }

            if(request.GetType() == typeof(NodeIdentifyRequest))
            {
                var signatureValid = await nodeRegistryService.VerifyNodeSignatureAsync(request as NodeIdentifyRequest);
                if (!signatureValid)
                {
                    context.Result = new BadRequestObjectResult(new HandshakeError
                    {
                        Error = new ErrorDetails
                        {
                            Code = "ERR_INVALID_SIGNATURE",
                            Message = "Node signature verification failed",
                            Retryable = false
                        }
                    });
                    return;
                }
            }

            context.HttpContext.Items["ChannelId"] = channelId;
            context.HttpContext.Items["ChannelContext"] = channelContext;
            context.HttpContext.Items["DecryptedRequest"] = request;

            await next();
        }
    }
}
