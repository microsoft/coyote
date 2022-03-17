// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using PetImages.Entities;
using PetImages.Exceptions;

namespace PetImages.Storage
{
    public static class StorageHelper
    {
        public static async Task<bool> DoesItemExist<T>(ICosmosContainer container, string partitionKey, string id)
            where T : DbItem
        {
            try
            {
                await container.GetItem<T>(partitionKey, id);
                return true;
            }
            catch (DatabaseItemDoesNotExistException)
            {
                return false;
            }
        }
    }
}
