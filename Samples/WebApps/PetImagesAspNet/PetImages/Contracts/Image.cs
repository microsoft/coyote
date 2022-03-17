// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PetImages.Entities;

namespace PetImages.Contracts
{
    public class Image
    {
        public string Name { get; set; }

        public string ImageType { get; set; }

        public string[] Tags { get; set; }

        public byte[] Content { get; set; }

        public ImageItem ToItem()
        {
            return new ImageItem()
            {
                Id = Name,
                StorageName = Name,
                ImageType = ImageType,
                Tags = Tags
            };
        }
    }
}
