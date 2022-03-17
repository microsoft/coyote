// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;

namespace ImageGallery.Store.Cosmos
{
    /// <summary>
    /// Interface of an Azure Cosmos DB client provider. This can be implemented
    /// for production or with a mock for (systematic) testing.
    /// </summary>
    public interface IClientProvider
    {
        Task<IDatabaseProvider> CreateDatabaseAsync(string id);

        Task<IDatabaseProvider> CreateDatabaseIfNotExistsAsync(string id);

        IDatabaseProvider GetDatabase(string id);
    }
}
