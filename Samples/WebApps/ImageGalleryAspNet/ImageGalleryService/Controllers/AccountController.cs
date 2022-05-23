// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using ImageGallery.Logging;
using ImageGallery.Models;
using ImageGallery.Store.AzureStorage;
using ImageGallery.Store.Cosmos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ImageGallery.Controllers
{
    [ApiController]
    public class AccountController : ControllerBase
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

        private readonly IDatabaseProvider DatabaseProvider;
        private IContainerProvider AccountContainer;
        private readonly IBlobContainerProvider StorageProvider;
        private readonly ILogger Logger;

        public AccountController(IDatabaseProvider databaseProvider, IBlobContainerProvider storageProvider, ILogger<ApplicationLogs> logger)
        {
            this.DatabaseProvider = databaseProvider;
            this.StorageProvider = storageProvider;
            this.Logger = logger;
        }

        private async Task<IContainerProvider> GetOrCreateContainer()
        {
            await InjectYieldsAtMethodStart();
            if (this.AccountContainer == null)
            {
                this.AccountContainer = await this.DatabaseProvider.CreateContainerIfNotExistsAsync(Constants.AccountCollectionName, "/id");
            }
            return this.AccountContainer;
        }

        [HttpPut]
        [Produces(typeof(ActionResult<Account>))]
        [Route("api/account/create")]
        public async Task<ActionResult<Account>> Create(Account account)
        {
            await InjectYieldsAtMethodStart();
            this.Logger.LogInformation("Creating account with id '{0}' (name: '{1}', email: '{2}').",
                account.Id, account.Name, account.Email);

            // Check if the account exists in Cosmos DB.
            var container = await GetOrCreateContainer();
            var exists = await container.ExistsItemAsync<AccountEntity>(account.Id, account.Id);
            if (exists)
            {
                return this.NotFound();
            }

            // BUG: calling create on the Cosmos DB container after checking if the account exists is racy
            // and can, for example, fail due to another concurrent request. Typically someone could write
            // a create or update request, that uses the `UpsertItemAsync` Cosmos DB API, but we dont use
            // it here just for the purposes of this buggy sample service.

            // The account does not exist, so create it in Cosmos DB.
            var entity = await container.CreateItemAsync(new AccountEntity(account));
            return this.Ok(entity.GetAccount());
        }

        [HttpPut]
        [Produces(typeof(ActionResult<Account>))]
        [Route("api/account/update")]
        public async Task<ActionResult<Account>> Update(Account account)
        {
            await InjectYieldsAtMethodStart();
            this.Logger.LogInformation("Updating account with id '{0}' (name: '{1}', email: '{2}').",
                account.Id, account.Name, account.Email);

            // Check if the account exists in Cosmos DB.
            var container = await GetOrCreateContainer();
            var exists = await container.ExistsItemAsync<AccountEntity>(account.Id, account.Id);
            if (!exists)
            {
                return this.NotFound();
            }

            // BUG: calling update on the Cosmos DB container after checking if the account exists is racy
            // and can, for example, fail due to another concurrent request. This throws an exception
            // that the controller does not handle, and thus is reported as a 500. This can be fixed
            // by properly handling ReplaceItemAsync and returning a `NotFound` instead.

            // Update the account in Cosmos DB.
            var entity = await container.ReplaceItemAsync(new AccountEntity(account));
            return this.Ok(entity.GetAccount());
        }

        [HttpGet]
        [Produces(typeof(ActionResult<Account>))]
        [Route("api/account/get/")]
        public async Task<ActionResult<Account>> Get(string id)
        {
            await InjectYieldsAtMethodStart();
            this.Logger.LogInformation("Getting account with id '{0}'.", id);

            // Check if the account exists in Cosmos DB.
            var container = await GetOrCreateContainer();
            var exists = await container.ExistsItemAsync<AccountEntity>(id, id);
            if (!exists)
            {
                return this.NotFound();
            }

            // BUG: calling get on the Cosmos DB container after checking if the account exists is racy
            // and can, for example, fail due to another concurrent request that deleted the account.

            // The account exists, so get it from Cosmos DB.
            var entity = await container.ReadItemAsync<AccountEntity>(id, id);
            return this.Ok(entity.GetAccount());
        }

        [HttpDelete]
        [Produces(typeof(ActionResult))]
        [Route("api/account/delete/")]
        public async Task<ActionResult> Delete(string id)
        {
            await InjectYieldsAtMethodStart();
            this.Logger.LogInformation("Deleting account with id '{0}'.", id);

            // Check if the account exists in Cosmos DB.
            var container = await GetOrCreateContainer();
            var exists = await container.ExistsItemAsync<AccountEntity>(id, id);
            if (!exists)
            {
                return this.NotFound();
            }

            // BUG: calling the following APIs after checking if the account exists is racy and can
            // fail due to another concurrent request.

            // The account exists, so delete it from Cosmos DB.
            await container.DeleteItemAsync<AccountEntity>(id, id);

            // Finally, if there is an image container for this account, then also delete it.
            var containerName = Constants.GetContainerName(id);
            await this.StorageProvider.DeleteContainerIfExistsAsync(containerName);

            return this.Ok();
        }
    }
}
