// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;

namespace ImageGallery.Tests.Mocks.Clients
{
    public class ServiceResponse<T>
    {
        public HttpStatusCode? StatusCode { get; set; }

        public T Resource { get; set; }
    }
}