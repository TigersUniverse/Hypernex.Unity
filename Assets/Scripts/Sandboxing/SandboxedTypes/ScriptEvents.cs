using System;
using Hypernex.Game;
using Hypernex.Tools;
using Nexbox;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public class ScriptEvents
    {
        public ScriptEvents() => throw new Exception("Cannot Instantiate ScriptEvents");

        private GameInstance g;

        internal ScriptEvents(GameInstance g) => this.g = g;
        
        internal Action<string> OnUserJoin = userid => { };
        internal Action<string> OnUserLeave = userid => { };
        internal Action<string, object[]> OnServerNetworkEvent = (eventName, eventArgs) => { };

        public void Subscribe(ScriptEvent scriptEvent, SandboxFunc callback)
        {
            switch (scriptEvent)
            {
                case ScriptEvent.OnUserJoin:
                    OnUserJoin += userid => QuickInvoke.InvokeActionOnMainThread((Action) delegate
                    {
                        SandboxFuncTools.InvokeSandboxFunc(callback, userid);
                    });
                    break;
                case ScriptEvent.OnUserLeave:
                    OnUserLeave += userid => QuickInvoke.InvokeActionOnMainThread((Action) delegate
                    {
                        SandboxFuncTools.InvokeSandboxFunc(callback, userid);
                    });
                    break;
                case ScriptEvent.OnServerNetworkEvent:
                    OnServerNetworkEvent += (eventName, eventArgs) => QuickInvoke.InvokeActionOnMainThread((Action)
                        delegate
                        {
                            SandboxFuncTools.InvokeSandboxFunc(callback, eventName, eventArgs);
                        });
                    break;
            }
        }
    }
}