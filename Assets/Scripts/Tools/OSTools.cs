using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hypernex.Tools
{
    public static class OSTools
    {
        public static void ReplaceAllShaders(Scene s)
        {
            foreach (GameObject rootGameObject in s.GetRootGameObjects())
            {
                ReplaceAllShaders(rootGameObject.transform);
            }
            ReplaceShader(RenderSettings.skybox, RenderSettings.skybox.shader);
        }
        
        public static void ReplaceAllShaders(Material m) => ReplaceShader(m, m.shader);
        
        public static void ReplaceAllShaders(Transform t)
        {
            Renderer[] r = t.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in r)
            {
                foreach (Material material in renderer.materials)
                {
                    ReplaceShader(material, material.shader);
                }
            }
            CanvasRenderer[] uirs = t.GetComponentsInChildren<CanvasRenderer>();
            foreach (CanvasRenderer renderer in uirs)
            {
                for (int i = 0; i < renderer.materialCount; i++)
                {
                    Material m = renderer.GetMaterial(i);
                    ReplaceShader(m, m.shader);
                }
            }
        }

        private static void ReplaceShader(Material m, Shader s)
        {
            string shaderName = s.name;
            Shader searchedShader = Shader.Find(shaderName);
            if(searchedShader == null) searchedShader = Shader.Find("Universal Render Pipeline/Lit");
            m.shader = searchedShader;
        }
    }
}