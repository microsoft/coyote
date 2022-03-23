// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Coyote.Runtime;
using PetImages.Storage;

namespace PetImages.Tests.StorageMocks
{
    internal class MockBlobContainerProvider : IBlobContainer
    {
        private readonly Dictionary<string, Dictionary<string, byte[]>> Containers;
        private readonly object SyncObject;

        internal MockBlobContainerProvider()
        {
            this.Containers = new();
            this.SyncObject = new();
        }

        public Task CreateContainerAsync(string containerName)
        {
            // We invoke this Coyote API to explicitly insert a "scheduling point" during the test execution
            // where the Coyote scheduler should explore a potential interleaving with another concurrently
            // executing operation. As this is a write operation we invoke the 'Write' scheduling point with
            // the corresponding container name, which can help Coyote optimize exploration.
            SchedulingPoint.Write(containerName);
            this.Containers.TryAdd(containerName, new Dictionary<string, byte[]>());
            return Task.CompletedTask;
        }

        public Task CreateContainerIfNotExistsAsync(string containerName)
        {
            // We invoke this Coyote API to explicitly insert a "scheduling point" during the test execution
            // where the Coyote scheduler should explore a potential interleaving with another concurrently
            // executing operation. As this is a write operation we invoke the 'Write' scheduling point with
            // the corresponding container name, which can help Coyote optimize exploration.
            SchedulingPoint.Write(containerName);
            this.Containers.TryAdd(containerName, new Dictionary<string, byte[]>());
            return Task.CompletedTask;
        }

        public Task DeleteContainerAsync(string containerName)
        {
            // We invoke this Coyote API to explicitly insert a "scheduling point" during the test execution
            // where the Coyote scheduler should explore a potential interleaving with another concurrently
            // executing operation. As this is a write operation we invoke the 'Write' scheduling point with
            // the corresponding container name, which can help Coyote optimize exploration.
            SchedulingPoint.Write(containerName);
            this.Containers.Remove(containerName);
            return Task.CompletedTask;
        }

        public Task<bool> DeleteContainerIfExistsAsync(string containerName)
        {
            // We invoke this Coyote API to explicitly insert a "scheduling point" during the test execution
            // where the Coyote scheduler should explore a potential interleaving with another concurrently
            // executing operation. As this is a write operation we invoke the 'Write' scheduling point with
            // the corresponding container name, which can help Coyote optimize exploration.
            SchedulingPoint.Write(containerName);
            bool result = this.Containers.Remove(containerName);
            return Task.FromResult(result);
        }

        public Task CreateOrUpdateBlobAsync(string containerName, string blobName, byte[] blobContents)
        {
            // We invoke this Coyote API to explicitly insert a "scheduling point" during the test execution
            // where the Coyote scheduler should explore a potential interleaving with another concurrently
            // executing operation. As this is a write operation we invoke the 'Write' scheduling point with
            // the corresponding container name, which can help Coyote optimize exploration.
            SchedulingPoint.Write(containerName);
            this.Containers[containerName][blobName] = blobContents;
            return Task.CompletedTask;
        }

        public Task<byte[]> GetBlobAsync(string containerName, string blobName)
        {
            // We invoke this Coyote API to explicitly insert a "scheduling point" during the test execution
            // where the Coyote scheduler should explore a potential interleaving with another concurrently
            // executing operation. As this is a read operation we invoke the 'Read' scheduling point with
            // the corresponding container name, which can help Coyote optimize exploration.
            SchedulingPoint.Read(containerName);
            var result = this.Containers[containerName][blobName];
            return Task.FromResult(result);
        }

        public Task<bool> ExistsBlobAsync(string containerName, string blobName)
        {
            // We invoke this Coyote API to explicitly insert a "scheduling point" during the test execution
            // where the Coyote scheduler should explore a potential interleaving with another concurrently
            // executing operation. As this is a read operation we invoke the 'Read' scheduling point with
            // the corresponding container name, which can help Coyote optimize exploration.
            SchedulingPoint.Read(containerName);
            bool result = this.Containers.TryGetValue(containerName, out Dictionary<string, byte[]> container);
            return Task.FromResult(result && container.ContainsKey(blobName));
        }

        public Task DeleteBlobAsync(string containerName, string blobName)
        {
            // We invoke this Coyote API to explicitly insert a "scheduling point" during the test execution
            // where the Coyote scheduler should explore a potential interleaving with another concurrently
            // executing operation. As this is a write operation we invoke the 'Write' scheduling point with
            // the corresponding container name, which can help Coyote optimize exploration.
            SchedulingPoint.Write(containerName);
            this.Containers[containerName].Remove(blobName);
            return Task.CompletedTask;
        }

        public Task<bool> DeleteBlobIfExistsAsync(string containerName, string blobName)
        {
            // We invoke this Coyote API to explicitly insert a "scheduling point" during the test execution
            // where the Coyote scheduler should explore a potential interleaving with another concurrently
            // executing operation. As this is a write operation we invoke the 'Write' scheduling point with
            // the corresponding container name, which can help Coyote optimize exploration.
            SchedulingPoint.Write(containerName);
            if (!this.Containers.TryGetValue(containerName, out Dictionary<string, byte[]> container))
            {
                return Task.FromResult(false);
            }

            bool result = container.Remove(blobName);
            return Task.FromResult(result);
        }

        public Task DeleteAllBlobsAsync(string containerName)
        {
            // We invoke this Coyote API to explicitly insert a "scheduling point" during the test execution
            // where the Coyote scheduler should explore a potential interleaving with another concurrently
            // executing operation. As this is a write operation we invoke the 'Write' scheduling point with
            // the corresponding container name, which can help Coyote optimize exploration.
            SchedulingPoint.Write(containerName);
            if (this.Containers.TryGetValue(containerName, out Dictionary<string, byte[]> container))
            {
                container.Clear();
            }

            return Task.CompletedTask;
        }
    }
}
