using System;
using Hypernex.CCK;
using Hypernex.Game;
using Hypernex.Sandboxing.SandboxedTypes;
using Hypernex.UI.Templates;
using Nexbox;
using Nexbox.Interpreters;
using UnityEngine;
using Logger = Hypernex.CCK.Logger;

namespace Hypernex.Sandboxing
{
    public class Sandbox : IDisposable
    {
        private IInterpreter interpreter;
        internal Runtime Runtime;

        private void OnLog(bool avatar, NexboxScript script, object o)
        {
            string h = avatar ? "AVATAR" : "WORLD";
            string t = $"[{h}] [{script.Name}{script.GetExtensionFromLanguage()}] {o}";
            ConsoleTemplate.AddMessage(t);
            Logger.CurrentLogger.Debug(t);
        }
        
        public Sandbox(NexboxScript script, GameInstance gameInstance, GameObject attached)
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
            interpreter.StartSandbox(o => OnLog(false, script, o));
            Runtime = SandboxForwarding.Forward(attached, interpreter, SandboxRestriction.Local, null, gameInstance);
            interpreter.RunScript(script.Script, e => Logger.CurrentLogger.Error($"[{script.Name}{script.GetExtensionFromLanguage()}] {e}"));
        }
        
        public Sandbox(NexboxScript script, Transform playerRoot, GameObject attached)
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
            interpreter.StartSandbox(o => OnLog(true, script, o));
            Runtime = SandboxForwarding.Forward(attached, interpreter, SandboxRestriction.LocalAvatar, playerRoot, null);
            interpreter.RunScript(script.Script, e => Logger.CurrentLogger.Error($"[{script.Name}{script.GetExtensionFromLanguage()}] {e}"));
        }

        public void Dispose()
        {
            Runtime?.Dispose();
            interpreter.Stop();
        }
    }
}