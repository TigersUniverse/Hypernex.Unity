using System;
using System.Collections.Generic;
using System.Linq;
using Hypernex.CCK.Unity;
using Hypernex.Configuration;
using Hypernex.Game;
using Hypernex.Game.Avatar;
using Hypernex.Player;
using Hypernex.Tools;
using Hypernex.UI.Templates;
using HypernexSharp.APIObjects;
using UnityEngine;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public class LocalAvatarLocalAvatar
    {
        internal static List<string> AssignedTags = new();
        internal static List<string> ExtraneousKeys = new();

        private Transform playerRoot;
        private bool isLocalAvatar;
        private LocalPlayer localPlayer;
        private NetPlayer netPlayer;

        public LocalAvatarLocalAvatar() => throw new Exception("Cannot instantiate LocalAvatarLocalAvatar");

        internal LocalAvatarLocalAvatar(Transform playerRoot)
        {
            this.playerRoot = playerRoot;
            isLocalAvatar = playerRoot.GetComponent<LocalPlayer>() != null;
            if (isLocalAvatar)
                localPlayer = playerRoot.GetComponent<LocalPlayer>();
            else
                netPlayer = playerRoot.GetComponent<NetPlayer>();
        }

        private AvatarCreator GetAvatarCreator()
        {
            if (localPlayer != null)
                return localPlayer.avatar;
            return netPlayer.Avatar;
        }

        public bool IsLocalClient() => isLocalAvatar;
        public bool IsLocalPlayerId(string userid) => APIPlayer.APIUser.Id == userid;

        public bool IsHost()
        {
            if(isLocalAvatar)
                return GameInstance.FocusedInstance?.isHost ?? false;
            return GameInstance.FocusedInstance?.host.Id == netPlayer.UserId;
        }

        public Item GetAvatarObject(HumanBodyBones humanBodyBones)
        {
            if (playerRoot == null)
                return null;
            Transform bone = GetAvatarCreator().GetBoneFromHumanoid(humanBodyBones);
            if (bone == null)
                return null;
            return new Item(bone);
        }

        public Item GetAvatarObjectByPath(string path)
        {
            if (playerRoot == null)
                return null;
            Transform bone = GetAvatarCreator().Avatar.transform.Find(path);
            if (bone == null)
                return null;
            return new Item(bone);
        }

        public ReadonlyItem GetPlayerRoot()
        {
            if (playerRoot == null)
                return null;
            return new ReadonlyItem(playerRoot);
        }

        public bool IsAvatarItem(Item item) => AnimationUtility.GetRootOfChild(item.t) == playerRoot;
        public bool IsAvatarItem(ReadonlyItem item) => AnimationUtility.GetRootOfChild(item.item.t) == playerRoot;
        
        public AvatarParameter[] GetAvatarParameters()
        {
            AvatarCreator ac = GetAvatarCreator();
            if (ac == null)
                return Array.Empty<AvatarParameter>();
            List<AvatarParameter> parameterNames = new();
            foreach (AnimatorPlayable avatarAnimatorPlayable in ac.AnimatorPlayables)
            {
                foreach (AnimatorControllerParameter parameter in avatarAnimatorPlayable.AnimatorControllerParameters)
                {
                    if (parameterNames.Count(x => x.Name == parameter.name) <= 0)
                        parameterNames.Add(new AvatarParameter(ac, avatarAnimatorPlayable,
                            parameter, true));
                }
            }
            return parameterNames.ToArray();
        }

        public AvatarParameter GetAvatarParameter(string parameterName)
        {
            AvatarCreator ac = GetAvatarCreator();
            if (ac == null)
                return null;
            foreach (AnimatorPlayable animatorPlayable in ac.AnimatorPlayables)
            {
                foreach (AnimatorControllerParameter parameter in animatorPlayable.AnimatorControllerParameters)
                {
                    if (parameter.name == parameterName)
                        return new AvatarParameter(ac, animatorPlayable, parameter, true);
                }
            }
            return null;
        }

        public bool IsExtraneousObjectPresent(string key)
        {
            if (isLocalAvatar)
            {
                if (localPlayer == null)
                    return false;
                return localPlayer.LocalPlayerSyncController.LastExtraneousObjects.ContainsKey(key);
            }
            if (netPlayer == null)
                return false;
            return netPlayer.LastExtraneousObjects.ContainsKey(key);
        }

        public string[] GetExtraneousObjectKeys()
        {
            List<string> keys = new();
            if (isLocalAvatar)
            {
                if (localPlayer == null)
                    return Array.Empty<string>();
                foreach (string key in localPlayer.LocalPlayerSyncController.LastExtraneousObjects.Keys)
                    keys.Add(key);
                return keys.ToArray();
            }
            if (netPlayer == null)
                return Array.Empty<string>();
            foreach (string key in netPlayer.LastExtraneousObjects.Keys)
                keys.Add(key);
            return keys.ToArray();
        }
        
        public object GetExtraneousObject(string key)
        {
            if (isLocalAvatar)
            {
                if (GameInstance.FocusedInstance == null)
                {
                    if (LocalPlayer.MoreExtraneousObjects.ContainsKey(key))
                        return LocalPlayer.MoreExtraneousObjects[key];
                }
                if (localPlayer == null || !localPlayer.LocalPlayerSyncController.LastExtraneousObjects.ContainsKey(key))
                    return null;
                return localPlayer.LocalPlayerSyncController.LastExtraneousObjects[key];
            }
            if (netPlayer == null || !netPlayer.LastExtraneousObjects.ContainsKey(key))
                return null;
            return netPlayer.LastExtraneousObjects[key];
        }

        public void AddExtraneousObject(string key, object value)
        {
            if (!isLocalAvatar)
                return;
            if (localPlayer == null)
                return;
            if(!ExtraneousKeys.Contains(key))
                ExtraneousKeys.Add(key);
            if (LocalPlayer.MoreExtraneousObjects.ContainsKey(key))
            {
                LocalPlayer.MoreExtraneousObjects[key] = value;
                return;
            }
            LocalPlayer.MoreExtraneousObjects.Add(key, value);
        }

        public void RemoveExtraneousObject(string key)
        {
            if (!isLocalAvatar)
                return;
            if (localPlayer == null)
                return;
            if (ExtraneousKeys.Contains(key))
                ExtraneousKeys.Remove(key);
            if (LocalPlayer.MoreExtraneousObjects.ContainsKey(key))
                LocalPlayer.MorePlayerAssignedTags.Remove(key);
        }
        
        public string[] GetPlayerAssignedTags()
        {
            if (isLocalAvatar)
            {
                if (localPlayer == null)
                    return null;
                return localPlayer.LocalPlayerSyncController.LastPlayerAssignedTags.ToArray();
            }
            if (netPlayer == null)
                return null;
            return netPlayer.LastPlayerTags.ToArray();
        }
        
        public void AddPlayerAssignedTag(string tag)
        {
            if (!isLocalAvatar)
                return;
            if (localPlayer == null)
                return;
            if(!AssignedTags.Contains(tag))
                AssignedTags.Add(tag);
            if (LocalPlayer.MorePlayerAssignedTags.Contains(tag))
                return;
            LocalPlayer.MorePlayerAssignedTags.Add(tag);
        }

        public void RemovePlayerAssignedTag(string tag)
        {
            if (!isLocalAvatar)
                return;
            if (localPlayer == null)
                return;
            if (AssignedTags.Contains(tag))
                AssignedTags.Remove(tag);
            if (LocalPlayer.MorePlayerAssignedTags.Contains(tag))
                LocalPlayer.MorePlayerAssignedTags.Remove(tag);
        }
        
        public void Scale(float scale)
        {
            if (!isLocalAvatar || (GameInstance.FocusedInstance != null && GameInstance.FocusedInstance.World != null &&
                                   !GameInstance.FocusedInstance.World.AllowScaling))
                return;
            if (LocalPlayer.Instance == null || CurrentAvatar.Instance == null)
                return;
            if(LocalPlayer.Instance.Dashboard.IsVisible)
                LocalPlayer.Instance.Dashboard.ToggleDashboard(LocalPlayer.Instance);
            CurrentAvatar.Instance.SizeAvatar(scale);
            if(!LocalPlayer.Instance.Dashboard.IsVisible)
                LocalPlayer.Instance.Dashboard.ToggleDashboard(LocalPlayer.Instance);
        }

        public void Respawn()
        {
            if (!isLocalAvatar)
                return;
            if (LocalPlayer.Instance == null || GameInstance.FocusedInstance == null)
                return;
            if (!GameInstance.FocusedInstance.World.AllowRespawn)
                return;
            LocalPlayer.Instance.Respawn();
        }

        public Pronouns GetPronouns()
        {
            if (netPlayer != null)
                return netPlayer.User.Bio.Pronouns;
            return APIPlayer.APIUser.Bio.Pronouns;
        }

        public void SetAvatar(string avatarId)
        {
            if (!isLocalAvatar)
                return;
            ConfigManager.SelectedConfigUser.CurrentAvatar = avatarId;
            if(LocalPlayer.Instance != null)
                LocalPlayer.Instance.LoadAvatar();
            ConfigManager.SaveConfigToFile();
        }
    }
}