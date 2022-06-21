// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PetImages.Contracts;
using PetImages.Entities;
using PetImages.Exceptions;
using PetImages.Storage;

namespace PetImages.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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

        private readonly IAccountContainer AccountContainer;

        public AccountController(IAccountContainer accountContainer)
        {
            this.AccountContainer = accountContainer;
        }

        /// <summary>
        /// Scenario 1: Buggy CreateAccountAsync version.
        /// </summary>
        [HttpPost("create")]
        [Produces(typeof(ActionResult<Account>))]
        public async Task<ActionResult<Account>> CreateAccountAsync(Account account)
        {
            await InjectYieldsAtMethodStart();

            var accountItem = account.ToItem();

            if (await StorageHelper.DoesItemExist<AccountItem>(
                this.AccountContainer,
                accountItem.PartitionKey,
                accountItem.Id))
            {
                return this.Conflict();
            }

            var createdAccountItem = await this.AccountContainer.CreateItem(accountItem);
            return this.Ok(createdAccountItem.ToAccount());
        }

        /// <summary>
        /// Scenario 1: Fixed CreateAccountAsync version.
        /// </summary>
        [HttpPost("create-fixed")]
        public async Task<ActionResult<Account>> CreateAccountAsyncFixed(Account account)
        {
            await InjectYieldsAtMethodStart();

            var accountItem = account.ToItem();

            try
            {
                accountItem = await this.AccountContainer.CreateItem(accountItem);
            }
            catch (DatabaseItemAlreadyExistsException)
            {
                return this.Conflict();
            }

            return this.Ok(accountItem.ToAccount());
        }   
    }
}
