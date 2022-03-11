// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if NET || NETCOREAPP3_1
using SystemCancellationToken = System.Threading.CancellationToken;
using SystemDelegatingHandler = System.Net.Http.DelegatingHandler;
using SystemHttpClientHandler = System.Net.Http.HttpClientHandler;
using SystemHttpMessageHandler = System.Net.Http.HttpMessageHandler;
using SystemHttpRequestMessage = System.Net.Http.HttpRequestMessage;
using SystemHttpResponseMessage = System.Net.Http.HttpResponseMessage;
using SystemTasks = System.Threading.Tasks;

namespace Microsoft.Coyote.Rewriting.Types.Net.Http
{
    /// <summary>
    /// Provides methods for controlling an HTTP client during testing.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public sealed class HttpMessageHandler : SystemDelegatingHandler
    {
        private HttpMessageHandler()
            : base()
        {
        }

        private HttpMessageHandler(SystemHttpMessageHandler innerHandler)
            : base(innerHandler)
        {
        }

        /// <summary>
        /// Creates a new delegating handler instance that is controlled during testing.
        /// </summary>
        public static SystemDelegatingHandler Create() => new HttpMessageHandler();

        /// <summary>
        /// Creates a new delegating handler instance that is controlled during testing.
        /// </summary>
        public static SystemDelegatingHandler Create(SystemHttpMessageHandler innerHandler) =>
            new HttpMessageHandler(innerHandler);

        /// <summary>
        /// Creates a new delegating handler instance that is controlled during testing.
        /// </summary>
        public static SystemDelegatingHandler CreateWithDefaultHandler() =>
            new HttpMessageHandler(new SystemHttpClientHandler());

#if NET
        /// <summary>
        /// Creates an instance of an HTTP response message based on the information
        /// provided in the HTTP request message.
        /// </summary>
        protected override SystemHttpResponseMessage Send(SystemHttpRequestMessage request,
            SystemCancellationToken cancellationToken) =>
            base.Send(HttpRequestMessage.WithRuntimeHeaders(request), cancellationToken);
#endif

        /// <summary>
        /// Creates an instance of an HTTP response message based on the information
        /// provided in the HTTP request message as an operation that will not block.
        /// </summary>
        protected override SystemTasks.Task<SystemHttpResponseMessage> SendAsync(SystemHttpRequestMessage request,
            SystemCancellationToken cancellationToken) =>
            base.SendAsync(HttpRequestMessage.WithRuntimeHeaders(request), cancellationToken);
    }
}
#endif
