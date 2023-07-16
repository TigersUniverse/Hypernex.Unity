using System;
using Hypernex.CCK;
using Hypernex.Game;
using Nexbox;
using Nexbox.Interpreters;
using UnityEngine;
using Logger = Hypernex.CCK.Logger;

namespace Hypernex.Sandboxing
{
    public class Sandbox : IDisposable
    {
        private IInterpreter interpreter;
        
        public Sandbox(NexboxScript script, GameInstance gameInstance)
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
            SandboxForwarding.Forward(interpreter, SandboxRestriction.Local, null, gameInstance);
            interpreter.RunScript(script.Script, e => Logger.CurrentLogger.Error($"[{script.Name}{script.GetExtensionFromLanguage()}] {e}"));
        }
        
        public Sandbox(NexboxScript script, Transform playerRoot)
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
            SandboxForwarding.Forward(interpreter, SandboxRestriction.LocalAvatar, playerRoot, null);
            interpreter.RunScript(script.Script, e => Logger.CurrentLogger.Error($"[{script.Name}{script.GetExtensionFromLanguage()}] {e}"));
        }

        public void Dispose() => interpreter.Stop();
    }
}