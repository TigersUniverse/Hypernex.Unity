using Hypernex.Tools;
using Nexbox;
using Unity.VisualScripting;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public static class Colliders
    {
        public static void OnTriggerEnter(Item item, SandboxFunc s)
        {
            ColliderEvents collider = item.t.GetComponent<ColliderEvents>();
            if (collider == null)
                collider = item.t.AddComponent<ColliderEvents>();
            collider.TriggerEnter += c => SandboxFuncTools.InvokeSandboxFunc(s, new Collider(c));
        }
        
        public static void OnTriggerStay(Item item, SandboxFunc s)
        {
            ColliderEvents collider = item.t.GetComponent<ColliderEvents>();
            if (collider == null)
                collider = item.t.AddComponent<ColliderEvents>();
            collider.TriggerStay += c => SandboxFuncTools.InvokeSandboxFunc(s, new Collider(c));
        }
        
        public static void OnTriggerExit(Item item, SandboxFunc s)
        {
            ColliderEvents collider = item.t.GetComponent<ColliderEvents>();
            if (collider == null)
                collider = item.t.AddComponent<ColliderEvents>();
            collider.TriggerExit += c => SandboxFuncTools.InvokeSandboxFunc(s, new Collider(c));
        }
        
        public static void OnCollisionEnter(Item item, SandboxFunc s)
        {
            ColliderEvents collider = item.t.GetComponent<ColliderEvents>();
            if (collider == null)
                collider = item.t.AddComponent<ColliderEvents>();
            collider.CollisionEnter += c => SandboxFuncTools.InvokeSandboxFunc(s, new Collision(c));
        }

        public static void OnCollisionStay(Item item, SandboxFunc s)
        {
            ColliderEvents collider = item.t.GetComponent<ColliderEvents>();
            if (collider == null)
                collider = item.t.AddComponent<ColliderEvents>();
            collider.CollisionStay += c => SandboxFuncTools.InvokeSandboxFunc(s, new Collision(c));
        }
        
        public static void OnCollisionExit(Item item, SandboxFunc s)
        {
            ColliderEvents collider = item.t.GetComponent<ColliderEvents>();
            if (collider == null)
                collider = item.t.AddComponent<ColliderEvents>();
            collider.CollisionExit += c => SandboxFuncTools.InvokeSandboxFunc(s, new Collision(c));
        }
    }
}