// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;

namespace Microsoft.Coyote.Samples.AccountManager
{
    public class AccountManager
    {
        private readonly IDbCollection AccountCollection;

        public AccountManager(IDbCollection dbCollection)
        {
            this.AccountCollection = dbCollection;
        }

        // Returns true if the account is created, else false.
        public async Task<bool> CreateAccount(string accountName, string accountPayload)
        {
            if (await this.AccountCollection.DoesRowExist(accountName))
            {
                return false;
            }

            return await this.AccountCollection.CreateRow(accountName, accountPayload);
        }

        // Returns the accountPayload if the account is found, else null.
        public async Task<string> GetAccount(string accountName)
        {
            if (!await this.AccountCollection.DoesRowExist(accountName))
            {
                return null;
            }

            return await this.AccountCollection.GetRow(accountName);
        }

        // Returns true if the account is deleted, else false.
        public async Task<bool> DeleteAccount(string accountName)
        {
            if (!await this.AccountCollection.DoesRowExist(accountName))
            {
                return false;
            }

            return await this.AccountCollection.DeleteRow(accountName);
        }
    }
}
