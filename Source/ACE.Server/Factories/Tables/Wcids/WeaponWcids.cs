using ACE.Common;
using ACE.Database.Models.World;
using ACE.Entity.Enum;
using ACE.Server.Factories.Enum;

using WeenieClassName = ACE.Server.Factories.Enum.WeenieClassName;

namespace ACE.Server.Factories.Tables.Wcids
{
    public static class WeaponWcids
    {
        public static WeenieClassName Roll(TreasureDeath treasureDeath, ref TreasureWeaponType weaponType)
        {
            switch (weaponType)
            {
                /*case TreasureWeaponType.Sword:
                    return RollSwordWcid(treasureDeath);

                case TreasureWeaponType.Mace:
                    return RollMaceWcid(treasureDeath);

                case TreasureWeaponType.Axe:
                    return RollAxeWcid(treasureDeath);

                case TreasureWeaponType.Spear:
                    return RollSpearWcid(treasureDeath);

                case TreasureWeaponType.Unarmed:
                    return RollUnarmedWcid(treasureDeath);

                case TreasureWeaponType.Staff:
                    return RollStaffWcid(treasureDeath);

                case TreasureWeaponType.Dagger:
                    return RollDaggerWcid(treasureDeath);*/

                case TreasureWeaponType.Axe:
                case TreasureWeaponType.Dagger:
                case TreasureWeaponType.Mace:
                case TreasureWeaponType.Spear:
                case TreasureWeaponType.Staff:
                case TreasureWeaponType.Sword:
                case TreasureWeaponType.Unarmed:
                    if (ConfigManager.Config.Server.WorldRuleset <= Common.Ruleset.Infiltration)
                    {
                        switch (weaponType)
                        {
                            case TreasureWeaponType.Sword:
                                return RollSwordWcid(treasureDeath, ref weaponType);

                            case TreasureWeaponType.Mace:
                                return RollMaceWcid(treasureDeath, ref weaponType);

                            case TreasureWeaponType.Axe:
                                return RollAxeWcid(treasureDeath, ref weaponType);

                            case TreasureWeaponType.Spear:
                                return RollSpearWcid(treasureDeath, ref weaponType);

                            case TreasureWeaponType.Unarmed:
                                return RollUnarmedWcid(treasureDeath);

                            case TreasureWeaponType.Staff:
                                return RollStaffWcid(treasureDeath);

                            case TreasureWeaponType.Dagger:
                                return RollDaggerWcid(treasureDeath, ref weaponType);
                        }
                    }
                    return RollMeleeWeapon(ref weaponType);

                case TreasureWeaponType.Bow:
                    return RollBowWcid(treasureDeath, ref weaponType);

                case TreasureWeaponType.Crossbow:
                    return RollCrossbowWcid(treasureDeath, ref weaponType);

                case TreasureWeaponType.Atlatl:
                    return RollAtlatlWcid(treasureDeath, ref weaponType);

                case TreasureWeaponType.Caster:
                    return RollCaster(treasureDeath);

                case TreasureWeaponType.TwoHandedWeapon:
                    return RollTwoHandedWeaponWcid(ref weaponType);
            }
            return WeenieClassName.undef;
        }

        public static WeenieClassName RollMeleeWeapon(ref TreasureWeaponType weaponType)
        {
            // retail did something silly here --
            // instead of having an even chance to roll between heavy/light/finesse,
            // they kept everything divied up by weaponType

            // doing something slightly less silly here...

            // basically throwing out the weaponType here,
            // rolling for an even chance between heavy/light/finesse,
            // and then an even chance into each weapon for that skill

            // if you wish to maintain a profile closer to retail overall,
            // heavy 36% | light 30% | finesse 34%

            // however, it still wouldn't match up exactly,
            // as each weaponType still had slightly different chances,
            // which were most likely engrained deeply in the per-weaponType chance tables

            var weaponSkill = (MeleeWeaponSkill)ThreadSafeRandom.Next(1, 3);

            switch (weaponSkill)
            {
                case MeleeWeaponSkill.HeavyWeapons:
                    return HeavyWeaponWcids.Roll(out weaponType);

                case MeleeWeaponSkill.LightWeapons:
                    return LightWeaponWcids.Roll(out weaponType);

                case MeleeWeaponSkill.FinesseWeapons:
                    return FinesseWeaponWcids.Roll(out weaponType);
            }
            return WeenieClassName.undef;
        }

        public static TreasureHeritageGroup RollHeritage(TreasureDeath treasureDeath)
        {
            return HeritageChance.Roll(treasureDeath.UnknownChances);
        }

        public static WeenieClassName RollSwordWcid(TreasureDeath treasureDeath, ref TreasureWeaponType weaponType)
        {
            var heritage = RollHeritage(treasureDeath);

            switch (heritage)
            {
                case TreasureHeritageGroup.Aluvian:
                    return SwordWcids_Aluvian.Roll(treasureDeath.Tier, out weaponType);

                case TreasureHeritageGroup.Gharundim:
                    return SwordWcids_Gharundim.Roll(treasureDeath.Tier, out weaponType);

                case TreasureHeritageGroup.Sho:
                    return SwordWcids_Sho.Roll(treasureDeath.Tier, out weaponType);
            }
            return WeenieClassName.undef;
        }

        public static WeenieClassName RollMaceWcid(TreasureDeath treasureDeath, ref TreasureWeaponType weaponType)
        {
            var heritage = RollHeritage(treasureDeath);

            return MaceWcids.Roll(heritage, treasureDeath.Tier, out weaponType);
        }

        public static WeenieClassName RollAxeWcid(TreasureDeath treasureDeath, ref TreasureWeaponType weaponType)
        {
            var heritage = RollHeritage(treasureDeath);

            return AxeWcids.Roll(heritage, treasureDeath.Tier, out weaponType);
        }

        public static WeenieClassName RollSpearWcid(TreasureDeath treasureDeath, ref TreasureWeaponType weaponType)
        {
            var heritage = RollHeritage(treasureDeath);

            return SpearWcids.Roll(heritage, treasureDeath.Tier, out weaponType);
        }

        public static WeenieClassName RollUnarmedWcid(TreasureDeath treasureDeath)
        {
            var heritage = RollHeritage(treasureDeath);

            return UnarmedWcids.Roll(heritage, treasureDeath.Tier);
        }

        public static WeenieClassName RollStaffWcid(TreasureDeath treasureDeath)
        {
            var heritage = RollHeritage(treasureDeath);

            return StaffWcids.Roll(heritage, treasureDeath.Tier);
        }

        public static WeenieClassName RollDaggerWcid(TreasureDeath treasureDeath, ref TreasureWeaponType weaponType)
        {
            var heritage = RollHeritage(treasureDeath);

            switch (heritage)
            {
                case TreasureHeritageGroup.Aluvian:
                case TreasureHeritageGroup.Sho:
                    return DaggerWcids_Aluvian_Sho.Roll(treasureDeath.Tier, out weaponType);

                case TreasureHeritageGroup.Gharundim:
                    return DaggerWcids_Gharundim.Roll(treasureDeath.Tier, out weaponType);
            }
            return WeenieClassName.undef;
        }

        public static WeenieClassName RollBowWcid(TreasureDeath treasureDeath, ref TreasureWeaponType weaponType)
        {
            var heritage = RollHeritage(treasureDeath);

            switch (heritage)
            {
                case TreasureHeritageGroup.Aluvian:
                    return BowWcids_Aluvian.Roll(treasureDeath.Tier, out weaponType);

                case TreasureHeritageGroup.Gharundim:
                    return BowWcids_Gharundim.Roll(treasureDeath.Tier, out weaponType);

                case TreasureHeritageGroup.Sho:
                    return BowWcids_Sho.Roll(treasureDeath.Tier, out weaponType);
            }
            return WeenieClassName.undef;
        }

        public static WeenieClassName RollCrossbowWcid(TreasureDeath treasureDeath, ref TreasureWeaponType weaponType)
        {
            return CrossbowWcids.Roll(treasureDeath.Tier, out weaponType);
        }

        public static WeenieClassName RollAtlatlWcid(TreasureDeath treasureDeath, ref TreasureWeaponType weaponType)
        {
            return AtlatlWcids.Roll(treasureDeath.Tier, out weaponType);
        }

        public static WeenieClassName RollCaster(TreasureDeath treasureDeath)
        {
            return CasterWcids.Roll(treasureDeath.Tier);
        }

        public static WeenieClassName RollTwoHandedWeaponWcid(ref TreasureWeaponType weaponType)
        {
            return TwoHandedWeaponWcids.Roll(out weaponType);
        }
    }
}
