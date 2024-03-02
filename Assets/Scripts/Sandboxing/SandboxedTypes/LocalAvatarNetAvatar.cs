using System;
using System.Collections.Generic;
using Hypernex.Game;
using Hypernex.Player;
using Hypernex.Tools;
using HypernexSharp.APIObjects;
using UnityEngine;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public static class LocalAvatarNetAvatar
    {
        public static string[] ActiveUserIds
        {
            get
            {
                if (GameInstance.FocusedInstance == null)
                    return Array.Empty<string>();
                List<User> l = GameInstance.FocusedInstance.ConnectedUsers;
                List<string> s = new List<string>();
                foreach (User user in l)
                    s.Add(user.Id);
                return s.ToArray();
            }
        }

        private static NetPlayer GetNetPlayer(string userid)
        {
            if (GameInstance.FocusedInstance == null)
                return null;
            return PlayerManagement.GetNetPlayer(GameInstance.FocusedInstance, userid);
        }

        public static ReadonlyItem GetAvatarObject(string userid, HumanBodyBones humanBodyBones)
        {
            if (userid == APIPlayer.APIUser.Id)
            {
                if (LocalPlayer.Instance == null || LocalPlayer.Instance.avatar == null)
                    return null;
                Transform b = LocalPlayer.Instance.avatar.GetBoneFromHumanoid(humanBodyBones);
                if (b == null)
                    return null;
                return new ReadonlyItem(b);
            }
            NetPlayer netPlayer = GetNetPlayer(userid);
            if (netPlayer == null)
                return null;
            if (netPlayer.Avatar == null || netPlayer.Avatar == null)
                return null;
            Transform bone = netPlayer.Avatar.GetBoneFromHumanoid(humanBodyBones);
            if (bone == null)
                return null;
            return new ReadonlyItem(bone);
        }

        public static ReadonlyItem GetAvatarObjectByPath(string userid, string path)
        {
            if (userid == APIPlayer.APIUser.Id)
            {
                if (LocalPlayer.Instance == null || LocalPlayer.Instance.avatar == null)
                    return null;
                Transform b = LocalPlayer.Instance.avatar.Avatar.transform.Find(path);
                if (b == null)
                    return null;
                return new ReadonlyItem(b);
            }
            NetPlayer netPlayer = GetNetPlayer(userid);
            if (netPlayer == null)
                return null;
            if (netPlayer.Avatar == null)
                return null;
            Transform bone = netPlayer.Avatar.Avatar.transform.Find(path);
            if (bone == null)
                return null;
            return new ReadonlyItem(bone);
        }

        public static bool IsAvatarItem(Item item, string userid = "")
        {
            {
                LocalPlayer localPlayer = AnimationUtility.GetRootOfChild(item.t).gameObject.GetComponent<LocalPlayer>();
                NetPlayer netPlayer = AnimationUtility.GetRootOfChild(item.t).gameObject.GetComponent<NetPlayer>();
                if (!string.IsNullOrEmpty(userid))
                {
                    if (netPlayer != null && netPlayer.User.Id == userid)
                        return true;
                    if (localPlayer != null && APIPlayer.APIUser.Id == userid)
                        return true;
                }
                return netPlayer != null;
            }
        }

        public static bool IsAvatarItem(ReadonlyItem item, string userid = "")
        {
            LocalPlayer localPlayer = AnimationUtility.GetRootOfChild(item.item.t).gameObject.GetComponent<LocalPlayer>();
            NetPlayer netPlayer = AnimationUtility.GetRootOfChild(item.item.t).gameObject.GetComponent<NetPlayer>();
            if (!string.IsNullOrEmpty(userid))
            {
                if (netPlayer != null && netPlayer.User.Id == userid)
                    return true;
                if (localPlayer != null && APIPlayer.APIUser.Id == userid)
                    return true;
            }
            return netPlayer != null;
        }

        public static ReadonlyItem[] GetAllChildrenInAvatar(string userid)
        {
            if (userid == APIPlayer.APIUser.Id)
            {
                if (LocalPlayer.Instance == null || LocalPlayer.Instance.avatar == null)
                    return null;
                List<ReadonlyItem> i = new List<ReadonlyItem>();
                foreach (Transform transform in LocalPlayer.Instance.avatar.Avatar.GetComponentsInChildren<Transform>())
                    i.Add(new ReadonlyItem(transform));
                return i.ToArray();
            }
            NetPlayer netPlayer = GetNetPlayer(userid);
            if (netPlayer == null)
                return null;
            if (netPlayer.Avatar == null)
                return null;
            List<ReadonlyItem> items = new List<ReadonlyItem>();
            foreach (Transform transform in netPlayer.Avatar.Avatar.GetComponentsInChildren<Transform>())
                items.Add(new ReadonlyItem(transform));
            return items.ToArray();
        }

        public static string[] GetSelfAssignedTags(string userid)
        {
            if (userid == APIPlayer.APIUser.Id)
                return LocalPlayer.Instance == null ? null : LocalPlayer.Instance.LocalPlayerSyncController.LastPlayerAssignedTags.ToArray();
            NetPlayer netPlayer = GetNetPlayer(userid);
            if (netPlayer == null)
                return null;
            return netPlayer.LastPlayerTags.ToArray();
        }

        public static object GetExtraneousObject(string userid, string key)
        {
            if (userid == APIPlayer.APIUser.Id)
            {
                if (LocalPlayer.Instance == null || !LocalPlayer.Instance.LocalPlayerSyncController.LastExtraneousObjects.ContainsKey(key))
                    return null;
                return LocalPlayer.Instance.LocalPlayerSyncController.LastExtraneousObjects[key];
            }
            NetPlayer netPlayer = GetNetPlayer(userid);
            if (netPlayer == null)
                return null;
            if (netPlayer.LastExtraneousObjects.ContainsKey(key))
                return netPlayer.LastExtraneousObjects[key];
            return null;
        }

        public static object GetParameterValue(string userid, string parameterName)
        {
            if (userid == APIPlayer.APIUser.Id)
            {
                if (LocalPlayer.Instance == null || LocalPlayer.Instance.avatar == null)
                    return null;
                return LocalPlayer.Instance.avatar.GetParameter(parameterName);
            }
            NetPlayer netPlayer = GetNetPlayer(userid);
            if (netPlayer == null)
                return null;
            if (netPlayer.Avatar == null)
                return null;
            return netPlayer.Avatar.GetParameter(parameterName);
        }
        
        public static string GetUsername(string userid)
        {
            if (userid == APIPlayer.APIUser.Id)
                return APIPlayer.APIUser.Username;
            NetPlayer netPlayer = GetNetPlayer(userid);
            if (netPlayer == null)
                return String.Empty;
            return netPlayer.User?.Username ?? String.Empty;
        }
        
        public static string GetDisplayName(string userid)
        {
            if (userid == APIPlayer.APIUser.Id)
            {
                if(APIPlayer.APIUser.Bio == null)
                    return String.Empty;
                return APIPlayer.APIUser.Bio.DisplayName ?? String.Empty;
            }
            NetPlayer netPlayer = GetNetPlayer(userid);
            if (netPlayer == null || netPlayer.User == null || netPlayer.User.Bio == null)
                return String.Empty;
            return netPlayer.User.Bio.DisplayName ?? String.Empty;
        }

        public static Pronouns GetPronouns(string userid)
        {
            if (userid == APIPlayer.APIUser.Id)
                return APIPlayer.APIUser.Bio.Pronouns;
            NetPlayer netPlayer = GetNetPlayer(userid);
            if (netPlayer == null)
                return null;
            return netPlayer.User.Bio.Pronouns;
        }
    }
}