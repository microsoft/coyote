// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;

namespace Microsoft.Coyote.Samples.AccountManager.ETags
{
    public interface IDbCollection
    {
        Task<bool> CreateRow(string key, string value);

        Task<bool> DoesRowExist(string key);

        Task<(string value, Guid etag)> GetRow(string key);

        Task<bool> UpdateRow(string key, string value, Guid etag);

        Task<bool> DeleteRow(string key);
    }
}
