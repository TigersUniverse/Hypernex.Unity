#if UNITY_IOS
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEditor.iOS.Xcode.Extensions;
using UnityEngine;
using System.IO;
using System.Linq;

public class FixiOSFrameworkPaths
{
    static readonly string[] extensions = { ".framework", ".bundle", ".a", ".dylib" };

    [PostProcessBuild]
    public static void OnPostProcessBuild(BuildTarget target, string path)
    {
        if (target != BuildTarget.iOS) return;
        string pbxPath = PBXProject.GetPBXProjectPath(path);
        PBXProject pbx = new PBXProject();
        pbx.ReadFromFile(pbxPath);
        string unityFramework = pbx.GetUnityFrameworkTargetGuid();
        string destFrameworks = Path.Combine(path, "Frameworks");
        Directory.CreateDirectory(destFrameworks);
        int copied = 0;
        foreach (var plugin in Directory.GetFileSystemEntries(Application.dataPath, "*", SearchOption.AllDirectories))
        {
            if (plugin.Contains("/MacOS/") || plugin.Contains("/macos/") || plugin.Contains("/osx/"))
                continue;
            string ext = Path.GetExtension(plugin);
            if (!extensions.Contains(ext))
                continue;
            string name = Path.GetFileName(plugin);
            string dest = Path.Combine(destFrameworks, name);
            if (Directory.Exists(dest) || File.Exists(dest))
            {
                FileUtil.DeleteFileOrDirectory(dest);
                FileUtil.DeleteFileOrDirectory(dest + ".meta");
            }
            FileUtil.CopyFileOrDirectory(plugin, dest);
            string projPath = "Frameworks/" + name;
            string guid = pbx.AddFile(projPath, projPath);
            pbx.AddFileToBuild(unityFramework, guid);
            if (ext == ".framework" || ext == ".dylib")
                pbx.AddFileToEmbedFrameworks(unityFramework, guid);
            copied++;
        }
        pbx.AddBuildProperty(unityFramework, "LIBRARY_SEARCH_PATHS", "$(PROJECT_DIR)/Frameworks");
        pbx.WriteToFile(pbxPath);
        Debug.Log($"Copied and linked {copied} native iOS plugins.");
    }
}
#endif