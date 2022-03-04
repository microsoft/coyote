﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Web
{
    /// <summary>
    /// Represents an HTTP operation that can be controlled during testing.
    /// </summary>
    internal class HttpOperation : ControlledOperation
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
        private HttpOperation(ulong operationId, HttpMethod method, string path, OperationGroup group)
            : base(operationId, $"{method}({operationId})", group, IsMethodReadOnly(method))
        {
            this.Method = method;
            this.Path = path;
        }

        /// <summary>
        /// Creates a new <see cref="HttpOperation"/> from the specified parameters.
        /// </summary>
        internal static HttpOperation Create(HttpMethod method, string path, CoyoteRuntime runtime,
            ControlledOperation source)
        {
            // Assign the group of the source operation, if its owner is also an HTTP operation.
            OperationGroup group = source?.Group.Owner is HttpOperation httpSource ? httpSource.Group : null;
            group = null;

            ulong operationId = runtime.GetNextOperationId();
            var op = new HttpOperation(operationId, method, path, group);
            if (op == op.Group.Owner)
            {
                op.Group.Msg = $"({method} {path})";
                if (source?.Group.Owner is HttpOperation httpSource2)
                {
                    op.Group.RootId = httpSource2.Group.RootId;
                }
                else
                {
                    op.Group.RootId = op.Group.Id;
                }
            }

            System.Console.WriteLine($"--------> Creating HTTP operation '{op}' with group {op.Group} and owner {op.Group.Owner.Name} ({op.Group.Msg} | {op.Group.RootId})");
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