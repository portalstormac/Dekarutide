using ACE.Common;
using ACE.Entity.Enum;
using ACE.Server.Entity;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.WorldObjects
{
    partial class Player
    {
        public bool SneakingPossible = false;
        public bool IsSneaking = false;
        private double NextSneakingCheckTime = 0;
        private static int SneakingCheckInterval = 5;
        private static uint EnterSneakingDifficulty = 50;

        public void BeginSneaking()
        {
            if (IsSneaking)
                return;

            if (NextSneakingCheckTime > Time.GetUnixTime())
                return;
            NextSneakingCheckTime = Time.GetFutureUnixTime(SneakingCheckInterval);

            var result = TestSneakingInternal(EnterSneakingDifficulty);
            if (result == SneakingTestResult.Success)
            {
                IsSneaking = true;
                Session.Network.EnqueueSend(new GameMessageSystemChat($"You start sneaking.", ChatMessageType.Broadcast));
            }
            else if(result == SneakingTestResult.Failure)
                Session.Network.EnqueueSend(new GameMessageSystemChat($"You fail on your attempt to start sneaking.", ChatMessageType.Broadcast));

            // TODO: figure out a way to set translucency or other visual cue without having to update the entire object as that sends the player into portal space.
            //Translucency = 0.5f;            
            //EnqueueBroadcast(new GameMessageUpdateObject(this));
            ////EnqueueBroadcast(new GameMessagePublicUpdatePropertyFloat(this, PropertyFloat.Translucency, (double)Translucency));
        }

        public void EndSneaking(string message = null, bool preventSneaking = false)
        {
            if (!IsSneaking)
            {
                if(preventSneaking)
                    NextSneakingCheckTime = Time.GetFutureUnixTime(SneakingCheckInterval);
                return;
            }

            IsSneaking = false;
            NextSneakingCheckTime = Time.GetFutureUnixTime(SneakingCheckInterval);

            Session.Network.EnqueueSend(new GameMessageSystemChat(message == null ? "You stop sneaking." : message, ChatMessageType.Broadcast));

            //Translucency = 0.0f;
            //EnqueueBroadcast(new GameMessageCreateObject(this));
            ////EnqueueBroadcast(new GameMessagePublicUpdatePropertyFloat(this, PropertyFloat.Translucency, (double)Translucency));
        }

        public bool TestSneaking(Creature creature, double distanceSquared, string failureMessage)
        {
            if (!IsSneaking)
                return false;

            if (distanceSquared < 2)
            {
                EndSneaking();
                return false;
            }
            else if (distanceSquared < creature.VisualAwarenessRangeSq)
            {
                var difficulty = (uint)((creature.Level ?? 1) * 3.0f);
                return TestSneaking(difficulty, failureMessage);
            }
            else
                return true;
        }

        public bool TestSneaking(Creature creature, string failureMessage)
        {
            if (!IsSneaking)
                return false;
            return TestSneaking(creature, PhysicsObj.get_distance_sq_to_object(creature.PhysicsObj, true), failureMessage);
        }

        public bool TestSneaking(uint difficulty, string failureMessage)
        {
            if(TestSneakingInternal(difficulty) != SneakingTestResult.Success)
            {
                EndSneaking(failureMessage);
                return false;
            }
            else
                return true;
        }

        private enum SneakingTestResult
        {
            Untrained,
            Failure,
            Success
        }

        private SneakingTestResult TestSneakingInternal(uint difficulty)
        {
            var sneakingSkill = GetCreatureSkill(Skill.Sneaking);
            if (sneakingSkill.AdvancementClass < SkillAdvancementClass.Trained)
                return SneakingTestResult.Untrained;

            var chance = SkillCheck.GetSkillChance(sneakingSkill.Current, difficulty);
            if (chance > ThreadSafeRandom.Next(0.0f, 1.0f))
            {
                Proficiency.OnSuccessUse(this, sneakingSkill, difficulty);
                return SneakingTestResult.Success;
            }
            return SneakingTestResult.Failure;
        }

        public void UpdateSneakingState()
        {
            if (!IsAttemptingToSneak)
                return;

            var minterp = PhysicsObj.get_minterp();
            if ((minterp.RawState.ForwardCommand == (uint)MotionCommand.Ready || minterp.RawState.ForwardCommand == (uint)MotionCommand.Invalid) && minterp.RawState.SideStepCommand == (uint)MotionCommand.Invalid)
            {
                // Special case: standing still won't turn sneaking on or off.
                SneakingPossible = true;
                return;
            }

            if (HasAnyMovement())
            {
                var isRunning = CheckIsRunning();

                if (IsJumping || isRunning)
                    SneakingPossible = false;
                else
                    SneakingPossible = true;
            }
            else
                SneakingPossible = true;

            if (!SneakingPossible && IsSneaking)
                EndSneaking();
            else if (SneakingPossible && !IsSneaking)
                BeginSneaking();
        }

        public bool IsAware(WorldObject wo)
        {
            return wo.IsAware(this);
        }

        public bool TestAwareness(WorldObject wo)
        {
            var awarenessSkill = GetCreatureSkill(Skill.Awareness);
            var chance = SkillCheck.GetSkillChance(awarenessSkill.Current, (uint)(wo.ResistAwareness ?? 0));
            if (chance > ThreadSafeRandom.Next(0.0f, 1.0f))
            {
                Proficiency.OnSuccessUse(this, awarenessSkill, (uint)(wo.ResistAwareness ?? 0));
                return true;
            }
            return false;
        }
    }
}
