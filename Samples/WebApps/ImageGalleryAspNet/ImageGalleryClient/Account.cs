// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace ImageGallery.Models
{
    public class Account
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Password { get; set; }

        public string Email { get; set; }

        public Account()
        {
        }

        public Account(string id, string name, string email)
        {
            this.Id = id;
            this.Name = name;
            this.Email = email;
        }

        public override bool Equals(object obj) => obj is Account account && Id == account.Id;

        public override int GetHashCode() => this.Id.GetHashCode();
    }
}
