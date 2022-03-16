// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.Samples.DrinksServingRobot
{
    internal class Glass
    {
        public DrinkType DrinkType { get; set; }

        public int DrinkLevel { get; private set; }

        public Glass(DrinkType drinkType, int drinkLevel)
        {
            this.DrinkType = drinkType;
            this.DrinkLevel = drinkLevel;
        }
    }
}
