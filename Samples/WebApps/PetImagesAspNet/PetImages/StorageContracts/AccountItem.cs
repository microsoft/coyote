// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PetImages.Contracts;

namespace PetImages.Entities
{
    public class AccountItem : DbItem
    {
        public override string PartitionKey => Id;

        public Account ToAccount()
        {
            return new Account()
            {
                Name = Id
            };
        }
    }
}
