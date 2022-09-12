
using ACE.Server.Command.Handlers;
using ACE.Server.Entity.Actions;

namespace ACE.Server.Network.GameAction.Actions
{
    public static class GameActionLoginComplete
    {
        /// <summary>
        /// This is called when the client player exits portal space
        /// It includes initial login, as well as portaling / teleporting
        /// </summary>
        [GameAction(GameActionType.LoginComplete)]
        public static void Handle(ClientMessage message, Session session)
        {
            session.Player.OnTeleportComplete();

            if (!session.Player.FirstEnterWorldDone)
            {
                session.Player.FirstEnterWorldDone = true;
                session.Player.SendPropertyUpdatesAndOverrides();

                // Let's take the opportinity to send an activity recommendation to the player.
                var recommendationChain = new ActionChain();
                recommendationChain.AddDelaySeconds(10);
                recommendationChain.AddAction(session.Player, () =>
                {
                    PlayerCommands.HandleSingleRecommendation(session);
                });
                recommendationChain.EnqueueChain();
            }
        }
    }
}
