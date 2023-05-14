using System;
using System.Collections.Generic;
using Hypernex.Game;
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
            NetPlayer netPlayer = GetNetPlayer(userid);
            if (netPlayer == null)
                return null;
            if (netPlayer.Avatar == null || netPlayer.mainAnimator == null)
                return null;
            Transform bone = netPlayer.mainAnimator.GetBoneTransform(humanBodyBones);
            if (bone == null)
                return null;
            return new ReadonlyItem(bone);
        }

        public static ReadonlyItem GetAvatarObjectByPath(string userid, string path)
        {
            NetPlayer netPlayer = GetNetPlayer(userid);
            if (netPlayer == null)
                return null;
            if (netPlayer.Avatar == null)
                return null;
            Transform bone = netPlayer.Avatar.transform.Find(path);
            if (bone == null)
                return null;
            return new ReadonlyItem(bone);
        }

        public static ReadonlyItem[] GetAllChildrenInAvatar(string userid, string path)
        {
            NetPlayer netPlayer = GetNetPlayer(userid);
            if (netPlayer == null)
                return null;
            if (netPlayer.Avatar == null)
                return null;
            List<ReadonlyItem> items = new List<ReadonlyItem>();
            foreach (Transform transform in netPlayer.Avatar.GetComponentsInChildren<Transform>())
                items.Add(new ReadonlyItem(transform));
            return items.ToArray();
        }
    }
}