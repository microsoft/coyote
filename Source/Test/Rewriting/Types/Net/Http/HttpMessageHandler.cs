// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if NET || NETCOREAPP3_1
using Microsoft.Coyote.Runtime;

using SystemCancellationToken = System.Threading.CancellationToken;
using SystemDelegatingHandler = System.Net.Http.DelegatingHandler;
using SystemHttpClientHandler = System.Net.Http.HttpClientHandler;
using SystemHttpMessageHandler = System.Net.Http.HttpMessageHandler;
using SystemHttpRequestMessage = System.Net.Http.HttpRequestMessage;
using SystemHttpResponseMessage = System.Net.Http.HttpResponseMessage;
using SystemTasks = System.Threading.Tasks;
using SystemThread = System.Threading.Thread;

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

#if NET5_0
        /// <summary>
        /// Creates an instance of an HTTP response message based on the information
        /// provided in the HTTP request message.
        /// </summary>
        protected override SystemHttpResponseMessage Send(SystemHttpRequestMessage request, CancellationToken cancellationToken) =>
            base.Send(AssignRuntimeId(request), cancellationToken);
#endif

        /// <summary>
        /// Creates an instance of an HTTP response message based on the information
        /// provided in the HTTP request message as an operation that will not block.
        /// </summary>
        protected override SystemTasks.Task<SystemHttpResponseMessage> SendAsync(SystemHttpRequestMessage request,
            SystemCancellationToken cancellationToken) =>
            base.SendAsync(AssignRuntimeId(request), cancellationToken);

        /// <summary>
        /// Assigns the current runtime id to the headers of the specified request.
        /// </summary>
        private static SystemHttpRequestMessage AssignRuntimeId(SystemHttpRequestMessage request)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy != SchedulingPolicy.None)
            {
                string runtimeId = runtime.Id.ToString();
                IO.Debug.WriteLine("<CoyoteDebug> Assigned runtime id '{0}' to '{1} {2}' request from thread '{3}'.",
                    runtimeId, request.Method, request.RequestUri, SystemThread.CurrentThread.ManagedThreadId);
                request.Headers.Add("ms-coyote-runtime-id", runtimeId);
            }

            return request;
        }
    }
}
#endif
