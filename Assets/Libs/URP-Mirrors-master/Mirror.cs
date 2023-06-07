using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[ExecuteInEditMode]
public class Mirror : MonoBehaviour
{
    [Serializable]
    public class Info
    {
        public Camera cam;
        public RTHandle rtMain;
        public RTHandle rtAlt;
    }

    public GameObject thisGameObject => mrObj;
    private GameObject mrObj;
    private GameObject camObj;
    private Camera linkedCam;
    // private RTHandle rt;
    // private RTHandle rt2;
    private MeshRenderer mr;
    [Min(0.001f)]
    public float renderScale = 1f;
    public Vector2Int renderSize = Vector2Int.zero;
    private float oldRenderScale = 1f;
    private Vector2Int oldRenderSize = Vector2Int.zero;
    private Material mat;
    private List<Info> infos = new List<Info>();

    private void Awake()
    {
        mr = GetComponent<MeshRenderer>();
    }

    private void OnEnable()
    {
        if (mrObj == null)
            mrObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        mrObj.name = name + "_Quad";
        mrObj.transform.SetParent(transform, false);
        mrObj.hideFlags = HideFlags.HideAndDontSave;
        mr = mrObj.GetComponent<MeshRenderer>();
        if (camObj == null)
            camObj = new GameObject(name + "_Camera", typeof(Camera));
        camObj.transform.SetParent(transform, false);
        camObj.hideFlags = HideFlags.HideAndDontSave;
        linkedCam = camObj.GetComponent<Camera>();
        linkedCam.enabled = false;
        if (mat == null)
            mat = new Material(Shader.Find("VirtualBrightPlayz/Mirror"));
        mr.sharedMaterial = mat;
        RTHandles.Initialize(Screen.width, Screen.height);
        oldRenderScale = renderScale;
        oldRenderSize = renderSize;
        RenderPipelineManager.beginCameraRendering += OnCameraRender;
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.update += EditorUpdate;
    #endif
    }

    // https://forum.unity.com/threads/camera-current-returns-null-when-calling-it-in-onwillrenderobject-with-universalrp.929880/
    private static Plane[] frustrumPlanes = new Plane[6];

    private static bool IsVisible(Camera camera, Bounds bounds)
    {
        GeometryUtility.CalculateFrustumPlanes(camera, frustrumPlanes);
        return GeometryUtility.TestPlanesAABB(frustrumPlanes, bounds);
    }

    private RTHandle AllocMain()
    {
        if (renderSize.x <= 0 || renderSize.y <= 0)
            return RTHandles.Alloc(Vector2.one * renderScale, name: $"{name}_Main");
        return RTHandles.Alloc(renderSize.x, renderSize.y, name: $"{name}_Main");
    }

    private RTHandle AllocAlt()
    {
        if (renderSize.x <= 0 || renderSize.y <= 0)
            return RTHandles.Alloc(Vector2.one * renderScale, name: $"{name}_Alt");
        return RTHandles.Alloc(renderSize.x, renderSize.y, name: $"{name}_Alt");
    }

    private void OnCameraRender(ScriptableRenderContext arg1, Camera arg2)
    {
        if (IsVisible(arg2, mr.bounds))
        {
            OnWillRenderObjectWCam(arg2, true);
        }
    }

    private void LateUpdate()
    {
        if (oldRenderScale != renderScale || oldRenderSize != renderSize)
        {
            OnDisable();
            OnEnable();
        }
        foreach (var cam in Camera.allCameras)
        {
            if (IsVisible(cam, mr.bounds))
            {
                OnWillRenderObjectWCam(cam, false);
            }
        }
    }

#if UNITY_EDITOR
    private void EditorUpdate()
    {
        foreach (Camera view in UnityEditor.SceneView.GetAllSceneCameras())
        {
            if (IsVisible(view, mr.bounds))
            {
                OnWillRenderObjectWCam(view, false);
            }
        }
    }
#endif

    private void OnDisable()
    {
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.update -= EditorUpdate;
    #endif
        RenderPipelineManager.beginCameraRendering -= OnCameraRender;
        foreach (var info in infos)
        {
            RTHandles.Release(info.rtMain);
            RTHandles.Release(info.rtAlt);
        }
        infos.Clear();
    }

    private void OnDestroy()
    {
        DestroyImmediate(mrObj);
        DestroyImmediate(camObj);
    }

    private void OnWillRenderObjectWCam(Camera current, bool render)
    {
        if (current == linkedCam)
            return;
        int camIdx = infos.FindIndex(x => x.cam == current);
        if (camIdx == -1)
        {
            infos.Add(new Info()
            {
                cam = current,
                rtMain = AllocMain(),
                rtAlt = AllocAlt(),
            });
            camIdx = infos.Count - 1;
        }
        var rt = infos[camIdx].rtMain;
        var rt2 = infos[camIdx].rtAlt;
        if (linkedCam && thisGameObject)
        {
            if (current.stereoEnabled)
            {
                var pos = current.transform.position;
                var rot = current.transform.rotation;
                var pos2 = current.transform.position;
                var rot2 = current.transform.rotation;

                var mat = current.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right);
                var mat3 = current.GetStereoViewMatrix(Camera.StereoscopicEye.Right);

                var mat2 = current.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left);
                var mat4 = current.GetStereoViewMatrix(Camera.StereoscopicEye.Left);

                RenderCam(current, pos, rot, mat, rt, "_MainTex", render);
                RenderCam(current, pos2, rot2, mat2, rt2, "_AltTex", render);
            }
            else
            {
                var pos = current.transform.position;
                var rot = current.transform.rotation;
                var mat = current.projectionMatrix;

                RenderCam(current, pos, rot, mat, rt, "_MainTex", render);
            }
        }
    }

    public void RenderCam(Camera current, Vector3 pos, Quaternion rot, Matrix4x4 mat, RTHandle rt3, string texId, bool render)
    {
        if (rt3 == null)
            return;
        if (!render)
        {
            linkedCam.stereoTargetEye = StereoTargetEyeMask.None;

            var prevRenderMat = linkedCam.projectionMatrix;
            var prevRenderNear = linkedCam.nearClipPlane;
            var prevPos = linkedCam.transform.position;
            var prevRot = linkedCam.transform.rotation;

            linkedCam.fieldOfView = current.fieldOfView;
            linkedCam.nearClipPlane = current.nearClipPlane;
            linkedCam.farClipPlane = current.farClipPlane;
            linkedCam.projectionMatrix = mat;

            linkedCam.transform.localPosition = Vector3.Reflect(thisGameObject.transform.InverseTransformPoint(pos), Vector3.forward);
            linkedCam.transform.localRotation = Quaternion.LookRotation(Vector3.Reflect(thisGameObject.transform.InverseTransformDirection(rot * Vector3.forward), Vector3.forward), Vector3.Reflect(thisGameObject.transform.InverseTransformDirection(rot * Vector3.up), Vector3.forward));

            Transform clipPlane = thisGameObject.transform;
            int dot = System.Math.Sign(Vector3.Dot(clipPlane.forward, (clipPlane.position - linkedCam.transform.position)));

            Vector3 camSpacePos = (linkedCam.worldToCameraMatrix).MultiplyPoint(clipPlane.position);
            Vector3 camSpaceNormal = (linkedCam.worldToCameraMatrix).MultiplyVector(clipPlane.forward) * dot;
            float camSpaceDist = -Vector3.Dot(camSpacePos, camSpaceNormal);
            Vector4 clipPlaneCamSpace = new Vector4(camSpaceNormal.x, camSpaceNormal.y, camSpaceNormal.z, camSpaceDist);
            var renderMat = linkedCam.CalculateObliqueMatrix(clipPlaneCamSpace);
            linkedCam.projectionMatrix = renderMat;
            // linkedCam.nearClipPlane = Vector3.Distance(linkedCam.transform.position, clipPlane.position);

            linkedCam.forceIntoRenderTexture = true;
            var oldrt = linkedCam.targetTexture;
            linkedCam.targetTexture = rt3;

            RenderPipeline.SubmitRenderRequest(linkedCam, new UniversalRenderPipeline.SingleCameraRequest()
            {
                destination = rt3,
            });
            linkedCam.targetTexture = oldrt;
        }

        this.mat.SetTexture(texId, rt3);
    }
}
