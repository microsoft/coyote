// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Microsoft.Coyote.Samples.AccountManager
{
    public class InMemoryDbCollection : IDbCollection
    {
        private readonly ConcurrentDictionary<string, string> Collection;

        public InMemoryDbCollection()
        {
            this.Collection = new ConcurrentDictionary<string, string>();
        }

        public Task<bool> CreateRow(string key, string value)
        {
            return Task.Run(() =>
            {
                bool success = this.Collection.TryAdd(key, value);
                if (!success)
                {
                    throw new RowAlreadyExistsException();
                }

                return true;
            });
        }

        public Task<bool> DoesRowExist(string key)
        {
            return Task.Run(() =>
            {
                return this.Collection.ContainsKey(key);
            });
        }

        public Task<string> GetRow(string key)
        {
            return Task.Run(() =>
            {
                bool success = this.Collection.TryGetValue(key, out string value);
                if (!success)
                {
                    throw new RowNotFoundException();
                }

                return value;
            });
        }

        public Task<bool> DeleteRow(string key)
        {
            return Task.Run(() =>
            {
                bool success = this.Collection.TryRemove(key, out string _);
                if (!success)
                {
                    throw new RowNotFoundException();
                }

                return true;
            });
        }
    }
}
