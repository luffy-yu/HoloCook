//  
// Copyright (c) 2024 Liuchuan Yu. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//


namespace HoloCook.Menu
{
    public static class Static
    {
        public enum Stuff
        {
            // pancake
            Pan,
            Egg1,
            Egg2,
            Banana,
            Spoon,
            Plate,
            Bowl, // shared
            TearedBanana,
            BananaSlices,
            Oil,
            Whisk,
            Turner,
            Knife,
            Board,
            // cocktail
            Ice,
            BigCup,
            SmallCup,
            Beer,
            Rum,
            LimeJuice
        }

        public static string GetName(Stuff s)
        {
            return s.ToString();
        }
    }
}