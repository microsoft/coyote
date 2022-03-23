// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using PetImages.Entities;

namespace PetImages.Storage
{
    internal class CosmosContainer : IAccountContainer, IImageContainer
    {
        public Task<T> CreateItem<T>(T row)
            where T : DbItem =>
            throw new NotImplementedException();

        public Task<T> GetItem<T>(string partitionKey, string id)
           where T : DbItem =>
            throw new NotImplementedException();

        public Task<T> UpsertItem<T>(T row)
            where T : DbItem =>
            throw new NotImplementedException();

        public Task DeleteItem(string partitionKey, string id) =>
            throw new NotImplementedException();
    }
}
