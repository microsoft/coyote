// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace ImageGallery.Models
{
    public class Image
    {
        public string AccountId { get; set; }

        public string Name { get; set; }

        public byte[] Contents { get; set; }

        public Image()
        {
        }

        public Image(string accountId, string name, byte[] contents)
        {
            this.AccountId = accountId;
            this.Name = name;
            this.Contents = contents;
        }
    }
}
