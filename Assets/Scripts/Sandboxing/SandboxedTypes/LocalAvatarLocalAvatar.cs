using System.Collections.Generic;
using Hypernex.Game;
using Hypernex.Tools;
using UnityEngine;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public static class LocalAvatarLocalAvatar
    {
        internal static List<string> AssignedTags = new();
        internal static List<string> ExtraneousKeys = new();

        public static Item GetAvatarObject(HumanBodyBones humanBodyBones)
        {
            if (LocalPlayer.Instance == null || LocalPlayer.Instance.avatar == null)
                return null;
            Transform bone = LocalPlayer.Instance.avatar.GetBoneFromHumanoid(humanBodyBones);
            if (bone == null)
                return null;
            return new Item(bone);
        }

        public static Item GetAvatarObjectByPath(string path)
        {
            if (LocalPlayer.Instance == null || LocalPlayer.Instance.avatar == null)
                return null;
            Transform bone = LocalPlayer.Instance.avatar.Avatar.transform.Find(path);
            if (bone == null)
                return null;
            return new Item(bone);
        }
        
        public static bool IsAvatarItem(Item item) =>
            AnimationUtility.GetRootOfChild(item.t).gameObject.GetComponent<LocalPlayer>() != null;
        
        public static bool IsAvatarItem(ReadonlyItem item) =>
            AnimationUtility.GetRootOfChild(item.item.t).gameObject.GetComponent<LocalPlayer>() != null;

        public static object GetParameter(string parameterName)
        {
            if (LocalPlayer.Instance == null || LocalPlayer.Instance.avatar == null)
                return null;
            return LocalPlayer.Instance.avatar.GetParameter(parameterName);
        }

        public static void SetParameter(string name, bool value)
        {
            if (LocalPlayer.Instance == null || LocalPlayer.Instance.avatar == null)
                return;
            LocalPlayer.Instance.avatar.SetParameter(name, value);
        }
        
        public static void SetParameter(string name, int value)
        {
            if (LocalPlayer.Instance == null || LocalPlayer.Instance.avatar == null)
                return;
            LocalPlayer.Instance.avatar.SetParameter(name, value);
        }
        
        public static void SetParameter(string name, float value)
        {
            if (LocalPlayer.Instance == null || LocalPlayer.Instance.avatar == null)
                return;
            LocalPlayer.Instance.avatar.SetParameter(name, value);
        }
        
        public static object GetExtraneousObject(string key)
        {
            if (LocalPlayer.Instance == null)
                return null;
            if (!LocalPlayer.Instance.LastExtraneousObjects.ContainsKey(key))
                return null;
            return LocalPlayer.Instance.LastExtraneousObjects[key];
        }

        public static void AddExtraneousObject(string key, object value)
        {
            if (LocalPlayer.Instance == null)
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

        public static void RemoveExtraneousObject(string key)
        {
            if (LocalPlayer.Instance == null)
                return;
            if (ExtraneousKeys.Contains(key))
                ExtraneousKeys.Remove(key);
            if (LocalPlayer.MoreExtraneousObjects.ContainsKey(key))
                LocalPlayer.MorePlayerAssignedTags.Remove(key);
        }
        
        public static string[] GetPlayerAssignedTags()
        {
            if (LocalPlayer.Instance == null)
                return null;
            return LocalPlayer.Instance.LastPlayerAssignedTags.ToArray();
        }
        
        public static void AddPlayerAssignedTag(string tag)
        {
            if (LocalPlayer.Instance == null)
                return;
            if(!AssignedTags.Contains(tag))
                AssignedTags.Add(tag);
            if (LocalPlayer.MorePlayerAssignedTags.Contains(tag))
                return;
            LocalPlayer.MorePlayerAssignedTags.Add(tag);
        }

        public static void RemovePlayerAssignedTag(string tag)
        {
            if (LocalPlayer.Instance == null)
                return;
            if (AssignedTags.Contains(tag))
                AssignedTags.Remove(tag);
            if (LocalPlayer.MorePlayerAssignedTags.Contains(tag))
                LocalPlayer.MorePlayerAssignedTags.Remove(tag);
        }
    }
}