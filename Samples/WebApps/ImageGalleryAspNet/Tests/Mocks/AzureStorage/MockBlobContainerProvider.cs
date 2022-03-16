// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using ImageGallery.Logging;
using ImageGallery.Store.AzureStorage;
using Microsoft.Extensions.Logging;

namespace ImageGallery.Tests.Mocks.AzureStorage
{
    /// <summary>
    /// Mock implementation of an Azure Storage blob container provider.
    /// </summary>
    internal class MockBlobContainerProvider : IBlobContainerProvider
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte[]>> Containers;
        private readonly MockLogger Logger;

        internal MockBlobContainerProvider(MockLogger logger)
        {
            this.Containers = new ConcurrentDictionary<string, ConcurrentDictionary<string, byte[]>>();
            this.Logger = logger;
        }

        public async Task CreateContainerAsync(string containerName)
        {
            // Used to model asynchrony in the request.
            await Task.Yield();

            this.Logger.LogInformation("Creating container '{0}'.", containerName);
            this.Containers.TryAdd(containerName, new ConcurrentDictionary<string, byte[]>());
        }

        public async Task CreateContainerIfNotExistsAsync(string containerName)
        {
            await Task.Yield();

            this.Logger.LogInformation("Creating container '{0}' if it does not exist.", containerName);
            this.Containers.TryAdd(containerName, new ConcurrentDictionary<string, byte[]>());
        }

        public async Task DeleteContainerAsync(string containerName)
        {
            await Task.Yield();

            this.Logger.LogInformation("Deleting container '{0}'.", containerName);
            this.Containers.TryRemove(containerName, out ConcurrentDictionary<string, byte[]> _);
        }

        public async Task<bool> DeleteContainerIfExistsAsync(string containerName)
        {
            await Task.Yield();

            this.Logger.LogInformation("Deleting container '{0}' if it exists.", containerName);
            return this.Containers.TryRemove(containerName, out ConcurrentDictionary<string, byte[]> _);
        }

        public async Task CreateBlobAsync(string containerName, string blobName, byte[] blobContents)
        {
            await Task.Yield();

            this.Logger.LogInformation("Creating blob '{0}' in container '{1}'.", blobName, containerName);
            this.Containers[containerName].TryAdd(blobName, blobContents);
        }

        public async Task<byte[]> GetBlobAsync(string containerName, string blobName)
        {
            await Task.Yield();

            this.Logger.LogInformation("Getting blob '{0}' from container '{1}'.", blobName, containerName);
            return this.Containers[containerName][blobName];
        }

        public async Task<bool> ExistsBlobAsync(string containerName, string blobName)
        {
            await Task.Yield();

            this.Logger.LogInformation("Checking if blob '{0}' exists in container '{1}'.", blobName, containerName);
            return this.Containers.TryGetValue(containerName, out ConcurrentDictionary<string, byte[]> container) &&
                container.ContainsKey(blobName);
        }

        public async Task DeleteBlobAsync(string containerName, string blobName)
        {
            await Task.Yield();

            this.Logger.LogInformation("Deleting blob '{0}' from container '{1}'.", blobName, containerName);
            this.Containers[containerName].TryRemove(blobName, out byte[] _);
        }

        public async Task<bool> DeleteBlobIfExistsAsync(string containerName, string blobName)
        {
            await Task.Yield();

            this.Logger.LogInformation("Deleting blob '{0}' from container '{1}' if it exists.", blobName, containerName);
            if (!this.Containers.TryGetValue(containerName, out ConcurrentDictionary<string, byte[]> container))
            {
                return false;
            }

            return container.TryRemove(blobName, out byte[] _);
        }

        public async Task DeleteAllBlobsAsync(string containerName)
        {
            await Task.Yield();

            this.Logger.LogInformation("Deleting container '{0}'.", containerName);
            if (this.Containers.TryGetValue(containerName, out ConcurrentDictionary<string, byte[]> container))
            {
                container.Clear();
            }
        }

        public async Task<BlobPage> GetBlobListAsync(string containerName, string continuationId, int pageSize)
        {
            await Task.Yield();
            if (!this.Containers.TryGetValue(containerName, out ConcurrentDictionary<string, byte[]> container))
            {
                return null;
            }
            this.Logger.LogInformation("Getting image list '{0}' starting at {1}.", containerName, continuationId);

            List<string> keys = new List<string>(container.Keys);
            keys.Sort();
            int start = 0;
            if (!string.IsNullOrEmpty(continuationId))
            {
                int.TryParse(continuationId, out start);
            }

            List<string> names = new List<string>();
            int i = start;
            while (i < start + pageSize && i < keys.Count)
            {
                names.Add(keys[i++]);
            }

            if (names.Count == 0)
            {
                return null;
            }

            return new BlobPage() { Names = names.ToArray(), ContinuationId = i.ToString() };
        }
    }
}
