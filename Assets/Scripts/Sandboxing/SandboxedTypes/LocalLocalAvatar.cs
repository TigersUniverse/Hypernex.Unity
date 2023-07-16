using System;
using System.Collections.Generic;
using System.Linq;
using Hypernex.CCK.Unity;
using Hypernex.Game;
using Hypernex.Networking.Messages.Data;
using Hypernex.Player;
using Hypernex.Tools;
using UnityEngine;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    // I know this class name sounds stupid, but it is the LocalAvatar for Local scripts
    public static class LocalLocalAvatar
    {
        public static bool IsLocalPLayerId(string userid) => APIPlayer.APIUser.Id == userid;
        
        public static ReadonlyItem GetAvatarObject(HumanBodyBones humanBodyBones)
        {
            if (LocalPlayer.Instance == null || LocalPlayer.Instance.avatar == null)
                return null;
            Transform bone = LocalPlayer.Instance.avatar.GetBoneFromHumanoid(humanBodyBones);
            if (bone == null)
                return null;
            return new ReadonlyItem(bone);
        }

        public static ReadonlyItem GetAvatarObjectByPath(string path)
        {
            if (LocalPlayer.Instance == null || LocalPlayer.Instance.avatar == null)
                return null;
            Transform bone = LocalPlayer.Instance.avatar.Avatar.transform.Find(path);
            if (bone == null)
                return null;
            return new ReadonlyItem(bone);
        }

        public static Item GetPlayerRoot()
        {
            if (LocalPlayer.Instance == null)
                return null;
            return new Item(LocalPlayer.Instance.transform);
        }

        public static bool IsAvatarItem(Item item) =>
            AnimationUtility.GetRootOfChild(item.t).gameObject.GetComponent<LocalPlayer>() != null;
        
        public static bool IsAvatarItem(ReadonlyItem item) =>
            AnimationUtility.GetRootOfChild(item.item.t).gameObject.GetComponent<LocalPlayer>() != null;

        public static void TeleportTo(float3 position)
        {
            if (LocalPlayer.Instance == null)
                return;
            LocalPlayer.Instance.transform.position = NetworkConversionTools.float3ToVector3(position);
        }

        public static void Rotate(float4 rotation)
        {
            if (LocalPlayer.Instance == null)
                return;
            LocalPlayer.Instance.transform.rotation = NetworkConversionTools.float4ToQuaternion(rotation);
        }

        public static AvatarParameter[] GetAvatarParameters()
        {
            if (LocalPlayer.Instance == null || LocalPlayer.Instance.avatar == null)
                return Array.Empty<AvatarParameter>();
            List<AvatarParameter> parameterNames = new();
            foreach (AnimatorPlayable avatarAnimatorPlayable in LocalPlayer.Instance.avatar.AnimatorPlayables)
            {
                foreach (AnimatorControllerParameter parameter in avatarAnimatorPlayable.AnimatorControllerParameters)
                {
                    if (parameterNames.Count(x => x.Name == parameter.name) <= 0)
                        parameterNames.Add(new AvatarParameter(LocalPlayer.Instance.avatar, avatarAnimatorPlayable,
                            parameter, false));
                }
            }
            return parameterNames.ToArray();
        }
        
        public static AvatarParameter GetAvatarParameter(string parameterName)
        {
            if (LocalPlayer.Instance == null || LocalPlayer.Instance.avatar == null)
                return null;
            foreach (AnimatorPlayable animatorPlayable in LocalPlayer.Instance.avatar.AnimatorPlayables)
            {
                foreach (AnimatorControllerParameter parameter in animatorPlayable.AnimatorControllerParameters)
                {
                    if (parameter.name == parameterName)
                        return new AvatarParameter(LocalPlayer.Instance.avatar, animatorPlayable, parameter, false);
                }
            }
            return null;
        }

        public static bool IsExtraneousObjectPresent(string key)
        {
            if (LocalPlayer.Instance == null)
                return false;
            return LocalPlayer.Instance.LastExtraneousObjects.ContainsKey(key);
        }

        public static string[] GetExtraneousObjectKeys()
        {
            List<string> keys = new();
            foreach (string key in LocalPlayer.Instance.LastExtraneousObjects.Keys)
                keys.Add(key);
            return keys.ToArray();
        }

        public static object GetExtraneousObject(string key)
        {
            if (LocalPlayer.Instance == null)
                return null;
            if (!LocalPlayer.Instance.LastExtraneousObjects.ContainsKey(key))
                return null;
            return LocalPlayer.Instance.LastExtraneousObjects[key];
        }

        public static string[] GetPlayerAssignedTags()
        {
            if (LocalPlayer.Instance == null)
                return null;
            return LocalPlayer.Instance.LastPlayerAssignedTags.ToArray();
        }

        public static void Respawn()
        {
            if (LocalPlayer.Instance == null)
                return;
            LocalPlayer.Instance.Respawn();
        }
    }
}