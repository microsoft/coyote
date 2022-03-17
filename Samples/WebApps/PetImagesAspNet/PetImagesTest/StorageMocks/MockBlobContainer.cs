// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Threading.Tasks;
using PetImages.Storage;

namespace PetImagesTest.StorageMocks
{
    internal class MockBlobContainerProvider : IBlobContainer
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte[]>> Containers;

        internal MockBlobContainerProvider()
        {
            this.Containers = new ConcurrentDictionary<string, ConcurrentDictionary<string, byte[]>>();
        }

        public Task CreateContainerAsync(string containerName)
        {
            return Task.Run(() =>
            {
                this.Containers.TryAdd(containerName, new ConcurrentDictionary<string, byte[]>());
            });
        }

        public Task CreateContainerIfNotExistsAsync(string containerName)
        {
            return Task.Run(() =>
            {
                this.Containers.TryAdd(containerName, new ConcurrentDictionary<string, byte[]>());
            });
        }

        public Task DeleteContainerAsync(string containerName)
        {
            return Task.Run(() =>
            {
                this.Containers.TryRemove(containerName, out ConcurrentDictionary<string, byte[]> _);
            });
        }

        public Task<bool> DeleteContainerIfExistsAsync(string containerName)
        {
            return Task.Run(() =>
                {
                    return this.Containers.TryRemove(containerName, out ConcurrentDictionary<string, byte[]> _);
                });
        }

        public Task CreateOrUpdateBlobAsync(string containerName, string blobName, byte[] blobContents)
        {
            return Task.Run(() =>
            {
                this.Containers[containerName].AddOrUpdate(blobName, blobContents, (_, oldContents) => blobContents);
            });
        }

        public Task<byte[]> GetBlobAsync(string containerName, string blobName)
        {
            return Task.Run(() =>
            {
                return this.Containers[containerName][blobName];
            });
        }

        public Task<bool> ExistsBlobAsync(string containerName, string blobName)
        {
            return Task.Run(() =>
            {
                return this.Containers.TryGetValue(containerName, out ConcurrentDictionary<string, byte[]> container) &&
                    container.ContainsKey(blobName);
            });
        }

        public Task DeleteBlobAsync(string containerName, string blobName)
        {
            return Task.Run(() =>
            {
                this.Containers[containerName].TryRemove(blobName, out byte[] _);
            });
        }

        public Task<bool> DeleteBlobIfExistsAsync(string containerName, string blobName)
        {
            return Task.Run(() =>
            {
                if (!this.Containers.TryGetValue(containerName, out ConcurrentDictionary<string, byte[]> container))
                {
                    return false;
                }

                return container.TryRemove(blobName, out byte[] _);
            });
        }

        public Task DeleteAllBlobsAsync(string containerName)
        {
            return Task.Run(() =>
            {
                if (this.Containers.TryGetValue(containerName, out ConcurrentDictionary<string, byte[]> container))
                {
                    container.Clear();
                }
            });
        }
    }
}
