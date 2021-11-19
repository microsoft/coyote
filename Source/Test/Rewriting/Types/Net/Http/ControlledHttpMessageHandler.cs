// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Rewriting.Types
{
    /// <summary>
    /// Provides methods for controlling <see cref="HttpClient"/> during testing.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public sealed class ControlledHttpMessageHandler : DelegatingHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ControlledHttpMessageHandler"/> class.
        /// </summary>
        private ControlledHttpMessageHandler()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ControlledHttpMessageHandler"/> class.
        /// </summary>
        private ControlledHttpMessageHandler(HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
        }

        /// <summary>
        /// Creates a new <see cref="DelegatingHandler"/> instance that is controlled during testing.
        /// </summary>
        public static DelegatingHandler Create() =>
            new ControlledHttpMessageHandler();

        /// <summary>
        /// Creates a new <see cref="DelegatingHandler"/> instance that is controlled during testing.
        /// </summary>
        public static DelegatingHandler Create(HttpMessageHandler innerHandler) =>
            new ControlledHttpMessageHandler(innerHandler);

        /// <summary>
        /// Creates a new <see cref="DelegatingHandler"/> instance that is controlled during testing.
        /// </summary>
        public static DelegatingHandler CreateWithDefaultHandler() =>
            new ControlledHttpMessageHandler(new HttpClientHandler());

#if NET5_0
        /// <summary>
        /// Creates an instance of <see cref="HttpResponseMessage"/> based on the information
        /// provided in the <see cref="HttpRequestMessage"/>.
        /// </summary>
        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken) =>
            base.Send(AssignRuntimeId(request), cancellationToken);
#endif

        /// <summary>
        /// Creates an instance of <see cref="HttpResponseMessage"/> based on the information
        /// provided in the <see cref="HttpRequestMessage"/> as an operation that will not block.
        /// </summary>
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            base.SendAsync(AssignRuntimeId(request), cancellationToken);

        /// <summary>
        /// Assigns the current runtime id to the headers of the specified request.
        /// </summary>
        private static HttpRequestMessage AssignRuntimeId(HttpRequestMessage request)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy != SchedulingPolicy.None)
            {
                string runtimeId = runtime.Id.ToString();
                IO.Debug.WriteLine("<CoyoteDebug> Assigned runtime id '{0}' to '{1} {2}' request from thread '{3}'.",
                    runtimeId, request.Method, request.RequestUri, Thread.CurrentThread.ManagedThreadId);
                request.Headers.Add("ms-coyote-runtime-id", runtimeId);
            }

            return request;
        }
    }
}
