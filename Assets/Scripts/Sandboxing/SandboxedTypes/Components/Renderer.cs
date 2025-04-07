using System;
using System.Collections.Generic;
using Hypernex.Game;
using Nexbox;

namespace Hypernex.Sandboxing.SandboxedTypes.Components
{
    public class Renderer
    {
        private const string WRITE_ERROR = "Cannot write when in readonly mode!";
        private readonly bool read;
        private readonly Item item;
        private UnityEngine.MeshFilter f;
        private UnityEngine.MeshRenderer r;

        public Renderer(Item i)
        {
            read = i.IsReadOnly;
            item = i;
            f = i.t.GetComponent<UnityEngine.MeshFilter>();
            r = i.t.GetComponent<UnityEngine.MeshRenderer>();
            if(f == null || r == null)
                throw new Exception("No Renderer found on Item at " + i.Path);
        }
        
        public bool Enabled
        {
            get => r == null ? false : r.enabled;
            set
            {
                if(read || r == null) return;
                r.enabled = value;
            }
        }

        public Mesh Mesh
        {
            get
            {
                if (read)
                    return null;
                return new Mesh(f.sharedMesh);
            }
            set
            {
                if (read)
                    throw new Exception(WRITE_ERROR);
                f.sharedMesh = value.r;
            }
        }

        public void SetMaterialsFromAssets(string[] assets)
        {
            if (read)
                throw new Exception(WRITE_ERROR);
            List<UnityEngine.Material> materials = new List<UnityEngine.Material>();
            for (int i = 0; i < assets.Length; i++)
            {
                UnityEngine.Material material = (UnityEngine.Material)SandboxTools.GetObjectFromWorldResource(assets[i], GameInstance.GetInstanceFromScene(item.t.gameObject.scene));
                materials.Add(material);
            }
            r.SetSharedMaterials(materials);
        }
    }
}