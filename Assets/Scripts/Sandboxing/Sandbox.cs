using System;
using Hypernex.CCK;
using Hypernex.Game;
using Nexbox;
using Nexbox.Interpreters;

namespace Hypernex.Sandboxing
{
    public class Sandbox : IDisposable
    {
        private IInterpreter interpreter;
        
        public Sandbox(NexboxScript script, SandboxRestriction sandboxRestriction, GameInstance gameInstance = null)
        {
            switch (script.Language)
            {
                case NexboxLanguage.Lua:
                    interpreter = new LuaInterpreter();
                    break;
                case NexboxLanguage.JavaScript:
                    interpreter = new JavaScriptInterpreter();
                    break;
                default:
                    throw new Exception("Unknown NexboxScript language");
            }
            SandboxForwarding.Forward(interpreter, sandboxRestriction, gameInstance);
            interpreter.RunScript(script.Script);
        }

        public void Dispose() => interpreter.Stop();
    }
}