// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PetImages.Entities;
using PetImages.Exceptions;

using Container = System.Collections.Concurrent.ConcurrentDictionary<string, PetImages.Entities.DbItem>;
using Database = System.Collections.Concurrent.ConcurrentDictionary<
    string, System.Collections.Concurrent.ConcurrentDictionary<string, PetImages.Entities.DbItem>>;

namespace PetImages.Tests.StorageMocks
{
    internal class MockCosmosState
    {
        private readonly Database Database;

        internal MockCosmosState()
        {
            this.Database = new();
        }

        internal void CreateContainer(string containerName)
        {
            EnsureContainerDoesNotExistInDatabase(containerName);
            _ = this.Database.TryAdd(containerName, new Container());
        }

        internal void GetContainer(string containerName)
        {
            EnsureContainerExistsInDatabase(containerName);
        }

        internal void DeleteContainer(string containerName)
        {
            EnsureContainerExistsInDatabase(containerName);
        }

        internal void CreateItem(string containerName, DbItem item)
        {
            EnsureItemDoesNotExistInDatabase(containerName, item.PartitionKey, item.Id);
            var container = this.Database[containerName];
            _ = container.TryAdd(GetCombinedKey(item.PartitionKey, item.Id), item);
        }

        internal void UpsertItem(string containerName, DbItem item)
        {
            EnsureContainerExistsInDatabase(containerName);
            var container = this.Database[containerName];
            _ = container.TryAdd(GetCombinedKey(item.PartitionKey, item.Id), item);
        }

        internal DbItem GetItem(string containerName, string partitionKey, string id)
        {
            EnsureItemExistsInDatabase(containerName, partitionKey, id);
            var container = this.Database[containerName];
            _ = container.TryGetValue(GetCombinedKey(partitionKey, id), out DbItem item);
            return item;
        }

        internal void DeleteItem(string containerName, string partitionKey, string id)
        {
            EnsureItemExistsInDatabase(containerName, partitionKey, id);
            var container = this.Database[containerName];
            _ = container.TryRemove(GetCombinedKey(partitionKey, id), out DbItem _);
        }

        internal void EnsureContainerDoesNotExistInDatabase(string containerName)
        {
            if (this.Database.ContainsKey(containerName))
            {
                throw new DatabaseContainerAlreadyExists();
            }
        }

        internal void EnsureContainerExistsInDatabase(string containerName)
        {
            if (!this.Database.ContainsKey(containerName))
            {
                throw new DatabaseContainerDoesNotExist();
            }
        }

        internal void EnsureItemExistsInDatabase(string containerName, string partitionKey, string id)
        {
            EnsureContainerExistsInDatabase(containerName);
            var container = this.Database[containerName];

            if (!container.ContainsKey(GetCombinedKey(partitionKey, id)))
            {
                throw new DatabaseItemDoesNotExistException();
            }
        }

        internal void EnsureItemDoesNotExistInDatabase(string containerName, string partitionKey, string id)
        {
            EnsureContainerExistsInDatabase(containerName);
            var container = this.Database[containerName];

            if (container.ContainsKey(GetCombinedKey(partitionKey, id)))
            {
                throw new DatabaseItemAlreadyExistsException();
            }
        }

        internal static string GetCombinedKey(string partitionKey, string id) => $"{partitionKey}_{id}";
    }
}
