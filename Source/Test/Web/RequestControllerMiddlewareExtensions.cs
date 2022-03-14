// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if NET || NETCOREAPP3_1
using Microsoft.AspNetCore.Builder;
using Microsoft.Coyote.Rewriting.Types.Web;

namespace Microsoft.Coyote.Web
{
    /// <summary>
    /// Middleware for controlling an ASP.NET web application during testing.
    /// </summary>
    public static class RequestControllerMiddlewareExtensions
    {
        /// <summary>
        /// Adds the request controller middleware to the specified builder.
        /// </summary>
        /// <remarks>
        /// This middleware should be added in the beginning of an ASP.NET middleware
        /// pipeline that should be controlled by Coyote during testing.
        /// </remarks>
        public static IApplicationBuilder UseRequestController(this IApplicationBuilder builder) =>
            builder.UseMiddleware<RequestControllerMiddleware>();
    }
}
#endif
