using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class OnnxYouWillBeCopied : IPostprocessBuildWithReport
{
    public int callbackOrder => 100;
    
    public void OnPostprocessBuild(BuildReport report)
    {
        try
        {
            BuildTarget target = report.summary.platform;
            BuildResult result = report.summary.result;
            // do not ask me why the build result is unknown. i couldn't tell you
            if (result != BuildResult.Succeeded && result != BuildResult.Unknown) return;
            if (target != BuildTarget.StandaloneWindows64) return;
            string pluginsDir = Path.Combine(Path.GetDirectoryName(report.summary.outputPath)!, "Hypernex.Unity_Data", "Plugins", "x86_64");
            string currentDirectory =
                Path.Combine("Assets", "Libs", "Microsoft", "ML.OnnxRuntime", "win-x64", "native");
            File.Copy(Path.Combine(currentDirectory, "onnxruntime.dll"), Path.Combine(pluginsDir, "onnxruntime.dll"),
                true);
            File.Copy(Path.Combine(currentDirectory, "onnxruntime_providers_shared.dll"),
                Path.Combine(pluginsDir, "onnxruntime_providers_shared.dll"), true);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            Debug.LogWarning("Failed to copy onnx win-x64 plugins! You will need to copy them yourself manually.");
        }
    }
}