using System;
using Hypernex.CCK;
using Hypernex.Game;
using Hypernex.Tools;
using Nexbox;

namespace Hypernex.Sandboxing.SandboxedTypes.Handlers
{
    public class Translator : IDisposable
    {
        private GameInstance gameInstance;
        private SandboxRestriction sandboxRestriction;
        private TranslateCore translateCore = new TranslateCore();
        
        public Translator() => throw new Exception("Cannot instantiate Translator!");
        internal Translator(GameInstance gameInstance, SandboxRestriction sandboxRestriction)
        {
            this.gameInstance = gameInstance;
            this.sandboxRestriction = sandboxRestriction;
            if (sandboxRestriction == SandboxRestriction.LocalAvatar)
                throw new Exception("Cannot use Translator on an Avatar!");
        }

        public void Translate(string sourceText, string toLanguage, object callback) =>
            Translate(sourceText, "auto", toLanguage, callback);
        
        public async void Translate(string sourceText, string fromLanguage, string toLanguage, object callback)
        {
            SandboxFunc func = SandboxFuncTools.TryConvert(callback);
            try
            {
                string t = await translateCore.GetTranslation(sourceText, fromLanguage, toLanguage);
                SandboxFuncTools.InvokeSandboxFunc(func, t);
            }
            catch (Exception e)
            {
                Logger.CurrentLogger.Critical(e);
            }
        }

        public void Dispose()
        {
            translateCore?.Dispose();
        }
    }
}