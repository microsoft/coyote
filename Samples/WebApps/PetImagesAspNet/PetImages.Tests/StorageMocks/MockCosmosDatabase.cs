// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Runtime;
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
            // We invoke this Coyote API to explicitly insert a "scheduling point" during the test execution
            // where the Coyote scheduler should explore a potential interleaving with another concurrently
            // executing operation. As this is a write operation we invoke the 'Write' scheduling point with
            // the corresponding container name, which can help Coyote optimize exploration.
            SchedulingPoint.Write(containerName);
            this.State.CreateContainer(containerName);
            ICosmosContainer container = new MockCosmosContainer(containerName, this.State);
            return Task.FromResult(container);
        }

        public Task<ICosmosContainer> GetContainer(string containerName)
        {
            // We invoke this Coyote API to explicitly insert a "scheduling point" during the test execution
            // where the Coyote scheduler should explore a potential interleaving with another concurrently
            // executing operation. As this is a read operation we invoke the 'Read' scheduling point with
            // the corresponding container name, which can help Coyote optimize exploration.
            this.State.EnsureContainerExistsInDatabase(containerName);
            ICosmosContainer container = new MockCosmosContainer(containerName, this.State);
            return Task.FromResult(container);
        }
    }
}
