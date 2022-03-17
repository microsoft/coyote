// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace ImageGallery.Store.AzureStorage
{
    public class BlobPage
    {
        public string[] Names { get; set; }

        public string ContinuationId { get; set; }
    }
}
