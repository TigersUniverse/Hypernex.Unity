using System;
using System.Collections.Generic;
using Hypernex.Game;
using Hypernex.Tools;
using UnityEngine;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public class LocalAvatarLocalAvatar
    {
        internal static List<string> AssignedTags = new();
        internal static List<string> ExtraneousKeys = new();

        private Transform avatarRoot;
        private bool isLocalAvatar;
        private LocalPlayer localPlayer;
        private NetPlayer netPlayer;

        public LocalAvatarLocalAvatar() => throw new Exception("Cannot instantiate LocalAvatarLocalAvatar");

        internal LocalAvatarLocalAvatar(Transform avatarRoot)
        {
            this.avatarRoot = avatarRoot;
            isLocalAvatar = avatarRoot.GetComponent<LocalPlayer>() != null;
            if (isLocalAvatar)
                localPlayer = avatarRoot.GetComponent<LocalPlayer>();
            else
                netPlayer = avatarRoot.GetComponent<NetPlayer>();
        }

        private AvatarCreator GetAvatarCreator()
        {
            if (localPlayer != null)
                return localPlayer.avatar;
            return netPlayer.Avatar;
        }

        public Item GetAvatarObject(HumanBodyBones humanBodyBones)
        {
            if (avatarRoot == null)
                return null;
            Transform bone = GetAvatarCreator().GetBoneFromHumanoid(humanBodyBones);
            if (bone == null)
                return null;
            return new Item(bone);
        }

        public Item GetAvatarObjectByPath(string path)
        {
            if (avatarRoot == null)
                return null;
            Transform bone = GetAvatarCreator().Avatar.transform.Find(path);
            if (bone == null)
                return null;
            return new Item(bone);
        }

        public bool IsAvatarItem(Item item) => AnimationUtility.GetRootOfChild(item.t) == avatarRoot;
        public bool IsAvatarItem(ReadonlyItem item) => AnimationUtility.GetRootOfChild(item.item.t) == avatarRoot;

        public object GetParameter(string parameterName) =>
            avatarRoot == null ? null : GetAvatarCreator().GetParameter(parameterName);

        public void SetParameter(string name, bool value)
        {
            if (!isLocalAvatar || localPlayer == null)
                return;
            localPlayer.avatar.SetParameter(name, value);
        }
        
        public void SetParameter(string name, int value)
        {
            if (!isLocalAvatar || localPlayer == null)
                return;
            localPlayer.avatar.SetParameter(name, value);
        }
        
        public void SetParameter(string name, float value)
        {
            if (!isLocalAvatar || localPlayer == null)
                return;
            localPlayer.avatar.SetParameter(name, value);
        }
        
        public object GetExtraneousObject(string key)
        {
            if (isLocalAvatar)
            {
                if (localPlayer == null || !localPlayer.LastExtraneousObjects.ContainsKey(key))
                    return null;
                return localPlayer.LastExtraneousObjects[key];
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
                return localPlayer.LastPlayerAssignedTags.ToArray();
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
    }
}