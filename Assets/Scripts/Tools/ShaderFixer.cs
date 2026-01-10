using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hypernex.Tools
{
    /// <summary>
    /// A tool which will replace broken shaders with built-in ones (designed specifically for macOS). 
    /// Will automatically fix new materials on renderers if they are animated.
    /// </summary>
    public class ShaderFixer : MonoBehaviour
    {
        public static void ReplaceAllShaders(Scene s)
        {
            foreach (GameObject rootGameObject in s.GetRootGameObjects())
            {
                ReplaceAllShaders(rootGameObject.transform);
            }
            ReplaceShader(RenderSettings.skybox, RenderSettings.skybox.shader);
        }
        
        public static void ReplaceAllShaders(Transform t)
        {
            Renderer[] r = t.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in r)
            {
                ShaderFixer instance = renderer.GetComponent<ShaderFixer>();
                if (instance == null) renderer.gameObject.AddComponent<ShaderFixer>();
            }
            CanvasRenderer[] uirs = t.GetComponentsInChildren<CanvasRenderer>(true);
            foreach (CanvasRenderer renderer in uirs)
            {
                ShaderFixer instance = renderer.GetComponent<ShaderFixer>();
                if (instance == null) renderer.gameObject.AddComponent<ShaderFixer>();
            }
        }

        public static void ReplaceAllShaders(Material m) => ReplaceShader(m, m.shader);

        private static void ReplaceShader(Material m, Shader s)
        {
            string shaderName = s.name;
            Shader searchedShader = Shader.Find(shaderName);
            if(searchedShader == null) searchedShader = Shader.Find("Universal Render Pipeline/Lit");
            m.shader = searchedShader;
        }

        private CanvasRenderer canvasRenderer;
        private new Renderer renderer;
        private List<Material> fixedMaterials = new List<Material>();

        private void OnEnable()
        {
            canvasRenderer = GetComponent<CanvasRenderer>();
            renderer = GetComponent<Renderer>();
        }

        private void LateUpdate()
        {
            if(canvasRenderer != null)
            {
                for (int i = 0; i < canvasRenderer.materialCount; i++)
                {
                    Material m = canvasRenderer.GetMaterial(i);
                    if(fixedMaterials.Contains(m)) continue;
                    ReplaceShader(m, m.shader);
                    fixedMaterials.Add(m);
                }
            }
            if (renderer != null)
            {
                Material[] materials = renderer.materials;
                foreach (Material m in materials)
                {
                    if(fixedMaterials.Contains(m)) continue;
                    ReplaceShader(m, m.shader);
                    fixedMaterials.Add(m);
                }
            }
        }
    }
}