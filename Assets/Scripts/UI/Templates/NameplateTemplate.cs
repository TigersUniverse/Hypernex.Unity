using System;
using System.Globalization;
using Hypernex.Game;
using Hypernex.Game.Avatar;
using Hypernex.UI.Abstraction;
using HypernexSharp.APIObjects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Logger = Hypernex.CCK.Logger;

namespace Hypernex.UI.Templates
{
    [RequireComponent(typeof(UserRender))]
    public class NameplateTemplate : MonoBehaviour
    {
        [HideInInspector] public NetPlayer np;
        public RectTransform BadgesContainer;
        public Slider DownloadProgress;
        public TMP_Text DownloadProgressText;

        private UserRender UserRender;
        private Transform FollowTransform;
        private TMP_Text pronounText;
        private AvatarCreator lastAvatar;

        public void Render(User user)
        {
            UserRender = GetComponent<UserRender>();
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                child.gameObject.SetActive(true);
            }
            UserRender.Render(user);
            if (user.Badges == null)
            {
                Logger.CurrentLogger.Warn("[API] User " + user.Id + " has null Badges! This is usually an API fault. Please report this to system administrators!");
                return;
            }
            user.Badges.ForEach(x => BadgeRankHandler.GetBadgeHandlerByName(x)?.ApplyToNameplate(this, user));
            BadgeRankHandler.GetRankHandlersByRank(user.Rank).ForEach(x => x.ApplyToNameplate(this, user));
        }

        public void OnNewAvatar(AvatarCreator avatarCreator)
        {
            if (lastAvatar == avatarCreator)
                return;
            lastAvatar = avatarCreator;
            if(FollowTransform != null)
                Destroy(FollowTransform.gameObject);
            FollowTransform = new GameObject("nametagalign_" + Guid.NewGuid()).transform;
            Transform head = avatarCreator.GetBoneFromHumanoid(HumanBodyBones.Head);
            if (head == null)
            {
                FollowTransform.parent = transform;
                FollowTransform.transform.localPosition = new Vector3(0, transform.localScale.y + 1.5f, 0);
                return;
            }
            FollowTransform.transform.parent = head;
            FollowTransform.transform.localPosition = avatarCreator.headRotator.rootReference * head.up;
            Vector3 newpos = FollowTransform.transform.localPosition;
            newpos.y -= 0.1f;
            FollowTransform.transform.localPosition = newpos;
            FollowTransform.transform.SetParent(avatarCreator.Avatar.transform, true);
        }

        private void Update()
        {
            transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);
            if (np == null || !np.IsLoadingAvatar)
            {
                DownloadProgress.gameObject.SetActive(false);
                return;
            }
            DownloadProgress.gameObject.SetActive(true);
            DownloadProgress.value = np.AvatarDownloadPercentage;
            DownloadProgressText.text = np.AvatarDownloadPercentage <= 0
                ? "0%"
                : np.AvatarDownloadPercentage.ToString("P0", CultureInfo.CurrentCulture);
        }

        private void FixedUpdate()
        {
            if (FollowTransform != null)
                transform.position = FollowTransform.position;
            else if (np != null)
                transform.position = new Vector3(np.transform.position.x,
                    np.transform.position.y + np.transform.localScale.y + 1, np.transform.position.z);
            transform.rotation =
                Quaternion.LookRotation((transform.position - LocalPlayer.Instance.Camera.transform.position).normalized);
        }

        private void OnDestroy()
        {
            if(FollowTransform != null)
                Destroy(FollowTransform.gameObject);
        }
    }
}