// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if NET || NETCOREAPP3_1
using System.Runtime.CompilerServices;
using SystemHttpClient = System.Net.Http.HttpClient;
using SystemHttpMessageHandler = System.Net.Http.HttpMessageHandler;

namespace Microsoft.Coyote.Rewriting.Types.Net.Http
{
    /// <summary>
    /// Provides methods for controlling an HTTP client during testing.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class HttpClient
    {
        /// <summary>
        /// Creates a new instance of the HTTP client class that is controlled during testing.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemHttpClient Create() =>
          new SystemHttpClient(HttpMessageHandler.CreateWithDefaultHandler());

        /// <summary>
        /// Creates a new instance of the HTTP client class that is controlled during testing.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemHttpClient Create(SystemHttpMessageHandler handler) =>
          new SystemHttpClient(HttpMessageHandler.Create(handler));

        /// <summary>
        /// Creates a new instance of the HTTP client class that is controlled during testing.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemHttpClient Create(SystemHttpMessageHandler handler, bool disposeHandler) =>
          new SystemHttpClient(HttpMessageHandler.Create(handler), disposeHandler);
    }
}
#endif
