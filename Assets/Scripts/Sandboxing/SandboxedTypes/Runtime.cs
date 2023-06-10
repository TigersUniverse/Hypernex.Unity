using System.Collections.Generic;
using Nexbox;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public static class Runtime
    {
        internal static List<SandboxFunc> OnUpdates => new (onUpdates);
        private static List<SandboxFunc> onUpdates = new ();

        public static void OnUpdate(SandboxFunc s) => onUpdates.Add(s);
        public static void RemoveOnUpdate(SandboxFunc s) => onUpdates.Remove(s);
    }
}