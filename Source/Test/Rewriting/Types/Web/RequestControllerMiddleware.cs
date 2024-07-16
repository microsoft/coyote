// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if NET
using System;
using Microsoft.Coyote.Rewriting.Types.Net.Http;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Web;
using SystemTask = System.Threading.Tasks.Task;
using SystemTaskCreationOptions = System.Threading.Tasks.TaskCreationOptions;
using SystemThread = System.Threading.Thread;
using WebFramework = Microsoft.AspNetCore.Http;

namespace Microsoft.Coyote.Rewriting.Types.Web
{
    /// <summary>
    /// Middleware for controlling a web application during testing.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public sealed class RequestControllerMiddleware
    {
        /// <summary>
        /// Invokes the next middleware in the pipeline.
        /// </summary>
        private readonly WebFramework.RequestDelegate Next;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestControllerMiddleware"/> class.
        /// </summary>
        public RequestControllerMiddleware(WebFramework.RequestDelegate next)
        {
            this.Next = next;
        }

        /// <summary>
        /// Invokes the middleware to controls the specified request.
        /// </summary>
#pragma warning disable CA1822
        public async SystemTask InvokeAsync(WebFramework.HttpContext context)
        {
            WebFramework.HttpRequest request = context.Request;
            if (request != null && TryExtractRuntime(request, out CoyoteRuntime runtime))
            {
                runtime.LogWriter.LogDebug("[coyote::debug] Runtime '{0}' takes control of the '{1} {2}' handler on thread '{3}'.",
                    runtime.Id, request.Method, request.Path, SystemThread.CurrentThread.ManagedThreadId);
                TryExtractSourceOperation(request, runtime, out ControlledOperation source);
                var op = HttpOperation.Create(ToHttpMethod(request.Method), request.Path, runtime, source);
                await runtime.TaskFactory.StartNew(state =>
                    {
                        SystemTask task = this.Next(context);
                        TaskServices.WaitUntilTaskCompletes(runtime, op, task);
                        task.GetAwaiter().GetResult();
                    },
                    op,
                    default,
                    runtime.TaskFactory.CreationOptions | SystemTaskCreationOptions.DenyChildAttach,
                    runtime.TaskFactory.Scheduler);
            }
            else
            {
                await this.Next(context);
            }
        }

        /// <summary>
        /// Tries to return the runtime instance that has the identifier defined in the value
        /// of the <see cref="HttpRequestHeader.RuntimeId"/> header of the specified request,
        /// if there is such a header available.
        /// </summary>
        /// <remarks>
        /// The header is removed from the request after the runtime is retrieved.
        /// </remarks>
        private static bool TryExtractRuntime(WebFramework.HttpRequest request, out CoyoteRuntime runtime)
        {
            if (request.Headers.TryGetValue(HttpRequestHeader.RuntimeId, out var runtimeId) &&
                System.Guid.TryParse(runtimeId, out Guid value))
            {
                request.Headers.Remove(HttpRequestHeader.RuntimeId);
                RuntimeProvider.TryGetFromId(System.Guid.Parse(runtimeId), out runtime);
                return true;
            }

            runtime = null;
            return false;
        }

        /// <summary>
        /// Tries to return the source operation that has the identifier defined in the value
        /// of the <see cref="HttpRequestHeader.SourceOperationId"/> header of the specified
        /// request, if there is such a header available.
        /// </summary>
        /// <remarks>
        /// The header is removed from the request after the operation is retrieved.
        /// </remarks>
        private static bool TryExtractSourceOperation(WebFramework.HttpRequest request, CoyoteRuntime runtime,
            out ControlledOperation op)
        {
            if (request.Headers.TryGetValue(HttpRequestHeader.SourceOperationId, out var sourceOpId) &&
                ulong.TryParse(sourceOpId, out ulong value))
            {
                request.Headers.Remove(HttpRequestHeader.SourceOperationId);
                op = runtime.GetOperationWithId(value);
                return true;
            }

            op = null;
            return false;
        }

        /// <summary>
        /// Returns an <see cref="HttpMethod"/> from the specified string.
        /// </summary>
        private static HttpMethod ToHttpMethod(string method)
        {
            switch (method)
            {
                case "GET":
                    return HttpMethod.Get;
                case "HEAD":
                    return HttpMethod.Head;
                case "POST":
                    return HttpMethod.Post;
                case "PUT":
                    return HttpMethod.Put;
                case "DELETE":
                    return HttpMethod.Delete;
                case "CONNECT":
                    return HttpMethod.Connect;
                case "OPTIONS":
                    return HttpMethod.Options;
                case "TRACE":
                    return HttpMethod.Trace;
                case "PATCH":
                    return HttpMethod.Patch;
                default:
                    throw new ArgumentException($"Unsupported '{method}' HTTP method.");
            }
        }
    }
}
#endif
