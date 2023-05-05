using System;
using System.IO;
using System.Linq;
using Hypernex.Networking.Messages;
using Hypernex.Networking.Messages.Data;
using Hypernex.Player;
using Hypernex.Tools;
using HypernexSharp.API;
using HypernexSharp.API.APIResults;
using HypernexSharp.APIObjects;
using HypernexSharp.Socketing.SocketMessages;
using UnityEngine;
using UnityEngine.SceneManagement;
using Avatar = Hypernex.CCK.Unity.Avatar;

namespace Hypernex.Game
{
    public class NetPlayer : MonoBehaviour
    {
        public string UserId;
        private Scene scene;
        public User User;
        private string AvatarId;
        private SharedAvatarToken avatarFileToken;
        private AvatarMeta avatarMeta;
        private Builds avatarBuild;
        [HideInInspector] public Avatar Avatar;
        private Animator mainAnimator;
        
        private Vector3 headOffset;
        
        public float volume = 1f;
        private AudioClip voice;

        private void OnUser(CallbackResult<GetUserResult> result)
        {
            if (!result.success)
            {
                APIPlayer.APIObject.GetUser(OnUser, UserId, isUserId: true);
                return;
            }
            QuickInvoke.InvokeActionOnMainThread(new Action(() => User = result.result.UserData));
        }

        private void OnAvatarDownload(Stream stream)
        {
            if (stream == Stream.Null)
            {
                if (avatarFileToken == null)
                    APIPlayer.APIObject.GetFile(OnAvatarDownload, avatarMeta.OwnerId, avatarBuild.FileId);
                else
                    APIPlayer.APIObject.GetFile(OnAvatarDownload, avatarMeta.OwnerId, avatarBuild.FileId,
                        avatarFileToken.avatarToken);
                return;
            }
            QuickInvoke.InvokeActionOnMainThread(new Action(() =>
            {
                string path = Path.Combine(DownloadTools.DownloadsPath, $"{AvatarId}.hna");
                if(File.Exists(path))
                    File.Delete(path);
                using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite,
                           FileShare.ReadWrite | FileShare.Delete))
                {
                    stream.CopyTo(fs);
                    Avatar = AssetBundleTools.LoadAvatarFromFile(path);
                    HandleNewAvatar();
                }
            }));
        }

        private void OnAvatar(CallbackResult<MetaCallback<AvatarMeta>> result)
        {
            if (!result.success)
            {
                APIPlayer.APIObject.GetAvatarMeta(OnAvatar, AvatarId);
                return;
            }
            if (result.result.Meta.Id == AvatarId)
                QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                {
                    avatarMeta = result.result.Meta;
                    if (SocketManager.SharedAvatarTokens.Count(x =>
                            x.avatarId == AvatarId && x.fromUserId == UserId) > 0)
                    {
                        SharedAvatarToken sharedAvatarToken = SocketManager.SharedAvatarTokens.First(x =>
                            x.avatarId == AvatarId && x.fromUserId == UserId);
                        avatarFileToken = sharedAvatarToken;
                    }
                    Builds b = null;
                    foreach (Builds metaBuild in result.result.Meta.Builds)
                        if (metaBuild.BuildPlatform == AssetBundleTools.Platform)
                            b = metaBuild;
                    if (b == null)
                        return;
                    avatarBuild = b;
                    if (avatarFileToken == null)
                        APIPlayer.APIObject.GetFile(OnAvatarDownload, result.result.Meta.OwnerId, b.FileId);
                    else
                        APIPlayer.APIObject.GetFile(OnAvatarDownload, result.result.Meta.OwnerId, b.FileId,
                            avatarFileToken.avatarToken);
                }));
        }

        public void Start()
        {
            GameObject g = new GameObject("VoicePosition");
            g.transform.SetParent(transform, false);
        }

        public void Init(string userid, Scene s)
        {
            UserId = userid;
            scene = s;
            APIPlayer.APIObject.GetUser(OnUser, UserId, isUserId: true);
        }
        
        // https://stackoverflow.com/q/16078254/12968919
        // https://stackoverflow.com/a/16180762/12968919
        private float[] ConvertByteToFloat(byte[] array) 
        {
            float[] floatArr = new float[array.Length / 4];
            for (int i = 0; i < floatArr.Length; i++) 
            {
                if (BitConverter.IsLittleEndian) 
                    Array.Reverse(array, i * 4, 4);
                floatArr[i] = BitConverter.ToSingle(array, i*4) / 0x80000000;
            }
            return floatArr;
        }

        public void VoiceUpdate(PlayerVoice playerVoice)
        {
            if (Avatar != null && Avatar.gameObject.scene == scene)
            {
                float[] data = ConvertByteToFloat(playerVoice.Bytes);
                AudioClip clip = AudioClip.Create(UserId + "_voice", data.Length, playerVoice.Channels,
                    playerVoice.SampleRate, false);
                clip.SetData(data, 0);
                AudioSource.PlayClipAtPoint(clip,
                    headOffset - mainAnimator.GetBoneTransform(HumanBodyBones.Head).position, volume);
            }
        }

        public void NetworkUpdate(PlayerUpdate playerUpdate)
        {
            if (!string.IsNullOrEmpty(playerUpdate.AvatarId) && string.IsNullOrEmpty(AvatarId) ||
                playerUpdate.AvatarId != AvatarId)
            {
                AvatarId = playerUpdate.AvatarId;
                APIPlayer.APIObject.GetAvatarMeta(OnAvatar, AvatarId);
            }
            if (Avatar.transform.parent == transform)
            {
                foreach (NetworkedObject networkedObject in playerUpdate.TrackedObjects)
                {
                    Transform target;
                    if (string.IsNullOrEmpty(networkedObject.ObjectLocation))
                        target = transform;
                    else
                        target = transform.Find(networkedObject.ObjectLocation);
                    if (target != null)
                    {
                        Vector3 position = NetworkConversionTools.float3ToVector3(networkedObject.Position);
                        Quaternion rotation = NetworkConversionTools.float4ToQuaternion(networkedObject.Rotation);
                        target.SetPositionAndRotation(position, rotation);
                    }
                }
            }
        }

        private void HandleNewAvatar()
        {
            // This is invoked after an Avatar AssetBundle is downloaded and loaded
            GameObject newAvatarObject = Instantiate(Avatar.gameObject);
            Avatar = newAvatarObject.GetComponent<Avatar>();
            mainAnimator = newAvatarObject.GetComponent<Animator>();
            headOffset = Avatar.SpeechPosition - mainAnimator.GetBoneTransform(HumanBodyBones.Head).position;
            SceneManager.MoveGameObjectToScene(newAvatarObject, scene);
            // ReSharper disable once Unity.InstantiateWithoutParent
            newAvatarObject.transform.SetParent(transform, false);
        }
    }
}