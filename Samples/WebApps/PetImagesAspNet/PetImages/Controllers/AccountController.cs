// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PetImages.Contracts;
using PetImages.Entities;
using PetImages.Exceptions;
using PetImages.Storage;

namespace PetImages.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly ICosmosContainer CosmosContainer;

        public AccountController(ICosmosContainer cosmosDb)
        {
            this.CosmosContainer = cosmosDb;
        }

        /// <summary>
        /// Scenario 1: Buggy CreateAccountAsync version.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Account>> CreateAccountAsync(Account account)
        {
            var accountItem = account.ToItem();

            if (await StorageHelper.DoesItemExist<AccountItem>(
                this.CosmosContainer,
                accountItem.PartitionKey,
                accountItem.Id))
            {
                return this.Conflict();
            }

            var createdAccountItem = await this.CosmosContainer.CreateItem(accountItem);

            return this.Ok(createdAccountItem.ToAccount());
        }

        /// <summary>
        /// Scenario 1: Fixed CreateAccountAsync version.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Account>> CreateAccountAsyncFixed(Account account)
        {
            var accountItem = account.ToItem();

            try
            {
                accountItem = await this.CosmosContainer.CreateItem(accountItem);
            }
            catch (DatabaseItemAlreadyExistsException)
            {
                return this.Conflict();
            }

            return this.Ok(accountItem.ToAccount());
        }   
    }
}
