using System;
using Hypernex.Networking.Messages.Data;
using Hypernex.Tools;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace Hypernex.Sandboxing.SandboxedTypes.Components
{
    public class NavSurface
    {
        private readonly Item item;
        private readonly bool read;
        private NavMeshSurface surf;
        
        private static NavMeshSurface GetSurface(Item item)
        {
            NavMeshSurface a = item.t.GetComponent<NavMeshSurface>();
            if (a == null)
                return null;
            return a;
        }
        
        public bool Enabled
        {
            get => surf == null ? false : surf.enabled;
            set
            {
                if (read || surf == null) return;
                surf.enabled = value;
            }
        }

        public NavSurface(Item i)
        {
            item = i;
            read = i.IsReadOnly;
            surf = GetSurface(i);
            if (surf == null) throw new Exception("No NavMeshSurface found on Item at " + i.Path);
        }

        public void BuildMesh()
        {
            if (read || surf == null) return;
            surf.BuildNavMesh();
        }
    }
}