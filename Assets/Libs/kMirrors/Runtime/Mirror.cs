using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;
using UnityEngine.XR;
using Object = UnityEngine.Object;

namespace kTools.Mirrors
{
    /// <summary>
    /// Mirror Object component.
    /// </summary>
    [AddComponentMenu("kTools/Mirror"), ExecuteInEditMode]
    [RequireComponent(typeof(Camera), typeof(UniversalAdditionalCameraData))]
    public class Mirror : MonoBehaviour
    {
#region Enumerations
        /// <summary>
        /// Camera override enumeration for Mirror properties
        /// <summary>
        public enum MirrorCameraOverride
        {
            UseSourceCameraSettings,
            Off,
        }

        /// <summary>
        /// Scope enumeration for Mirror output destination
        /// <summary>
        public enum OutputScope
        {
            Global,
            Local,
        }
#endregion

#region Serialized Fields
        [SerializeField]
        public bool CustomCameraControl;

        [SerializeField]
        public float MinRenderDistance = 0.01f;

        [SerializeField]
        public float MaxRenderDistance = 1000f;
        
        [SerializeField]
        float m_Offset;

        [SerializeField]
        int m_LayerMask;

        [SerializeField]
        OutputScope m_Scope;

        [SerializeField]
        List<Renderer> m_Renderers;

        [SerializeField]
        float m_TextureScale;

        [SerializeField]
        MirrorCameraOverride m_AllowHDR;

        [SerializeField]
        MirrorCameraOverride m_AllowMSAA;
#endregion

#region Fields
        const string kGizmoPath = "Packages/com.kink3d.mirrors/Gizmos/Mirror.png";
        Camera m_ReflectionCamera;
        UniversalAdditionalCameraData m_CameraData;
        RenderTexture m_RenderTexture;
        RenderTexture m_RenderTextureL;
        RenderTexture m_RenderTextureR;
        RenderTextureDescriptor m_PreviousDescriptor;
        RenderTextureDescriptor m_PreviousDescriptorStereo;
#endregion

#region Constructors
        public Mirror()
        {
            // Set data
            m_Offset = 0.01f;
            m_LayerMask = -1;
            m_Scope = OutputScope.Global;
            m_Renderers = new List<Renderer>();
            m_TextureScale = 1.0f;
            m_AllowHDR = MirrorCameraOverride.UseSourceCameraSettings;
            m_AllowMSAA = MirrorCameraOverride.UseSourceCameraSettings;
        }
#endregion

#region Properties
        /// <summary>Offset value for oplique near clip plane.</summary>
        public float offest
        {
            get => m_Offset;
            set => m_Offset = value;
        }

        /// <summary>Which layers should the Mirror render.</summary>
        public LayerMask layerMask
        {
            get => m_LayerMask;
            set => m_LayerMask = value;
        }

        /// <summary>
        /// Global output renders to the global texture. Only one Mirror can be global.
        /// Local output renders to one texture per Mirror, this is set on all elements of the Renderers list.
        /// </summary>
        public OutputScope scope
        {
            get => m_Scope;
            set => m_Scope = value;
        }

        /// <summary>Renderers to set the reflection texture on.</summary>
        public List<Renderer> renderers
        {
            get => m_Renderers;
            set => m_Renderers = value;
        }

        /// <summary>Scale value applied to the size of the source camera texture.</summary>
        public float textureScale
        {
            get => m_TextureScale;
            set => m_TextureScale = value;
        }

        /// <summary>Should reflections be rendered in HDR.</summary>
        public MirrorCameraOverride allowHDR
        {
            get => m_AllowHDR;
            set => m_AllowHDR = value;
        }

        /// <summary>Should reflections be resolved with MSAA.</summary>
        public MirrorCameraOverride allowMSAA
        {
            get => m_AllowMSAA;
            set => m_AllowMSAA = value;
        }

        Camera reflectionCamera
        {
            get
            {
                if(m_ReflectionCamera == null)
                    m_ReflectionCamera = GetComponent<Camera>();
                return m_ReflectionCamera;
            }
        }

        UniversalAdditionalCameraData cameraData
        {
            get
            {
                if(m_CameraData == null)
                    m_CameraData = GetComponent<UniversalAdditionalCameraData>();
                return m_CameraData;
            }
        }
#endregion

#region State
        public static Action<Mirror> OnMirrorCreation = mirror => { };
        public static List<Mirror> Mirrors => new(mirrors);
        private static List<Mirror> mirrors = new();

        public Action<ScriptableRenderContext, Camera> OnCameraRender = (context, c) => { };
        private bool didCustom;

        void OnEnable()
        {
            OnMirrorCreation.Invoke(this);
            // Callbacks
            if(!CustomCameraControl)
                RenderPipelineManager.beginCameraRendering += BeginCameraRendering;
            else
            {
                OnCameraRender += BeginCameraRendering;
                didCustom = true;
            }
            // Initialize Components
            InitializeCamera();
            // Cache
            mirrors.Add(this);
        }

        void OnDisable()
        {
            // Callbacks
            if(!didCustom)
                RenderPipelineManager.beginCameraRendering -= BeginCameraRendering;
            else
            {
                OnCameraRender -= BeginCameraRendering;
                didCustom = false;
            }
            
            // Dispose RenderTexture
            SafeDestroyObject(m_RenderTexture);
            SafeDestroyObject(m_RenderTextureL);
            SafeDestroyObject(m_RenderTextureR);
            // Remove Cache
            mirrors.Remove(this);
        }
#endregion

#region Initialization
        void InitializeCamera()
        {
            // Setup Camera
            reflectionCamera.cameraType = CameraType.Reflection;
            reflectionCamera.targetTexture = m_RenderTexture;

            // Setup AdditionalCameraData
            cameraData.renderShadows = false;
            cameraData.requiresColorOption = CameraOverrideOption.Off;
            cameraData.requiresDepthOption = CameraOverrideOption.Off;
        }
#endregion

#region RenderTexture
        RenderTextureDescriptor GetDescriptor(Camera camera)
        {
            // Get scaled Texture size
            var width = (int)Mathf.Max(camera.pixelWidth * textureScale, 4);
            var height = (int)Mathf.Max(camera.pixelHeight * textureScale, 4);

            // Get Texture format
            var hdr = allowHDR == MirrorCameraOverride.UseSourceCameraSettings ? camera.allowHDR : false;
            var renderTextureFormat = hdr ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
            if (IsStereo(camera))
            {
                var desc = XRSettings.eyeTextureDesc;
                desc.width = (int)Mathf.Max(desc.width * textureScale, 4);
                desc.height = (int)Mathf.Max(desc.height * textureScale, 4);
                width = desc.width;
                height = desc.height;
                // return desc;
            }
            return new RenderTextureDescriptor(width, height, renderTextureFormat, 16) { autoGenerateMips = true, useMipMap = true };
        }
#endregion

#region Rendering
        bool IsStereo(Camera camera) => XRSettings.enabled && camera.GetUniversalAdditionalCameraData().allowXRRendering && camera.cameraType != CameraType.SceneView;

        void BeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            // Never render Mirrors for Preview or Reflection cameras
            if(camera.cameraType == CameraType.Preview || camera.cameraType == CameraType.Reflection)
                return;
            var camData = camera.GetUniversalAdditionalCameraData();
            if (camData.renderType == CameraRenderType.Overlay)
                return;

            // Profiling command
            CommandBuffer cmd = CommandBufferPool.Get($"Mirror {gameObject.GetInstanceID()}");
            using (new ProfilingSample(cmd, $"Mirror {gameObject.GetInstanceID()}"))
            {
                ExecuteCommand(context, cmd);

                // Test for Descriptor changes
                var descriptor = GetDescriptor(camera);
                if (IsStereo(camera))
                {
                    if (m_RenderTextureL == null || m_RenderTextureR == null || !descriptor.Equals(m_PreviousDescriptorStereo))
                    {
                        // Dispose RenderTexture
                        if (m_RenderTextureL != null)
                            SafeDestroyObject(m_RenderTextureL);
                        if (m_RenderTextureR != null)
                            SafeDestroyObject(m_RenderTextureR);
                        
                        // Create new RenderTexture
                        m_RenderTextureL = new RenderTexture(descriptor);
                        m_RenderTextureR = new RenderTexture(descriptor);
                        m_PreviousDescriptorStereo = descriptor;
                        // reflectionCamera.targetTexture = m_RenderTexture;
                    }
                }
                else
                {
                    if (m_RenderTexture == null || !descriptor.Equals(m_PreviousDescriptor))
                    {
                        // Dispose RenderTexture
                        if (m_RenderTexture != null)
                        {
                            SafeDestroyObject(m_RenderTexture);
                        }
                        
                        // Create new RenderTexture
                        m_RenderTexture = new RenderTexture(descriptor);
                        m_PreviousDescriptor = descriptor;
                        // reflectionCamera.targetTexture = m_RenderTexture;
                    }
                }
                
                // Execute
                if (IsStereo(camera))
                {
                    RenderMirror(context, camera, Camera.MonoOrStereoscopicEye.Left);
                    RenderMirror(context, camera, Camera.MonoOrStereoscopicEye.Right);
                }
                else
                    RenderMirror(context, camera, Camera.MonoOrStereoscopicEye.Mono);
                SetShaderUniforms(context, cmd, camera);
            }
            ExecuteCommand(context, cmd);
            CommandBufferPool.Release(cmd);
        }

        void RenderMirror(ScriptableRenderContext context, Camera camera, Camera.MonoOrStereoscopicEye eye)
        {
            // Apply settings
            reflectionCamera.nearClipPlane = MinRenderDistance;
            reflectionCamera.farClipPlane = MaxRenderDistance;
            
            // Mirror the view matrix
            var mirrorMatrix = GetMirrorMatrix();
            reflectionCamera.worldToCameraMatrix = GetViewMatrix(camera, eye) * mirrorMatrix;
            reflectionCamera.projectionMatrix = GetProjectionMatrix(camera, eye);

            // Make oplique projection matrix where near plane is mirror plane
            var mirrorPlane = GetMirrorPlane(reflectionCamera);
            var projectionMatrix = reflectionCamera.CalculateObliqueMatrix(mirrorPlane);
            reflectionCamera.projectionMatrix = projectionMatrix;
            
            // Miscellanious camera settings
            reflectionCamera.cullingMask = layerMask;
            reflectionCamera.allowHDR = allowHDR == MirrorCameraOverride.UseSourceCameraSettings ? camera.allowHDR : false;
            reflectionCamera.allowMSAA = allowMSAA == MirrorCameraOverride.UseSourceCameraSettings ? camera.allowMSAA : false;
            reflectionCamera.enabled = false;
            switch (eye)
            {
                default:
                    reflectionCamera.targetTexture = m_RenderTexture;
                    break;
                case Camera.MonoOrStereoscopicEye.Left:
                    reflectionCamera.targetTexture = m_RenderTextureL;
                    break;
                case Camera.MonoOrStereoscopicEye.Right:
                    reflectionCamera.targetTexture = m_RenderTextureR;
                    break;
            }
            Debug.Assert(reflectionCamera.targetTexture != null);

            // Render reflection camera with inverse culling
            GL.invertCulling = true;
            UniversalRenderPipeline.RenderSingleCamera(context, reflectionCamera);
            GL.invertCulling = false;
        }
#endregion

#region Projection
        Matrix4x4 GetMirrorMatrix()
        {
            // Setup
            var position = transform.position;
            var normal = transform.forward;
            var depth = -Vector3.Dot(normal, position) - offest;

            // Create matrix
            var mirrorMatrix = new Matrix4x4()
            {
                m00 = (1f - 2f * normal.x  * normal.x),
                m01 = (-2f     * normal.x  * normal.y),
                m02 = (-2f     * normal.x  * normal.z),
                m03 = (-2f     * depth     * normal.x),
                m10 = (-2f     * normal.y  * normal.x),
                m11 = (1f - 2f * normal.y  * normal.y),
                m12 = (-2f     * normal.y  * normal.z),
                m13 = (-2f     * depth     * normal.y),
                m20 = (-2f     * normal.z  * normal.x),
                m21 = (-2f     * normal.z  * normal.y),
                m22 = (1f - 2f * normal.z  * normal.z),
                m23 = (-2f     * depth     * normal.z),
                m30 = 0f,
                m31 = 0f,
                m32 = 0f,
                m33 = 1f,
            };
            return mirrorMatrix;
        }

        Matrix4x4 GetViewMatrix(Camera camera, Camera.MonoOrStereoscopicEye eye)
        {
            switch (eye)
            {
                default:
                case Camera.MonoOrStereoscopicEye.Mono:
                    return camera.worldToCameraMatrix;
                case Camera.MonoOrStereoscopicEye.Left:
                    return camera.GetStereoViewMatrix(Camera.StereoscopicEye.Left);
                case Camera.MonoOrStereoscopicEye.Right:
                    return camera.GetStereoViewMatrix(Camera.StereoscopicEye.Right);
            }
        }

        Matrix4x4 GetProjectionMatrix(Camera camera, Camera.MonoOrStereoscopicEye eye)
        {
            switch (eye)
            {
                default:
                case Camera.MonoOrStereoscopicEye.Mono:
                    return camera.projectionMatrix;
                case Camera.MonoOrStereoscopicEye.Left:
                    return camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left);
                case Camera.MonoOrStereoscopicEye.Right:
                    return camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right);
            }
        }
    
        Vector4 GetMirrorPlane(Camera camera)
        {
            // Calculate mirror plane in camera space.
            var pos = transform.position - Vector3.forward * 0.1f;
            var normal = transform.forward;
            var offsetPos = pos + normal * offest;
            var cpos = camera.worldToCameraMatrix.MultiplyPoint(offsetPos);
            var cnormal = camera.worldToCameraMatrix.MultiplyVector(normal);
            return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
        }
#endregion

#region Output
        void SetShaderUniforms(ScriptableRenderContext context, CommandBuffer cmd, Camera camera)
        {
            var block = new MaterialPropertyBlock();
            switch(scope)
            {
                case OutputScope.Global:
                    // Globals
                    cmd.SetGlobalTexture("_ReflectionMap", m_RenderTexture);
                    ExecuteCommand(context, cmd);

                    // Property Blocm
                    block.SetFloat("_LocalMirror", 0.0f);
                    foreach(var renderer in renderers)
                    {
                        renderer.SetPropertyBlock(block);
                    }
                    break;
                case OutputScope.Local:
                    if (m_Renderers == null || renderers == null)
                        break;
                    // Keywords
                    Shader.EnableKeyword("_BLEND_MIRRORS");

                    // Property Block
                    if (m_RenderTexture != null)
                        block.SetTexture("_LocalReflectionMap", m_RenderTexture);
                    if (m_RenderTextureL != null)
                        block.SetTexture("_LeftMap", m_RenderTextureL);
                    if (m_RenderTextureR != null)
                        block.SetTexture("_RightMap", m_RenderTextureR);
                    block.SetFloat("_LocalMirror", 1.0f);
                    block.SetFloat("_IsXR", IsStereo(camera) ? 1f : 0f);
                    foreach(var renderer in renderers)
                    {
                        if(renderer == null) continue;
                        renderer.SetPropertyBlock(block);
                    }
                    break;
            }
        }
#endregion

#region CommandBufer
        void ExecuteCommand(ScriptableRenderContext context, CommandBuffer cmd)
        {
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }
#endregion

#region Object
        void SafeDestroyObject(Object obj)
        {
            if(obj == null)
                return;
            
            #if UNITY_EDITOR
            DestroyImmediate(obj);
            #else
            Destroy(obj);
            #endif
        }
#endregion

#region AssetMenu
#if UNITY_EDITOR
        // Add a menu item to Mirrors
        [UnityEditor.MenuItem("GameObject/kTools/Mirror", false, 10)]
        static void CreateMirrorObject(UnityEditor.MenuCommand menuCommand)
        {
            // Create Mirror
            GameObject go = new GameObject("New Mirror", typeof(Mirror));
            
            // Transform
            UnityEditor.GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            
            // Undo and Selection
            UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            UnityEditor.Selection.activeObject = go;
        }
#endif
#endregion

#region Gizmos
#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            // Setup
            var bounds = new Vector3(1.0f, 1.0f, 0.0f);
            var color = new Color32(0, 120, 255, 255);
            var selectedColor = new Color32(255, 255, 255, 255);
            var isSelected = UnityEditor.Selection.activeObject == gameObject;

            // Draw Gizmos
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = isSelected ? selectedColor : color;
            Gizmos.DrawIcon(transform.position, kGizmoPath, true);
            Gizmos.DrawWireCube(Vector3.zero, bounds);
        }
#endif
#endregion        
    }
}