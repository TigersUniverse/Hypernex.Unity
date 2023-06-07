using System;
using System.Reflection;
using Hypernex.Game;
using UnityEngine.Events;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public class SandboxAction
    {
        internal GameInstance i = GameInstance.FocusedInstance;
        internal UnityAction ua;
        internal Action a;

        public SandboxAction SetAction(object func)
        {
            /*a = () => func.DynamicInvoke();
            ua = () => func.DynamicInvoke();*/
            a = () =>
            {
                foreach (MethodInfo methodInfo in func.GetType().GetMethods())
                {
                    if (methodInfo.Name.Contains("Call"))
                    {
                        if (methodInfo.GetParameters().Length > 0) continue;
                        methodInfo.Invoke(func, Array.Empty<object>());
                        break;
                    }
                    if (methodInfo.Name.Contains("Invoke"))
                    {
                        object[] p = new object[2];
                        p[0] = null;
                        p[1] = null;
                        methodInfo.Invoke(func, p);
                        break;
                    }
                }
            };
            ua = () => a.Invoke();
            return this;
        }
    }
}