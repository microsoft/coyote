// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.Samples.DrinksServingRobot
{
    internal class DrinkOrder
    {
        public readonly ClientDetails ClientDetails;

        public DrinkOrder(ClientDetails clientDetails)
        {
            this.ClientDetails = clientDetails;
        }
    }
}
