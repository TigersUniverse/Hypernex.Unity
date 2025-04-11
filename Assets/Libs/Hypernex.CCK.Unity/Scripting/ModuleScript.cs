#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hypernex.CCK.Unity.Scripting
{
    public abstract class ModuleScript : ScriptableObject
    {
        public abstract NexboxLanguage Language { get; }
        public string FileName;
        public string Text;

#if UNITY_EDITOR

        [MenuItem("Assets/Create/Hypernex/Scripts/Lua File", false, 80)]
        public static void CreateLuaFile() => CreateFile("NewScript", "lua", "print(\"Hello World!\")");
        
        [MenuItem("Assets/Create/Hypernex/Scripts/JavaScript File", false, 80)]
        public static void CreateJavaScriptFile() => CreateFile("NewScript", "js", "print(\"Hello World!\")");
        
        private static void CreateFile(string defaultName, string extension, string code)
        {
            string path = GetSelectedPathOrFallback();
            string filePath = EditorUtility.SaveFilePanel("Save Script to Location", path, defaultName, extension);
            File.WriteAllText(filePath, code);
            AssetDatabase.Refresh();
        }

        private static string GetSelectedPathOrFallback()
        {
            string path = "Assets";
            foreach (Object obj in Selection.GetFiltered(typeof(Object), SelectionMode.Assets))
            {
                string assetPath = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(assetPath) && Directory.Exists(assetPath))
                {
                    path = assetPath;
                    break;
                }
            }
            return path;
        }
#endif
        
        public static implicit operator NexboxScript(ModuleScript localScript) =>
            new(localScript.Language, localScript.Text)
            {
                Name = System.IO.Path.GetFileNameWithoutExtension(localScript.FileName)
            };
    }
}