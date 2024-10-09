using Hypernex.Tools;
using Nexbox;
using Unity.VisualScripting;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public static class Colliders
    {
        public static void OnTriggerEnter(Item item, object o)
        {
            SandboxFunc s = SandboxFuncTools.TryConvert(o);
            ColliderEvents collider = item.t.GetComponent<ColliderEvents>();
            if (collider == null)
                collider = item.t.AddComponent<ColliderEvents>();
            collider.TriggerEnter += c =>
                SandboxFuncTools.InvokeSandboxFunc(s, new Collider(c, item.IsReadOnly));
        }
        
        public static void OnTriggerStay(Item item, object o)
        {
            SandboxFunc s = SandboxFuncTools.TryConvert(o);
            ColliderEvents collider = item.t.GetComponent<ColliderEvents>();
            if (collider == null)
                collider = item.t.AddComponent<ColliderEvents>();
            collider.TriggerStay += c => SandboxFuncTools.InvokeSandboxFunc(s, new Collider(c, item.IsReadOnly));
        }
        
        public static void OnTriggerExit(Item item, object o)
        {
            SandboxFunc s = SandboxFuncTools.TryConvert(o);
            ColliderEvents collider = item.t.GetComponent<ColliderEvents>();
            if (collider == null)
                collider = item.t.AddComponent<ColliderEvents>();
            collider.TriggerExit += c =>
                SandboxFuncTools.InvokeSandboxFunc(s, new Collider(c, item.IsReadOnly));
        }
        
        public static void OnCollisionEnter(Item item, object o)
        {
            SandboxFunc s = SandboxFuncTools.TryConvert(o);
            ColliderEvents collider = item.t.GetComponent<ColliderEvents>();
            if (collider == null)
                collider = item.t.AddComponent<ColliderEvents>();
            collider.CollisionEnter += c =>
                SandboxFuncTools.InvokeSandboxFunc(s, new Collision(c, item.IsReadOnly));
        }

        public static void OnCollisionStay(Item item, object o)
        {
            SandboxFunc s = SandboxFuncTools.TryConvert(o);
            ColliderEvents collider = item.t.GetComponent<ColliderEvents>();
            if (collider == null)
                collider = item.t.AddComponent<ColliderEvents>();
            collider.CollisionStay += c =>
                SandboxFuncTools.InvokeSandboxFunc(s, new Collision(c, item.IsReadOnly));
        }
        
        public static void OnCollisionExit(Item item, object o)
        {
            SandboxFunc s = SandboxFuncTools.TryConvert(o);
            ColliderEvents collider = item.t.GetComponent<ColliderEvents>();
            if (collider == null)
                collider = item.t.AddComponent<ColliderEvents>();
            collider.CollisionExit += c =>
                SandboxFuncTools.InvokeSandboxFunc(s, new Collision(c, item.IsReadOnly));
        }
    }
}