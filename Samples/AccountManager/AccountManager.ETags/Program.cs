// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Coyote.Samples.AccountManager.ETags
{
    public static class Program
    {
        public static async Task Main()
        {
            await TestConcurrentAccountUpdate();
        }

        [Microsoft.Coyote.SystematicTesting.Test]
        public static async Task TestAccountUpdate()
        {
            // Initialize the mock in-memory DB and account manager.
            var dbCollection = new InMemoryDbCollection();
            var accountManager = new AccountManager(dbCollection);

            string accountName = "MyAccount";

            // Create the account, it should complete successfully and return true.
            var result = await accountManager.CreateAccount(accountName, "first_version", 1);
            Assert.True(result);

            result = await accountManager.UpdateAccount(accountName, "second_version", 2);
            Assert.True(result);

            result = await accountManager.UpdateAccount(accountName, "second_version_alt", 2);
            Assert.False(result);
        }

        [Microsoft.Coyote.SystematicTesting.Test]
        public static async Task TestConcurrentAccountUpdate()
        {
            // Initialize the mock in-memory DB and account manager.
            var dbCollection = new InMemoryDbCollection();
            var accountManager = new AccountManager(dbCollection);

            string accountName = "MyAccount";

            // Create the account, it should complete successfully and return true.
            var result = await accountManager.CreateAccount(accountName, "first_version", 1);
            Assert.True(result);

            // Call UpdateAccount twice without awaiting, which makes both methods run
            // asynchronously with each other.
            var task1 = accountManager.UpdateAccount(accountName, "second_version", 2);
            var task2 = accountManager.UpdateAccount(accountName, "second_version_alt", 2);

            // Then wait both requests to complete.
            await Task.WhenAll(task1, task2);

            // Finally, assert that only one of the two requests succeeded and the other
            // failed. Note that we do not know which one of the two succeeded as the
            // requests ran concurrently (this is why we use an exclusive OR).
            Assert.True(task1.Result ^ task2.Result);
        }

        [Microsoft.Coyote.SystematicTesting.Test]
        public static async Task TestGetAccountAfterConcurrentUpdate()
        {
            // Initialize the mock in-memory DB and account manager.
            var dbCollection = new InMemoryDbCollection();
            var accountManager = new AccountManager(dbCollection);

            string accountName = "MyAccount";

            // Create the account, it should complete successfully and return true.
            var result = await accountManager.CreateAccount(accountName, "first_version", 1);
            Assert.True(result);

            // Call UpdateAccount twice without awaiting, which makes both methods run
            // asynchronously with each other.
            var task1 = accountManager.UpdateAccount(accountName, "second_version", 2);
            var task2 = accountManager.UpdateAccount(accountName, "third_version", 3);

            // Then wait both requests to complete.
            await Task.WhenAll(task1, task2);

            // Finally, get the account and assert that the version is always 3,
            // which is the latest updated version.
            var account = await accountManager.GetAccount(accountName);
            Assert.True(account.Version == 3);
        }
    }
}
