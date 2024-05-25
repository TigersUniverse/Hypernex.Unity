using System;
using Hypernex.Game;
using Hypernex.Game.Avatar;
using Hypernex.Tools;
using HypernexSharp.APIObjects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Logger = Hypernex.CCK.Logger;

namespace Hypernex.UI.Templates
{
    public class NameplateTemplate : MonoBehaviour
    {
        public NetPlayer np;
        public RawImage Banner;
        public RawImage Pfp;
        public Image Status;
        public TMP_Text Username;
        public GameObject PronounContainer;

        public Texture2D DefaultPfp;
        public Texture2D DefaultBanner;

        public Camera MainCamera;
        private Transform FollowTransform;
        
        private TMP_Text pronounText;
        private AvatarCreator lastAvatar;

        public void Render(User user)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                child.gameObject.SetActive(true);
            }
            Username.text = !string.IsNullOrEmpty(user.Bio.DisplayName) ? user.Bio.DisplayName : user.Username;
            switch (user.Bio.Status)
            {
                case HypernexSharp.APIObjects.Status.Online:
                    Status.color = ColorTools.RGBtoHSV(44, 224, 44);
                    break;
                case HypernexSharp.APIObjects.Status.Absent:
                    Status.color = ColorTools.RGBtoHSV(255, 187, 15);
                    break;
                case HypernexSharp.APIObjects.Status.Party:
                    Status.color = ColorTools.RGBtoHSV(41, 185, 255);
                    break;
                case HypernexSharp.APIObjects.Status.DoNotDisturb:
                    Status.color = ColorTools.RGBtoHSV(224, 44, 44);
                    break;
                default:
                    Status.color = ColorTools.RGBtoHSV(128, 128, 128);
                    break;
            }
            if (user.Bio.Pronouns != null)
            {
                (pronounText == null ? pronounText = PronounContainer.transform.GetChild(0).GetComponent<TMP_Text>() : pronounText)!.text =
                    user.Bio.Pronouns.ToString();
                PronounContainer.SetActive(true);
            }
            else
                PronounContainer.SetActive(false);
            if (!string.IsNullOrEmpty(user.Bio.PfpURL))
                DownloadTools.DownloadBytes(user.Bio.PfpURL,
                    bytes =>
                    {
                        if (GifRenderer.IsGif(bytes))
                        {
                            GifRenderer gifRenderer = Pfp.gameObject.AddComponent<GifRenderer>();
                            gifRenderer.LoadGif(bytes);
                        }
                        else
                            Pfp.texture = ImageTools.BytesToTexture2D(user.Bio.PfpURL, bytes);
                    });
            else
                Pfp.texture = DefaultPfp;
            if (!string.IsNullOrEmpty(user.Bio.BannerURL))
                DownloadTools.DownloadBytes(user.Bio.BannerURL,
                    bytes =>
                    {
                        if (GifRenderer.IsGif(bytes))
                        {
                            GifRenderer gifRenderer = Banner.gameObject.AddComponent<GifRenderer>();
                            gifRenderer.LoadGif(bytes);
                        }
                        else
                            Banner.texture = ImageTools.BytesToTexture2D(user.Bio.BannerURL, bytes);
                    });
            else
                Banner.texture = DefaultBanner;
            if (user.Badges == null)
            {
                Logger.CurrentLogger.Warn("User " + user.Id + " has null Badges! This is usually an API fault. Please report this to system administrators!");
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
            FollowTransform.transform.SetParent(avatarCreator.Avatar.transform, true);
        }
        
        private void Update() => transform.localScale = new Vector3(0.003f, 0.003f, 0.003f);

        private void FixedUpdate()
        {
            if (FollowTransform != null)
                transform.position = FollowTransform.position;
            else if (np != null)
                transform.position = new Vector3(np.transform.position.x,
                    np.transform.position.y + np.transform.localScale.y + 1, np.transform.position.z);
            transform.rotation =
                Quaternion.LookRotation((transform.position - MainCamera.transform.position).normalized);
        }

        private void OnDestroy()
        {
            if(FollowTransform != null)
                Destroy(FollowTransform.gameObject);
        }
    }
}