#nullable enable
using System.Collections.Generic;

namespace Nexport.BuiltinMessages
{
    [Msg]
    public class ServerClientChange
    {
        [MsgKey(1)] public string MessageId = typeof(ServerClientChange).FullName;
        [MsgKey(2)] public ClientIdentifier[]? ConnectedClients;
        [MsgKey(3)] public ClientIdentifier? LocalClientIdentifier;
            
        public ServerClientChange(){}
        public ServerClientChange(List<ClientIdentifier> clients) => ConnectedClients = clients.ToArray();
    }
}