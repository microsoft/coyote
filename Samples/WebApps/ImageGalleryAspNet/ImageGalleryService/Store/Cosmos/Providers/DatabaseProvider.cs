// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace ImageGallery.Store.Cosmos
{
    /// <summary>
    /// Production implementation of an Azure Cosmos DB database provider.
    /// </summary>
    public class DatabaseProvider : IDatabaseProvider
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

        private readonly Database Database;

        public DatabaseProvider(Database database)
        {
            this.Database = database;
        }

        public async Task<IContainerProvider> CreateContainerAsync(string id, string partitionKeyPath)
        {
            await InjectYieldsAtMethodStart();
            var container = await this.Database.CreateContainerAsync(id, partitionKeyPath);
            return new ContainerProvider(container);
        }

        public async Task<IContainerProvider> CreateContainerIfNotExistsAsync(string id, string partitionKeyPath)
        {
            await InjectYieldsAtMethodStart();
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
