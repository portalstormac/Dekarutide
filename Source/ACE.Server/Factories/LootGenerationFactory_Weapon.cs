using ACE.Common;
using ACE.Database.Models.World;
using ACE.Entity.Enum;
using ACE.Server.Factories.Tables;
using ACE.Server.WorldObjects;

namespace ACE.Server.Factories
{
    public static partial class LootGenerationFactory
    {
        private static WorldObject CreateWeapon(TreasureDeath profile, bool isMagical)
        {
            int chance = ThreadSafeRandom.Next(1, 100);

            // Aligning drop ratio to better align with retail - HarliQ 11/11/19
            // Melee - 42%
            // Missile - 36%
            // Casters - 22%

            return chance switch
            {
                var rate when (rate < 43) => CreateMeleeWeapon(profile, isMagical),
                var rate when (rate > 42 && rate < 79) => CreateMissileWeapon(profile, isMagical),
                _ => CreateCaster(profile, isMagical),
            };
        }

        private static float RollWeaponSpeedMod(TreasureDeath treasureDeath)
        {
            var qualityLevel = QualityChance.Roll(treasureDeath);

            if (qualityLevel == 0)
                return 1.0f;    // no bonus

            var rng = (float)ThreadSafeRandom.Next(-0.025f, 0.025f);

            // min/max range: 67.5% - 100%
            var weaponSpeedMod = 1.0f - (qualityLevel * 0.025f + rng);

            //Console.WriteLine($"WeaponSpeedMod: {weaponSpeedMod}");

            return weaponSpeedMod;
        }

        private static float RollCrushingBlow(TreasureDeath treasureDeath, bool isCaster)
        {
            if (Common.ConfigManager.Config.Server.WorldRuleset != Common.Ruleset.CustomDM)
                return 0.0f;

            var chance = ExtraWeaponEffects.GetCrushingBlowChanceForTier(treasureDeath.Tier);
            if (chance > ThreadSafeRandom.Next(0.0f, 1.0f))
            {
                if (isCaster)
                    return 0.5f;
                else
                    return 2.0f;
            }
            return 0.0f;
        }

        private static float RollBitingStrike(TreasureDeath treasureDeath)
        {
            if (Common.ConfigManager.Config.Server.WorldRuleset != Common.Ruleset.CustomDM)
                return 0.0f;

            var chance = ExtraWeaponEffects.GetBitingStrikeChanceForTier(treasureDeath.Tier);
            if (chance > ThreadSafeRandom.Next(0.0f, 1.0f))
                return 0.15f;
            return 0.0f;
        }

        private static CreatureType RollSlayerType(TreasureDeath treasureDeath)
        {
            if (Common.ConfigManager.Config.Server.WorldRuleset != Common.Ruleset.CustomDM)
                return CreatureType.Invalid;

            var chance = SlayerTypeChance.GetSlayerChanceForTier(treasureDeath.Tier);
            if (chance > ThreadSafeRandom.Next(0.0f, 1.0f))
                return SlayerTypeChance.Roll(treasureDeath);
            return CreatureType.Invalid;
        }

        private static float RollSlayerAmount(TreasureDeath treasureDeath)
        {
            if (Common.ConfigManager.Config.Server.WorldRuleset != Common.Ruleset.CustomDM)
                return 0.0f;
            return 1.5f;
        }
    }
}
