// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using PetImages.Storage;

namespace PetImagesTest.StorageMocks
{
    public class MockCosmosDatabase : ICosmosDatabase
    {
        private readonly MockCosmosState State;

        public MockCosmosDatabase(MockCosmosState state)
        {
            this.State = state;
        }

        public Task<ICosmosContainer> CreateContainerAsync(string containerName)
        {
            return Task.Run<ICosmosContainer>(() =>
            {
                this.State.CreateContainer(containerName);
                return new MockCosmosContainer(containerName, this.State);
            });
        }

        public Task<ICosmosContainer> GetContainer(string containerName)
        {
            return Task.Run<ICosmosContainer>(() =>
            {
                this.State.EnsureContainerExistsInDatabase(containerName);
                return new MockCosmosContainer(containerName, this.State);
            });
        }
    }
}
