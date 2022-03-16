// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.Coyote.Samples.DrinksServingRobot
{
    internal static class Drinks
    {
        public static readonly List<DrinkType> ForAdults = new List<DrinkType>
        {
            DrinkType.Absinth,
            DrinkType.Beer,
            DrinkType.Champagne,
            DrinkType.Gin,
            DrinkType.Gin_And_Tonic,
            DrinkType.Margarita,
            DrinkType.Pina_Colada,
            DrinkType.Rum,
            DrinkType.Rum_And_Coke,
            DrinkType.Sparkling_Wine,
            DrinkType.Tequila,
            DrinkType.Vodka,
            DrinkType.Vodka_And_Orange,
            DrinkType.Whiskey_With_Ice,
            DrinkType.Wine,
        };

        public static readonly List<DrinkType> ForMinors = new List<DrinkType>
        {
            DrinkType.Arctic_Shark_Glacier_Punch,
            DrinkType.Banana_Berry_Smoothie,
            DrinkType.Candy_Apple_Punch,
            DrinkType.Coke,
            DrinkType.Fresca,
            DrinkType.Frostie_Orange_Smoothie,
            DrinkType.Frozen_Hot_Chocolate,
            DrinkType.Ginger_Ale,
            DrinkType.Hawaiian_Punch_Summer_Drink,
            DrinkType.Lemonade,
            DrinkType.Sprite,
            DrinkType.Swamp_Juice,
            DrinkType.Tonic_Water,
            DrinkType.Water,
            DrinkType.WaterMelonLemonade
        };
    }

    internal enum DrinkType
    {
        Absinth,
        Beer,
        Champagne,
        Gin,
        Gin_And_Tonic,
        Margarita,
        Pina_Colada,
        Rum,
        Rum_And_Coke,
        Sparkling_Wine,
        Tequila,
        Vodka,
        Vodka_And_Orange,
        Whiskey_With_Ice,
        Wine,

        // For Minors
        Arctic_Shark_Glacier_Punch,
        Banana_Berry_Smoothie,
        Candy_Apple_Punch,
        Coke,
        Fresca,
        Frostie_Orange_Smoothie,
        Frozen_Hot_Chocolate,
        Ginger_Ale,
        Hawaiian_Punch_Summer_Drink,
        Lemonade,
        Sprite,
        Swamp_Juice,
        Tonic_Water,
        Water,
        WaterMelonLemonade,
    }
}
