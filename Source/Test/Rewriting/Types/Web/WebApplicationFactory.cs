// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if NET || NETCOREAPP3_1
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Coyote.Rewriting.Types.Net.Http;
using SystemDelegatingHandler = System.Net.Http.DelegatingHandler;
using SystemHttpClient = System.Net.Http.HttpClient;
using WebTesting = Microsoft.AspNetCore.Mvc.Testing;

namespace Microsoft.Coyote.Rewriting.Types.Web
{
    /// <summary>
    /// Factory for bootstrapping a controlled web application in memory during testing.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class WebApplicationFactory
    {
        /// <summary>
        /// Creates an instance of an HTTP client that automatically follows redirects and handles cookies.
        /// </summary>
        public static SystemHttpClient CreateClient<TEntryPoint>(WebTesting.WebApplicationFactory<TEntryPoint> factory)
            where TEntryPoint : class => CreateClient(factory, factory.ClientOptions);

        /// <summary>
        /// Creates an instance of an HTTP client that automatically follows redirects and handles cookies.
        /// </summary>
        public static SystemHttpClient CreateClient<TEntryPoint>(WebTesting.WebApplicationFactory<TEntryPoint> factory,
            WebTesting.WebApplicationFactoryClientOptions options)
            where TEntryPoint : class =>
            CreateDefaultClient(factory, options.BaseAddress, CreateHandlers(options).ToArray());

        /// <summary>
        /// Creates a new instance of an HTTP client that can be used to send an HTTP request message to
        /// the server. The base address of the HTTP client instance will be set to http://localhost.
        /// </summary>
        public static SystemHttpClient CreateDefaultClient<TEntryPoint>(WebTesting.WebApplicationFactory<TEntryPoint> factory,
            params SystemDelegatingHandler[] handlers)
            where TEntryPoint : class
        {
            var delegatingHandlers = handlers.ToList();
            delegatingHandlers.Insert(0, HttpMessageHandler.Create());
            return factory.CreateDefaultClient(delegatingHandlers.ToArray());
        }

        /// <summary>
        /// Creates a new instance of an HTTP client that can be used to send an HTTP request message to the server.
        /// </summary>
        public static SystemHttpClient CreateDefaultClient<TEntryPoint>(
            WebTesting.WebApplicationFactory<TEntryPoint> factory, Uri baseAddress,
            params SystemDelegatingHandler[] handlers)
            where TEntryPoint : class
        {
            var client = CreateDefaultClient(factory, handlers);
            client.BaseAddress = baseAddress;
            return client;
        }

        private static IEnumerable<SystemDelegatingHandler> CreateHandlers(
            WebTesting.WebApplicationFactoryClientOptions options)
        {
            if (options.AllowAutoRedirect)
            {
                yield return new WebTesting.Handlers.RedirectHandler(options.MaxAutomaticRedirections);
            }

            if (options.HandleCookies)
            {
                yield return new WebTesting.Handlers.CookieContainerHandler();
            }
        }
    }
}
#endif
