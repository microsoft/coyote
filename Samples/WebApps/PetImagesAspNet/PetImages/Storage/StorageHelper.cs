// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using PetImages.Entities;
using PetImages.Exceptions;

namespace PetImages.Storage
{
    public static class StorageHelper
    {
        public static async Task InjectYieldsAtMethodStart()
        {
            string envYiledLoop = Environment.GetEnvironmentVariable("YIELDS_METHOD_START");
            int envYiledLoopInt = 0;
            if (envYiledLoop != null)
            {
#pragma warning disable CA1305 // Specify IFormatProvider
                envYiledLoopInt = int.Parse(envYiledLoop);
#pragma warning restore CA1305 // Specify IFormatProvider
            }

            for (int i = 0; i < envYiledLoopInt; i++)
            {
                await Task.Yield();
            }
        }

        public static async Task<bool> DoesItemExist<T>(ICosmosContainer container, string partitionKey, string id)
            where T : DbItem
        {
            await InjectYieldsAtMethodStart();

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
