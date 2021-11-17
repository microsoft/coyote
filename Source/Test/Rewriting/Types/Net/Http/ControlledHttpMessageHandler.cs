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
        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy != SchedulingPolicy.None)
            {
                request.Headers.Add("ms-coyote-runtime-id", runtime.Id.ToString());
            }

            return base.Send(request, cancellationToken);
        }
#endif

        /// <summary>
        /// Creates an instance of <see cref="HttpResponseMessage"/> based on the information
        /// provided in the <see cref="HttpRequestMessage"/> as an operation that will not block.
        /// </summary>
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy != SchedulingPolicy.None)
            {
                System.Console.WriteLine($">>> ADDING HEADER FOR RUNTIME WITH ID {runtime.Id}");
                request.Headers.Add("ms-coyote-runtime-id", runtime.Id.ToString());
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
