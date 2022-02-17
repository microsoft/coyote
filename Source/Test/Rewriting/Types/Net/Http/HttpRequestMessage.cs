// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if NET || NETCOREAPP3_1
using System;
using System.Runtime.CompilerServices;
using Microsoft.Coyote.Runtime;
using SystemHttpMethod = System.Net.Http.HttpMethod;
using SystemHttpRequestMessage = System.Net.Http.HttpRequestMessage;
using SystemThread = System.Threading.Thread;

namespace Microsoft.Coyote.Rewriting.Types.Net.Http
{
    /// <summary>
    /// Provides methods for controlling an HTTP client during testing.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class HttpRequestMessage
    {
        /// <summary>
        /// Creates a new instance of the HTTP request message class that is controlled during testing.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemHttpRequestMessage Create() =>
          WithRuntimeHeaders(new SystemHttpRequestMessage());

        /// <summary>
        /// Creates a new instance of the HTTP request message class that is controlled during testing.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemHttpRequestMessage Create(SystemHttpMethod method, string requestUri) =>
          WithRuntimeHeaders(new SystemHttpRequestMessage(method, requestUri));

        /// <summary>
        /// Creates a new instance of the HTTP request message class that is controlled during testing.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemHttpRequestMessage Create(SystemHttpMethod method, Uri requestUri) =>
          WithRuntimeHeaders(new SystemHttpRequestMessage(method, requestUri));

        /// <summary>
        /// Returns the specified HTTP request with runtime related information assigned to its headers.
        /// </summary>
        internal static SystemHttpRequestMessage WithRuntimeHeaders(SystemHttpRequestMessage request)
        {
            if (request != null)
            {
                var runtime = CoyoteRuntime.Current;
                if (runtime.SchedulingPolicy != SchedulingPolicy.None)
                {
                    if (!request.Headers.Contains(HttpRequestHeader.RuntimeId))
                    {
                        // Assigns a header containing the identifier of the currently executing runtime.
                        string runtimeId = runtime.Id.ToString();
                        IO.Debug.WriteLine("<CoyoteDebug> Assigned runtime id '{0}' to '{1} {2}' request from thread '{3}'.",
                            runtimeId, request.Method, request.RequestUri, SystemThread.CurrentThread.ManagedThreadId);
                        request.Headers.Add(HttpRequestHeader.RuntimeId, runtimeId);
                    }

                    if (!request.Headers.Contains(HttpRequestHeader.SourceOperationId))
                    {
                        // Assigns a header containing the identifier of the currently executing operation.
                        var op = runtime.GetExecutingOperation();
                        if (op != null)
                        {
                            IO.Debug.WriteLine("<CoyoteDebug> Assigned operation id '{0}' to '{1} {2}' request from thread '{3}'.",
                                op.Id, request.Method, request.RequestUri, SystemThread.CurrentThread.ManagedThreadId);
                            request.Headers.Add(HttpRequestHeader.SourceOperationId, op.Id.ToString());
                        }
                    }
                }
            }

            return request;
        }
    }
}
#endif
