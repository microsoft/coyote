// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace ImageGallery.Models
{
    public class ImageList
    {
        public string AccountId { get; set; }

        public string[] Names { get; set; }

        public string ContinuationId { get; set; }

        public ImageList()
        {
        }

        public ImageList(string accountId, string[] names, string continuationId)
        {
            this.AccountId = accountId;
            this.Names = names;
            this.ContinuationId = continuationId;
        }
    }
}
