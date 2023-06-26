using System.Collections.Generic;
using Nexbox;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public static class Runtime
    {
        internal static List<SandboxFunc> OnUpdates => new (onUpdates);
        private static List<SandboxFunc> onUpdates = new ();

        internal static List<SandboxFunc> OnLateUpdates => new(onLateUpdates);
        private static List<SandboxFunc> onLateUpdates = new();

        public static void OnUpdate(SandboxFunc s) => onUpdates.Add(s);
        public static void RemoveOnUpdate(SandboxFunc s) => onUpdates.Remove(s);
        public static void OnLateUpdate(SandboxFunc s) => onLateUpdates.Add(s);
        public static void RemoveOnLateUpdate(SandboxFunc s) => onLateUpdates.Remove(s);
    }
}