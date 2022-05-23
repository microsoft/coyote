// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using ImageGallery.Logging;
using ImageGallery.Store.Cosmos;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace ImageGallery.Tests.Mocks.Cosmos
{
    /// <summary>
    /// Mock implementation of an Azure Cosmos DB client provider.
    /// </summary>
    internal class MockClientProvider : IClientProvider
    {
        public static async Task InjectYieldsAtMethodStart()
        {
            string envYiledLoop = Environment.GetEnvironmentVariable("YIELDS_METHOD_START");
            int envYiledLoopInt = 0;
            if (envYiledLoop != null)
            {
#pragma warning disable CA1305 // Specify IFormatProvider
                envYiledLoopInt = int.Parse(envYiledLoop);
#pragma warning restore CA1305 // Specify IFormatProvider
            }

            for (int i = 0; i < envYiledLoopInt; i++)
            {
                await Task.Yield();
            }
        }

        private readonly MockCosmosState CosmosState;
        private readonly MockLogger Logger;

        internal MockClientProvider(MockCosmosState cosmosState, MockLogger logger)
        {
            this.CosmosState = cosmosState;
            this.Logger = logger;
        }

        public async Task<IDatabaseProvider> CreateDatabaseAsync(string id)
        {
            await InjectYieldsAtMethodStart();
            // Used to model asynchrony in the request.
            await Task.Yield();

            this.Logger.LogInformation("Creating database '{0}'.", id);
            this.CosmosState.EnsureDatabaseDoesNotExist(id);
            this.CosmosState.Databases[id] = new ConcurrentDictionary<string,
                ConcurrentDictionary<PartitionKey, ConcurrentDictionary<string, object>>>();
            return new MockDatabaseProvider(id, this.CosmosState, this.Logger);
        }

        public async Task<IDatabaseProvider> CreateDatabaseIfNotExistsAsync(string id)
        {
            await InjectYieldsAtMethodStart();
            await Task.Yield();

            this.Logger.LogInformation("Creating database '{0}' if it does not exist.", id);
            this.CosmosState.Databases[id] = new ConcurrentDictionary<string,
                ConcurrentDictionary<PartitionKey, ConcurrentDictionary<string, object>>>();
            return new MockDatabaseProvider(id, this.CosmosState, this.Logger);
        }

        public IDatabaseProvider GetDatabase(string id)
        {
            this.Logger.LogInformation("Getting database '{0}'.", id);
            this.CosmosState.EnsureDatabaseExists(id);
            return new MockDatabaseProvider(id, this.CosmosState, this.Logger);
        }
    }
}
