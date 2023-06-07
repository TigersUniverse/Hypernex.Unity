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
            interpreter.StartSandbox(o => Logger.CurrentLogger.Log($"[{script.Name}{script.GetExtensionFromLanguage()}] {o}"));
            SandboxForwarding.Forward(interpreter, sandboxRestriction, gameInstance);
            interpreter.RunScript(script.Script, e => Logger.CurrentLogger.Error($"[{script.Name}{script.GetExtensionFromLanguage()}] {e}"));
        }

        public void Dispose() => interpreter.Stop();
    }
}