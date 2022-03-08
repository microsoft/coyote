// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if NET || NETCOREAPP3_1
using Microsoft.AspNetCore.Builder;

namespace Microsoft.Coyote.Rewriting.Types.Web
{
    /// <summary>
    /// Middleware for controlling a web application during testing.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class RequestControllerMiddlewareExtensions
    {
        /// <summary>
        /// Adds the request control middleware to the specified builder.
        /// </summary>
        public static IApplicationBuilder UseRequestController(this IApplicationBuilder builder) =>
            builder.UseMiddleware<RequestControllerMiddleware>();
    }
}
#endif
