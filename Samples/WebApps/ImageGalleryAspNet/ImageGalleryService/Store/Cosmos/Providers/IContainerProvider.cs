// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace ImageGallery.Store.Cosmos
{
    /// <summary>
    /// Interface of an Azure Cosmos DB container provider. This can be implemented
    /// for production or with a mock for (systematic) testing.
    /// </summary>
    public interface IContainerProvider
    {
        Task<T> CreateItemAsync<T>(T entity, ItemRequestOptions requestOptions = null)
            where T : CosmosEntity;

        Task<T> ReplaceItemAsync<T>(T entity, ItemRequestOptions requestOptions = null)
            where T : CosmosEntity;

        Task<T> UpsertItemAsync<T>(T entity, ItemRequestOptions requestOptions = null)
            where T : CosmosEntity;

        Task DeleteItemAsync<T>(string id, string partitionKey, ItemRequestOptions requestOptions = null)
            where T : CosmosEntity;

        Task<T> ReadItemAsync<T>(string partitionKey, string id, ItemRequestOptions requestOptions = null)
            where T : CosmosEntity;

        Task<IList<T>> ReadItemsAcrossPartitionsAsync<T>(Expression<Func<T, bool>> predicate)
            where T : CosmosEntity;

        Task<IList<T>> ReadItemsInPartitionAsync<T>(string partitionKey, Expression<Func<T, bool>> predicate)
            where T : CosmosEntity;

        Task<bool> ExistsItemAsync<T>(string partitionKey, string id, ItemRequestOptions requestOptions = null)
            where T : CosmosEntity;
    }
}
