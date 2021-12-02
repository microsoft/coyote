// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if NET || NETCOREAPP3_1
using System.Net.Http;
using System.Runtime.CompilerServices;

namespace Microsoft.Coyote.Rewriting.Types
{
    /// <summary>
    /// Provides methods for controlling <see cref="HttpClient"/> during testing.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class ControlledHttpClient
    {
        /// <summary>
        /// Creates a new instance of the <see cref="HttpClient"/> class that is controlled during testing.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HttpClient Create() =>
          new HttpClient(ControlledHttpMessageHandler.CreateWithDefaultHandler());

        /// <summary>
        /// Creates a new instance of the <see cref="HttpClient"/> class that is controlled during testing.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HttpClient Create(HttpMessageHandler handler) =>
          new HttpClient(ControlledHttpMessageHandler.Create(handler));

        /// <summary>
        /// Creates a new instance of the <see cref="HttpClient"/> class that is controlled during testing.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HttpClient Create(HttpMessageHandler handler, bool disposeHandler) =>
          new HttpClient(ControlledHttpMessageHandler.Create(handler), disposeHandler);
    }
}
#endif
