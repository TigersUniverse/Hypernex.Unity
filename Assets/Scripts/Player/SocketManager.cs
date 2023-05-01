using System;
using Hypernex.Tools;
using Hypernex.UI.Templates;
using HypernexSharp.APIObjects;
using HypernexSharp.Socketing.SocketResponses;
using HypernexSharp.SocketObjects;

namespace Hypernex.Player
{
    public class SocketManager
    {
        public static Action<InstanceOpened, WorldMeta> OnInstanceOpened { get; set; } =
            (openedInstance, worldMeta) => { };
        public static Action<JoinedInstance, WorldMeta> OnInstanceJoined { get; set; } =
            (joinedInstance, worldMeta) => { };
        public static Action<LeftInstance> OnInstanceLeft { get; set; } = instanceLeft => { };
        public static Action<GotInvite> OnInvite { get; set; } = invite => { };

        public static void InitSocket()
        {
            // Assuming APIPlayer IsFullReady
            APIPlayer.UserSocket.OnSocketEvent += response =>
            {
                switch (response.message.ToLower())
                {
                    case "joinedinstance":
                        JoinedInstance joinedInstance = (JoinedInstance) response;
                        WorldTemplate.GetWorldMeta(joinedInstance.worldId, meta =>
                        {
                            if(meta != null)
                                QuickInvoke.InvokeActionOnMainThread(OnInstanceJoined, joinedInstance, meta);
                            else
                                LeaveInstance(joinedInstance.gameServerId, joinedInstance.instanceId);
                        });
                        break;
                    case "instanceopened":
                        InstanceOpened instanceOpened = (InstanceOpened) response;
                        WorldTemplate.GetWorldMeta(instanceOpened.worldId, meta =>
                        {
                            if(meta != null)
                                QuickInvoke.InvokeActionOnMainThread(OnInstanceOpened, instanceOpened, meta);
                        });
                        break;
                    case "leftinstance":
                        LeftInstance leftInstance = (LeftInstance) response;
                        QuickInvoke.InvokeActionOnMainThread(OnInstanceLeft, leftInstance);
                        break;
                    case "gotinvite":
                        GotInvite gotInvite = (GotInvite) response;
                        QuickInvoke.InvokeActionOnMainThread(OnInvite, gotInvite);
                        break;
                    // TODO: Implement other messages
                }
            };
        }

        public static void CreateInstance(WorldMeta worldMeta, InstancePublicity instancePublicity = InstancePublicity.Anyone,
            InstanceProtocol instanceProtocol = InstanceProtocol.KCP)
        {
            if (APIPlayer.IsFullReady)
            {
                APIPlayer.UserSocket.RequestNewInstance(worldMeta, instancePublicity, instanceProtocol);
            }
        }
        
        public static void JoinInstance(SafeInstance instance)
        {
            if (APIPlayer.IsFullReady)
            {
                APIPlayer.UserSocket.JoinInstance(instance.GameServerId, instance.InstanceId);
            }
        }

        public static void LeaveInstance(string gameServerId, string instanceId)
        {
            if(APIPlayer.IsFullReady)
                APIPlayer.UserSocket.LeaveInstance(gameServerId, instanceId);
        }
    }
}