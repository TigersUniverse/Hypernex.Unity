using System;
using UnityEngine.Events;

namespace Hypernex.Tools
{
    public static class QuickInvoke
    {
        public static void InvokeActionOnMainThread(object action, params object[] args) => UnityMainThreadDispatcher
            .Instance().Enqueue(() => action.GetType().GetMethod("Invoke").Invoke(action, args));
        
        public static void InvokeActionOnMainThreadObject(object action, object[] args) => UnityMainThreadDispatcher
            .Instance().Enqueue(() => action.GetType().GetMethod("Invoke").Invoke(action, args));
        
        public static void OverwriteListener(UnityEvent u, Action newAction)
        {
            u.RemoveAllListeners();
            u.AddListener(newAction.Invoke);
        }
        
        public static void OverwriteListener<T0>(UnityEvent<T0> u, Action<T0> newAction)
        {
            u.RemoveAllListeners();
            u.AddListener(newAction.Invoke);
        }
    }
}
