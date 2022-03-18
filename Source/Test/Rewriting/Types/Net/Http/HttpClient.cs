// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if NET || NETCOREAPP3_1
using System.Reflection;
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

        /// <summary>
        /// Injects logic that takes control of the specified http client.
        /// </summary>
        public static SystemHttpClient Control(SystemHttpClient client)
        {
            // If the client is already disposed, do nothing.
            var disposedField = client.GetType().GetField("_disposed", BindingFlags.NonPublic | BindingFlags.Instance);
            if ((bool)disposedField?.GetValue(client))
            {
                return client;
            }

            // Access the message handler and other properties through reflection.
            var handlerField = client.GetType().GetField("_handler", BindingFlags.NonPublic | BindingFlags.Instance);
            var handler = (SystemHttpMessageHandler)handlerField?.GetValue(client);
            var disposeHandlerField = client.GetType().GetField("_disposeHandler", BindingFlags.NonPublic | BindingFlags.Instance);
            var disposeHandler = (bool)disposeHandlerField?.GetValue(client);
            return new SystemHttpClient(HttpMessageHandler.Create(handler), disposeHandler);
        }
    }
}
#endif
