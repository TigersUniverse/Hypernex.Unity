using System;
using System.Collections.Generic;
using System.Linq;
using Hypernex.Game;
using Hypernex.Tools;
using Hypernex.UI.Templates;
using HypernexSharp.APIObjects;
using HypernexSharp.Socketing.SocketMessages;
using HypernexSharp.Socketing.SocketResponses;
using HypernexSharp.SocketObjects;

namespace Hypernex.Player
{
    public class SocketManager
    {
        private static List<SharedAvatarToken> avatarTokens = new();
        public static List<SharedAvatarToken> SharedAvatarTokens => new(avatarTokens);
        public static Action<SharedAvatarToken> OnAvatarToken { get; set; } = token => { };

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
                    case "sharedavatartoken":
                        SharedAvatarToken sharedAvatarToken = (SharedAvatarToken) response;
                        QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                        {
                            if (SharedAvatarTokens.Count(x =>
                                    x.avatarId == sharedAvatarToken.avatarId &&
                                    x.fromUserId == sharedAvatarToken.fromUserId) > 0)
                            {
                                foreach (SharedAvatarToken avatarToken in avatarTokens)
                                    if (avatarToken.avatarId == sharedAvatarToken.avatarId &&
                                        avatarToken.fromUserId == sharedAvatarToken.fromUserId)
                                        avatarTokens.Remove(avatarToken);
                            }
                            avatarTokens.Add(sharedAvatarToken);
                            OnAvatarToken.Invoke(sharedAvatarToken);
                        }));
                        break;
                    // TODO: Implement other messages
                }
            };
            APIPlayer.UserSocket.OnOpen += () => QuickInvoke.InvokeActionOnMainThread(APIPlayer.OnSocketConnect);
            APIPlayer.UserSocket.Open();
            GameInstance.Init();
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