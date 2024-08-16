using System.Collections.Generic;
using Hypernex.CCK.Unity;
using Hypernex.Networking.Messages.Data;
using Hypernex.Player;
using Hypernex.Tools;

namespace Hypernex.Configuration.ConfigMeta
{
    public class ConfigUser
    {
        public string Server;
        public string UserId;
        public string Username;
        public string TokenContent;

        public string CurrentAvatar;
        public string HomeWorld;
        public List<string> SavedAvatars = new ();
        public List<string> SavedWorlds = new ();

        public bool UseFacialTracking;
        public Dictionary<string, string> FacialTrackingSettings = new();

        public string Theme;
        public int EmojiType;
        public AudioCompression AudioCompression = AudioCompression.Opus;
        public string GestureType = "hypernex";

        public float VoicesBoost = 0f;
        public float WorldAudioVolume = 0f;
        public float AvatarAudioVolume = 0f;
        public bool NoiseSuppression;

        public float VRPlayerHeight;
        public bool UseSnapTurn;
        public float SnapTurnAngle = 45f;
        public float SmoothTurnSpeed = 1f;

        public AllowedAvatarComponent AnyoneAvatarComponents = new(true, true, true, true, true, true);
        public AllowedAvatarComponent FriendsAvatarComponents = new(true, true, true, true, true, true);

        public float2 DefaultCameraDimensions = new(1920, 1080);

        public AllowedAvatarComponent GetAllowedAvatarComponents(string userId)
        {
            if (APIPlayer.APIUser.Friends.Contains(userId)) return FriendsAvatarComponents;
            return AnyoneAvatarComponents;
        }

        public void Clone(ConfigUser c)
        {
            Server = c.Server;
            UserId = c.UserId;
            Username = c.Username;
            TokenContent = c.TokenContent;
            CurrentAvatar = c.CurrentAvatar;
            HomeWorld = c.HomeWorld;
            SavedAvatars = c.SavedAvatars;
            SavedWorlds = c.SavedWorlds;
            UseFacialTracking = c.UseFacialTracking;
            FacialTrackingSettings = c.FacialTrackingSettings;
            Theme = c.Theme;
            EmojiType = c.EmojiType;
            AudioCompression = c.AudioCompression;
            GestureType = c.GestureType;
            VoicesBoost = c.VoicesBoost;
            WorldAudioVolume = c.WorldAudioVolume;
            AvatarAudioVolume = c.AvatarAudioVolume;
            NoiseSuppression = c.NoiseSuppression;
            VRPlayerHeight = c.VRPlayerHeight;
            UseSnapTurn = c.UseSnapTurn;
            SnapTurnAngle = c.SnapTurnAngle;
            SmoothTurnSpeed = c.SmoothTurnSpeed;
        }
    }
}