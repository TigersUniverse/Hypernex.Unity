using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hypernex.Configuration;
using Hypernex.Game.Bindings;
using Hypernex.Networking.Messages.Data;
using Hypernex.Tools;
using Hypernex.UI;
using Hypernex.UI.Components;
using Hypernex.UIActions.Data;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using MessageButtons = Hypernex.UI.Abstraction.MessageButtons;
using MessageMeta = Hypernex.UI.Abstraction.MessageMeta;
using MessageUrgency = Hypernex.UI.Abstraction.MessageUrgency;

namespace Hypernex.Game
{
    [RequireComponent(typeof(Grabbable))]
    [RequireComponent(typeof(DontDestroyMe))]
    public class HandleCamera : MonoBehaviour, IDisposable
    {
        private const string TEMPLATE_CAMERA_NAME = "HandleCamera";
        private const string CAMERA_CANVAS_NAME = "CameraCanvas";
        private const string HANDLE_CAMERA_CANVAS_NAME = "HandleCameraCanvas";
        
        public static HandleCamera[] allCameras => new List<HandleCamera>(handleCameras).Where(x => x != null).ToArray();

        private static readonly List<HandleCamera> handleCameras = new();

        public static HandleCamera Create()
        {
            GameObject newCamera = Instantiate(DontDestroyMe.GetNotDestroyedObject("Templates").transform
                .Find(TEMPLATE_CAMERA_NAME)).gameObject;
            string n = "camera_" + Guid.NewGuid();
            while(DontDestroyMe.Cache.ContainsKey(n))
                n = "camera_" + Guid.NewGuid();
            newCamera.name = n;
            newCamera.transform.parent = null;
            return newCamera.AddComponent<HandleCamera>();
        }

        public static void DisposeAll()
        {
            foreach (HandleCamera handleCamera in allCameras)
                handleCamera.Dispose();
        }

        public bool IsGrabbable => grabbable.enabled;
        public bool IsOutputting { get; private set; }
        public bool IsAnchored => transform.parent == LocalPlayer.Instance.transform;
        public bool AttachedToTracker => attachedTracker != null;
        public bool LookingAtAvatar { get; private set; }
        
        public float WaitTime;

        private Vector3 OriginalPosition;
        private Quaternion OriginalRotation;
        private Camera LinkedCamera;
        private Canvas HandleCameraCanvas;
        private RectTransform LayoutContainer;
        private Grabbable grabbable;
        private DontDestroyMe dontDestroyMe;
        private RenderTexture rt;
        private RawImage StreamRenderOutput;
        
        private RawImage CameraRenderOutput;
        private Button DisposeButton;
        public Button DimensionsSelect;
        public TMP_Text DimensionsText;
        public Button GrabbableButton;
        public Button OutputButton;
        public Button AnchorButton;
        public Button TrackerAttachmentButton;
        public Button WaitTimeInput;
        public TMP_Text WaitTimeText;
        public Button LookAtAvatarButton;

        private XRTracker attachedTracker;
        private bool isCapturing;
        private bool requestCapture;
        private float lastSelectedD;
        private float2 d;
        private float2 lastD;
        private CameraDimensions cameraDimensions = CameraDimensions.p1080;
        private WaitTimes waitTimes = WaitTimes.s0;

        public void UpdateDimensions(float2 dd)
        {
            d = dd;
            if(DimensionsText == null) return;
            DimensionsText.text = dd.y + "p";
        }

        public void SetCameraProperties(float2 dimensions) =>
            SetCameraProperties((int) dimensions.x, (int) dimensions.y);

        public void SetCameraProperties(int width, int height)
        {
            if (rt != null)
            {
                rt.DiscardContents();
                rt.Release();
            }
            rt = new RenderTexture(width, height, 16);
            rt.useDynamicScale = false;
            rt.Create();
            CameraRenderOutput.texture = rt;
            StreamRenderOutput.texture = rt;
            LinkedCamera.targetTexture = rt;
        }

        public void ToggleGrabbable()
        {
            grabbable.enabled = !IsGrabbable;
            GrabbableButton.GetComponent<ToggleButton>().isOn = grabbable.enabled;
        }

        public void ToggleOutput()
        {
            // Can't output if we already are
            if (allCameras.Count(x => x.IsOutputting && x != this) > 0)
                return;
            switch (IsOutputting)
            {
                case false:
                    lastD = new(d.x, d.y);
                    UpdateDimensions(new float2(Screen.width, Screen.height));
                    SetCameraProperties(new float2(Screen.width, Screen.height));
                    StreamRenderOutput.transform.parent.gameObject.GetComponent<Canvas>().enabled = true;
                    LinkedCamera.GetUniversalAdditionalCameraData().antialiasing =
                        Camera.main.GetUniversalAdditionalCameraData().antialiasing;
                    IsOutputting = true;
                    break;
                case true:
                    StreamRenderOutput.transform.parent.gameObject.GetComponent<Canvas>().enabled = false;
                    IsOutputting = false;
                    UpdateDimensions(lastD);
                    SetCameraProperties((int)CameraRenderOutput.rectTransform.rect.width / 2,
                        (int)CameraRenderOutput.rectTransform.rect.height / 2);
                    LinkedCamera.GetUniversalAdditionalCameraData().antialiasing = AntialiasingMode.None;
                    break;
            }
            OutputButton.GetComponent<ToggleButton>().isOn = IsOutputting;
        }

        public void ToggleAnchor()
        {
            if(IsAnchored)
            {
                transform.parent = null;
                dontDestroyMe.Register();
            }
            else
            {
                dontDestroyMe.MoveToScene(SceneManager.GetActiveScene());
                transform.parent = LocalPlayer.Instance.transform;
            }
            AnchorButton.GetComponent<ToggleButton>().isOn = IsAnchored;
        }

        public void ToggleTrackerAttachment()
        {
            if (AttachedToTracker)
                attachedTracker = null;
            else
                attachedTracker = FindCameraTracker();
            TrackerAttachmentButton.GetComponent<ToggleButton>().isOn = AttachedToTracker;
        }

        public void ToggleLookAtAvatar()
        {
            LookingAtAvatar = !LookingAtAvatar;
            if (!LookingAtAvatar)
            {
                LinkedCamera.transform.localPosition = OriginalPosition;
                LinkedCamera.transform.localRotation = OriginalRotation;
            }
            LookAtAvatarButton.GetComponent<ToggleButton>().isOn = LookingAtAvatar;
        }

        public IEnumerator Capture()
        {
            if (!isCapturing)
            {
                isCapturing = true;
                yield return new WaitForSeconds(WaitTime);
                SetCameraProperties(d);
                UniversalAdditionalCameraData mainUniversal = LocalPlayer.Instance.Camera.GetUniversalAdditionalCameraData();
                UniversalAdditionalCameraData thisUniversal = LinkedCamera.GetUniversalAdditionalCameraData();
                thisUniversal.antialiasing = mainUniversal.antialiasing;
                thisUniversal.renderPostProcessing = true;
                yield return new WaitForSeconds(0.01f);
                requestCapture = true;
            }
        }

        public static string GetPhotoPath()
        {
            DateTime dateTime = DateTime.Now;
            string photosPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            string folder = Path.Combine(photosPath, "Hypernex", dateTime.Year.ToString(), dateTime.Month.ToString());
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            string fileName =
                $"Hypernex-{dateTime.Year}-{dateTime.Month}-{(int) dateTime.DayOfWeek}_{dateTime.Hour}-{dateTime.Minute}-{dateTime.Second}.{dateTime.Millisecond}.png";
            return Path.Combine(folder, fileName);
        }

        private XRTracker FindCameraTracker()
        {
            if (LocalPlayer.Instance == null)
                return null;
            foreach (XRTracker xrTracker in LocalPlayer.Instance.transform.GetComponentsInChildren<XRTracker>())
            {
                if (xrTracker.TrackerRole == XRTrackerRole.Camera)
                    return xrTracker;
            }
            return null;
        }

        private TextureFormat RTFormatToTFormat(RenderTextureFormat rtf)
        {
            // not everything
            switch (rtf)
            {
                case RenderTextureFormat.ARGB32:
                    return TextureFormat.ARGB32;
                case RenderTextureFormat.RGB565:
                    return TextureFormat.RGB565;
                case RenderTextureFormat.ARGB64:
                    return TextureFormat.ARGB4444;
                case RenderTextureFormat.R16:
                    return TextureFormat.R16;
                case RenderTextureFormat.R8:
                    return TextureFormat.R8;
            }
            throw new Exception("Unable to find TextureFormat for " + rtf);
        }

        private void __capture() => StartCoroutine(Capture());

        private void FindHandleCameraReferences()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform t = transform.GetChild(i);
                t.gameObject.SetActive(true);
            }
            LocalPlayer.Instance.Camera.enabled = false;
            LocalPlayer.Instance.Camera.enabled = true;
            grabbable = GetComponent<Grabbable>();
            grabbable.ApplyVelocity = false;
            grabbable.GrabByLaser = false;
            grabbable.IgnoreLaserColoring = true;
            grabbable.OnBindingGrab += binding => binding.TriggerClick += __capture;
            grabbable.OnBindingRelease += binding => binding.TriggerClick -= __capture;
            dontDestroyMe = GetComponent<DontDestroyMe>();
            LinkedCamera = transform.Find("Camera").GetComponent<Camera>();
            LinkedCamera.stereoTargetEye = StereoTargetEyeMask.None;
            StreamRenderOutput = DontDestroyMe.GetNotDestroyedObject(CAMERA_CANVAS_NAME).transform.GetChild(0)
                .GetComponent<RawImage>();
            HandleCameraCanvas = transform.Find(HANDLE_CAMERA_CANVAS_NAME).GetComponent<Canvas>();
            CameraRenderOutput = HandleCameraCanvas.transform.GetChild(0).GetComponent<RawImage>();
            SetCameraProperties((int)CameraRenderOutput.rectTransform.rect.width / 2,
                (int)CameraRenderOutput.rectTransform.rect.height / 2);
            UpdateDimensions(ConfigManager.SelectedConfigUser != null
                ? ConfigManager.SelectedConfigUser.DefaultCameraDimensions
                : new(1920, 1080));
            LayoutContainer = HandleCameraCanvas.transform.GetChild(1).GetChild(0).GetComponent<RectTransform>();
            DisposeButton = LayoutContainer.GetChild(7).GetComponent<Button>();
            QuickInvoke.OverwriteListener(DisposeButton.onClick, Dispose);
            DimensionsSelect = LayoutContainer.GetChild(2).GetComponent<Button>();
            DimensionsText = DimensionsSelect.transform.GetChild(1).GetComponent<TMP_Text>();
            DimensionsText.text = d.y + "p";
            QuickInvoke.OverwriteListener(DimensionsSelect.onClick, () =>
            {
                int d = (int) cameraDimensions;
                d = d + 1;
                if (d >= (int) CameraDimensions.Max) d = (int) CameraDimensions.p720;
                cameraDimensions = (CameraDimensions) d;
                switch (cameraDimensions)
                {
                    case CameraDimensions.p720:
                        UpdateDimensions(new(1280,720));
                        break;
                    case CameraDimensions.p1080:
                        UpdateDimensions(new(1920, 1080));
                        break;
                    case CameraDimensions.p1440:
                        UpdateDimensions(new(2560, 1440));
                        break;
                    case CameraDimensions.p2160:
                        UpdateDimensions(new(4096, 2160));
                        break;
                    case CameraDimensions.p4320:
                        UpdateDimensions(new(7680, 4320));
                        break;
                    case CameraDimensions.p8640:
                        UpdateDimensions(new(15360, 8640));
                        break;
                }
            });
            GrabbableButton = LayoutContainer.GetChild(0).GetComponent<Button>();
            GrabbableButton.GetComponent<ToggleButton>().isOn = grabbable.enabled;
            QuickInvoke.OverwriteListener(GrabbableButton.onClick, ToggleGrabbable);
            OutputButton = LayoutContainer.GetChild(1).GetComponent<Button>();
            QuickInvoke.OverwriteListener(OutputButton.onClick, ToggleOutput);
            AnchorButton = LayoutContainer.GetChild(6).GetComponent<Button>();
            QuickInvoke.OverwriteListener(AnchorButton.onClick, ToggleAnchor);
            TrackerAttachmentButton = LayoutContainer.GetChild(5).GetComponent<Button>();
            QuickInvoke.OverwriteListener(TrackerAttachmentButton.onClick, ToggleTrackerAttachment);
            WaitTimeInput = LayoutContainer.GetChild(4).GetComponent<Button>();
            WaitTimeText = WaitTimeInput.transform.GetChild(0).GetComponent<TMP_Text>();
            WaitTimeText.text = $"Timer ({Convert.ToInt32(WaitTime)}s)";
            QuickInvoke.OverwriteListener(WaitTimeInput.onClick, () =>
            {
                int t = (int) waitTimes;
                t = t + 1;
                if (t >= (int) WaitTimes.Max) t = 0;
                waitTimes = (WaitTimes) t;
                switch (waitTimes)
                {
                    case WaitTimes.s0:
                        WaitTime = 0;
                        break;
                    case WaitTimes.s3:
                        WaitTime = 3;
                        break;
                    case WaitTimes.s5:
                        WaitTime = 5;
                        break;
                    case WaitTimes.s10:
                        WaitTime = 10;
                        break;
                }
                WaitTimeText.text = $"Timer ({Convert.ToInt32(WaitTime)}s)";
            });
            LookAtAvatarButton = LayoutContainer.GetChild(3).GetComponent<Button>();
            QuickInvoke.OverwriteListener(LookAtAvatarButton.onClick, ToggleLookAtAvatar);
        }

        private void OnCameraEndRender(ScriptableRenderContext context, Camera c)
        {
            if (Camera.main != c)
                return;
            if (rt != null)
            {
                if (requestCapture && rt.width == (int)d.x && rt.height == (int)d.y)
                {
                    RenderTexture oldRT = RenderTexture.active;
                    RenderTexture.active = rt;
                    TextureFormat tf = RTFormatToTFormat(rt.format);
                    Texture2D copiedRt = new Texture2D(rt.width, rt.height, tf, false);
                    copiedRt.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                    copiedRt.Apply();
                    RenderTexture.active = oldRT;
                    byte[] data = copiedRt.EncodeToPNG();
                    string file = GetPhotoPath();
                    FileStream fs = new FileStream(file, FileMode.OpenOrCreate, FileAccess.ReadWrite,
                        FileShare.ReadWrite | FileShare.Delete);
                    fs.Write(data, 0, data.Length);
                    fs.Dispose();
                    Destroy(copiedRt);
                    if(IsOutputting)
                        SetCameraProperties(Screen.width, Screen.height);
                    else
                    {
                        SetCameraProperties((int) CameraRenderOutput.rectTransform.rect.width / 2,
                            (int) CameraRenderOutput.rectTransform.rect.height / 2);
                        UniversalAdditionalCameraData universalAdditionalCameraData = LinkedCamera.GetUniversalAdditionalCameraData();
                        universalAdditionalCameraData.antialiasing = AntialiasingMode.None;
                        universalAdditionalCameraData.renderPostProcessing = false;
                    }
                    OverlayNotification.AddMessageToQueue(new MessageMeta(MessageUrgency.Info, MessageButtons.None)
                    {
                        Header = "Saved Photo",
                        Description = "Saved Photo to " + file
                    });
                    requestCapture = false;
                    isCapturing = false;
                }
            }
        }

        private void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += RenderPipelineManagerOnbeginCameraRendering;
            RenderPipelineManager.endCameraRendering += OnCameraEndRender;
            FindHandleCameraReferences();
            Transform reference = Camera.main.transform;
            float s = LocalPlayer.Instance.transform.localScale.y;
            transform.position = reference.position + reference.forward * (0.73f * s);
            transform.rotation = Quaternion.LookRotation((transform.position - reference.position).normalized);
            OriginalPosition = LinkedCamera.transform.localPosition;
            OriginalRotation = LinkedCamera.transform.localRotation;
            handleCameras.Add(this);
        }

        private void RenderPipelineManagerOnbeginCameraRendering(ScriptableRenderContext arg1, Camera arg2)
        {
            UniversalAdditionalCameraData mainUniversal = LocalPlayer.Instance.Camera.GetUniversalAdditionalCameraData();
            if(arg2 == LinkedCamera && requestCapture)
            {
                LinkedCamera.GetUniversalAdditionalCameraData().antialiasing = mainUniversal.antialiasing;
                return;
            }
            if (IsOutputting)
                return;
            LinkedCamera.GetUniversalAdditionalCameraData().antialiasing = AntialiasingMode.None;
        }

        private void Update()
        {
            LocalPlayer localPlayer = LocalPlayer.Instance;
            if (localPlayer != null && !IsAnchored)
                transform.localScale = new Vector3(localPlayer.transform.localScale.x,
                    localPlayer.transform.localScale.y, localPlayer.transform.localScale.z);
            if (attachedTracker != null)
            {
                if(IsGrabbable)
                    ToggleGrabbable();
                Transform thisTransform = transform;
                Transform cameraTransform = attachedTracker.transform;
                thisTransform.position = cameraTransform.position;
                thisTransform.rotation = cameraTransform.rotation;
            }
            if(LookingAtAvatar)
            {
                Transform reference = Camera.main.transform;
                if (localPlayer != null && LocalPlayer.Instance.avatar != null)
                {
                    Transform chest = LocalPlayer.Instance.avatar.GetBoneFromHumanoid(HumanBodyBones.Chest);
                    Transform head = LocalPlayer.Instance.avatar.GetBoneFromHumanoid(HumanBodyBones.Head);
                    if (chest != null && head != null)
                        reference = Vector3.Distance(transform.position, head.position) < 2 ? head : chest;
                    else if (chest != null)
                        reference = chest;
                    else if (head != null)
                        reference = head;
                }
                LinkedCamera.transform.LookAt(reference);
            }
            if(!LocalPlayer.IsVR && Input.GetKeyDown(KeyCode.Backspace)) Dispose();
        }

        private void OnDestroy()
        {
            RenderPipelineManager.beginCameraRendering -= RenderPipelineManagerOnbeginCameraRendering;
            RenderPipelineManager.endCameraRendering -= OnCameraEndRender;
            if (rt != null)
            {
                rt.DiscardContents();
                rt.Release();
                Destroy(rt);
            }
            StopAllCoroutines();
            handleCameras.Remove(this);
            if(IsOutputting)
                ToggleOutput();
        }

        public void Dispose() => Destroy(gameObject);
    }

    public enum CameraDimensions
    {
        p720,
        p1080,
        p1440,
        p2160,
        p4320,
        p8640,
        Max
    }

    public enum WaitTimes
    {
        s0,
        s3,
        s5,
        s10,
        Max
    }
}