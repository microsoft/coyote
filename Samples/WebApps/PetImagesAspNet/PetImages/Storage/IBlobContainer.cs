// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;

namespace PetImages.Storage
{
    /// <summary>
    /// Interface of an Azure Storage blob container provider. This can be implemented
    /// for production or with a mock for (systematic) testing.
    /// </summary>
    public interface IBlobContainer
    {
        Task CreateContainerAsync(string containerName);

        Task CreateContainerIfNotExistsAsync(string containerName);

        Task DeleteContainerAsync(string containerName);

        Task<bool> DeleteContainerIfExistsAsync(string containerName);

        Task CreateOrUpdateBlobAsync(string containerName, string blobName, byte[] blobContents);

        Task<byte[]> GetBlobAsync(string containerName, string blobName);

        Task<bool> ExistsBlobAsync(string containerName, string blobName);

        Task DeleteBlobAsync(string containerName, string blobName);

        Task<bool> DeleteBlobIfExistsAsync(string containerName, string blobName);
    }
}
