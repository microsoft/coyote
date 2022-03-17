// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Coyote.Samples.DrinksServingRobot
{
    internal class Utilities
    {
        public static Location GetRandomLocation(Func<int, int> randomInteger, int minX, int minY, int maxX, int maxY)
        {
            return new Location(randomInteger(maxX - minX) + minX, randomInteger(maxY - minY) + minY);
        }

        public static PersonType GetRandomPersonType(Func<int, int> randomInteger)
        {
            var personTypes = (PersonType[])Enum.GetValues(typeof(PersonType));
            var randomIndex = randomInteger(personTypes.Length);
            return personTypes[randomIndex];
        }
    }
}
