using ACE.Common;
using ACE.Database;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Entity;
using ACE.Server.Entity.Actions;
using ACE.Server.Factories;
using ACE.Server.Network.GameEvent.Events;
using ACE.Server.Network.GameMessages.Messages;
using System;
using Time = ACE.Common.Time;

namespace ACE.Server.WorldObjects
{
    /// <summary>
    /// Activates an object based on collision
    /// </summary>
    public class PressurePlate : WorldObject
    {
        /// <summary>
        /// The last time this pressure plate was activated
        /// </summary>
        public DateTime LastUseTime;

        /// <summary>
        /// A new biota be created taking all of its values from weenie.
        /// </summary>
        public PressurePlate(Weenie weenie, ObjectGuid guid) : base(weenie, guid)
        {
            SetEphemeralValues();
        }

        /// <summary>
        /// Restore a WorldObject from the database.
        /// </summary>
        public PressurePlate(Biota biota) : base(biota)
        {
            SetEphemeralValues();
        }

        private void SetEphemeralValues()
        {
            if (UseSound == 0)
                UseSound = Sound.TriggerActivated;

            DefaultActive = Active;
            NextRearm = 0;
        }

        public override void SetLinkProperties(WorldObject wo)
        {
            wo.ActivationTarget = Guid.Full;
        }

        /// <summary>
        /// Called when a player runs over the pressure plate
        /// </summary>
        public override void OnCollideObject(WorldObject wo)
        {
            OnActivate(wo);
        }

        public bool NextActivationIsFromUse = false;

        /// <summary>
        /// Activates the object linked to a pressure plate
        /// </summary>
        public override void OnActivate(WorldObject activator)
        {
            if (!Active)
                return;

            // handle monsters walking on pressure plates
            if (!(activator is Player player))
                return;

            if (!NextActivationIsFromUse && ResistAwareness.HasValue && player.TestSneaking((uint)ResistAwareness, "You fail to avoid the trigger, you are no longer sneaking!"))
                return;
            NextActivationIsFromUse = false;

            // prevent continuous event stream
            // TODO: should this go in base.OnActivate()?

            var currentTime = DateTime.UtcNow;
            if (currentTime < LastUseTime + TimeSpan.FromSeconds(2))
                return;

            LastUseTime = currentTime;

            player.EnqueueBroadcast(new GameMessageSound(player.Guid, UseSound));

            base.OnActivate(activator);
        }

        public override void ActOnUse(WorldObject wo)
        {
        }

        public override bool EnterWorld()
        {
            DetermineTier();

            return base.EnterWorld();
        }

        public override void Heartbeat(double currentUnixTime)
        {
            base.Heartbeat(currentUnixTime);

            if (NextRearm != 0 && NextRearm <= currentUnixTime)
                Active = true;
        }

        public void DetermineTier()
        {
            var instances = DatabaseManager.World.GetCachedInstancesByLandblock(CurrentLandblock.Id.Landblock);

            foreach (var instance in instances)
            {
                Position instancePos = new Position(instance.ObjCellId, instance.OriginX, instance.OriginY, instance.OriginZ, instance.AnglesX, instance.AnglesY, instance.AnglesZ, instance.AnglesW);
                if (Location.DistanceTo(instancePos) < 50)
                {
                    var weenie = DatabaseManager.World.GetCachedWeenie(instance.WeenieClassId);

                    var deathTreasureId = weenie.GetProperty(PropertyDataId.DeathTreasureType) ?? 0;
                    if (deathTreasureId != 0)
                    {
                        var deathTreasure = LootGenerationFactory.GetTweakedDeathTreasureProfile(deathTreasureId, this);
                        if (deathTreasure.Tier > (Tier ?? 0))
                        {
                            Tier = deathTreasure.Tier;
                        }
                    }
                }
            }

            if (Tier.HasValue)
            {
                ResistLockpick = Tier * 75;
                ResistAwareness = Tier * 50;
            }
        }

        private bool DefaultActive;
        private double NextRearm;
        private static int DisarmLength = 300;

        public void Disarm()
        {
            if (!DefaultActive)
                return;

            if (Active)
            {
                Active = false;
                NextRearm = Time.GetFutureUnixTime(DisarmLength);

                EnqueueBroadcast(new GameMessagePublicUpdatePropertyInt(this, PropertyInt.Active, Active ? 1 : 0));
            }
            else
                NextRearm = Time.GetFutureUnixTime(DisarmLength);
        }

        public void Rearm()
        {
            if (!DefaultActive || Active)
                return;

            Active = true;
            NextRearm = 0;

            EnqueueBroadcast(new GameMessagePublicUpdatePropertyInt(this, PropertyInt.Active, Active ? 1 : 0));
        }

        public void AttemptDisarm(Player player, WorldObject unlocker)
        {
            if (!DefaultActive)
            {
                return;
            }

            ActionChain chain = new ActionChain();

            chain.AddAction(player, () =>
            {
                if (player.Skills[Skill.Lockpick].AdvancementClass < SkillAdvancementClass.Trained)
                {
                    player.Session.Network.EnqueueSend(new GameEventUseDone(player.Session, WeenieError.YouArentTrainedInLockpicking));
                    return;
                }

                uint difficulty = (uint)(ResistLockpick ?? 0);
                if (unlocker.WeenieType == WeenieType.Lockpick)
                {
                    var lockpickSkill = player.GetCreatureSkill(Skill.Lockpick);
                    var effectiveLockpickSkill = UnlockerHelper.GetEffectiveLockpickSkill(player, unlocker);

                    var pickChance = SkillCheck.GetSkillChance(effectiveLockpickSkill, difficulty);

                    bool success = false;
                    var chance = ThreadSafeRandom.Next(0.0f, 1.0f);
                    if (chance < pickChance)
                    {
                        success = true;
                        Proficiency.OnSuccessUse(player, lockpickSkill, difficulty);
                        Disarm();
                    }

                    UnlockerHelper.SendDisarmResultMessage(player, UnlockerHelper.ConsumeUnlocker(player, unlocker, this), this, success);
                }
                else
                {
                    player.Session.Network.EnqueueSend(new GameEventUseDone(player.Session, WeenieError.YouCannotLockOrUnlockThat));
                }
            });

            chain.EnqueueChain();
        }
    }
}
