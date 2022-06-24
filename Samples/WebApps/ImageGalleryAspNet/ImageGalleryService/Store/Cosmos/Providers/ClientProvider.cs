// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace ImageGallery.Store.Cosmos
{
    /// <summary>
    /// Production implementation of an Azure Cosmos DB client provider.
    /// </summary>
    public class ClientProvider : IClientProvider, IDisposable
    {
        private bool IsDsposed = false;

        protected CosmosClient CosmosClient { get; }

        public ClientProvider(string connectionString)
        {
            this.CosmosClient = new CosmosClient(connectionString);
        }

        public async Task<IDatabaseProvider> CreateDatabaseAsync(string id)
        {
            var database = await this.CosmosClient.CreateDatabaseAsync(id);
            return new DatabaseProvider(database);
        }

        public async Task<IDatabaseProvider> CreateDatabaseIfNotExistsAsync(string id)
        {
            var database = await this.CosmosClient.CreateDatabaseIfNotExistsAsync(id);
            return new DatabaseProvider(database);
        }

        public IDatabaseProvider GetDatabase(string id)
        {
            var database = this.CosmosClient.GetDatabase(id);
            return new DatabaseProvider(database);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.IsDsposed)
            {
                return;
            }

            if (disposing)
            {
                this.CosmosClient.Dispose();
            }

            this.IsDsposed = true;
        }
    }
}
