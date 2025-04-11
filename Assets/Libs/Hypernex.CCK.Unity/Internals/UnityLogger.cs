using System;

namespace Hypernex.CCK.Unity.Internals
{
    public class UnityLogger : Logger
    {
        public static Action<object> OnDebug = o => { };
        public static Action<object> OnLog = o => { };
        public static Action<object> OnWarn = o => { };
        public static Action<object> OnError = o => { };
        public static Action<Exception> OnCritical = o => { };

        public override void Debug(object o)
        {
            UnityEngine.Debug.Log(o);
            OnDebug.Invoke(o);
        }
        
        public override void Log(object o)
        {
            UnityEngine.Debug.Log(o);
            OnLog.Invoke(o);
        }
        
        public override void Warn(object o)
        {
            UnityEngine.Debug.LogWarning(o);
            OnWarn.Invoke(o);
        }

        public override void Error(object o)
        {
            UnityEngine.Debug.LogError(o);
            OnError.Invoke(o);
        }
        
        public override void Critical(Exception e)
        {
            UnityEngine.Debug.LogException(e);
            OnCritical.Invoke(e);
        }
    }
    
    public class UnityPluginLogger : Logger
    {
        public static Action<object> OnDebug = o => { };
        public static Action<object> OnLog = o => { };
        public static Action<object> OnWarn = o => { };
        public static Action<object> OnError = o => { };
        public static Action<Exception> OnCritical = o => { };

        private string identifier;

        public UnityPluginLogger(string identifier) => this.identifier = identifier;

        public override void Debug(object o)
        {
            o = $"[{identifier}] {o}";
            UnityEngine.Debug.Log(o);
            OnDebug.Invoke(o);
        }
        
        public override void Log(object o)
        {
            o = $"[{identifier}] {o}";
            UnityEngine.Debug.Log(o);
            OnLog.Invoke(o);
        }
        
        public override void Warn(object o)
        {
            o = $"[{identifier}] {o}";
            UnityEngine.Debug.LogWarning(o);
            OnWarn.Invoke(o);
        }

        public override void Error(object o)
        {
            o = $"[{identifier}] {o}";
            UnityEngine.Debug.LogError(o);
            OnError.Invoke(o);
        }
        
        public override void Critical(Exception e)
        {
            UnityEngine.Debug.LogException(e);
            OnCritical.Invoke(e);
        }
    }
}