// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if NET || NETCOREAPP3_1
using System;
using System.Reflection;
using SystemHttpClient = System.Net.Http.HttpClient;
using SystemHttpMessageHandler = System.Net.Http.HttpMessageHandler;
using SystemHttpMessageInvoker = System.Net.Http.HttpMessageInvoker;

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
        public static SystemHttpClient Create() =>
            new SystemHttpClient(HttpMessageHandler.CreateWithDefaultHandler());

        /// <summary>
        /// Creates a new instance of the HTTP client class that is controlled during testing.
        /// </summary>
        public static SystemHttpClient Create(SystemHttpMessageHandler handler) =>
            new SystemHttpClient(HttpMessageHandler.Create(handler));

        /// <summary>
        /// Creates a new instance of the HTTP client class that is controlled during testing.
        /// </summary>
        public static SystemHttpClient Create(SystemHttpMessageHandler handler, bool disposeHandler) =>
            new SystemHttpClient(HttpMessageHandler.Create(handler), disposeHandler);

        /// <summary>
        /// Injects logic that takes control of the specified http client.
        /// </summary>
        public static SystemHttpClient Control(SystemHttpClient client)
        {
            Type baseType = client.GetType().BaseType;
            if (baseType.FullName != typeof(SystemHttpMessageInvoker).FullName)
            {
                return client;
            }

            // If the client is already disposed, do nothing.
            var disposedField = baseType.GetField("_disposed", BindingFlags.NonPublic | BindingFlags.Instance);
            if ((bool)disposedField?.GetValue(client))
            {
                return client;
            }

            // Access the message handler and other properties through reflection.
            var handlerField = baseType.GetField("_handler", BindingFlags.NonPublic | BindingFlags.Instance);
            var handler = (SystemHttpMessageHandler)handlerField?.GetValue(client);
            var disposeHandlerField = baseType.GetField("_disposeHandler", BindingFlags.NonPublic | BindingFlags.Instance);
            var disposeHandler = (bool)disposeHandlerField?.GetValue(client);

            // Create the controlled client and set the same properties.
            var controlledClient = new SystemHttpClient(HttpMessageHandler.Create(handler), disposeHandler);
            controlledClient.BaseAddress = client.BaseAddress;
#if NET
            controlledClient.DefaultVersionPolicy = client.DefaultVersionPolicy;
#endif
            controlledClient.DefaultRequestVersion = client.DefaultRequestVersion;
            controlledClient.MaxResponseContentBufferSize = client.MaxResponseContentBufferSize;
            controlledClient.Timeout = client.Timeout;
            return controlledClient;
        }
    }
}
#endif
