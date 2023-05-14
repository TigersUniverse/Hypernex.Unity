using System;
using System.Collections.Generic;
using Hypernex.Game;
using UnityEngine;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public class LocalNetAvatar
    {
        private GameInstance gameInstance;

        public LocalNetAvatar()
        {
            throw new Exception("Cannot instantiate LocalNetAvatar!");
        }
        internal LocalNetAvatar(GameInstance instance) => gameInstance = instance;
        
        private NetPlayer GetNetPlayer(string userid)
        {
            if (gameInstance == null)
                return null;
            return PlayerManagement.GetNetPlayer(gameInstance, userid);
        }

        public ReadonlyItem GetAvatarObject(string userid, HumanBodyBones humanBodyBones)
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

        public ReadonlyItem GetAvatarObjectByPath(string userid, string path)
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
        
        public ReadonlyItem[] GetAllChildrenInAvatar(string userid, string path)
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