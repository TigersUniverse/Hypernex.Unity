using System;
using Hypernex.Tools;
using HypernexSharp.APIObjects;
using HypernexSharp.Socketing.SocketResponses;

namespace Hypernex.Player
{
    public class InstanceManager
    {
        public static Action<JoinedInstance> OnInstanceJoined { get; set; } = joinedInstance => { };

        public static void InitSocket()
        {
            // Assuming APIPlayer IsFullReady
            APIPlayer.UserSocket.OnSocketEvent += response =>
            {
                switch (response.message.ToLower())
                {
                    case "joinedinstance":
                        JoinedInstance joinedInstance = (JoinedInstance) response;
                        QuickInvoke.InvokeActionOnMainThread(OnInstanceJoined, joinedInstance);
                        break;
                    // TODO: Implement other messages
                }
            };
        }
        
        public static void JoinInstance(SafeInstance instance, WorldMeta worldMeta)
        {
            if (APIPlayer.IsFullReady)
            {
                APIPlayer.UserSocket.JoinInstance(instance.GameServerId, instance.InstanceId);
            }
        }
    }
}