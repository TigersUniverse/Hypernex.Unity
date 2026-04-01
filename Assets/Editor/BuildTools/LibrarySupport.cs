using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace Hypernex.Editor.BuildTools
{
    public static class LibrarySupport
    {
        private static Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        private static Dictionary<string, bool> typeCache = new ();
        
        private static bool TypeNameExists(string fullTypeName)
        {
            if (typeCache.TryGetValue(fullTypeName, out bool r)) return r;
            bool exists = false;
            foreach (Assembly assembly in assemblies)
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if (!type.FullName?.Contains(fullTypeName) ?? true) continue;
                    exists = true;
                }
            }
            if(!typeCache.ContainsKey(fullTypeName)) typeCache.Add(fullTypeName, exists);
            return exists;
        }


        public static bool IsVLCPresent() => TypeNameExists("OnLoad");
        public static bool IsMagicaPresent() => TypeNameExists("MagicaCloth");
        
        public static void AddScriptingDefineSymbol(string define)
        {
            BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            string[] defines;
            PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup, out defines);
            if(defines.Contains(define)) return;
            List<string> clone = new List<string>(defines);
            if(!clone.Contains(define))
                clone.Add(define);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, clone.ToArray());
        }
    
        public static void RemoveScriptingDefineSymbol(string define)
        {
            BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            string[] defines;
            PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup, out defines);
            if(!defines.Contains(define)) return;
            List<string> clone = new List<string>(defines);
            if(clone.Contains(define))
                clone.Remove(define);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, clone.ToArray());
        }
    }
}