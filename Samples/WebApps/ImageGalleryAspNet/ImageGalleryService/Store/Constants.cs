// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace ImageGallery
{
    public static class Constants
    {
        public static string DatabaseName = "ImageGalleryDB";
        public static string AccountCollectionName = "Accounts";
        public static string GalleryContainerName = "Gallery";

        public static string GetContainerName(string accountId) => $"{GalleryContainerName}-{accountId}".ToLowerInvariant();
    }
}
