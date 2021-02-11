using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace Micser.Infrastructure
{
    public static class SpecificSpaStaticFileMiddlewareExtensions
    {
        /// <summary>
        /// Registers the <see cref="SpecificSpaStaticFileMiddleware"/> middleware. This allows returning of 404 errors for non-existing files within the specified request paths.
        /// </summary>
        public static IApplicationBuilder UseSpecificSpaStaticFiles(this IApplicationBuilder app, Action<SpecificSpaStaticFileOptions> options)
        {
            var optionsInstance = new SpecificSpaStaticFileOptions();
            options(optionsInstance);
            app.UseMiddleware<SpecificSpaStaticFileMiddleware>(optionsInstance);
            app.UseSpaStaticFiles();
            return app;
        }
    }

    public class SpecificSpaStaticFileMiddleware
    {
        private readonly IFileProvider _fileProvider;
        private readonly RequestDelegate _next;
        private readonly SpecificSpaStaticFileOptions _options;

        public SpecificSpaStaticFileMiddleware(RequestDelegate next, IFileProvider fileProvider, SpecificSpaStaticFileOptions options)
        {
            _next = next;
            _fileProvider = fileProvider;
            _options = options;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (_options.RequestPaths.Any(sp => httpContext.Request.Path.StartsWithSegments(sp)))
            {
                var fileInfo = _fileProvider.GetFileInfo(_options.RootPath + httpContext.Request.Path);

                if (!fileInfo.Exists)
                {
                    throw new NotFoundApiException();
                }
            }

            await _next(httpContext).ConfigureAwait(false);
        }
    }

    public class SpecificSpaStaticFileOptions
    {
        public SpecificSpaStaticFileOptions()
        {
            RequestPaths = new List<PathString>();
            RootPath = "";
        }

        public List<PathString> RequestPaths { get; }

        public string RootPath { get; private set; }

        public SpecificSpaStaticFileOptions WithRequestPaths(params PathString[] requestPaths)
        {
            RequestPaths.AddRange(requestPaths);
            return this;
        }

        public SpecificSpaStaticFileOptions WithRootPath(string rootPath)
        {
            RootPath = rootPath;
            return this;
        }
    }
}