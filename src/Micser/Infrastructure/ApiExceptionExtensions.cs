using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Micser.Models;

namespace Micser.Infrastructure
{
    public static class ApiExceptionExtensions
    {
        public static IApplicationBuilder UseApiExceptionHandler(this IApplicationBuilder app, ILogger logger)
        {
            return app.UseExceptionHandler(appError =>
            {
                appError.Run(async context =>
                {
                    var contextFeature = context.Features.Get<IExceptionHandlerFeature?>();

                    if (contextFeature != null)
                    {
                        logger.LogError(contextFeature.Error, contextFeature.Error.Message);

                        context.Response.StatusCode = contextFeature.Error switch
                        {
                            ApiException apiException => (int)apiException.StatusCode,
                            _ => (int)HttpStatusCode.InternalServerError,
                        };

                        var messageId = contextFeature.Error switch
                        {
                            ApiException apiException => apiException.MessageId,
                            _ => "error.unknown"
                        };

                        var result = new JsonResult(new ApiError((HttpStatusCode)context.Response.StatusCode, messageId, contextFeature.Error.Message));
                        var actionContext = new ActionContext(context, context.GetRouteData(), new ActionDescriptor());
                        await result.ExecuteResultAsync(actionContext).ConfigureAwait(false);
                    }

                    await context.Response.CompleteAsync().ConfigureAwait(false);
                });
            });
        }
    }
}