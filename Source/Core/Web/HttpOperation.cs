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
            : base(operationId, $"{method}HttpOp({operationId})")
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
    }
}
