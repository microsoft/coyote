// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Random;
using PetImages.Entities;
using PetImages.Storage;
using PetImagesTest.Exceptions;

namespace PetImagesTest.StorageMocks
{
    public class MockCosmosContainer : ICosmosContainer
    {
        private readonly string ContainerName;
        private readonly MockCosmosState State;
        private readonly Generator Generator;
        private bool EmitRandomizedFaults;

        public MockCosmosContainer(string containerName, MockCosmosState state)
        {
            this.ContainerName = containerName;
            this.State = state;
            this.Generator = Generator.Create();
            this.EmitRandomizedFaults = false;
        }

        public Task<T> CreateItem<T>(T item)
            where T : DbItem
        {
            var itemCopy = TestHelper.Clone(item);

            return Task.Run(() =>
            {
                if (this.EmitRandomizedFaults && this.Generator.NextBoolean())
                {
                    throw new SimulatedDatabaseFaultException();
                }

                this.State.CreateItem(this.ContainerName, itemCopy);
                return itemCopy;
            });
        }

        public Task<T> GetItem<T>(string partitionKey, string id)
            where T : DbItem
        {
            return Task.Run(() =>
            {
                if (this.EmitRandomizedFaults && this.Generator.NextBoolean())
                {
                    throw new SimulatedDatabaseFaultException();
                }

                var item = this.State.GetItem(this.ContainerName, partitionKey, id);

                var itemCopy = TestHelper.Clone((T)item);

                return itemCopy;
            });
        }

        public Task<T> UpsertItem<T>(T item)
            where T : DbItem
        {
            return Task.Run(() =>
            {
                if (this.EmitRandomizedFaults && this.Generator.NextBoolean())
                {
                    throw new SimulatedDatabaseFaultException();
                }

                var itemCopy = TestHelper.Clone(item);
                this.State.UpsertItem(this.ContainerName, itemCopy);
                return itemCopy;
            });
        }

        public Task DeleteItem(string partitionKey, string id)
        {
            return Task.Run(() =>
            {
                if (this.EmitRandomizedFaults && this.Generator.NextBoolean())
                {
                    throw new SimulatedDatabaseFaultException();
                }

                this.State.DeleteItem(this.ContainerName, partitionKey, id);
            });
        }

        public void EnableRandomizedFaults()
        {
            this.EmitRandomizedFaults = true;
        }

        public void DisableRandomizedFaults()
        {
            this.EmitRandomizedFaults = false;
        }

    }
}
