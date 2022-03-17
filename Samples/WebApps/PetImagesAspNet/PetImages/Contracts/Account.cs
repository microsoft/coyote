// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PetImages.Entities;

namespace PetImages.Contracts
{
    public class Account
    {
        public string Name { get; set; }

        public AccountItem ToItem()
        {
            return new AccountItem()
            {
                Id = Name
            };
        }
    }
}
