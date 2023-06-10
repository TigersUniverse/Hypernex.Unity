using System;
using Hypernex.Networking.Messages;
using Nexport;

namespace Hypernex.Game
{
    public static class MessageHandler
    {
        public static void HandleMessage(GameInstance gameInstance, MsgMeta msgMeta, MessageChannel channel)
        {
            switch (msgMeta.DataId)
            {
                case "Hypernex.Networking.Messages.PlayerUpdate":
                {
                    PlayerUpdate playerUpdate = (PlayerUpdate) Convert.ChangeType(msgMeta.Data, typeof(PlayerUpdate));
                    PlayerManagement.HandlePlayerUpdate(gameInstance, playerUpdate);
                    break;
                }
                case "Hypernex.Networking.Messages.PlayerVoice":
                {
                    PlayerVoice playerVoice = (PlayerVoice) Convert.ChangeType(msgMeta.Data, typeof(PlayerVoice));
                    PlayerManagement.HandlePlayerVoice(gameInstance, playerVoice);
                    break;
                }
                case "Hypernex.Networking.Messages.RespondAuth":
                {
                    RespondAuth respondAuth = (RespondAuth) Convert.ChangeType(msgMeta.Data, typeof(RespondAuth));
                    GameInstance instance = GameInstance.FocusedInstance;
                    if (instance == null)
                        break;
                    if (instance.gameServerId == respondAuth.GameServerId &&
                        instance.instanceId == respondAuth.InstanceId)
                        instance.authed = true;
                    break;
                }
                case "Hypernex.Networking.Messages.PlayerMessage":
                {
                    // TODO: Implement chatbox system (this will go in NetPlayer)
                    break;
                }
            }
        }
    }
}