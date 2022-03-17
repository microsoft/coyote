// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using ImageGallery.Logging;
using ImageGallery.Store.Cosmos;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace ImageGallery.Tests.Mocks.Cosmos
{
    /// <summary>
    /// Mock implementation of an Azure Cosmos DB container provider.
    /// </summary>
    internal class MockContainerProvider : IContainerProvider
    {
        private readonly string DatabaseName;
        private readonly string ContainerName;
        private readonly MockCosmosState CosmosState;
        private readonly MockLogger Logger;

        internal MockContainerProvider(string databaseName, string containerName, MockCosmosState cosmosState, MockLogger logger)
        {
            this.DatabaseName = databaseName;
            this.ContainerName = containerName;
            this.CosmosState = cosmosState;
            this.Logger = logger;
        }

        public async Task<T> CreateItemAsync<T>(T entity, ItemRequestOptions requestOptions = null)
            where T : CosmosEntity
        {
            // Used to model asynchrony in the request.
            await Task.Yield();

            this.Logger.LogInformation("Creating new item with partition key '{0}' and id '{1}' in container '{2}' of database '{3}'.",
                entity.PartitionKey, entity.Id, this.ContainerName, this.DatabaseName);

            var timestamp = DateTime.UtcNow;
            entity.CreatedTime = timestamp;
            entity.ChangedTime = timestamp;

            var container = this.CosmosState.EnsureContainerExistsInDatabaseAndGetIt(this.DatabaseName, this.ContainerName);
            var key = new PartitionKey(entity.PartitionKey);
            this.CosmosState.EnsurePartitionKeyInEntityMatchesPartitionKey(entity, key);
            this.CosmosState.EnsureIdIsNotEmpty(entity);

            if (!container.ContainsKey(key))
            {
                container[key] = new ConcurrentDictionary<string, object>();
            }

            var logicalPartition = container[key];
            this.CosmosState.EnsureLogicalPartitionDoesNotContainId(logicalPartition, entity.Id);

            logicalPartition[entity.Id] = entity;
            return entity;
        }

        public async Task<T> ReplaceItemAsync<T>(T entity, ItemRequestOptions requestOptions = null)
            where T : CosmosEntity
        {
            await Task.Yield();

            this.Logger.LogInformation("Replacing item with partition key '{0}' and id '{1}' in container '{2}' of database '{3}'.",
                entity.PartitionKey, entity.Id, this.ContainerName, this.DatabaseName);

            var timestamp = DateTime.UtcNow;
            entity.ChangedTime = timestamp;

            var container = this.CosmosState.EnsureContainerExistsInDatabaseAndGetIt(this.DatabaseName, this.ContainerName);
            var key = new PartitionKey(entity.PartitionKey);
            this.CosmosState.EnsurePartitionKeyInEntityMatchesPartitionKey(entity, key);
            this.CosmosState.EnsureContainerContainsPartitionKey(container, key);

            var logicalPartition = container[key];
            this.CosmosState.EnsureLogicalPartitionContainsId(logicalPartition, entity.Id);

            logicalPartition[entity.Id] = entity;
            return entity;
        }

        public async Task<T> UpsertItemAsync<T>(T entity, ItemRequestOptions requestOptions = null)
            where T : CosmosEntity
        {
            await Task.Yield();

            this.Logger.LogInformation("Upserting item with partition key '{0}' and id '{1}' in container '{2}' of database '{3}'.",
                entity.PartitionKey, entity.Id, this.ContainerName, this.DatabaseName);

            var timestamp = DateTime.UtcNow;
            if (!entity.CreatedTime.HasValue)
            {
                entity.CreatedTime = timestamp;
            }

            entity.ChangedTime = timestamp;

            var container = this.CosmosState.EnsureContainerExistsInDatabaseAndGetIt(this.DatabaseName, this.ContainerName);
            var key = new PartitionKey(entity.PartitionKey);
            this.CosmosState.EnsurePartitionKeyInEntityMatchesPartitionKey(entity, key);

            if (!container.ContainsKey(key))
            {
                container[key] = new ConcurrentDictionary<string, object>();
            }

            var logicalPartition = container[key];
            logicalPartition[entity.Id] = entity;
            return entity;
        }

        public async Task DeleteItemAsync<T>(string id, string partitionKey, ItemRequestOptions requestOptions = null)
            where T : CosmosEntity
        {
            await Task.Yield();

            this.Logger.LogInformation("Deleting item with partition key '{0}' and id '{1}' in container '{2}' of database '{3}'.",
                partitionKey, id, this.ContainerName, this.DatabaseName);

            var container = this.CosmosState.EnsureContainerExistsInDatabaseAndGetIt(this.DatabaseName, this.ContainerName);
            var key = new PartitionKey(partitionKey);
            this.CosmosState.EnsureContainerContainsPartitionKey(container, key);

            var logicalPartition = container[key];
            this.CosmosState.EnsureLogicalPartitionContainsId(logicalPartition, id);

            logicalPartition.Remove(id, out object _);
            this.CosmosState.EnsureLogicalPartitionDoesNotContainId(logicalPartition, id);
        }

        public async Task<T> ReadItemAsync<T>(string partitionKey, string id, ItemRequestOptions requestOptions = null)
            where T : CosmosEntity
        {
            await Task.Yield();

            this.Logger.LogInformation("Reading item with partition key '{0}' and id '{1}' in container '{2}' of database '{3}'.",
                partitionKey, id, this.ContainerName, this.DatabaseName);

            var container = this.CosmosState.EnsureContainerExistsInDatabaseAndGetIt(this.DatabaseName, this.ContainerName);
            var key = new PartitionKey(partitionKey);
            this.CosmosState.EnsureContainerContainsPartitionKey(container, key);

            var logicalPartition = container[key];
            this.CosmosState.EnsureLogicalPartitionContainsId(logicalPartition, id);
            return (T)logicalPartition[id];
        }

        public async Task<IList<T>> ReadItemsAcrossPartitionsAsync<T>(Expression<Func<T, bool>> predicate)
            where T : CosmosEntity
        {
            await Task.Yield();

            this.Logger.LogInformation("Reading items across partitions in container '{1}' of database '{2}'.",
                this.ContainerName, this.DatabaseName);

            var container = this.CosmosState.EnsureContainerExistsInDatabaseAndGetIt(this.DatabaseName, this.ContainerName);
            var result = new List<T>();
            foreach (var partition in container)
            {
                var logicalPartition = partition.Value;
                var compiledPredicate = predicate.Compile();
                result.AddRange(logicalPartition.Keys.
                    Where(id => compiledPredicate((T)logicalPartition[id])).
                    Select(id => (T)logicalPartition[id]));
            }

            return result;
        }

        public async Task<IList<T>> ReadItemsInPartitionAsync<T>(string partitionKey, Expression<Func<T, bool>> predicate)
            where T : CosmosEntity
        {
            await Task.Yield();

            this.Logger.LogInformation("Reading items with partition key '{0}' in container '{1}' of database '{2}'.",
                partitionKey, this.ContainerName, this.DatabaseName);

            var container = this.CosmosState.EnsureContainerExistsInDatabaseAndGetIt(this.DatabaseName, this.ContainerName);
            var key = new PartitionKey(partitionKey);
            if (!container.ContainsKey(key))
            {
                return new List<T>();
            }

            var logicalPartition = container[key];
            var compiledPredicate = predicate.Compile();
            return logicalPartition.Keys.
                Where(id => compiledPredicate((T)logicalPartition[id])).
                Select(id => (T)logicalPartition[id]).ToList();
        }

        public async Task<bool> ExistsItemAsync<T>(string partitionKey, string id, ItemRequestOptions requestOptions = null)
            where T : CosmosEntity
        {
            await Task.Yield();

            this.Logger.LogInformation("Checking if item with partition key '{0}' and id '{1}' exists in container '{2}' of database '{3}'.",
                partitionKey, id, this.ContainerName, this.DatabaseName);

            try
            {
                var container = this.CosmosState.EnsureContainerExistsInDatabaseAndGetIt(this.DatabaseName, this.ContainerName);
                var key = new PartitionKey(partitionKey);
                this.CosmosState.EnsureContainerContainsPartitionKey(container, key);

                var logicalPartition = container[key];
                this.CosmosState.EnsureLogicalPartitionContainsId(logicalPartition, id);
                return true;
            }
            catch (Exception ex) when (ExceptionHelper.IsCosmosExceptionWithStatusCode(ex, System.Net.HttpStatusCode.NotFound))
            {
                return false;
            }
        }
    }
}
