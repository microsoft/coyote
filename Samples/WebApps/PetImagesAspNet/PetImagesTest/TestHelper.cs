// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;

namespace PetImagesTest
{
    public static class TestHelper
    {
        public static T Clone<T>(T obj)
        {
            return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(obj));
        }
    }
}
