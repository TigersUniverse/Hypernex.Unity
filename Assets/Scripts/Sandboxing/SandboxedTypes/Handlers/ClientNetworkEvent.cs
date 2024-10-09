using System;
using System.Collections.Generic;
using System.Linq;
using Hypernex.Game;
using Hypernex.Networking.Messages;
using Hypernex.Player;
using Nexport;

namespace Hypernex.Sandboxing.SandboxedTypes.Handlers
{
    public class ClientNetworkEvent
    {
        private const string ERROR_STRING = "Cannot send to clients from client!";
        private GameInstance gameInstance;

        public ClientNetworkEvent()
        {
            throw new Exception("Cannot instantiate ClientNetworkEvent!");
        }

        internal ClientNetworkEvent(GameInstance gameInstance) => this.gameInstance = gameInstance;

        public void SendToClient(string s1, string s2, object[] o1, MessageChannel m1 = MessageChannel.Reliable) =>
            throw new Exception(ERROR_STRING);
        
        public void SendToAllClients(string s1, object[] o1, MessageChannel m1 = MessageChannel.Reliable) =>
            throw new Exception(ERROR_STRING);

        public void SendToServer(string eventName, object[] data = null, 
            MessageChannel messageChannel = MessageChannel.Reliable)
        {
            NetworkedEvent networkedEvent = new NetworkedEvent
            {
                Auth = new JoinAuth
                {
                    UserId = APIPlayer.APIUser.Id,
                    TempToken = gameInstance.userIdToken
                },
                EventName = eventName,
                Data = new List<object> {data?.ToArray() ?? Array.Empty<object>()}
            };
            gameInstance.SendMessage(typeof(NetworkedEvent).FullName, Msg.Serialize(networkedEvent), messageChannel);
        }
    }
}