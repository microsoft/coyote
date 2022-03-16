// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace ImageGallery.Models
{
    public class ErrorViewModel
    {
        public string RequestId { get; set; }

        public string Message { get; set; }

        public string Trace { get;set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}