// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;

namespace ImageGallery.Store.Cosmos
{
    /// <summary>
    /// Interface of an Azure Cosmos DB database provider. This can be implemented
    /// for production or with a mock for (systematic) testing.
    /// </summary>
    public interface IDatabaseProvider
    {
        Task<IContainerProvider> CreateContainerAsync(string id, string partitionKeyPath);

        Task<IContainerProvider> CreateContainerIfNotExistsAsync(string id, string partitionKeyPath);

        IContainerProvider GetContainer(string id);

        Task DeleteAsync();
    }
}
