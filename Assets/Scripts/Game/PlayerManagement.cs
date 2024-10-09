using System.Collections.Generic;
using Hypernex.Networking.Messages;
using Hypernex.Networking.Messages.Bulk;
using Hypernex.Player;
using HypernexSharp.APIObjects;
using UnityEngine;
using UnityEngine.SceneManagement;
using Logger = Hypernex.CCK.Logger;

namespace Hypernex.Game
{
    public class PlayerManagement
    {
        private static Dictionary<GameInstance, List<NetPlayer>> players = new();
        public static Dictionary<GameInstance, List<NetPlayer>> Players => new(players);

        public static NetPlayer GetNetPlayer(GameInstance instance, string userid)
        {
            foreach (KeyValuePair<GameInstance,List<NetPlayer>> keyValuePair in Players)
            {
                if (keyValuePair.Key.gameServerId == instance.gameServerId && keyValuePair.Key.instanceId == instance.instanceId)
                    foreach (NetPlayer netPlayer in keyValuePair.Value)
                        if (netPlayer.UserId == userid)
                            return netPlayer;
            }
            return null;
        }

        private static NetPlayer GetOrCreateNetPlayer(GameInstance instance, string userid)
        {
            if (!Players.ContainsKey(instance))
                return null;
            NetPlayer netPlayer = GetNetPlayer(instance, userid);
            if (netPlayer != null)
                return netPlayer;
            GameObject gameObject = new GameObject(userid);
            netPlayer = gameObject.AddComponent<NetPlayer>();
            netPlayer.Init(userid, instance);
            SceneManager.MoveGameObjectToScene(gameObject, instance.loadedScene);
            players[instance].Add(netPlayer);
            return netPlayer;
        }
        
        public static void HandlePlayerUpdate(GameInstance gameInstance, PlayerUpdate playerUpdate)
        {
            if (playerUpdate.Auth.UserId == APIPlayer.APIUser?.Id || string.IsNullOrEmpty(playerUpdate.Auth.UserId))
                return;
            NetPlayer netPlayer = GetOrCreateNetPlayer(gameInstance, playerUpdate.Auth.UserId);
            if (netPlayer != null)
                netPlayer.NetworkUpdate(playerUpdate);
            else
                Logger.CurrentLogger.Debug(
                    $"NetPlayer not found for {gameInstance.gameServerId}/{gameInstance.instanceId}/{playerUpdate.Auth.UserId}");
        }

        public static void HandlePlayerDataUpdate(GameInstance gameInstance, PlayerDataUpdate playerDataUpdate)
        {
            if (playerDataUpdate.Auth.UserId == APIPlayer.APIUser?.Id ||
                string.IsNullOrEmpty(playerDataUpdate.Auth.UserId))
                return;
            NetPlayer netPlayer = GetOrCreateNetPlayer(gameInstance, playerDataUpdate.Auth.UserId);
            if (netPlayer != null)
                netPlayer.NetworkDataUpdate(playerDataUpdate);
            else
                Logger.CurrentLogger.Debug(
                    $"NetPlayer not found for {gameInstance.gameServerId}/{gameInstance.instanceId}/{playerDataUpdate.Auth.UserId}");
        }

        public static void HandleWeightedObjectUpdate(GameInstance gameInstance, WeightedObjectUpdate weightedObjectUpdate)
        {
            if (weightedObjectUpdate.Auth.UserId == APIPlayer.APIUser?.Id ||
                string.IsNullOrEmpty(weightedObjectUpdate.Auth.UserId))
                return;
            NetPlayer netPlayer = GetOrCreateNetPlayer(gameInstance, weightedObjectUpdate.Auth.UserId);
            if (netPlayer != null)
                netPlayer.WeightedObject(weightedObjectUpdate);
            else
                Logger.CurrentLogger.Debug(
                    $"NetPlayer not found for {gameInstance.gameServerId}/{gameInstance.instanceId}/{weightedObjectUpdate.Auth.UserId}");
        }
        
        public static void HandleWeightedObjectUpdate(GameInstance gameInstance, BulkWeightedObjectUpdate weightedObjectUpdates)
        {
            if (weightedObjectUpdates.Reset)
            {
                NetPlayer netPlayer = GetOrCreateNetPlayer(gameInstance, weightedObjectUpdates.Auth.UserId);
                if (netPlayer != null)
                    netPlayer.ResetWeightedObjects(weightedObjectUpdates);
                else
                    Logger.CurrentLogger.Debug(
                        $"NetPlayer not found for {gameInstance.gameServerId}/{gameInstance.instanceId}/{weightedObjectUpdates.Auth.UserId}");
                return;
            }
            foreach (WeightedObjectUpdate weightedObjectUpdate in weightedObjectUpdates.WeightedObjectUpdates)
            {
                weightedObjectUpdate.Auth = weightedObjectUpdates.Auth;
                HandleWeightedObjectUpdate(gameInstance, weightedObjectUpdate);
            }
        }

        public static void HandlePlayerObjectUpdate(GameInstance gameInstance, PlayerObjectUpdate playerObjectUpdate)
        {
            if (playerObjectUpdate.Auth.UserId == APIPlayer.APIUser?.Id || string.IsNullOrEmpty(playerObjectUpdate.Auth.UserId))
                return;
            NetPlayer netPlayer = GetOrCreateNetPlayer(gameInstance, playerObjectUpdate.Auth.UserId);
            if (netPlayer != null)
                netPlayer.NetworkObjectUpdate(playerObjectUpdate);
            else
                Logger.CurrentLogger.Debug(
                    $"NetPlayer not found for {gameInstance.gameServerId}/{gameInstance.instanceId}/{playerObjectUpdate.Auth.UserId}");
        }

        public static void HandlePlayerVoice(GameInstance gameInstance, PlayerVoice playerVoice)
        {
            if (playerVoice.Auth.UserId == APIPlayer.APIUser?.Id || string.IsNullOrEmpty(playerVoice.Auth.UserId))
                return;
            NetPlayer netPlayer = GetOrCreateNetPlayer(gameInstance, playerVoice.Auth.UserId);
            if (netPlayer != null)
                netPlayer.VoiceUpdate(playerVoice);
            else
                Logger.CurrentLogger.Debug(
                    $"NetPlayer not found for {gameInstance.gameServerId}/{gameInstance.instanceId}/{playerVoice.Auth.UserId}");
        }

        public static void PlayerLeave(GameInstance gameInstance, User user)
        {
            if (!Players.ContainsKey(gameInstance))
                return;
            NetPlayer netPlayer = GetNetPlayer(gameInstance, user.Id);
            if (netPlayer != null)
            {
                players[gameInstance].Remove(netPlayer);
                Object.Destroy(netPlayer.gameObject);
            }
            gameInstance.LocalScriptEvents?.OnUserLeave.Invoke(user.Id);
            gameInstance.AvatarScriptEvents?.OnUserLeave.Invoke(user.Id);
            if (!gameInstance.isHost) return;
            // Claim all NetworkSyncs that have Host Only
            foreach (GameObject rootGameObject in gameInstance.loadedScene.GetRootGameObjects())
            {
                Transform[] ts = rootGameObject.GetComponentsInChildren<Transform>(true);
                foreach (Transform transform in ts)
                {
                    NetworkSync networkSync = transform.gameObject.GetComponent<NetworkSync>();
                    if (networkSync == null) continue;
                    if(networkSync.InstanceHostOnly)
                        networkSync.Claim();
                }
            }
        }

        internal static void CreateGameInstance(GameInstance gameInstance)
        {
            if(!Players.ContainsKey(gameInstance))
                players.Add(gameInstance, new List<NetPlayer>());
        }

        internal static void DestroyGameInstance(GameInstance gameInstance)
        {
            if (Players.ContainsKey(gameInstance))
                players.Remove(gameInstance);
        }
    }
}