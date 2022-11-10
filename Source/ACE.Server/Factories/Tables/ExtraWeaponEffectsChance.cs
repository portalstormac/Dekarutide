using System.Collections.Generic;

using ACE.Server.Factories.Entity;
using ACE.Database.Models.World;
using ACE.Entity.Enum;

namespace ACE.Server.Factories.Tables
{
    public static class ExtraWeaponEffects
    {
        public static float GetCrushingBlowChanceForTier(int tier)
        {
            switch (tier)
            {
                case 1:
                default:
                    return 0.01f;
                case 2:
                    return 0.01f;
                case 3:
                    return 0.01f;
                case 4:
                    return 0.01f;
                case 5:
                    return 0.01f;
                case 6:
                    return 0.01f;
                case 7:
                    return 0.01f;
                case 8:
                    return 0.01f;
            }
        }
        public static float GetBitingStrikeChanceForTier(int tier)
        {
            switch (tier)
            {
                case 1:
                default:
                    return 0.01f;
                case 2:
                    return 0.01f;
                case 3:
                    return 0.01f;
                case 4:
                    return 0.01f;
                case 5:
                    return 0.01f;
                case 6:
                    return 0.01f;
                case 7:
                    return 0.01f;
                case 8:
                    return 0.01f;
            }
        }
    }
}
