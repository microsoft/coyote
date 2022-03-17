// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace PetImages.Entities
{
    public abstract class DbItem
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        public abstract string PartitionKey { get; }
    }
}
