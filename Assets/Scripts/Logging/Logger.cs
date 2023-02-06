using System;
using UnityEngine;

public class Logger : MonoBehaviour
{
    public static Logger CurrentLogger;
    
    private void OnEnable()
    {
        CurrentLogger = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Log(object o) => Debug.Log(o);
    public void Warn(object o) => Debug.LogWarning(o);
    public void Error(object o) => Debug.LogError(o);
    public void Critical(Exception e) => Debug.LogException(e);
}