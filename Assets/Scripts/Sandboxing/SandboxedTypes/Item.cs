using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Hypernex.Game;
using Hypernex.Networking.Messages.Data;
using Hypernex.Sandboxing.SandboxedTypes.Components;
using Hypernex.Tools;
using UnityEngine;
using Light = Hypernex.Sandboxing.SandboxedTypes.Components.Light;
using Object = UnityEngine.Object;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public class Item
    {
        private const string WRITE_ERROR = "Cannot write when in readonly mode!";
        internal Transform t;
        private bool read;

        public Item()
        {
            throw new Exception("Item cannot be created by a Script!");
        }

        internal Item(Transform t, bool read)
        {
            this.t = t;
            this.read = read;
        }

        public bool IsReadOnly => read;

        public string Name => t.name;

        public bool Enabled
        {
            get => t.gameObject.activeSelf;
            set
            {
                if(read) throw new Exception(WRITE_ERROR);
                t.gameObject.SetActive(value);
            }
        }

        public string Path => AnimationUtility.CalculateTransformPath(t, null);

        public Item Parent
        {
            get
            {
                if (t.parent == null)
                    return null;
                IPlayer player = Avatar.GetPlayerRootFromChild(t);
                if (player == null)
                {
                    // Not a part of the player, assume read
                    return new Item(t.parent, read);
                }
                // Don't get the parent of a PlayerRoot
                if (t == player.transform)
                    return null;
                bool r = read;
                // If we're going from Avatar to PlayerRoot, flip Read only if Local
                if (player.IsLocal)
                    r = !r;
                return new Item(t.parent, r);
            }
            set
            {
                if(read) throw new Exception(WRITE_ERROR);
                Transform root = AnimationUtility.GetRootOfChild(t);
                if (root.GetComponent<LocalPlayer>() != null || root.GetComponent<NetPlayer>() != null)
                    return;
                t.parent = value.t;
            }
        }
        
        public float3 Position
        {
            get => NetworkConversionTools.Vector3Tofloat3(t.position);
            set
            {
                if(read) throw new Exception(WRITE_ERROR);
                t.position = NetworkConversionTools.float3ToVector3(value);
            }
        }

        public float4 Rotation
        {
            get => NetworkConversionTools.QuaternionTofloat4(t.rotation);
            set
            {
                if(read) throw new Exception(WRITE_ERROR);
                t.rotation = NetworkConversionTools.float4ToQuaternion(value);
            }
        }

        public float3 LocalPosition
        {
            get => NetworkConversionTools.Vector3Tofloat3(t.localPosition);
            set
            {
                if(read) throw new Exception(WRITE_ERROR);
                t.localPosition = NetworkConversionTools.float3ToVector3(value);
            }
        }
        
        public float4 LocalRotation
        {
            get => NetworkConversionTools.QuaternionTofloat4(t.localRotation);
            set
            {
                if(read) throw new Exception(WRITE_ERROR);
                t.localRotation = NetworkConversionTools.float4ToQuaternion(value);
            }
        }

        public float3 LocalSize
        {
            get => NetworkConversionTools.Vector3Tofloat3(t.localScale);
            set
            {
                if(read) throw new Exception(WRITE_ERROR);
                t.localScale = NetworkConversionTools.float3ToVector3(value);
            }
        }

        public int ChildCount => t.childCount;

        public Item[] Children
        {
            get
            {
                List<Item> items = new();
                for (int i = 0; i < t.childCount; i++)
                {
                    Item item = GetChildByIndex(i);
                    if(item == null) continue;
                    Transform root = AnimationUtility.GetRootOfChild(item.t);
                    if (root.GetComponent<LocalPlayer>() == null && root.GetComponent<NetPlayer>() == null)
                        items.Add(item);
                }
                return items.ToArray();
            }
        }

        public bool CanCollide
        {
            get
            {
                UnityEngine.Collider c = t.GetComponent<UnityEngine.Collider>();
                if (c == null)
                    return false;
                return c.enabled;
            }
            set
            {
                if(read) throw new Exception(WRITE_ERROR);
                UnityEngine.Collider c = t.GetComponent<UnityEngine.Collider>();
                if (c == null)
                    return;
                c.enabled = value;
            }
        }

        public Collider Collider => new Collider(t.GetComponent<UnityEngine.Collider>(), read);
        public Collider[] Colliders => t.GetComponents<UnityEngine.Collider>().Select(x => new Collider(x, read)).ToArray();

        public Item GetChildByIndex(int i)
        {
            Transform tr = t.GetChild(i);
            if (tr != null)
            {
                IPlayer player = Avatar.GetPlayerRootFromChild(t);
                if (player == null)
                {
                    // Not a part of the player, assume read
                    return new Item(tr, read);
                }
                bool r = read;
                // If we are going from the player root to a child, flip read
                if (t == player.transform)
                    r = !r;
                return new Item(tr, r);
            }
            return null;
        }

        public Item GetChildByName(string name)
        {
            Transform tr = t.Find(name);
            if (tr != null)
            {
                IPlayer player = Avatar.GetPlayerRootFromChild(t);
                if (player == null)
                {
                    // Not a part of the player, assume read
                    return new Item(tr, read);
                }
                bool r = read;
                // If we are going from the player root to a child, flip read
                if (t == player.transform)
                    r = !r;
                return new Item(tr, r);
            }
            return null;
        }

        private string GetSafeName(Transform parent, Transform exclude, string newName)
        {
            int sameNameCount = 0;
            string safeName = newName;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child == exclude) continue;
                // Check if the name matches the base name or the numbered variant
                if (child.name == safeName)
                {
                    sameNameCount++;
                    safeName = newName + " (" + sameNameCount + ")";
                }
                else
                {
                    // Check for numbered names
                    string pattern = newName + " (" + sameNameCount + ")";
                    while (child.name == pattern)
                    {
                        sameNameCount++;
                        pattern = newName + " (" + sameNameCount + ")";
                    }
                    safeName = pattern;
                }
            }
            return safeName;
        }

        public Item Duplicate(string name = "")
        {
            if (read) throw new Exception(WRITE_ERROR);
            bool canDuplicate = false;
            IPlayer player = Avatar.GetPlayerRootFromChild(t);
            if (player == null)
            {
                // Not a part of the player, duplicate
                canDuplicate = true;
            }
            else
            {
                // Allowed to duplicate because we are in write mode and the item is not the player root
                canDuplicate = t != player.transform;
            }
            if (!canDuplicate) return null;
            Transform d = Object.Instantiate(t.gameObject).transform;
            Transform parent = t.parent;
            d.parent = parent;
            string newName = d.gameObject.name;
            if (!string.IsNullOrEmpty(name)) newName = name;
            d.gameObject.name = GetSafeName(parent, d, newName);
            return new Item(d, read);
        }

        private static readonly IReadOnlyDictionary<string, Type> ComponentTypes = new ReadOnlyDictionary<string, Type>(
            new Dictionary<string, Type>
            {
                ["animator"] = typeof(Components.Animator),
                ["audio"] = typeof(Audio),
                ["button"] = typeof(Button),
                ["dropdown"] = typeof(Dropdown),
                ["graphic"] = typeof(Graphic),
                ["scrollbar"] = typeof(Scrollbar),
                ["slider"] = typeof(Slider),
                ["text"] = typeof(Text),
                ["textinput"] = typeof(TextInput),
                ["toggle"] = typeof(Toggle),
                ["video"] = typeof(Video),
                ["physicsbody"] = typeof(PhysicsBody),
                ["interactables"] = typeof(Interactables),
                ["light"] = typeof(Light)
            });

        public object GetComponent(string componentName)
        {
            if (ComponentTypes.TryGetValue(componentName.ToLower(), out Type type))
                return Activator.CreateInstance(type, this);
            throw new Exception("Component Name " + componentName + " does not exist!");
        }
        
        public static bool operator ==(Item x, Item y) => x?.Equals(y) ?? false;
        public static bool operator !=(Item x, Item y) => !(x == y);
        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != typeof(Item))
                return false;
            return t == ((Item) obj).t;
        }
    }
}