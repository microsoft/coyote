// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Threading.Tasks;
using PetImages.Contracts;

#pragma warning disable SA1005
namespace PetImages
{
    public interface IClient : IDisposable
    {
        Task<HttpStatusCode> CreateAccountAsync(Account account);

        Task<HttpStatusCode> CreateImageAsync(string accountName, Image image);

        Task<(HttpStatusCode, Image)> CreateOrUpdateImageAsync(string accountName, Image image);

        Task<(HttpStatusCode, byte[])> GetImageAsync(string accountName, string imageName);

        Task<(HttpStatusCode, byte[])> GetImageThumbnailAsync(string accountName, string imageName);
    }
}
