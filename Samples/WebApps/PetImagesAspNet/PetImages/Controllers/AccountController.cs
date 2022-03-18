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
            var accountItem = account.ToItem();

            Console.WriteLine($"[{System.Threading.Thread.CurrentThread.ManagedThreadId}] AccountController.CreateAccountAsync: {accountItem.Id}");
            if (await StorageHelper.DoesItemExist<AccountItem>(
                this.AccountContainer,
                accountItem.PartitionKey,
                accountItem.Id))
            {
                Console.WriteLine($"[{System.Threading.Thread.CurrentThread.ManagedThreadId}] conflict");
                return this.Conflict();
            }

            Console.WriteLine($"[{System.Threading.Thread.CurrentThread.ManagedThreadId}] trying to create ...");
            var createdAccountItem = await this.AccountContainer.CreateItem(accountItem);
            return this.Ok(createdAccountItem.ToAccount());
        }

        /// <summary>
        /// Scenario 1: Fixed CreateAccountAsync version.
        /// </summary>
        [HttpPost("create-fixed")]
        public async Task<ActionResult<Account>> CreateAccountAsyncFixed(Account account)
        {
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
