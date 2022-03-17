// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.Samples.DrinksServingRobot
{
    internal class ClientDetails
    {
        public readonly PersonType PersonType;
        public readonly Location Coordinates;

        public ClientDetails(PersonType personType, Location coordinates)
        {
            this.PersonType = personType;
            this.Coordinates = coordinates;
        }
    }

    internal enum PersonType
    {
        Adult,
        Minor
    }
}
