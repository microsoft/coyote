// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Threading.Tasks;
using PetImages.Storage;

namespace PetImages.Tests.StorageMocks
{
    internal class MockBlobContainerProvider : IBlobContainer
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte[]>> Containers;
        private readonly object SyncObject;

        internal MockBlobContainerProvider()
        {
            this.Containers = new();
            this.SyncObject = new();
        }

        public Task CreateContainerAsync(string containerName)
        {
            lock (this.SyncObject)
            {
                this.Containers.TryAdd(containerName, new ConcurrentDictionary<string, byte[]>());
                return Task.CompletedTask;
            }
        }

        public Task CreateContainerIfNotExistsAsync(string containerName)
        {
            lock (this.SyncObject)
            {
                this.Containers.TryAdd(containerName, new ConcurrentDictionary<string, byte[]>());
                return Task.CompletedTask;
            }
        }

        public Task DeleteContainerAsync(string containerName)
        {
            lock (this.SyncObject)
            {
                this.Containers.TryRemove(containerName, out ConcurrentDictionary<string, byte[]> _);
                return Task.CompletedTask;
            }
        }

        public Task<bool> DeleteContainerIfExistsAsync(string containerName)
        {
            lock (this.SyncObject)
            {
                bool result = this.Containers.TryRemove(containerName, out ConcurrentDictionary<string, byte[]> _);
                return Task.FromResult(result);
            }
        }

        public Task CreateOrUpdateBlobAsync(string containerName, string blobName, byte[] blobContents)
        {
            lock (this.SyncObject)
            {
                this.Containers[containerName].AddOrUpdate(blobName, blobContents, (_, oldContents) => blobContents);
                return Task.CompletedTask;
            }
        }

        public Task<byte[]> GetBlobAsync(string containerName, string blobName)
        {
            lock (this.SyncObject)
            {
                var result = this.Containers[containerName][blobName];
                return Task.FromResult(result);
            }
        }

        public Task<bool> ExistsBlobAsync(string containerName, string blobName)
        {
            lock (this.SyncObject)
            {
                bool result = this.Containers.TryGetValue(containerName, out ConcurrentDictionary<string, byte[]> container);
                return Task.FromResult(result && container.ContainsKey(blobName));
            }
        }

        public Task DeleteBlobAsync(string containerName, string blobName)
        {
            lock (this.SyncObject)
            {
                this.Containers[containerName].TryRemove(blobName, out byte[] _);
                return Task.CompletedTask;
            }
        }

        public Task<bool> DeleteBlobIfExistsAsync(string containerName, string blobName)
        {
            lock (this.SyncObject)
            {
                if (!this.Containers.TryGetValue(containerName, out ConcurrentDictionary<string, byte[]> container))
                {
                    return Task.FromResult(false);
                }
 
                bool result = container.TryRemove(blobName, out byte[] _);
                return Task.FromResult(result);
            }
        }

        public Task DeleteAllBlobsAsync(string containerName)
        {
            lock (this.SyncObject)
            {
                if (this.Containers.TryGetValue(containerName, out ConcurrentDictionary<string, byte[]> container))
                {
                    container.Clear();
                }

                return Task.CompletedTask;
            }
        }
    }
}
