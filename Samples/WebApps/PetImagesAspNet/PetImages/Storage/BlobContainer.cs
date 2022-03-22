// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;

namespace PetImages.Storage
{
    internal class BlobContainer : IBlobContainer
    {
        public Task CreateContainerAsync(string containerName) =>
            throw new NotImplementedException();

        public Task CreateContainerIfNotExistsAsync(string containerName) =>
            throw new NotImplementedException();

        public Task DeleteContainerAsync(string containerName) =>
            throw new NotImplementedException();

        public Task<bool> DeleteContainerIfExistsAsync(string containerName) =>
            throw new NotImplementedException();

        public Task CreateOrUpdateBlobAsync(string containerName, string blobName, byte[] blobContents) =>
            throw new NotImplementedException();

        public Task<byte[]> GetBlobAsync(string containerName, string blobName) =>
            throw new NotImplementedException();

        public Task<bool> ExistsBlobAsync(string containerName, string blobName) =>
            throw new NotImplementedException();

        public Task DeleteBlobAsync(string containerName, string blobName) =>
            throw new NotImplementedException();

        public Task<bool> DeleteBlobIfExistsAsync(string containerName, string blobName) =>
            throw new NotImplementedException();
    }
}
