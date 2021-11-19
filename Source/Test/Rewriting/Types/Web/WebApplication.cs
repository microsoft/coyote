// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing.Handlers;
using Microsoft.Coyote.Runtime;
using WebFramework = Microsoft.AspNetCore.Http;
using WebTesting = Microsoft.AspNetCore.Mvc.Testing;

namespace Microsoft.Coyote.Rewriting.Types
{
    /// <summary>
    /// Provides methods for controlling a web application during testing.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class WebApplication
    {
        /// <summary>
        /// Controls the specified request.
        /// </summary>
        public static Task ControlRequest(WebFramework.HttpContext context, WebFramework.RequestDelegate next)
        {
            WebFramework.HttpRequest request = context.Request;
            if (request.Headers.TryGetValue("ms-coyote-runtime-id", out var runtimeId))
            {
                request.Headers.Remove("ms-coyote-runtime-id");
                if (RuntimeProvider.TryGetFromId(System.Guid.Parse(runtimeId), out CoyoteRuntime runtime))
                {
                    IO.Debug.WriteLine("<CoyoteDebug> Invoking '{0} {1}' handler on runtime '{2}' from thread '{3}'.",
                        request.Method, request.Path, runtimeId, Thread.CurrentThread.ManagedThreadId);
                    return runtime.TaskFactory.StartNew(() =>
                    {
                        Task task = next.Invoke(context);
                        runtime.WaitUntilTaskCompletes(task);
                        task.GetAwaiter().GetResult();
                    },
                    default,
                    runtime.TaskFactory.CreationOptions | TaskCreationOptions.DenyChildAttach,
                    runtime.TaskFactory.Scheduler);
                }
            }

            return next.Invoke(context);
        }

        /// <summary>
        /// Creates an instance of <see cref="HttpClient"/> that automatically follows redirects and handles cookies.
        /// </summary>
        public static HttpClient CreateClient<TEntryPoint>(WebTesting.WebApplicationFactory<TEntryPoint> factory)
            where TEntryPoint : class => CreateClient(factory, factory.ClientOptions);

        /// <summary>
        /// Creates an instance of <see cref="HttpClient"/> that automatically follows redirects and handles cookies.
        /// </summary>
        public static HttpClient CreateClient<TEntryPoint>(WebTesting.WebApplicationFactory<TEntryPoint> factory,
            WebTesting.WebApplicationFactoryClientOptions options)
            where TEntryPoint : class =>
            CreateDefaultClient(factory, options.BaseAddress, CreateHandlers(options).ToArray());

        /// <summary>
        /// Creates a new instance of an <see cref="HttpClient"/> that can be used to send <see cref="HttpRequestMessage"/>
        /// to the server. The base address of the <see cref="HttpClient"/> instance will be set to http://localhost.
        /// </summary>
        public static HttpClient CreateDefaultClient<TEntryPoint>(WebTesting.WebApplicationFactory<TEntryPoint> factory,
            params DelegatingHandler[] handlers)
            where TEntryPoint : class
        {
            var delegatingHandlers = handlers.ToList();
            delegatingHandlers.Insert(0, ControlledHttpMessageHandler.Create());
            return factory.CreateDefaultClient(delegatingHandlers.ToArray());
        }

        /// <summary>
        /// Creates a new instance of an <see cref="HttpClient"/> that can be used to
        /// send <see cref="HttpRequestMessage"/> to the server.
        /// </summary>
        public static HttpClient CreateDefaultClient<TEntryPoint>(WebTesting.WebApplicationFactory<TEntryPoint> factory,
            Uri baseAddress, params DelegatingHandler[] handlers)
            where TEntryPoint : class
        {
            var client = CreateDefaultClient(factory, handlers);
            client.BaseAddress = baseAddress;
            return client;
        }

        private static IEnumerable<DelegatingHandler> CreateHandlers(WebTesting.WebApplicationFactoryClientOptions options)
        {
            if (options.AllowAutoRedirect)
            {
                yield return new RedirectHandler(options.MaxAutomaticRedirections);
            }

            if (options.HandleCookies)
            {
                yield return new CookieContainerHandler();
            }
        }
    }
}
