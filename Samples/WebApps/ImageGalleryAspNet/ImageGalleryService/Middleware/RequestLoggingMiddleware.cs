// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using ImageGallery.Logging;
using Microsoft.AspNetCore.Http;

namespace ImageGallery.Middleware
{
    /// <summary>
    /// This middleware associates each request with a unique id stored in an async local so that
    /// it can be easily retrieved during the request async call stack for logging purposes.
    /// </summary>
    public class RequestLoggingMiddleware
    {
        public static async Task InjectYieldsAtMethodStart()
        {
            string envYiledLoop = Environment.GetEnvironmentVariable("YIELDS_METHOD_START");
            int envYiledLoopInt = 0;
            if (envYiledLoop != null)
            {
#pragma warning disable CA1305 // Specify IFormatProvider
                envYiledLoopInt = int.Parse(envYiledLoop);
#pragma warning restore CA1305 // Specify IFormatProvider
            }

            for (int i = 0; i < envYiledLoopInt; i++)
            {
                await Task.Yield();
            }
        }

        private readonly RequestDelegate NextRequest;

        public RequestLoggingMiddleware(RequestDelegate next)
        {
            this.NextRequest = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            await InjectYieldsAtMethodStart();
            RequestId.Create(httpContext.TraceIdentifier);
            await NextRequest(httpContext);
        }
    }
}
