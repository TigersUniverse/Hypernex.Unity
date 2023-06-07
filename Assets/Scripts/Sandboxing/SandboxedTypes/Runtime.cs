using System.Collections.Generic;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public static class Runtime
    {
        internal static List<SandboxAction> OnUpdates => new (onUpdates);
        private static List<SandboxAction> onUpdates = new ();

        public static void OnUpdate(SandboxAction s) => onUpdates.Add(s);
        public static void RemoveOnUpdate(SandboxAction s) => onUpdates.Remove(s);
    }
}