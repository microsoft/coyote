// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using PetImages.Contracts;
using PetImages.Controllers;
using PetImages.Messaging;
using PetImages.Storage;
using PetImages.Tests.Exceptions;

#pragma warning disable SA1005
namespace PetImages.Tests
{
    internal class ServiceClient : IClient
    {
        private readonly HttpClient Client;

        internal ServiceClient(ServiceFactory factory)
        {
            this.Client = factory.CreateClient(new WebApplicationFactoryClientOptions()
            {
                AllowAutoRedirect = false,
                HandleCookies = false
            });
        }

        public async Task<HttpStatusCode> CreateAccountAsync(Account account)
        {
            var response = await this.Client.PostAsync(new Uri($"/api/account/create", UriKind.RelativeOrAbsolute),
                JsonContent.Create(account));
            return response.StatusCode;
        }

        public async Task<HttpStatusCode> CreateImageAsync(string accountName, Image image)
        {
            var response = await this.Client.PostAsync(new Uri($"/api/image/create/{accountName}",
                UriKind.RelativeOrAbsolute), JsonContent.Create(image));
            return response.StatusCode;
        }

        public async Task<(HttpStatusCode, Image)> CreateOrUpdateImageAsync(string accountName, Image image)
        {
            var response = await this.Client.PostAsync(new Uri($"/api/image/update/{accountName}",
                UriKind.RelativeOrAbsolute), JsonContent.Create(image));
            var stream = await response.Content.ReadAsStreamAsync();
            Image content = response.StatusCode == HttpStatusCode.OK ?
                await JsonSerializer.DeserializeAsync<Image>(stream) : null;
            return (response.StatusCode, content);
        }

        public async Task<(HttpStatusCode, byte[])> GetImageAsync(string accountName, string imageName)
        {
            var response = await this.Client.GetAsync(new Uri($"/api/image/contents/{accountName}/{imageName}",
                UriKind.RelativeOrAbsolute));
            var stream = await response.Content.ReadAsStreamAsync();
            byte[] content = response.StatusCode == HttpStatusCode.OK ?
                await JsonSerializer.DeserializeAsync<byte[]>(stream) : Array.Empty<byte>();
            return (response.StatusCode, content);
        }

        public async Task<(HttpStatusCode, byte[])> GetImageThumbnailAsync(string accountName, string imageName)
        {
            var response = await this.Client.GetAsync(new Uri($"/api/image/thumbnail/{accountName}/{imageName}",
                UriKind.RelativeOrAbsolute));
            var stream = await response.Content.ReadAsStreamAsync();
            byte[] content = response.StatusCode == HttpStatusCode.OK ?
                await JsonSerializer.DeserializeAsync<byte[]>(stream) : Array.Empty<byte>();
            return (response.StatusCode, content);
        }

        public void Dispose()
        {
            this.Client.Dispose();
        }
    }
}
