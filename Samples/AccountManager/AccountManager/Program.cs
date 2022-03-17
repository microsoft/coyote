// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Coyote.Samples.AccountManager
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                PrintUsage();
            }

            foreach (var arg in args)
            {
                if (arg[0] == '-')
                {
                    switch (arg.ToUpperInvariant().Trim('-'))
                    {
                        case "S":
                            Console.WriteLine("Running sequential test without Coyote ...");
                            await TestAccountCreation();
                            Console.WriteLine("Done.");
                            return;
                        case "C":
                            Console.WriteLine("Running concurrent test without Coyote ...");
                            await TestConcurrentAccountCreation();
                            Console.WriteLine("Done.");
                            return;
                        case "?":
                        case "H":
                        case "HELP":
                            PrintUsage();
                            return;
                        default:
                            Console.WriteLine("### Unknown arg: " + arg);
                            PrintUsage();
                            return;
                    }
                }
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: AccountManager [option]");
            Console.WriteLine("Options:");
            Console.WriteLine("  -s    Run sequential test without Coyote");
            Console.WriteLine("  -c    Run concurrent test without Coyote");
        }

        [Microsoft.Coyote.SystematicTesting.Test]
        public static async Task TestAccountCreation()
        {
            // Initialize the mock in-memory DB and account manager.
            var dbCollection = new InMemoryDbCollection();
            var accountManager = new AccountManager(dbCollection);

            // Create some dummy data.
            string accountName = "MyAccount";
            string accountPayload = "...";

            // Create the account, it should complete successfully and return true.
            var result = await accountManager.CreateAccount(accountName, accountPayload);
            Assert.True(result);

            // Create the same account again. The method should return false this time.
            result = await accountManager.CreateAccount(accountName, accountPayload);
            Assert.False(result);
        }

        [Microsoft.Coyote.SystematicTesting.Test]
        public static async Task TestConcurrentAccountCreation()
        {
            // Initialize the mock in-memory DB and account manager.
            var dbCollection = new InMemoryDbCollection();
            var accountManager = new AccountManager(dbCollection);

            // Create some dummy data.
            string accountName = "MyAccount";
            string accountPayload = "...";

            // Call CreateAccount twice without awaiting, which makes both methods run
            // asynchronously with each other.
            var task1 = accountManager.CreateAccount(accountName, accountPayload);
            // await Task.Delay(1); // Enable artificial delay to make bug harder to manifest.
            var task2 = accountManager.CreateAccount(accountName, accountPayload);

            // Then wait both requests to complete.
            await Task.WhenAll(task1, task2);

            // Finally, assert that only one of the two requests succeeded and the other
            // failed. Note that we do not know which one of the two succeeded as the
            // requests ran concurrently (this is why we use an exclusive OR).
            Assert.True(task1.Result ^ task2.Result);
        }

        [Microsoft.Coyote.SystematicTesting.Test]
        public static async Task TestConcurrentAccountDeletion()
        {
            // Initialize the mock in-memory DB and account manager.
            var dbCollection = new InMemoryDbCollection();
            var accountManager = new AccountManager(dbCollection);

            // Create some dummy data.
            string accountName = "MyAccount";
            string accountPayload = "...";

            // Create the account and wait for it to complete.
            await accountManager.CreateAccount(accountName, accountPayload);

            // Call DeleteAccount twice without awaiting, which makes both methods run
            // asynchronously with each other.
            var task1 = accountManager.DeleteAccount(accountName);
            var task2 = accountManager.DeleteAccount(accountName);

            // Then wait both requests to complete.
            await Task.WhenAll(task1, task2);

            // Finally, assert that only one of the two requests succeeded and the other
            // failed. Note that we do not know which one of the two succeeded as the
            // requests ran concurrently (this is why we use an exclusive OR).
            Assert.True(task1.Result ^ task2.Result);
        }

        [Microsoft.Coyote.SystematicTesting.Test]
        public static async Task TestConcurrentAccountCreationAndDeletion()
        {
            // Initialize the mock in-memory DB and account manager.
            var dbCollection = new InMemoryDbCollection();
            var accountManager = new AccountManager(dbCollection);

            // Create some dummy data.
            string accountName = "MyAccount";
            string accountPayload = "...";

            // Call CreateAccount and DeleteAccount without awaiting, which makes both
            // methods run asynchronously with each other.
            var createTask = accountManager.CreateAccount(accountName, accountPayload);
            var deleteTask = accountManager.DeleteAccount(accountName);

            // Then wait both requests to complete.
            await Task.WhenAll(createTask, deleteTask);

            // The CreateAccount request will always succeed, no matter what. The DeleteAccount
            // may or may not succeed depending on if it finds the account already created or
            // not created while executing concurrently with the CreateAccount request.
            Assert.True(createTask.Result);

            if (!deleteTask.Result)
            {
                // The DeleteAccount request didn't find the account and failed as expected.
                // We assert that the account payload is still available.
                string fetchedAccountPayload = await accountManager.GetAccount(accountName);
                Assert.Equal(accountPayload, fetchedAccountPayload);
            }
            else
            {
                // If CreateAccount and DeleteAccount both returned true, then the account
                // must have been created before the deletion happened.
                // We assert that the payload is not available, as the account was deleted.
                string fetchedAccountPayload = await accountManager.GetAccount(accountName);
                Assert.Null(fetchedAccountPayload);
            }
        }
    }
}
