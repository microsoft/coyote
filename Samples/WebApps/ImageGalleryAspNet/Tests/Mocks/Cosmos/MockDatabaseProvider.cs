// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Net;
using System.Threading.Tasks;
using ImageGallery.Logging;
using ImageGallery.Store.Cosmos;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace ImageGallery.Tests.Mocks.Cosmos
{
    /// <summary>
    /// Mock implementation of an Azure Cosmos DB database provider.
    /// </summary>
    internal class MockDatabaseProvider : IDatabaseProvider
    {
        private readonly string DatabaseName;
        private readonly MockCosmosState CosmosState;
        private readonly MockLogger Logger;

        internal MockDatabaseProvider(string databaseName, MockCosmosState cosmosState, MockLogger logger)
        {
            this.DatabaseName = databaseName;
            this.CosmosState = cosmosState;
            this.Logger = logger;
        }

        public async Task<IContainerProvider> CreateContainerAsync(string id, string partitionKeyPath)
        {
            // Used to model asynchrony in the request.
            await Task.Yield();

            this.Logger.LogInformation("Creating container '{0}' in database '{1}'.", id, this.DatabaseName);

            this.CosmosState.EnsureDatabaseExists(this.DatabaseName);

            var database = this.CosmosState.Databases[this.DatabaseName];
            if (string.IsNullOrEmpty(id))
            {
                throw MockCosmosState.CreateCosmosClientException(
                    $"The container name cannot be empty",
                    HttpStatusCode.BadRequest);
            }

            this.CosmosState.EnsureContainerDoesNotExistInDatabase(database, id);
            database[id] = new ConcurrentDictionary<PartitionKey, ConcurrentDictionary<string, object>>();
            return new MockContainerProvider(this.DatabaseName, id, this.CosmosState, this.Logger);
        }

        public async Task<IContainerProvider> CreateContainerIfNotExistsAsync(string id, string partitionKeyPath)
        {
            await Task.Yield();

            this.Logger.LogInformation("Creating container '{0}' in database '{1}' if it does not exist.", id, this.DatabaseName);

            this.CosmosState.EnsureDatabaseExists(this.DatabaseName);

            var database = this.CosmosState.Databases[this.DatabaseName];
            if (string.IsNullOrEmpty(id))
            {
                throw MockCosmosState.CreateCosmosClientException(
                    $"The container name cannot be empty",
                    HttpStatusCode.BadRequest);
            }

            if (!database.ContainsKey(id))
            {
                database[id] = new ConcurrentDictionary<PartitionKey, ConcurrentDictionary<string, object>>();
            }

            return new MockContainerProvider(this.DatabaseName, id, this.CosmosState, this.Logger);
        }

        public IContainerProvider GetContainer(string id)
        {
            this.Logger.LogInformation("Getting container '{0}' from database '{1}'.", id, this.DatabaseName);

            this.CosmosState.EnsureDatabaseExists(this.DatabaseName);
            var database = this.CosmosState.Databases[this.DatabaseName];
            this.CosmosState.EnsureContainerExistsInDatabase(database, id);
            return new MockContainerProvider(this.DatabaseName, id, this.CosmosState, this.Logger);
        }

        public async Task DeleteAsync()
        {
            await Task.Yield();
            this.Logger.LogInformation("Deleting database '{0}'.", this.DatabaseName);
            this.CosmosState.DeleteDatabase(this.DatabaseName);
        }
    }
}
