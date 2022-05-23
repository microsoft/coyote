// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace ImageGallery.Store.Cosmos
{
    /// <summary>
    /// Production implementation of an Azure Cosmos DB container provider.
    /// </summary>
    public class ContainerProvider : IContainerProvider
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

        private readonly Container Container;

        public ContainerProvider(Container container)
        {
            this.Container = container;
        }

        public async Task<T> CreateItemAsync<T>(T entity, ItemRequestOptions requestOptions = null)
            where T : CosmosEntity
        {
            await InjectYieldsAtMethodStart();
            var timestamp = DateTime.UtcNow;
            entity.CreatedTime = timestamp;
            entity.ChangedTime = timestamp;

            var response = await this.Container.CreateItemAsync<T>(entity, new PartitionKey(entity.PartitionKey), requestOptions);
            return response.Resource;
        }

        public async Task<T> ReplaceItemAsync<T>(T entity, ItemRequestOptions requestOptions = null)
            where T : CosmosEntity
        {
            await InjectYieldsAtMethodStart();
            var timestamp = DateTime.UtcNow;
            entity.ChangedTime = timestamp;

            var response = await this.Container.ReplaceItemAsync(entity, entity.Id, new PartitionKey(entity.PartitionKey), requestOptions);
            return response.Resource;
        }

        public async Task<T> UpsertItemAsync<T>(T entity, ItemRequestOptions requestOptions = null)
            where T : CosmosEntity
        {
            await InjectYieldsAtMethodStart();
            var timestamp = DateTime.UtcNow;

            if (!entity.CreatedTime.HasValue)
            {
                entity.CreatedTime = timestamp;
            }

            entity.ChangedTime = timestamp;

            var response = await this.Container.UpsertItemAsync(entity, new PartitionKey(entity.PartitionKey), requestOptions);
            return response.Resource;
        }

        public Task DeleteItemAsync<T>(string id, string partitionKey, ItemRequestOptions requestOptions = null)
            where T : CosmosEntity =>
            this.Container.DeleteItemAsync<T>(id, new PartitionKey(partitionKey), requestOptions);

        public async Task<T> ReadItemAsync<T>(string partitionKey, string id, ItemRequestOptions requestOptions = null)
            where T : CosmosEntity
        {
            await InjectYieldsAtMethodStart();
            var readResponse = await this.Container.ReadItemAsync<T>(id, new PartitionKey(partitionKey), requestOptions);
            return readResponse.Resource;
        }

        public async Task<IList<T>> ReadItemsAcrossPartitionsAsync<T>(Expression<Func<T, bool>> predicate)
            where T : CosmosEntity
        {
            await InjectYieldsAtMethodStart();
            var feedIterator = this.Container.GetItemLinqQueryable<T>(allowSynchronousQueryExecution: true)
                .Where(predicate)
                .ToFeedIterator();

            var results = new List<T>();

            while (feedIterator.HasMoreResults)
            {
                var response = await feedIterator.ReadNextAsync();
                results.AddRange(response.Resource);
            }

            return results;
        }

        public async Task<IList<T>> ReadItemsInPartitionAsync<T>(string partitionKey, Expression<Func<T, bool>> predicate)
            where T : CosmosEntity
        {
            await InjectYieldsAtMethodStart();
            var feedIterator = this.Container.GetItemLinqQueryable<T>(
                allowSynchronousQueryExecution: true,
                requestOptions: new QueryRequestOptions()
                {
                    PartitionKey = new PartitionKey(partitionKey)
                })
                .Where(predicate)
                .ToFeedIterator();

            var results = new List<T>();

            while (feedIterator.HasMoreResults)
            {
                var response = await feedIterator.ReadNextAsync();
                results.AddRange(response.Resource);
            }

            return results;
        }

        public async Task<bool> ExistsItemAsync<T>(string partitionKey, string id, ItemRequestOptions requestOptions = null)
            where T : CosmosEntity
        {
            await InjectYieldsAtMethodStart();
            try
            {
                await this.Container.ReadItemAsync<T>(id, new PartitionKey(partitionKey), requestOptions);
                return true;
            }
            catch (Exception ex) when (ExceptionHelper.IsCosmosExceptionWithStatusCode(ex, System.Net.HttpStatusCode.NotFound))
            {
                return false;
            }
        }
    }
}
