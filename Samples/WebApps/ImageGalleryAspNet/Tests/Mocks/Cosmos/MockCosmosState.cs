// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using ImageGallery.Logging;
using ImageGallery.Store.Cosmos;
using Microsoft.Azure.Cosmos;

namespace ImageGallery.Tests.Mocks.Cosmos
{
    using Container = System.Collections.Concurrent.ConcurrentDictionary<PartitionKey,
        System.Collections.Concurrent.ConcurrentDictionary<string, object>>;

    using Database = System.Collections.Concurrent.ConcurrentDictionary<string,
        System.Collections.Concurrent.ConcurrentDictionary<PartitionKey,
            System.Collections.Concurrent.ConcurrentDictionary<string, object>>>;

    using Databases = System.Collections.Concurrent.ConcurrentDictionary<string,
        System.Collections.Concurrent.ConcurrentDictionary<string,
        System.Collections.Concurrent.ConcurrentDictionary<PartitionKey,
            System.Collections.Concurrent.ConcurrentDictionary<string, object>>>>;

    using LogicalPartition = System.Collections.Concurrent.ConcurrentDictionary<string, object>;

    /// <summary>
    /// In-memory state for Cosmos DB that can be used by the Cosmos DB mocks during (systematic) testing.
    /// </summary>
    internal class MockCosmosState
    {
        internal readonly Databases Databases;

        private readonly MockLogger Logger;

        internal MockCosmosState(MockLogger logger)
        {
            this.Databases = new Databases();
            this.Logger = logger;
        }

        internal void EnsureDatabaseDoesNotExist(string databaseName)
        {
            if (this.Databases.ContainsKey(databaseName))
            {
                throw CreateCosmosClientException($"Database {databaseName} already exists", HttpStatusCode.Conflict);
            }
        }

        internal void EnsureDatabaseExists(string databaseName)
        {
            if (!this.Databases.ContainsKey(databaseName))
            {
                throw CreateCosmosClientException($"Database {databaseName} does not exist", HttpStatusCode.NotFound);
            }
        }

        internal void EnsureContainerDoesNotExistInDatabase(Database database, string containerName)
        {
            if (database.ContainsKey(containerName))
            {
                throw CreateCosmosClientException($"Container {containerName} already exists", HttpStatusCode.Conflict);
            }
        }

        internal void EnsureContainerExistsInDatabase(Database database, string containerName)
        {
            if (!database.ContainsKey(containerName))
            {
                throw CreateCosmosClientException($"Container {containerName} does not exist", HttpStatusCode.NotFound);
            }
        }

        internal Container EnsureContainerExistsInDatabaseAndGetIt(string databaseName, string containerName)
        {
            this.EnsureDatabaseExists(databaseName);
            var database = this.Databases[databaseName];
            this.EnsureContainerExistsInDatabase(database, containerName);
            return database[containerName];
        }

        internal void EnsureLogicalPartitionDoesNotContainId(LogicalPartition logicalPartition, string id)
        {
            if (logicalPartition.ContainsKey(id))
            {
                throw CreateCosmosClientException($"Partition already contains a row with id {id}", HttpStatusCode.Conflict);
            }
        }

        internal void EnsureContainerContainsPartitionKey(Container container, PartitionKey partitionKey)
        {
            if (!container.ContainsKey(partitionKey))
            {
                throw CreateCosmosClientException($"Container does not contain a partition with key {partitionKey}", HttpStatusCode.NotFound);
            }
        }

        internal void EnsureLogicalPartitionContainsId(LogicalPartition logicalPartition, string id)
        {
            if (!logicalPartition.ContainsKey(id))
            {
                throw CreateCosmosClientException($"Logical partition does not contain row with id {id}", HttpStatusCode.NotFound);
            }
        }

        internal void EnsurePartitionKeyInEntityMatchesPartitionKey<T>(T entity, PartitionKey partitionKey)
            where T : CosmosEntity
        {
            if (!partitionKey.Equals(new PartitionKey(entity.PartitionKey)))
            {
                throw CreateCosmosClientException(
                    $"Partition key extracted from entity {entity.PartitionKey} does not match given partition key {partitionKey}",
                    HttpStatusCode.BadRequest);
            }
        }

        internal void EnsureIdIsNotEmpty<T>(T entity)
            where T : CosmosEntity
        {
            if (string.IsNullOrWhiteSpace(entity.Id))
            {
                throw CreateCosmosClientException($"Entity must have a non-empty id {entity.Id}", HttpStatusCode.BadRequest);
            }
        }

        internal void DeleteDatabase(string databaseName)
        {
            this.Databases.TryRemove(databaseName, out Database _);
        }

        internal void Clear()
        {
            this.Databases.Clear();
        }

        internal static CosmosException CreateCosmosClientException(string message, HttpStatusCode statusCode) =>
            new CosmosException(message, statusCode, subStatusCode: 0, activityId: null, requestCharge: 0);
    }
}
