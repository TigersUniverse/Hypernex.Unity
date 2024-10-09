using System;
using Hypernex.Game;
using Hypernex.Tools;
using Nexbox;

namespace Hypernex.Sandboxing.SandboxedTypes.Handlers
{
    public class ScriptEvents
    {
        public ScriptEvents() => throw new Exception("Cannot Instantiate ScriptEvents");

        private SandboxRestriction sandboxRestriction;

        internal ScriptEvents(SandboxRestriction sandboxRestriction) => this.sandboxRestriction = sandboxRestriction;
        
        internal Action<string> OnUserJoin = userid => { };
        internal Action<string> OnUserLeave = userid => { };
        internal Action<string, object[]> OnServerNetworkEvent = (eventName, eventArgs) => { };

        public void Subscribe(ScriptEvent scriptEvent, object s)
        {
            SandboxFunc callback = SandboxFuncTools.TryConvert(s);
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
                    if(sandboxRestriction != SandboxRestriction.Local) break;
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