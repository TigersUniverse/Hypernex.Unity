using System;
using System.Collections.Generic;
using System.Linq;
using Hypernex.CCK.Unity.Internals;
using Hypernex.Game;
using Hypernex.Game.Avatar;
using Hypernex.Player;
using Hypernex.Tools;
using Hypernex.UI.Templates;
using UnityEngine;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public class Avatar
    {
        private GameInstance gameInstance;
        private Transform avatarRoot;
        private SandboxRestriction sandboxRestriction;
        private bool isLocalAvatar;
        private LocalPlayer localPlayer;
        private NetPlayer netPlayer;
        private AvatarCreator avatarCreator;

        public Avatar() => throw new Exception("Cannot instantiate LocalAvatar");

        internal Avatar(GameInstance gameInstance, AvatarCreator avatarCreator, SandboxRestriction sandboxRestriction)
        {
            this.gameInstance = gameInstance;
            this.sandboxRestriction = sandboxRestriction;
            this.avatarCreator = avatarCreator;
            avatarRoot = this.avatarCreator.Avatar.transform;
            Transform parent = avatarRoot.parent;
            LocalPlayer lp = parent.GetComponent<LocalPlayer>();
            isLocalAvatar = lp != null;
            if (isLocalAvatar)
                localPlayer = lp;
            else
                netPlayer = parent.GetComponent<NetPlayer>();
        }

        internal static IPlayer GetPlayerRootFromChild(Transform t)
        {
            if (t == null) return null;
            LocalPlayer localPlayer = LocalPlayer.Instance;
            if (AnimationUtility.IsChildOfTransform(t, localPlayer.transform) || t == localPlayer.transform)
                return localPlayer;
            foreach (KeyValuePair<GameInstance,List<NetPlayer>> keyValuePair in PlayerManagement.Players)
            {
                foreach (var player in keyValuePair.Value.Where(player => AnimationUtility.IsChildOfTransform(t, player.transform) || t == player.transform))
                {
                    return player;
                }
            }
            return null;
        }

        private bool IsRootTransform(Transform t) => t == avatarRoot.parent;

        private bool ShouldBeReadOnly(Transform t)
        {
            // If the bone is the root, it's writeable if we are Local
            if (IsRootTransform(t) && sandboxRestriction == SandboxRestriction.Local && isLocalAvatar) return false;
            // If the bone is the avatar root or a child of it, and we are local avatar, it is writeable
            bool isAnAvatarBone = t == avatarRoot || AnimationUtility.IsChildOfTransform(avatarRoot, t);
            if (isAnAvatarBone && sandboxRestriction == SandboxRestriction.LocalAvatar) return false;
            // Otherwise, it is read-only
            return true;
        }

        public bool IsLocalAvatar => isLocalAvatar;
        public string OwnerId => isLocalAvatar ? netPlayer.UserId : APIPlayer.APIUser.Id;

        public Item GetAvatarObject(HumanBodyBones humanBodyBones)
        {
            Transform bone = avatarCreator.GetBoneFromHumanoid(humanBodyBones);
            if (bone == null)
                return null;
            return new Item(bone, ShouldBeReadOnly(bone));
        }
        
        public Item GetAvatarObjectByPath(string path)
        {
            Transform bone = avatarRoot.Find(path);
            if (bone == null)
                return null;
            return new Item(bone, ShouldBeReadOnly(bone));
        }

        public Item GetPlayerRoot() => isLocalAvatar
            ? new Item(localPlayer.transform, sandboxRestriction != SandboxRestriction.Local)
            : new Item(netPlayer.transform, true);
        
        public bool IsAvatarItem(Item item) => AnimationUtility.GetRootOfChild(item.t) == avatarRoot.parent;
        
        public AvatarParameter[] GetAvatarParameters()
        {
            if (avatarCreator == null)
                return Array.Empty<AvatarParameter>();
            List<AvatarParameter> parameterNames = new();
            foreach (AnimatorPlayable avatarAnimatorPlayable in avatarCreator.AnimatorPlayables)
            {
                foreach (AnimatorControllerParameter parameter in avatarAnimatorPlayable.AnimatorControllerParameters)
                {
                    if (parameterNames.Count(x => x.Name == parameter.name) <= 0)
                        parameterNames.Add(new AvatarParameter(avatarCreator, avatarAnimatorPlayable, parameter,
                            sandboxRestriction == SandboxRestriction.LocalAvatar && isLocalAvatar));
                }
            }
            return parameterNames.ToArray();
        }
        
        public AvatarParameter GetAvatarParameter(string name)
        {
            if (avatarCreator == null)
                return null;
            foreach (AnimatorPlayable avatarAnimatorPlayable in avatarCreator.AnimatorPlayables)
            {
                foreach (AnimatorControllerParameter parameter in avatarAnimatorPlayable.AnimatorControllerParameters)
                {
                    if (parameter.name == name)
                        return new AvatarParameter(avatarCreator, avatarAnimatorPlayable, parameter,
                            sandboxRestriction == SandboxRestriction.LocalAvatar && isLocalAvatar);
                }
            }
            return null;
        }
        
        public void Scale(float scale)
        {
            if (!isLocalAvatar || gameInstance == null || (gameInstance.World != null && !gameInstance.World.AllowScaling &&
                                   sandboxRestriction != SandboxRestriction.Local))
                return;
            if (LocalPlayer.Instance == null || CurrentAvatar.Instance == null)
                return;
            if(LocalPlayer.Instance.Dashboard.IsVisible)
                LocalPlayer.Instance.Dashboard.ToggleDashboard(LocalPlayer.Instance);
            CurrentAvatar.Instance.SizeAvatar(scale);
            if(!LocalPlayer.Instance.Dashboard.IsVisible)
                LocalPlayer.Instance.Dashboard.ToggleDashboard(LocalPlayer.Instance);
        }
    }
}