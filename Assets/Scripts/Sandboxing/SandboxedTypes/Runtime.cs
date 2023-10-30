using System;
using System.Collections.Generic;
using Hypernex.CCK;
using Nexbox;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public class Runtime : IDisposable
    {
        public static List<Runtime> Instances => new(instances);
        private static List<Runtime> instances = new();
        
        internal List<SandboxFunc> OnFixedUpdates => new (onFixedUpdates);
        private List<SandboxFunc> onFixedUpdates = new ();
        
        internal List<SandboxFunc> OnUpdates => new (onUpdates);
        private List<SandboxFunc> onUpdates = new ();

        internal List<SandboxFunc> OnLateUpdates => new(onLateUpdates);
        private List<SandboxFunc> onLateUpdates = new();

        internal Runtime() => instances.Add(this);
        
        public void OnFixedUpdate(SandboxFunc s) => onFixedUpdates.Add(s);
        public void RemoveOnFixedUpdate(SandboxFunc s) => onFixedUpdates.Add(s);
        public void OnUpdate(SandboxFunc s) => onUpdates.Add(s);
        public void RemoveOnUpdate(SandboxFunc s) => onUpdates.Remove(s);
        public void OnLateUpdate(SandboxFunc s) => onLateUpdates.Add(s);
        public void RemoveOnLateUpdate(SandboxFunc s) => onLateUpdates.Remove(s);

        public void FixedUpdate() => OnFixedUpdates.ForEach(x =>
        {
            try
            {
                SandboxFuncTools.InvokeSandboxFunc(x);
            }
            catch (Exception e)
            {
                Logger.CurrentLogger.Error(e);
            }
        });

        public void Update() => OnUpdates.ForEach(x =>
        {
            try
            {
                SandboxFuncTools.InvokeSandboxFunc(x);
            }
            catch (Exception e)
            {
                Logger.CurrentLogger.Error(e);
            }
        });
        
        public void LateUpdate() => OnLateUpdates.ForEach(x =>
        {
            try
            {
                SandboxFuncTools.InvokeSandboxFunc(x);
            }
            catch (Exception e)
            {
                Logger.CurrentLogger.Error(e);
            }
        });
        
        public void Dispose()
        {
            onFixedUpdates.Clear();
            onUpdates.Clear();
            onLateUpdates.Clear();
            instances.Remove(this);
        }
    }
}