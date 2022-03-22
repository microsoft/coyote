// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;

namespace PetImages.Tests
{
    internal static class TestHelper
    {
        internal static T Clone<T>(T obj) =>
            JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(obj));
    }
}
