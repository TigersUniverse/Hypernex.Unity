using System;
using System.Collections.Generic;
using Hypernex.CCK;

namespace Hypernex.Game.Avatar.VisemeProviders
{
    public static class VisemeProvider
    {
        public static List<Type> VisemeProviders = new List<Type>
        {
            typeof(OpenVisemeProvider),
            typeof(OculusVisemeProvider)
        };

        private static Type VisemeProviderType { get; } = typeof(IVisemeProvider);

        public static IVisemeProvider GetVisemeProvider()
        {
            foreach (Type visemeProviderType in VisemeProviders)
            {
                try
                {
                    object instance = Activator.CreateInstance(visemeProviderType);
                    return (IVisemeProvider) instance;
                }
                catch (Exception e)
                {
                    Logger.CurrentLogger.Critical(e);
                }
            }
            return null;
        }
    }
}