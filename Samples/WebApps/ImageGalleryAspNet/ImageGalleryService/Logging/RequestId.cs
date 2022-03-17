// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;

namespace ImageGallery.Logging
{
    public static class RequestId
    {
        private static readonly AsyncLocal<string> AsyncLocalInstance = new AsyncLocal<string>();

        internal static string Create(string id)
        {
            AsyncLocalInstance.Value = id;
            return id;
        }

        public static string Get() => AsyncLocalInstance.Value;
    }

    public class ApplicationLogs
    {
    }
}
