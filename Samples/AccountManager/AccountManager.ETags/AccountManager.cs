// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace Microsoft.Coyote.Samples.AccountManager.ETags
{
    public class AccountManager
    {
        private readonly IDbCollection AccountCollection;

        public AccountManager(IDbCollection dbCollection)
        {
            this.AccountCollection = dbCollection;
        }

        // Returns true if the account is created, else false.
        public async Task<bool> CreateAccount(string accountName, string accountPayload, int accountVersion)
        {
            var account = new Account()
            {
                Name = accountName,
                Payload = accountPayload,
                Version = accountVersion
            };

            try
            {
                return await this.AccountCollection.CreateRow(accountName, JsonSerializer.Serialize(account));
            }
            catch (RowAlreadyExistsException)
            {
                return false;
            }
        }

        // Returns true if the account is updated, else false.
        public async Task<bool> UpdateAccount(string accountName, string accountPayload, int accountVersion)
        {
            Account existingAccount;
            Guid existingAccountETag;

            // Naive retry if ETags mismatch. In reality, you would use a proper retry policy.
            while (true)
            {
                try
                {
                    (string value, Guid etag) = await this.AccountCollection.GetRow(accountName);
                    existingAccount = JsonSerializer.Deserialize<Account>(value);
                    existingAccountETag = etag;
                }
                catch (RowNotFoundException)
                {
                    return false;
                }

                if (accountVersion <= existingAccount.Version)
                {
                    return false;
                }

                var updatedAccount = new Account()
                {
                    Name = accountName,
                    Payload = accountPayload,
                    Version = accountVersion
                };

                try
                {
                    return await this.AccountCollection.UpdateRow(accountName,
                        JsonSerializer.Serialize(updatedAccount), existingAccountETag);
                }
                catch (MismatchedETagException)
                {
                    continue;
                }
                catch (RowNotFoundException)
                {
                    return false;
                }
            }
        }

        // Returns the account if found, else null.
        public async Task<Account> GetAccount(string accountName)
        {
            try
            {
                (string value, Guid _) = await this.AccountCollection.GetRow(accountName);
                return JsonSerializer.Deserialize<Account>(value);
            }
            catch (RowNotFoundException)
            {
                return null;
            }
        }

        // Returns true if the account is deleted, else false.
        public async Task<bool> DeleteAccount(string accountName)
        {
            try
            {
                return await this.AccountCollection.DeleteRow(accountName);
            }
            catch (RowNotFoundException)
            {
                return false;
            }
        }
    }
}
