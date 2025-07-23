using System;
using System.Collections.Generic;
using Hypernex.Configuration;
using Hypernex.Game;
using Hypernex.Game.Avatar;
using Hypernex.Networking.Messages.Data;
using Hypernex.Tools;
using HypernexSharp.APIObjects;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public class Player
    {
        internal static List<string> AssignedTags = new();
        internal static List<string> ExtraneousKeys = new();
        
        private GameInstance gameInstance;
        private SandboxRestriction sandboxRestriction;
        private User user;
        private NetPlayer netPlayer;
        private LocalPlayer localPlayer;

        public Player() => throw new Exception("Cannot instantiate Player!");
        internal Player(GameInstance gameInstance, SandboxRestriction sandboxRestriction, User user, NetPlayer netPlayer)
        {
            this.gameInstance = gameInstance;
            this.sandboxRestriction = sandboxRestriction;
            this.user = user;
            this.netPlayer = netPlayer;
        }
        
        internal Player(GameInstance gameInstance, SandboxRestriction sandboxRestriction, User user, LocalPlayer localPlayer)
        {
            this.gameInstance = gameInstance;
            this.sandboxRestriction = sandboxRestriction;
            this.user = user;
            this.localPlayer = localPlayer;
        }

        private bool isLocalPlayer => localPlayer != null;
        private AvatarCreator currentAvatarCreator => localPlayer != null ? localPlayer.avatar : netPlayer.Avatar;

        private AvatarCreator lastAvatarCreator;
        private Avatar avatarCache;
        
        public bool IsHost => gameInstance != null && gameInstance.host != null && gameInstance.host.Id == user.Id;

        public Avatar Avatar
        {
            get
            {
                AvatarCreator avatarCreator = currentAvatarCreator;
                if (avatarCreator == null || avatarCreator.Avatar == null) return null;
                if (avatarCache != null && avatarCreator == lastAvatarCreator) return avatarCache;
                lastAvatarCreator = avatarCreator;
                avatarCache = new Avatar(gameInstance, avatarCreator, sandboxRestriction);
                return avatarCache;
            }
        }
        
        public Pronouns Pronouns => user.Bio.Pronouns;
        public string Username => user.Username;
        public string DisplayName => string.IsNullOrEmpty(user.Bio.DisplayName) ? user.Username : user.Bio.DisplayName;
        public bool IsVR => netPlayer != null ? netPlayer.lastVR : LocalPlayer.IsVR;
        
        public bool IsExtraneousObjectPresent(string key)
        {
            if (isLocalPlayer)
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
            if (isLocalPlayer)
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
            if (isLocalPlayer)
            {
                if (localPlayer == null)
                    return null;
                if (!localPlayer.LocalPlayerSyncController.LastExtraneousObjects.ContainsKey(key)) return null;
                return localPlayer.LocalPlayerSyncController.LastExtraneousObjects[key];
            }
            if (netPlayer == null)
                return null;
            if (!netPlayer.LastExtraneousObjects.ContainsKey(key)) return null;
            return netPlayer.LastExtraneousObjects[key];
        }
        
        public void AddExtraneousObject(string key, object value)
        {
            if (sandboxRestriction == SandboxRestriction.Local || !isLocalPlayer)
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
            if (sandboxRestriction == SandboxRestriction.Local || !isLocalPlayer)
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
            if (isLocalPlayer)
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
            if (sandboxRestriction == SandboxRestriction.Local || !isLocalPlayer)
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
            if (sandboxRestriction == SandboxRestriction.Local || !isLocalPlayer)
                return;
            if (localPlayer == null)
                return;
            if (AssignedTags.Contains(tag))
                AssignedTags.Remove(tag);
            if (LocalPlayer.MorePlayerAssignedTags.Contains(tag))
                LocalPlayer.MorePlayerAssignedTags.Remove(tag);
        }
        
        public void Respawn()
        {
            if (localPlayer == null || gameInstance == null)
                return;
            if (!gameInstance.World.AllowRespawn && sandboxRestriction != SandboxRestriction.Local)
                return;
            localPlayer.Respawn();
        }
        
        public void TeleportTo(float3 position)
        {
            if (localPlayer == null || sandboxRestriction != SandboxRestriction.Local)
                return;
            if(localPlayer.Dashboard.IsVisible)
                localPlayer.Dashboard.PositionDashboard(LocalPlayer.Instance);
            localPlayer.CharacterController.enabled = false;
            localPlayer.transform.position = NetworkConversionTools.float3ToVector3(position);
            localPlayer.CharacterController.enabled = true;
        }
        
        public void Rotate(float4 rotation)
        {
            if (localPlayer == null || sandboxRestriction != SandboxRestriction.Local)
                return;
            if(localPlayer.Dashboard.IsVisible)
                localPlayer.Dashboard.PositionDashboard(LocalPlayer.Instance);
            localPlayer.CharacterController.enabled = false;
            localPlayer.transform.rotation = NetworkConversionTools.float4ToQuaternion(rotation);
            localPlayer.CharacterController.enabled = true;
        }

        public void SetAvatar(string avatarId)
        {
            if (localPlayer == null || gameInstance == null)
                return;
            if(gameInstance.World.LockAvatarSwitching || sandboxRestriction != SandboxRestriction.Local)
                return;
            ConfigManager.SelectedConfigUser.CurrentAvatar = avatarId;
            localPlayer.LoadAvatar();
            ConfigManager.SaveConfigToFile();
        }
    }
}