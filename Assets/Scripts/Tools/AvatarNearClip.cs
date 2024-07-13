using System;
using System.Collections.Generic;
using System.Linq;
using Hypernex.Game.Avatar;
using UnityEngine;
using UnityEngine.Rendering;
using Logger = Hypernex.CCK.Logger;

namespace Hypernex.Tools
{
    /// <summary>
    /// The AvatarNearClip is a Script that automatically hides the Head of an Avatar and any other stray Renderers.
    /// Heavily documented because this took me FOREVER to figure out
    /// </summary>
    [RequireComponent(typeof(SkinnedMeshRenderer))]
    public class AvatarNearClip : MonoBehaviour
    {
        public static Action<ScriptableRenderContext, Camera> BeforeClip = (context, camera1) => { };
        public static List<AvatarNearClip> Instances => new (instances);
        
        internal static List<AvatarNearClip> instances = new();

        private static bool didInvokeClip;

        private SkinnedMeshRenderer skinnedMeshRenderer;
        private SkinnedMeshRenderer skinnedMeshRendererShadowClone;
        private Transform[] originalBones;
        private Transform[] excludedBones;
        private Transform bonefiller;
        private Camera r;
        private bool isr;
        private ShadowCastingMode skinnedMeshRendererShadowMode;
        private List<Renderer> strayRenderers = new();
        private Dictionary<Renderer, (Renderer, ShadowCastingMode)> strayRenderersShadows = new();
        private bool isStraySMR;

        public static bool UseFallback() =>
            !Instances.Any(x => x.skinnedMeshRenderer.enabled && x.gameObject.activeInHierarchy);

        /// <summary>
        /// Prepares the AvatarClip for use
        /// </summary>
        /// <param name="avatarCreator">The avatar to apply the clipping to</param>
        /// <param name="renderCamera">The camera the avatar is rendering on</param>
        /// <param name="ignoreStrayRenderers">If you don't want to clip accessories on an avatar</param>
        public bool Setup(AvatarCreator avatarCreator, Camera renderCamera, bool ignoreStrayRenderers = false)
        {
            // Setup our SkinnedMeshRenderer
            skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
            if (skinnedMeshRenderer == null ||
                (skinnedMeshRenderer != null && skinnedMeshRenderer.name.Contains("shadowclone_")))
            {
                Destroy(this);
                return false;
            }
            // This automatically makes it so the SkinnedMeshRenderer recreates its matrices, makes life a lot easier
            skinnedMeshRenderer.forceMatrixRecalculationPerRender = true;
            // Cache the original bones
            originalBones = skinnedMeshRenderer.bones;
            // Remember the original ShadowCastingMode
            skinnedMeshRendererShadowMode = skinnedMeshRenderer.shadowCastingMode;
            // Listen to RenderPipeline Events
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
            // Gets the head from the AvatarCreator
            Transform head = avatarCreator.GetBoneFromHumanoid(HumanBodyBones.Head);
            if (head == null)
            {
                // If no head is found, simply cleanup
                Logger.CurrentLogger.Warn("Cannot create Avatar NearClip! Head is null");
                Destroy(this);
                return false;
            }
            // If the SkinnedMeshRenderer has no root bone, and it's a part of the Head, then it's stray
            if (skinnedMeshRenderer.rootBone == null)
                isStraySMR = AnimationUtility.IsChildOfTransform(skinnedMeshRenderer.transform, head);
            // If the SkinnedMeshRenderer's root bone is a part of the Head, then it's also stray
            else if (AnimationUtility.IsChildOfTransform(skinnedMeshRenderer.rootBone, head))
                isStraySMR = true;
            else if (!originalBones.Contains(head))
            {
                if (skinnedMeshRenderer.rootBone != null &&
                    AnimationUtility.IsChildOfTransform(skinnedMeshRenderer.rootBone, head))
                {
                    // This SkinnedMeshRenderer's rootBone is apart of the head
                    head = skinnedMeshRenderer.rootBone;
                }
            }
            // Make sure our target renderCamera is set
            r = renderCamera;
            isr = ignoreStrayRenderers;
            if (isStraySMR)
                return true;
            // For some reason, if bone counts don't match, the SMR wont actually render
            bonefiller = new GameObject("bonefiller_" + Guid.NewGuid()).transform;
            // We position this at the Head, because this is technically our new head bone
            bonefiller.SetParent(head.parent, false);
            // Setting the scale to zero actually "hides" the bones
            bonefiller.localScale = Vector3.zero;
            // visibleExcludedBones are the Transforms that are visible when removing SMR bones
            List<Transform> visibleExcludedBones = new();
            foreach (Transform t in originalBones)
            {
                // If the Transform is our Head, or a child of our Head, we want to hide the Transform
                // Otherwise, make it visible!
                if(t == head || AnimationUtility.IsChildOfTransform(t, head))
                    visibleExcludedBones.Add(bonefiller);
                else
                    visibleExcludedBones.Add(t);
            }
            // Push our result to the excluded bones
            excludedBones = visibleExcludedBones.ToArray();
            if (!ignoreStrayRenderers)
            {
                // Get any other Renderers in the Head
                strayRenderers.Clear();
                foreach (Renderer r in head.GetComponentsInChildren<Renderer>())
                {
                    // If the Stray Renderer already exists, then skip over it
                    if(strayRenderers.Contains(r)) continue;
                    // Exclude any possible shadowclones or SkinnedMeshRenderers
                    if(!r.name.Contains("shadowclone_") && r.GetType() != typeof(SkinnedMeshRenderer))
                        strayRenderers.Add(r);
                }
            }
            return true;
        }

        private Renderer FindPossibleShadowClone(Transform r)
        {
            Renderer re = null;
            for (int i = 0; i < r.transform.childCount; i++)
            {
                // Get each child of a possible Shadow Clone
                Transform child = r.transform.GetChild(i);
                // See if the child contains a Renderer
                Renderer pr = child.gameObject.GetComponent<Renderer>();
                // If the renderer doesn't exist, is a SkinnedMeshRenderer, or isn't a Shadow Clone, keep looking
                if (pr == null || pr.GetType() == typeof(SkinnedMeshRenderer) ||
                    !pr.name.Contains("shadowclone_")) continue;
                // We found our Shadow Clone, stop looking
                re = pr;
                break;
            }
            return re;
        }

        /// <summary>
        /// Creates a clone of the SkinnedMeshRenderer and all Stray Renderers that are for shadows only
        /// </summary>
        public void CreateShadows()
        {
            // Only create a Shadow Clone if needed
            if (skinnedMeshRenderer.shadowCastingMode != ShadowCastingMode.Off)
            {
                // Create a new SkinnedMeshRenderer
                skinnedMeshRendererShadowClone = Instantiate(skinnedMeshRenderer.gameObject).GetComponent<SkinnedMeshRenderer>();
                // Name it shadowclone_ so wwe know it's a Shadow Clone
                skinnedMeshRendererShadowClone.name = "shadowclone_" + Guid.NewGuid();
                // Remove the duplicate AvatarNearClip
                Destroy(skinnedMeshRendererShadowClone.GetComponent<AvatarNearClip>());
                // Make sure we have the original bones
                skinnedMeshRendererShadowClone.bones = originalBones;
                // Make it a child of the skinnedMeshRenderer
                skinnedMeshRendererShadowClone.transform.SetParent(skinnedMeshRenderer.transform);
                skinnedMeshRendererShadowClone.transform.localRotation = new Quaternion(0,0,0,0);
                skinnedMeshRendererShadowClone.transform.localPosition = Vector3.zero;
                skinnedMeshRendererShadowClone.transform.localScale = Vector3.one;
                // Enable ShadowsOnly for the Shadow Clone
                skinnedMeshRendererShadowClone.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                // Disable Shadows for the SkinnedMeshRenderer
                skinnedMeshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            }
            // Check if we should shadow clone our Stray Renderers
            if (isr) return;
            foreach (Renderer strayRenderer in strayRenderers)
            {
                // Only create a Shadow Clone if needed
                if(strayRenderer.shadowCastingMode == ShadowCastingMode.Off) continue;
                // Check if a Shadow Clone already exists
                Renderer shadowClone = FindPossibleShadowClone(strayRenderer.transform);
                // If it doesn't create a new one
                if(shadowClone == null)
                    shadowClone = Instantiate(strayRenderer.gameObject).GetComponent<Renderer>();
                // Name it shadowclone_ so we know it's a Shadow Clone
                shadowClone.name = "shadowclone_" + Guid.NewGuid();
                // Make it a child of the Renderer
                shadowClone.transform.SetParent(strayRenderer.transform, false);
                // Enable ShadowsOnly for the Shadow Clone
                shadowClone.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                // Remember what the ShadowCastingMode was
                ShadowCastingMode rendererShadowMode = strayRenderer.shadowCastingMode;
                // Disable Shadows for the Renderer
                strayRenderer.shadowCastingMode = ShadowCastingMode.Off;
                // If there's a definition for our Renderer, it's probably old or Destroyed, remove it
                if (strayRenderersShadows.ContainsKey(strayRenderer))
                    strayRenderersShadows.Remove(strayRenderer);
                // Add our new Shadow Clone
                strayRenderersShadows.Add(strayRenderer, (shadowClone, rendererShadowMode));
            }
        }

        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera c)
        {
            if (c == r)
            {
                if (instances.FirstOrDefault(x => x.skinnedMeshRenderer.enabled && x.gameObject.activeInHierarchy) ==
                    this && !didInvokeClip)
                {
                    BeforeClip.Invoke(context, c);
                    didInvokeClip = true;
                }
                // If the Camera in this context is our renderCamera, then show the excludedBones
                if (isStraySMR)
                    skinnedMeshRenderer.enabled = false;
                else
                    skinnedMeshRenderer.bones = excludedBones;
                // Hide any Renderers in the Avatar
                strayRenderers.ForEach(x => x.enabled = false);
            }
        }
        
        private void OnEndCameraRendering(ScriptableRenderContext context, Camera c)
        {
            // Always revert back to the originalBones
            if (isStraySMR)
                skinnedMeshRenderer.enabled = true;
            else
                skinnedMeshRenderer.bones = originalBones;
            // Show the Renderers in the Avatar again
            strayRenderers.ForEach(x => x.enabled = true);
            // Allow clip again
            if (!isStraySMR) didInvokeClip = false;
        }

        private void Update()
        {
            // Sanity check for events
            for (int i = 0; i < Instances.Count; i++)
            {
                AvatarNearClip avatarNearClip = Instances[i];
                if(avatarNearClip == null)
                    instances.RemoveAt(i);
            }
        }

        // Run this on LateUpdate to make sure this is the last thing to happen
        private void LateUpdate()
        {
            // Check if the skinnedMeshRenderer is null or if there is no Shadow Clone yet
            if (skinnedMeshRenderer == null || skinnedMeshRendererShadowClone == null)
                return;
            // Get all Blendshapes in the SkinnedMeshRenderer
            for (int i = 0; i < skinnedMeshRendererShadowClone.sharedMesh.blendShapeCount; i++)
            {
                // Clone the Blendshapes from the original SkinnedMeshRenderer to the Shadow Clone
                skinnedMeshRendererShadowClone.SetBlendShapeWeight(i, skinnedMeshRenderer.GetBlendShapeWeight(i));
            }
        }

        private void OnEnable()
        {
            if(Instances.Contains(this)) return;
            instances.Add(this);
        }

        private void OnDestroy()
        {
            try
            {
                instances.Remove(this);
            } catch(Exception){}
            if (gameObject.name.Contains("shadowclone_"))
                return;
            // Revert all RenderPipeline Events
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
            // Remove our old bonefiller, if it existed
            if(bonefiller != null)
                Destroy(bonefiller.gameObject);
            // Revert back to old changes
            skinnedMeshRenderer.bones = originalBones;
            skinnedMeshRenderer.shadowCastingMode = skinnedMeshRendererShadowMode;
            if(skinnedMeshRendererShadowClone != null)
                Destroy(skinnedMeshRendererShadowClone.gameObject);
            foreach (KeyValuePair<Renderer,(Renderer, ShadowCastingMode)> strayRenderersShadow in strayRenderersShadows)
            {
                Destroy(strayRenderersShadow.Value.Item1.gameObject);
                strayRenderersShadow.Key.shadowCastingMode = strayRenderersShadow.Value.Item2;
            }
            strayRenderersShadows.Clear();
            strayRenderers.Clear();
        }
    }
}