using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hypernex.Networking.Messages;
using Hypernex.Networking.Messages.Data;
using Hypernex.Player;
using Hypernex.Tools;
using Hypernex.UI.Templates;
using HypernexSharp.API;
using HypernexSharp.API.APIResults;
using HypernexSharp.APIObjects;
using HypernexSharp.Socketing.SocketMessages;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hypernex.Game
{
    public class NetPlayer : MonoBehaviour
    {
        [HideInInspector] public string UserId;
        private GameInstance instance;
        private Scene scene;
        public User User;
        public AvatarCreator Avatar;
        
        private string AvatarId;
        private SharedAvatarToken avatarFileToken;
        private AvatarMeta avatarMeta;
        private Builds avatarBuild;
        private NameplateTemplate nameplateTemplate;
        
        private Vector3 headOffset;
        
        public float volume = 1f;
        private AudioClip voice;

        [HideInInspector] public List<string> LastPlayerTags = new();
        public Dictionary<string, object> LastExtraneousObjects = new();

        private void CreateNameplate()
        {
            GameObject np = Instantiate(DontDestroyMe.GetNotDestroyedObject("Templates").transform.Find("Nameplate")
                .gameObject);
            SceneManager.MoveGameObjectToScene(np, instance.loadedScene);
            // ReSharper disable once Unity.InstantiateWithoutParent
            np.transform.SetParent(transform);
            np.gameObject.SetActive(true);
            nameplateTemplate = np.GetComponent<NameplateTemplate>();
            nameplateTemplate.Render(User);
        }

        private void OnUser(CallbackResult<GetUserResult> result)
        {
            if (!result.success)
            {
                APIPlayer.APIObject.GetUser(OnUser, UserId, isUserId: true);
                return;
            }
            QuickInvoke.InvokeActionOnMainThread(new Action(() =>
            {
                User = result.result.UserData;
                CreateNameplate();
            }));
        }

        private void OnAvatarDownload(Stream stream)
        {
            waitingForAvatarToken = false;
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
                    StartCoroutine(AssetBundleTools.LoadAvatarFromFile(path, a =>
                    {
                        if (a == null)
                            return;
                        Avatar?.Dispose();
                        avatarUpdates.Clear();
                        Avatar = new AvatarCreator(this, a);
                        // NetPlayers do not run LocalScripts
                        /*foreach (NexboxScript localAvatarScript in a.LocalAvatarScripts)
                            Avatar.localAvatarSandboxes.Add(new Sandbox(localAvatarScript, SandboxRestriction.LocalAvatar));*/
                        headOffset = Avatar.Avatar.SpeechPosition -
                                     Avatar.GetBoneFromHumanoid(HumanBodyBones.Head).position;
                        // TODO: Resize based on avatar size
                        if (nameplateTemplate != null)
                            nameplateTemplate.transform.SetLocalPositionAndRotation(
                                new Vector3(0, transform.localScale.y + 0.9f, 0),
                                Quaternion.identity);
                    }));
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
                    Builds b = null;
                    foreach (Builds metaBuild in result.result.Meta.Builds)
                        if (metaBuild.BuildPlatform == AssetBundleTools.Platform)
                            b = metaBuild;
                    if (b == null)
                        return;
                    avatarBuild = b;
                    if (avatarMeta.Publicity == AvatarPublicity.OwnerOnly)
                    {
                        // TODO: Wait for token
                        if (SocketManager.SharedAvatarTokens.Count(x =>
                                x.avatarId == AvatarId && x.fromUserId == UserId) > 0)
                        {
                            SharedAvatarToken sharedAvatarToken = SocketManager.SharedAvatarTokens.First(x =>
                                x.avatarId == AvatarId && x.fromUserId == UserId);
                            avatarFileToken = sharedAvatarToken;
                        }
                        else
                        {
                            waitingForAvatarToken = true;
                            return;
                        }
                    }
                    if (avatarFileToken == null)
                        APIPlayer.APIObject.GetFile(OnAvatarDownload, result.result.Meta.OwnerId, b.FileId);
                    else
                        APIPlayer.APIObject.GetFile(OnAvatarDownload, result.result.Meta.OwnerId, b.FileId,
                            avatarFileToken.avatarToken);
                }));
        }

        private bool waitingForAvatarToken;

        public void Start()
        {
            GameObject g = new GameObject("VoicePosition");
            g.transform.SetParent(transform, false);
            SocketManager.OnAvatarToken += token =>
            {
                if (waitingForAvatarToken && token.fromUserId == UserId && token.avatarId == AvatarId)
                {
                    waitingForAvatarToken = false;
                    avatarFileToken = token;
                    APIPlayer.APIObject.GetFile(OnAvatarDownload, avatarMeta.OwnerId, avatarBuild.FileId,
                        avatarFileToken.avatarToken);
                }
            };
        }

        public void Update()
        {
            //float interpolationRatio = (float)elapsedFrames / interpolationFramesCount;
            if (instance != null && User == null)
            {
                foreach (User instanceConnectedUser in instance.ConnectedUsers)
                {
                    if (instanceConnectedUser.Id == UserId)
                        User = instanceConnectedUser;
                }
            }
            if (nameplateTemplate != null && Avatar != null)
            {
                Transform bone = Avatar.GetBoneFromHumanoid(HumanBodyBones.Head);
                if (bone != null)
                {
                    Vector3 newPos = bone.position;
                    newPos.y += 0.9f;
                    nameplateTemplate.transform.position = newPos;
                }
            }
        }

        private Dictionary<string, PlayerObjectUpdate> avatarUpdates = new();

        private void LateUpdate()
        {
            float interpolationRatio = Time.frameCount / Time.time;
            foreach (KeyValuePair<string,PlayerObjectUpdate> playerObjectUpdate in avatarUpdates)
            {
                Transform target = transform.Find(playerObjectUpdate.Key);
                NetworkedObject networkedObject = playerObjectUpdate.Value.Object;
                if (target != null)
                {
                    Vector3 position = NetworkConversionTools.float3ToVector3(networkedObject.Position);
                    //Quaternion rotation = NetworkConversionTools.float4ToQuaternion(networkedObject.Rotation);
                    //Vector3 localSize = NetworkConversionTools.float3ToVector3(networkedObject.Size);
                    if (string.IsNullOrEmpty(networkedObject.ObjectLocation))
                    {
                        /*target.position = position;
                        target.rotation = Quaternion.Euler(new Vector3(networkedObject.Rotation.x, networkedObject.Rotation.y,
                            networkedObject.Rotation.z));*/
                        target.position = Vector3.Lerp(target.position, position, interpolationRatio);
                        target.rotation = Quaternion.Lerp(target.rotation, Quaternion.Euler(new Vector3(
                            networkedObject.Rotation.x, networkedObject.Rotation.y,
                            networkedObject.Rotation.z)), interpolationRatio);
                    }
                    else
                    {
                        /*target.localPosition = position;
                        target.localRotation = Quaternion.Euler(new Vector3(networkedObject.Rotation.x, networkedObject.Rotation.y,
                            networkedObject.Rotation.z));*/
                        target.localPosition = Vector3.Lerp(target.localPosition, position, interpolationRatio);
                        target.localRotation = Quaternion.Lerp(target.localRotation, Quaternion.Euler(new Vector3(
                            networkedObject.Rotation.x, networkedObject.Rotation.y,
                            networkedObject.Rotation.z)), interpolationRatio);

                    }
                    //target.localScale = localSize;
                }
            }
        }

        public void Init(string userid, GameInstance gameInstance)
        {
            UserId = userid;
            scene = gameInstance.loadedScene;
            instance = gameInstance;
            APIPlayer.APIObject.GetUser(OnUser, UserId, isUserId: true);
        }

        public void VoiceUpdate(PlayerVoice playerVoice)
        {
            if (Avatar != null && Avatar.Avatar.gameObject.scene == scene)
                Avatar.opusHandler.DecodeFromVoice(playerVoice);
        }

        public void NetworkUpdate(PlayerUpdate playerUpdate)
        {
            if (!string.IsNullOrEmpty(playerUpdate.AvatarId) && (string.IsNullOrEmpty(AvatarId) ||
                playerUpdate.AvatarId != AvatarId))
            {
                AvatarId = playerUpdate.AvatarId;
                APIPlayer.APIObject.GetAvatarMeta(OnAvatar, AvatarId);
            }
            if (Avatar != null && Avatar.Avatar.transform.parent == transform)
            {
                foreach (KeyValuePair<string,float> weightedObject in playerUpdate.WeightedObjects)
                    Avatar.HandleNetParameter(weightedObject.Key, weightedObject.Value);
                LastPlayerTags = new List<string>(playerUpdate.PlayerAssignedTags);
                LastExtraneousObjects = new Dictionary<string, object>(playerUpdate.ExtraneousData);
                Avatar.audioSource.volume = playerUpdate.IsSpeaking ? volume : 0f;
            }
        }

        /*public void NetworkObjectUpdate(PlayerObjectUpdate playerObjectUpdate)
        {
            float interpolationRatio = interpolationFramesCount * Time.deltaTime;
            if (Avatar != null && Avatar.Avatar.transform.parent == transform)
            {
                NetworkedObject networkedObject = playerObjectUpdate.Object;
                Transform target;
                if (string.IsNullOrEmpty(networkedObject.ObjectLocation))
                    target = transform;
                else
                    target = transform.Find(networkedObject.ObjectLocation);
                if (target != null)
                {
                    Vector3 position = NetworkConversionTools.float3ToVector3(networkedObject.Position);
                    Quaternion rotation = NetworkConversionTools.float4ToQuaternion(networkedObject.Rotation);
                    Vector3 localSize = NetworkConversionTools.float3ToVector3(networkedObject.Size);
                    if (string.IsNullOrEmpty(networkedObject.ObjectLocation))
                    {
                        /*target.position = position;
                        target.rotation = Quaternion.Euler(new Vector3(networkedObject.Rotation.x, networkedObject.Rotation.y,
                            networkedObject.Rotation.z));#1#
                        target.position = Vector3.Lerp(target.position, position, interpolationRatio);
                        target.rotation = Quaternion.Lerp(target.rotation, Quaternion.Euler(new Vector3(
                            networkedObject.Rotation.x, networkedObject.Rotation.y,
                            networkedObject.Rotation.z)), interpolationRatio);
                    }
                    else
                    {
                        /*target.localPosition = position;
                        target.localRotation = Quaternion.Euler(new Vector3(networkedObject.Rotation.x, networkedObject.Rotation.y,
                            networkedObject.Rotation.z));#1#
                        target.localPosition = Vector3.Lerp(target.localPosition, position, interpolationRatio);
                        target.localRotation = Quaternion.Lerp(target.localRotation, Quaternion.Euler(new Vector3(
                            networkedObject.Rotation.x, networkedObject.Rotation.y,
                            networkedObject.Rotation.z)), interpolationRatio);

                    }
                    //target.localScale = localSize;
                }
            }
        }*/

        public void NetworkObjectUpdate(PlayerObjectUpdate playerObjectUpdate)
        {
            // TODO: Double Lerp
            if (string.IsNullOrEmpty(playerObjectUpdate.Object.ObjectLocation))
                playerObjectUpdate.Object.ObjectLocation = "";
            if (!avatarUpdates.ContainsKey(playerObjectUpdate.Object.ObjectLocation))
                avatarUpdates.Add(playerObjectUpdate.Object.ObjectLocation, playerObjectUpdate);
            else
                avatarUpdates[playerObjectUpdate.Object.ObjectLocation] = playerObjectUpdate;
        }
    }
}