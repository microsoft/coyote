// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace ImageGallery.Models
{
    public class GalleryViewModel
    {
        public string RequestId { get; set; }

        public string User { get; set; }

        public string[] Images{ get; set; }

        public string Continuation { get; set; }
    }
}