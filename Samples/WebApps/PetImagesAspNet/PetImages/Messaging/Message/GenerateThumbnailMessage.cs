// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PetImages.Messaging
{
    public class GenerateThumbnailMessage : Message
    {
        public string AccountName { get; set; }

        public string ImageStorageName { get; set; }
        public GenerateThumbnailMessage()
        {
            Type = GenerateThumbnailMessageType;
        }
    }
}
