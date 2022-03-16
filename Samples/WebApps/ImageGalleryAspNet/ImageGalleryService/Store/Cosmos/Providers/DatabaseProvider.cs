// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace ImageGallery.Store.Cosmos
{
    /// <summary>
    /// Production implementation of an Azure Cosmos DB database provider.
    /// </summary>
    public class DatabaseProvider : IDatabaseProvider
    {
        private readonly Database Database;

        public DatabaseProvider(Database database)
        {
            this.Database = database;
        }

        public async Task<IContainerProvider> CreateContainerAsync(string id, string partitionKeyPath)
        {
            var container = await this.Database.CreateContainerAsync(id, partitionKeyPath);
            return new ContainerProvider(container);
        }

        public async Task<IContainerProvider> CreateContainerIfNotExistsAsync(string id, string partitionKeyPath)
        {
            var container = await this.Database.CreateContainerIfNotExistsAsync(id, partitionKeyPath);
            return new ContainerProvider(container);
        }

        public IContainerProvider GetContainer(string id)
        {
            var container = this.Database.GetContainer(id);
            return new ContainerProvider(container);
        }

        public Task DeleteAsync() => this.Database.DeleteAsync();
    }
}
