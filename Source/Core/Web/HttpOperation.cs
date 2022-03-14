// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Web
{
    /// <summary>
    /// Represents an HTTP operation that can be controlled during testing.
    /// </summary>
    internal sealed class HttpOperation : ControlledOperation
    {
        /// <summary>
        /// The method invoked by this HTTP operation.
        /// </summary>
        internal readonly HttpMethod Method;

        /// <summary>
        /// The path invoked by this HTTP operation.
        /// </summary>
        internal readonly string Path;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpOperation"/> class.
        /// </summary>
        private HttpOperation(ulong operationId, HttpMethod method, string path)
            : base(operationId, $"Http{method}Op({operationId})")
        {
            this.Method = method;
            this.Path = path;
        }

        /// <summary>
        /// Creates a new <see cref="HttpOperation"/> from the specified parameters.
        /// </summary>
#pragma warning disable CA1801 // Parameter not used
        internal static HttpOperation Create(HttpMethod method, string path, CoyoteRuntime runtime,
            ControlledOperation source)
#pragma warning restore CA1801 // Parameter not used
        {
            ulong operationId = runtime.GetNextOperationId();
            var op = new HttpOperation(operationId, method, path);
            runtime.RegisterOperation(op);
            if (runtime.GetExecutingOperation() is null)
            {
                op.IsSourceUncontrolled = true;
            }

            return op;
        }

        /// <summary>
        /// Returns true if this HTTP method is read-only and cannot modify
        /// shared state, else false.
        /// </summary>
        private static bool IsMethodReadOnly(HttpMethod method) =>
            method is HttpMethod.Get || method is HttpMethod.Head || method is HttpMethod.Connect ||
            method is HttpMethod.Options || method is HttpMethod.Trace;
    }
}
