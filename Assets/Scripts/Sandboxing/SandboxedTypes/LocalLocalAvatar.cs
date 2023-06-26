using Hypernex.Game;
using Hypernex.Networking.Messages.Data;
using Hypernex.Tools;
using UnityEngine;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    // I know this class name sounds stupid, but it is the LocalAvatar for Local scripts
    public static class LocalLocalAvatar
    {
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
        
        public static object GetParameter(string parameterName)
        {
            if (LocalPlayer.Instance == null || LocalPlayer.Instance.avatar == null)
                return null;
            return LocalPlayer.Instance.avatar.GetParameter(parameterName);
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