// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;
using System.Threading.Tasks;
using ImageGallery.Logging;
using ImageGallery.Models;
using ImageGallery.Store.Cosmos;
using ImageGallery.Tests.Mocks.Clients;
using ImageGallery.Tests.Mocks.Cosmos;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ImageGallery.Tests
{
    [TestClass]
    public class UnitTests
    {
        [TestMethod]
        public async Task TestConcurrentAccountRequestsAsync()
        {
            var logger = new MockLogger();
            var cosmosState = new MockCosmosState(logger);

            var client = new MockImageGalleryClient(cosmosState, logger);
            await client.InitializeCosmosDbAsync();

            // Try create a new account, and wait for it to be created before proceeding with the test.
            var account = new Account("0", "alice", "alice@coyote.com");

            var result = await client.CreateAccountAsync(account);
            Assert.IsTrue(result);

            var updatedAccount = new Account("0", "alice", "alice@microsoft.com");

            // Try update the account and delete it concurrently, which can cause a data race and a bug.
            var updateTask = client.UpdateAccountAsync(updatedAccount);
            var deleteTask = client.DeleteAccountAsync(updatedAccount.Id);

            // Wait for the two concurrent requests to complete.
            await Task.WhenAll(updateTask, deleteTask);

            // Bug: the update request can nondeterministically fail due to an unhandled exception (500 error code).
            // See the `Update` handler in the account controller for more info.
            _ = updateTask.Result;

            var deleteAccountRes = deleteTask.Result;
            // deleteAccountRes.EnsureSuccessStatusCode();
            Assert.IsTrue(deleteAccountRes);
        }

        [TestMethod]
        public async Task TestConcurrentAccountAndImageRequestsAsync()
        {
            var logger = new MockLogger();
            var cosmosState = new MockCosmosState(logger);

            var client = new MockImageGalleryClient(cosmosState, logger);
            IDatabaseProvider databaseProvider = await client.InitializeCosmosDbAsync();

            // Try create a new account, and wait for it to be created before proceeding with the test.
            var account = new Account("0", "alice", "alice@coyote.com");
            await client.CreateAccountAsync(account);

            // Try store the image and delete the account concurrently, which can cause a data race and a bug.
            var image = new Image(account.Id, "beach", Encoding.Default.GetBytes("waves"));
            var storeImageTask = client.CreateOrUpdateImageAsync(image);
            var deleteAccountTask = client.DeleteAccountAsync(account.Id);

            // Wait for the two concurrent requests to complete.
            await Task.WhenAll(storeImageTask, deleteAccountTask);

            // BUG: The above two concurrent requests can race and result into the image being stored
            // in an "orphan" container in Azure Storage, even if the associated account was deleted.

            // Check that the image was deleted from Azure Storage.
            var exists = await client.AzureStorageProvider.ExistsBlobAsync(Constants.GetContainerName(account.Id), image.Name);
            if (exists)
            {
                throw new AssertFailedException("The image was not deleted from Azure Blob Storage.");
            }

            // Check that the account was deleted from Cosmos DB.
            var accountContainer = databaseProvider.GetContainer(Constants.AccountCollectionName);
            exists = await accountContainer.ExistsItemAsync<AccountEntity>(account.Id, account.Id);
            if (exists)
            {
                throw new AssertFailedException("The account was not deleted from Cosmos DB.");
            }
        }
    }
}
