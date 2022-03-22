// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using PetImages.Entities;

namespace PetImages.Storage
{
    /// <summary>
    /// Interface of a Cosmos DB container. This can be implemented
    /// for production or with a mock for (systematic) testing.
    /// </summary>
    public interface ICosmosContainer
    {
        Task<T> CreateItem<T>(T row)
            where T : DbItem;

        Task<T> GetItem<T>(string partitionKey, string id)
           where T : DbItem;

        Task<T> UpsertItem<T>(T row)
            where T : DbItem;

        Task DeleteItem(string partitionKey, string id);
    }

    /// <summary>
    /// Interface of a Cosmos DB account container. This can be implemented
    /// for production or with a mock for (systematic) testing.
    /// </summary>
    public interface IAccountContainer : ICosmosContainer
    {
    }

    /// <summary>
    /// Interface of a Cosmos DB image container. This can be implemented
    /// for production or with a mock for (systematic) testing.
    /// </summary>
    public interface IImageContainer : ICosmosContainer
    {
    }
}
