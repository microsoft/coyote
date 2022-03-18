// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using PetImages.Storage;

namespace PetImages.Tests.StorageMocks
{
    internal class MockCosmosDatabase : ICosmosDatabase
    {
        private readonly MockCosmosState State;
        private readonly object SyncObject;

        internal MockCosmosDatabase(MockCosmosState state)
        {
            this.State = state;
            this.SyncObject = new();
        }

        public Task<ICosmosContainer> CreateContainerAsync(string containerName)
        {
            lock (this.SyncObject)
            {
                this.State.CreateContainer(containerName);
                ICosmosContainer container = new MockCosmosContainer(containerName, this.State);
                return Task.FromResult(container);
            }
        }

        public Task<ICosmosContainer> GetContainer(string containerName)
        {
            lock (this.SyncObject)
            {
                this.State.EnsureContainerExistsInDatabase(containerName);
                ICosmosContainer container = new MockCosmosContainer(containerName, this.State);
                return Task.FromResult(container);
            }
        }
    }
}
